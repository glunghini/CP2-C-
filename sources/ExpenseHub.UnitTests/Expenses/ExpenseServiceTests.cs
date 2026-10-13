using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Application;
using ExpenseHub.Api.Application.Expenses;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Dtos;
using ExpenseHub.Api.Responses;
using ExpenseHub.Api.Security;
using ExpenseHub.UnitTests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Expenses;

/// <summary>
/// Testes das regras de role, ownership, estado e histórico do serviço de reembolsos.
/// </summary>
[TestClass]
public sealed class ExpenseServiceTests
{
    private static readonly DateTimeOffset _startTime = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly _pastDate = new(2026, 10, 10);

    private readonly InMemoryExpenseRepository _repository = new();
    private readonly FixedTimeProvider _clock = new(_startTime);
    private readonly ExpenseService _service;

    /// <summary>
    /// Cria o serviço com repositório em memória e relógio fixo.
    /// </summary>
    public ExpenseServiceTests()
    {
        _service = new ExpenseService(_repository, _clock);
    }

    private static CancellationToken Token => CancellationToken.None;

    /// <summary>
    /// O proprietário vem da identidade autenticada e o estado inicial é Draft.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task CreateByEmployeeStartsAsDraftOwnedByAuthenticatedUser()
    {
        ServiceResult<ExpenseResponse> result = await _service.CreateAsync(TestUsers.Ana, ValidRequest(), Token);

        Assert.IsNull(result.Error);
        Assert.IsNotNull(result.Value);
        Assert.AreEqual(TestUsers.Ana.UserId, result.Value.OwnerId);
        Assert.AreEqual(ExpenseStatus.Draft, result.Value.Status);
        Assert.AreEqual(_startTime.UtcDateTime, result.Value.CreatedAtUtc);
        Assert.HasCount(1, _repository.Expenses);
        Assert.HasCount(1, _repository.History);

        ExpenseHistory created = _repository.History[0];
        Assert.AreEqual(ExpenseAction.Created, created.Action);
        Assert.AreEqual(TestUsers.Ana.UserId, created.ActorId);
        Assert.IsNull(created.FromStatus);
        Assert.AreEqual(ExpenseStatus.Draft, created.ToStatus);
    }

    /// <summary>
    /// Perfis sem Employee não criam reembolsos, mesmo autenticados.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task CreateWithoutEmployeeRoleIsForbidden()
    {
        CurrentUser[] users = [TestUsers.Approver, TestUsers.Finance, TestUsers.Auditor, TestUsers.Admin, TestUsers.NoRole];

        foreach (CurrentUser user in users)
        {
            ServiceResult<ExpenseResponse> result = await _service.CreateAsync(user, ValidRequest(), Token);
            AssertError(ServiceErrorKind.Forbidden, result);
        }

        Assert.IsEmpty(_repository.Expenses);
        Assert.IsEmpty(_repository.History);
    }

    /// <summary>
    /// Data futura é rejeitada com base no relógio do servidor.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task CreateWithFutureDateIsRejected()
    {
        ExpenseDraftRequest request = ValidRequest();
        request.ExpenseDate = new DateOnly(2026, 10, 14);

        ServiceResult<ExpenseResponse> result = await _service.CreateAsync(TestUsers.Ana, request, Token);

        AssertError(ServiceErrorKind.Validation, result);
        Assert.IsEmpty(_repository.Expenses);
    }

    /// <summary>
    /// Categoria inexistente é rejeitada.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task CreateWithUnknownCategoryIsRejected()
    {
        ExpenseDraftRequest request = ValidRequest();
        request.CategoryId = 99;

        ServiceResult<ExpenseResponse> result = await _service.CreateAsync(TestUsers.Ana, request, Token);

        AssertError(ServiceErrorKind.Validation, result);
        Assert.IsEmpty(_repository.Expenses);
    }

    /// <summary>
    /// A descrição é validada depois de remover espaços nas extremidades.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task CreateRejectsDescriptionShorterThanTenCharactersAfterTrim()
    {
        ExpenseDraftRequest request = ValidRequest();
        request.Description = "     Táxi        ";

        ServiceResult<ExpenseResponse> result = await _service.CreateAsync(TestUsers.Ana, request, Token);

        AssertError(ServiceErrorKind.Validation, result);
    }

    /// <summary>
    /// A edição em Draft altera os dados e registra somente os campos modificados.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task UpdateByOwnerChangesDraftAndRecordsChangedFields()
    {
        ExpenseResponse draft = await CreateDraftAsync(TestUsers.Ana);
        ExpenseDraftRequest request = ValidRequest();
        request.Description = "Almoço com cliente da filial";
        request.Amount = 120.00m;

        ServiceResult<ExpenseResponse> result = await _service.UpdateAsync(TestUsers.Ana, draft.Id, request, Token);

        Assert.IsNotNull(result.Value);
        Assert.AreEqual(120.00m, result.Value.Amount);
        Assert.AreEqual("Almoço com cliente da filial", result.Value.Description);
        Assert.AreEqual(ExpenseStatus.Draft, result.Value.Status);

        ExpenseHistory update = _repository.History[^1];
        Assert.AreEqual(ExpenseAction.Updated, update.Action);
        Assert.AreEqual(ExpenseStatus.Draft, update.FromStatus);
        Assert.AreEqual(ExpenseStatus.Draft, update.ToStatus);
        Assert.IsNotNull(update.Changes);

        using JsonDocument changes = JsonDocument.Parse(update.Changes);
        Assert.AreEqual("85.50", changes.RootElement.GetProperty("amount").GetProperty("from").GetString());
        Assert.AreEqual("120.00", changes.RootElement.GetProperty("amount").GetProperty("to").GetString());
        Assert.IsTrue(changes.RootElement.TryGetProperty("description", out _));
        Assert.IsFalse(changes.RootElement.TryGetProperty("expenseDate", out _));
    }

    /// <summary>
    /// Editar com os mesmos dados não gera histórico.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task UpdateWithoutChangesDoesNotCreateHistory()
    {
        ExpenseResponse draft = await CreateDraftAsync(TestUsers.Ana);

        ServiceResult<ExpenseResponse> result = await _service.UpdateAsync(TestUsers.Ana, draft.Id, ValidRequest(), Token);

        Assert.IsNull(result.Error);
        Assert.HasCount(1, _repository.History);
    }

    /// <summary>
    /// Um Employee não enxerga nem altera o reembolso de outro Employee.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task UpdateByAnotherEmployeeReturnsNotFound()
    {
        ExpenseResponse draft = await CreateDraftAsync(TestUsers.Ana);
        ExpenseDraftRequest request = ValidRequest();
        request.Amount = 1.00m;

        ServiceResult<ExpenseResponse> result = await _service.UpdateAsync(TestUsers.Bruno, draft.Id, request, Token);

        AssertError(ServiceErrorKind.NotFound, result);
        Assert.AreEqual(85.50m, _repository.Expenses[0].Amount);
    }

    /// <summary>
    /// Quem enxerga o reembolso por outra role, mas não é o proprietário, não pode editá-lo.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task UpdateByVisibleNonOwnerIsForbidden()
    {
        ExpenseResponse submitted = await CreateSubmittedAsync(TestUsers.Ana);

        ServiceResult<ExpenseResponse> result = await _service.UpdateAsync(
            TestUsers.ApproverEmployee,
            submitted.Id,
            ValidRequest(),
            Token);

        AssertError(ServiceErrorKind.Forbidden, result);
    }

    /// <summary>
    /// Depois do envio, a edição retorna conflito.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task UpdateAfterSubmitReturnsConflict()
    {
        ExpenseResponse submitted = await CreateSubmittedAsync(TestUsers.Ana);

        ServiceResult<ExpenseResponse> result = await _service.UpdateAsync(TestUsers.Ana, submitted.Id, ValidRequest(), Token);

        AssertError(ServiceErrorKind.Conflict, result);
    }

    /// <summary>
    /// O envio muda Draft para Submitted e registra o histórico.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task SubmitMovesDraftToSubmitted()
    {
        ExpenseResponse draft = await CreateDraftAsync(TestUsers.Ana);

        ServiceResult<ExpenseResponse> result = await _service.SubmitAsync(TestUsers.Ana, draft.Id, Token);

        Assert.IsNotNull(result.Value);
        Assert.AreEqual(ExpenseStatus.Submitted, result.Value.Status);
        Assert.AreEqual(_startTime.UtcDateTime, result.Value.SubmittedAtUtc);

        ExpenseHistory submitted = _repository.History[^1];
        Assert.AreEqual(ExpenseAction.Submitted, submitted.Action);
        Assert.AreEqual(ExpenseStatus.Draft, submitted.FromStatus);
        Assert.AreEqual(ExpenseStatus.Submitted, submitted.ToStatus);
    }

    /// <summary>
    /// Repetir o envio retorna conflito e não duplica o histórico.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task SubmitTwiceReturnsConflictWithoutDuplicateHistory()
    {
        ExpenseResponse submitted = await CreateSubmittedAsync(TestUsers.Ana);
        int historyCount = _repository.History.Count;

        ServiceResult<ExpenseResponse> result = await _service.SubmitAsync(TestUsers.Ana, submitted.Id, Token);

        AssertError(ServiceErrorKind.Conflict, result);
        Assert.HasCount(historyCount, _repository.History);
    }

    /// <summary>
    /// Outro Employee não envia rascunho alheio.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task SubmitByAnotherEmployeeReturnsNotFound()
    {
        ExpenseResponse draft = await CreateDraftAsync(TestUsers.Ana);

        ServiceResult<ExpenseResponse> result = await _service.SubmitAsync(TestUsers.Bruno, draft.Id, Token);

        AssertError(ServiceErrorKind.NotFound, result);
        Assert.AreEqual(ExpenseStatus.Draft, _repository.Expenses[0].Status);
    }

    /// <summary>
    /// A aprovação muda Submitted para Approved e registra o aprovador.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task ApproveMovesSubmittedToApproved()
    {
        ExpenseResponse submitted = await CreateSubmittedAsync(TestUsers.Ana);
        _clock.Advance(TimeSpan.FromHours(1));

        ServiceResult<ExpenseResponse> result = await _service.ApproveAsync(TestUsers.Approver, submitted.Id, Token);

        Assert.IsNotNull(result.Value);
        Assert.AreEqual(ExpenseStatus.Approved, result.Value.Status);
        Assert.AreEqual(TestUsers.Approver.UserId, result.Value.DecidedById);
        Assert.AreEqual(_startTime.AddHours(1).UtcDateTime, result.Value.DecidedAtUtc);

        ExpenseHistory approved = _repository.History[^1];
        Assert.AreEqual(ExpenseAction.Approved, approved.Action);
        Assert.AreEqual(TestUsers.Approver.UserId, approved.ActorId);
        Assert.AreEqual(ExpenseStatus.Submitted, approved.FromStatus);
        Assert.AreEqual(ExpenseStatus.Approved, approved.ToStatus);
    }

    /// <summary>
    /// Um Approver que também é Employee não aprova o próprio reembolso.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task ApproveOwnExpenseIsForbidden()
    {
        ExpenseResponse submitted = await CreateSubmittedAsync(TestUsers.ApproverEmployee);

        ServiceResult<ExpenseResponse> result = await _service.ApproveAsync(TestUsers.ApproverEmployee, submitted.Id, Token);

        AssertError(ServiceErrorKind.Forbidden, result);
        Assert.AreEqual(ExpenseStatus.Submitted, _repository.Expenses[0].Status);
    }

    /// <summary>
    /// Somente Approver aprova.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task ApproveWithoutApproverRoleIsForbidden()
    {
        ExpenseResponse submitted = await CreateSubmittedAsync(TestUsers.Ana);

        ServiceResult<ExpenseResponse> byFinance = await _service.ApproveAsync(TestUsers.Finance, submitted.Id, Token);
        ServiceResult<ExpenseResponse> byAuditor = await _service.ApproveAsync(TestUsers.Auditor, submitted.Id, Token);
        ServiceResult<ExpenseResponse> byAdmin = await _service.ApproveAsync(TestUsers.Admin, submitted.Id, Token);

        AssertError(ServiceErrorKind.Forbidden, byFinance);
        AssertError(ServiceErrorKind.Forbidden, byAuditor);
        AssertError(ServiceErrorKind.Forbidden, byAdmin);
    }

    /// <summary>
    /// Rascunhos de outros usuários continuam privados para o Approver.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task ApproveDraftOfAnotherUserReturnsNotFound()
    {
        ExpenseResponse draft = await CreateDraftAsync(TestUsers.Ana);

        ServiceResult<ExpenseResponse> result = await _service.ApproveAsync(TestUsers.Approver, draft.Id, Token);

        AssertError(ServiceErrorKind.NotFound, result);
    }

    /// <summary>
    /// Repetir a aprovação retorna conflito e não cria histórico duplicado.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task ApproveTwiceReturnsConflictWithoutDuplicateHistory()
    {
        ExpenseResponse approved = await CreateApprovedAsync(TestUsers.Ana);
        int historyCount = _repository.History.Count;

        ServiceResult<ExpenseResponse> result = await _service.ApproveAsync(TestUsers.Approver, approved.Id, Token);

        AssertError(ServiceErrorKind.Conflict, result);
        Assert.HasCount(historyCount, _repository.History);
    }

    /// <summary>
    /// A reprovação exige justificativa válida e não altera nada quando ela é inválida.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task RejectWithInvalidJustificationIsRejected()
    {
        ExpenseResponse submitted = await CreateSubmittedAsync(TestUsers.Ana);
        int historyCount = _repository.History.Count;
        string?[] invalidJustifications = [null, string.Empty, "          ", "Curta", new string('x', 501)];

        foreach (string? justification in invalidJustifications)
        {
            RejectExpenseRequest request = new() { Justification = justification };
            ServiceResult<ExpenseResponse> result = await _service.RejectAsync(TestUsers.Approver, submitted.Id, request, Token);
            AssertError(ServiceErrorKind.Validation, result);
        }

        Assert.AreEqual(ExpenseStatus.Submitted, _repository.Expenses[0].Status);
        Assert.HasCount(historyCount, _repository.History);
    }

    /// <summary>
    /// A reprovação persiste a justificativa no reembolso e no histórico, e Rejected é final.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task RejectPersistsJustificationAndIsFinal()
    {
        ExpenseResponse submitted = await CreateSubmittedAsync(TestUsers.Ana);
        RejectExpenseRequest request = new() { Justification = "  Nota fiscal ilegível.  " };

        ServiceResult<ExpenseResponse> result = await _service.RejectAsync(TestUsers.Approver, submitted.Id, request, Token);

        Assert.IsNotNull(result.Value);
        Assert.AreEqual(ExpenseStatus.Rejected, result.Value.Status);
        Assert.AreEqual("Nota fiscal ilegível.", result.Value.RejectionReason);

        ExpenseHistory rejected = _repository.History[^1];
        Assert.AreEqual(ExpenseAction.Rejected, rejected.Action);
        Assert.AreEqual("Nota fiscal ilegível.", rejected.Justification);

        AssertError(ServiceErrorKind.Conflict, await _service.ApproveAsync(TestUsers.Approver, submitted.Id, Token));
        AssertError(ServiceErrorKind.Conflict, await _service.RejectAsync(TestUsers.Approver, submitted.Id, request, Token));
        AssertError(ServiceErrorKind.Conflict, await _service.PayAsync(TestUsers.Finance, submitted.Id, Token));
    }

    /// <summary>
    /// O Approver não reprova o próprio reembolso.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task RejectOwnExpenseIsForbidden()
    {
        ExpenseResponse submitted = await CreateSubmittedAsync(TestUsers.ApproverEmployee);
        RejectExpenseRequest request = new() { Justification = "Despesa fora da política." };

        ServiceResult<ExpenseResponse> result = await _service.RejectAsync(TestUsers.ApproverEmployee, submitted.Id, request, Token);

        AssertError(ServiceErrorKind.Forbidden, result);
    }

    /// <summary>
    /// O pagamento muda Approved para Paid e grava o registro com ator e horário do servidor.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task PayMovesApprovedToPaidAndCreatesPaymentRecord()
    {
        ExpenseResponse approved = await CreateApprovedAsync(TestUsers.Ana);
        _clock.Advance(TimeSpan.FromDays(1));

        ServiceResult<ExpenseResponse> result = await _service.PayAsync(TestUsers.Finance, approved.Id, Token);

        Assert.IsNotNull(result.Value);
        Assert.AreEqual(ExpenseStatus.Paid, result.Value.Status);

        Assert.HasCount(1, _repository.Payments);
        PaymentRecord payment = _repository.Payments[0];
        Assert.AreEqual(approved.Id, payment.ExpenseId);
        Assert.AreEqual(85.50m, payment.Amount);
        Assert.AreEqual(TestUsers.Finance.UserId, payment.PaidById);
        Assert.AreEqual(_startTime.AddDays(1).UtcDateTime, payment.PaidAtUtc);

        ExpenseHistory paid = _repository.History[^1];
        Assert.AreEqual(ExpenseAction.Paid, paid.Action);
        Assert.AreEqual(ExpenseStatus.Approved, paid.FromStatus);
        Assert.AreEqual(ExpenseStatus.Paid, paid.ToStatus);
    }

    /// <summary>
    /// Pagar um reembolso que ainda não foi aprovado retorna conflito.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task PaySubmittedExpenseReturnsConflict()
    {
        ExpenseResponse submitted = await CreateSubmittedAsync(TestUsers.Ana);

        ServiceResult<ExpenseResponse> result = await _service.PayAsync(TestUsers.Finance, submitted.Id, Token);

        AssertError(ServiceErrorKind.Conflict, result);
        Assert.IsEmpty(_repository.Payments);
    }

    /// <summary>
    /// Finance que também é Employee não paga o próprio reembolso.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task PayOwnExpenseIsForbidden()
    {
        ExpenseResponse approved = await CreateApprovedAsync(TestUsers.FinanceEmployee);

        ServiceResult<ExpenseResponse> result = await _service.PayAsync(TestUsers.FinanceEmployee, approved.Id, Token);

        AssertError(ServiceErrorKind.Forbidden, result);
        Assert.IsEmpty(_repository.Payments);
    }

    /// <summary>
    /// Repetir o pagamento retorna conflito e não cria segundo registro.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task PayTwiceReturnsConflictWithoutSecondPayment()
    {
        ExpenseResponse approved = await CreateApprovedAsync(TestUsers.Ana);
        await _service.PayAsync(TestUsers.Finance, approved.Id, Token);
        int historyCount = _repository.History.Count;

        ServiceResult<ExpenseResponse> result = await _service.PayAsync(TestUsers.Finance, approved.Id, Token);

        AssertError(ServiceErrorKind.Conflict, result);
        Assert.HasCount(1, _repository.Payments);
        Assert.HasCount(historyCount, _repository.History);
    }

    /// <summary>
    /// Somente Finance paga.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task PayWithoutFinanceRoleIsForbidden()
    {
        ExpenseResponse approved = await CreateApprovedAsync(TestUsers.Ana);

        ServiceResult<ExpenseResponse> result = await _service.PayAsync(TestUsers.Approver, approved.Id, Token);

        AssertError(ServiceErrorKind.Forbidden, result);
    }

    /// <summary>
    /// Cada perfil lista somente o seu escopo.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task ListAppliesScopeOfEachRole()
    {
        ExpenseResponse anaDraft = await CreateDraftAsync(TestUsers.Ana);
        ExpenseResponse anaSubmitted = await CreateSubmittedAsync(TestUsers.Ana);
        ExpenseResponse brunoApproved = await CreateApprovedAsync(TestUsers.Bruno);
        ExpenseResponse brunoPaid = await CreateApprovedAsync(TestUsers.Bruno);
        await _service.PayAsync(TestUsers.Finance, brunoPaid.Id, Token);

        Assert.AreEqual(DescribeIds(anaDraft.Id, anaSubmitted.Id), await ListIdsAsync(TestUsers.Ana));
        Assert.AreEqual(DescribeIds(brunoApproved.Id, brunoPaid.Id), await ListIdsAsync(TestUsers.Bruno));
        Assert.AreEqual(DescribeIds(anaSubmitted.Id), await ListIdsAsync(TestUsers.Approver));
        Assert.AreEqual(DescribeIds(brunoApproved.Id, brunoPaid.Id), await ListIdsAsync(TestUsers.Finance));
        Assert.AreEqual(
            DescribeIds(anaDraft.Id, anaSubmitted.Id, brunoApproved.Id, brunoPaid.Id),
            await ListIdsAsync(TestUsers.Auditor));
    }

    /// <summary>
    /// Admin sem role funcional não consulta reembolsos.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task AdminWithoutFunctionalRoleCannotReadExpenses()
    {
        ExpenseResponse submitted = await CreateSubmittedAsync(TestUsers.Ana);

        AssertError(ServiceErrorKind.Forbidden, await _service.ListAsync(TestUsers.Admin, Token));
        AssertError(ServiceErrorKind.Forbidden, await _service.GetAsync(TestUsers.Admin, submitted.Id, Token));
        AssertError(ServiceErrorKind.Forbidden, await _service.GetHistoryAsync(TestUsers.Admin, submitted.Id, Token));
    }

    /// <summary>
    /// Reembolso fora do escopo de leitura retorna 404, sem revelar sua existência.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task GetOutsideReadScopeReturnsNotFound()
    {
        ExpenseResponse draft = await CreateDraftAsync(TestUsers.Ana);

        AssertError(ServiceErrorKind.NotFound, await _service.GetAsync(TestUsers.Bruno, draft.Id, Token));
        AssertError(ServiceErrorKind.NotFound, await _service.GetAsync(TestUsers.Approver, draft.Id, Token));
        AssertError(ServiceErrorKind.NotFound, await _service.GetAsync(TestUsers.Finance, draft.Id, Token));
        AssertError(ServiceErrorKind.NotFound, await _service.GetAsync(TestUsers.Ana, Guid.NewGuid(), Token));

        ServiceResult<ExpenseResponse> byAuditor = await _service.GetAsync(TestUsers.Auditor, draft.Id, Token);
        Assert.IsNull(byAuditor.Error);
    }

    /// <summary>
    /// O histórico segue a visibilidade do reembolso e vem em ordem cronológica.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task HistoryFollowsExpenseVisibilityAndIsChronological()
    {
        ExpenseResponse approved = await CreateApprovedAsync(TestUsers.Ana);
        _clock.Advance(TimeSpan.FromMinutes(5));
        await _service.PayAsync(TestUsers.Finance, approved.Id, Token);

        AssertError(ServiceErrorKind.NotFound, await _service.GetHistoryAsync(TestUsers.Bruno, approved.Id, Token));
        AssertError(ServiceErrorKind.NotFound, await _service.GetHistoryAsync(TestUsers.Approver, approved.Id, Token));

        ServiceResult<IReadOnlyList<ExpenseHistoryResponse>> result = await _service.GetHistoryAsync(TestUsers.Auditor, approved.Id, Token);

        Assert.IsNotNull(result.Value);
        Assert.HasCount(4, result.Value);
        Assert.AreEqual(ExpenseAction.Created, result.Value[0].Action);
        Assert.AreEqual(ExpenseAction.Submitted, result.Value[1].Action);
        Assert.AreEqual(ExpenseAction.Approved, result.Value[2].Action);
        Assert.AreEqual(ExpenseAction.Paid, result.Value[3].Action);
        Assert.AreEqual(TestUsers.Finance.UserId, result.Value[3].ActorId);
    }

    /// <summary>
    /// Auditor consulta tudo, mas não altera dados.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task AuditorCannotChangeData()
    {
        ExpenseResponse submitted = await CreateSubmittedAsync(TestUsers.Ana);
        RejectExpenseRequest reject = new() { Justification = "Auditor tentando reprovar." };

        AssertError(ServiceErrorKind.Forbidden, await _service.CreateAsync(TestUsers.Auditor, ValidRequest(), Token));
        AssertError(ServiceErrorKind.Forbidden, await _service.UpdateAsync(TestUsers.Auditor, submitted.Id, ValidRequest(), Token));
        AssertError(ServiceErrorKind.Forbidden, await _service.SubmitAsync(TestUsers.Auditor, submitted.Id, Token));
        AssertError(ServiceErrorKind.Forbidden, await _service.ApproveAsync(TestUsers.Auditor, submitted.Id, Token));
        AssertError(ServiceErrorKind.Forbidden, await _service.RejectAsync(TestUsers.Auditor, submitted.Id, reject, Token));
        AssertError(ServiceErrorKind.Forbidden, await _service.PayAsync(TestUsers.Auditor, submitted.Id, Token));
        Assert.AreEqual(ExpenseStatus.Submitted, _repository.Expenses[0].Status);
    }

    /// <summary>
    /// Falha de concorrência retorna conflito e descarta o histórico pendente.
    /// </summary>
    /// <returns>Tarefa do teste.</returns>
    [TestMethod]
    public async Task ConcurrencyFailureReturnsConflictAndDiscardsHistory()
    {
        ExpenseResponse submitted = await CreateSubmittedAsync(TestUsers.Ana);
        int historyCount = _repository.History.Count;
        _repository.FailNextSave = true;

        ServiceResult<ExpenseResponse> result = await _service.ApproveAsync(TestUsers.Approver, submitted.Id, Token);

        AssertError(ServiceErrorKind.Conflict, result);
        Assert.HasCount(historyCount, _repository.History);
    }

    private static ExpenseDraftRequest ValidRequest()
    {
        return new ExpenseDraftRequest
        {
            Description = "Almoço com cliente",
            Amount = 85.50m,
            ExpenseDate = _pastDate,
            CategoryId = 1,
        };
    }

    private static void AssertError<T>(ServiceErrorKind expected, ServiceResult<T> result)
    {
        Assert.IsNotNull(result.Error);
        Assert.AreEqual(expected, result.Error.Kind);
    }

    private async Task<ExpenseResponse> CreateDraftAsync(CurrentUser owner)
    {
        ServiceResult<ExpenseResponse> result = await _service.CreateAsync(owner, ValidRequest(), Token);
        Assert.IsNotNull(result.Value);
        return result.Value;
    }

    private async Task<ExpenseResponse> CreateSubmittedAsync(CurrentUser owner)
    {
        ExpenseResponse draft = await CreateDraftAsync(owner);
        ServiceResult<ExpenseResponse> result = await _service.SubmitAsync(owner, draft.Id, Token);
        Assert.IsNotNull(result.Value);
        return result.Value;
    }

    private async Task<ExpenseResponse> CreateApprovedAsync(CurrentUser owner)
    {
        ExpenseResponse submitted = await CreateSubmittedAsync(owner);
        ServiceResult<ExpenseResponse> result = await _service.ApproveAsync(TestUsers.Approver, submitted.Id, Token);
        Assert.IsNotNull(result.Value);
        return result.Value;
    }

    private static string DescribeIds(params Guid[] ids)
    {
        return string.Join(',', ids.Order());
    }

    private async Task<string> ListIdsAsync(CurrentUser user)
    {
        ServiceResult<IReadOnlyList<ExpenseResponse>> result = await _service.ListAsync(user, Token);
        Assert.IsNotNull(result.Value);
        return DescribeIds(result.Value.Select(expense => expense.Id).ToArray());
    }
}
