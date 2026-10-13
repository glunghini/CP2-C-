using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Dtos;
using ExpenseHub.Api.Responses;
using ExpenseHub.Api.Security;

namespace ExpenseHub.Api.Application.Expenses;

/// <summary>
/// Regras de negócio dos reembolsos. Combina role, ownership e estado em cada operação,
/// mesmo quando a rota já exige uma role.
/// </summary>
internal sealed class ExpenseService
{
    internal const string NotFoundMessage = "Reembolso não encontrado.";
    internal const string ConcurrencyMessage = "O reembolso foi alterado por outra requisição. Consulte o estado atual.";

    private readonly IExpenseRepository _repository;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Inicializa o serviço.
    /// </summary>
    /// <param name="repository">Repositório de reembolsos.</param>
    /// <param name="timeProvider">Relógio do servidor.</param>
    public ExpenseService(IExpenseRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Cria um rascunho cujo proprietário é o usuário autenticado.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="request">Dados do rascunho.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso criado ou a falha.</returns>
    public async Task<ServiceResult<ExpenseResponse>> CreateAsync(
        CurrentUser user,
        ExpenseDraftRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(request);

        if (!user.IsInRole(AppRoles.Employee))
        {
            return Forbidden<ExpenseResponse>("Somente usuários com a role Employee podem criar reembolsos.");
        }

        DateTime nowUtc = UtcNow();
        ServiceResult<ExpenseDraftData> draft = await BuildDraftAsync(request, nowUtc, cancellationToken);
        if (draft.Error is not null)
        {
            return ServiceResult<ExpenseResponse>.Failure(draft.Error);
        }

        (Expense expense, ExpenseHistory history) = Expense.CreateDraft(user.UserId, draft.Value!, nowUtc);
        _repository.Add(expense);
        _repository.AddHistory(history);
        return await SaveAsync(expense, cancellationToken);
    }

    /// <summary>
    /// Edita um rascunho do próprio usuário e registra os campos alterados.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="id">Reembolso.</param>
    /// <param name="request">Novos dados.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso atualizado ou a falha.</returns>
    public async Task<ServiceResult<ExpenseResponse>> UpdateAsync(
        CurrentUser user,
        Guid id,
        ExpenseDraftRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(request);

        ServiceResult<Expense> access = await LoadForOwnerAsync(user, id, ExpenseAction.Updated, cancellationToken);
        if (access.Error is not null)
        {
            return ServiceResult<ExpenseResponse>.Failure(access.Error);
        }

        Expense expense = access.Value!;
        DateTime nowUtc = UtcNow();
        ServiceResult<ExpenseDraftData> draft = await BuildDraftAsync(request, nowUtc, cancellationToken);
        if (draft.Error is not null)
        {
            return ServiceResult<ExpenseResponse>.Failure(draft.Error);
        }

        ExpenseHistory? history = expense.UpdateDraft(draft.Value!, user.UserId, nowUtc);
        if (history is null)
        {
            return ServiceResult<ExpenseResponse>.Success(ExpenseMapper.ToResponse(expense));
        }

        _repository.AddHistory(history);
        return await SaveAsync(expense, cancellationToken);
    }

    /// <summary>
    /// Envia um rascunho do próprio usuário para aprovação.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="id">Reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso enviado ou a falha.</returns>
    public async Task<ServiceResult<ExpenseResponse>> SubmitAsync(CurrentUser user, Guid id, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        ServiceResult<Expense> access = await LoadForOwnerAsync(user, id, ExpenseAction.Submitted, cancellationToken);
        if (access.Error is not null)
        {
            return ServiceResult<ExpenseResponse>.Failure(access.Error);
        }

        Expense expense = access.Value!;
        _repository.AddHistory(expense.Submit(user.UserId, UtcNow()));
        return await SaveAsync(expense, cancellationToken);
    }

    /// <summary>
    /// Aprova um reembolso enviado por outro usuário.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="id">Reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso aprovado ou a falha.</returns>
    public async Task<ServiceResult<ExpenseResponse>> ApproveAsync(CurrentUser user, Guid id, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        ServiceResult<Expense> access = await LoadForWorkflowAsync(
            user,
            id,
            AppRoles.Approver,
            ExpenseAction.Approved,
            cancellationToken);
        if (access.Error is not null)
        {
            return ServiceResult<ExpenseResponse>.Failure(access.Error);
        }

        Expense expense = access.Value!;
        _repository.AddHistory(expense.Approve(user.UserId, UtcNow()));
        return await SaveAsync(expense, cancellationToken);
    }

    /// <summary>
    /// Reprova um reembolso enviado por outro usuário, com justificativa obrigatória.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="id">Reembolso.</param>
    /// <param name="request">Justificativa.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso reprovado ou a falha.</returns>
    public async Task<ServiceResult<ExpenseResponse>> RejectAsync(
        CurrentUser user,
        Guid id,
        RejectExpenseRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(request);

        ServiceResult<Expense> access = await LoadForWorkflowAsync(
            user,
            id,
            AppRoles.Approver,
            ExpenseAction.Rejected,
            cancellationToken);
        if (access.Error is not null)
        {
            return ServiceResult<ExpenseResponse>.Failure(access.Error);
        }

        string justification = ExpenseRules.Normalize(request.Justification);
        string? validationError = ExpenseRules.ValidateJustification(justification);
        if (validationError is not null)
        {
            return ServiceResult<ExpenseResponse>.Failure(ServiceErrorKind.Validation, validationError);
        }

        Expense expense = access.Value!;
        _repository.AddHistory(expense.Reject(user.UserId, justification, UtcNow()));
        return await SaveAsync(expense, cancellationToken);
    }

    /// <summary>
    /// Registra o pagamento de um reembolso aprovado de outro usuário.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="id">Reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso pago ou a falha.</returns>
    public async Task<ServiceResult<ExpenseResponse>> PayAsync(CurrentUser user, Guid id, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        ServiceResult<Expense> access = await LoadForWorkflowAsync(
            user,
            id,
            AppRoles.Finance,
            ExpenseAction.Paid,
            cancellationToken);
        if (access.Error is not null)
        {
            return ServiceResult<ExpenseResponse>.Failure(access.Error);
        }

        Expense expense = access.Value!;
        (ExpenseHistory history, PaymentRecord payment) = expense.Pay(user.UserId, UtcNow());
        _repository.AddHistory(history);
        _repository.AddPayment(payment);
        return await SaveAsync(expense, cancellationToken);
    }

    /// <summary>
    /// Lista os reembolsos visíveis para o perfil do usuário.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Os reembolsos visíveis ou a falha.</returns>
    public async Task<ServiceResult<IReadOnlyList<ExpenseResponse>>> ListAsync(CurrentUser user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (!user.IsInAnyRole(AppRoles.ExpenseReaders))
        {
            return Forbidden<IReadOnlyList<ExpenseResponse>>("O perfil do usuário não permite consultar reembolsos.");
        }

        IReadOnlyList<Expense> expenses = await _repository.ListAsync(ExpenseVisibility.For(user), cancellationToken);
        IReadOnlyList<ExpenseResponse> responses = expenses.Select(ExpenseMapper.ToResponse).ToList();
        return ServiceResult<IReadOnlyList<ExpenseResponse>>.Success(responses);
    }

    /// <summary>
    /// Consulta um reembolso visível para o usuário.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="id">Reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O reembolso ou a falha.</returns>
    public async Task<ServiceResult<ExpenseResponse>> GetAsync(CurrentUser user, Guid id, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        ServiceResult<Expense> access = await LoadReadableAsync(user, id, cancellationToken);
        return access.Error is null
            ? ServiceResult<ExpenseResponse>.Success(ExpenseMapper.ToResponse(access.Value!))
            : ServiceResult<ExpenseResponse>.Failure(access.Error);
    }

    /// <summary>
    /// Consulta o histórico de um reembolso, com a mesma visibilidade do reembolso.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="id">Reembolso.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>O histórico em ordem cronológica ou a falha.</returns>
    public async Task<ServiceResult<IReadOnlyList<ExpenseHistoryResponse>>> GetHistoryAsync(
        CurrentUser user,
        Guid id,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        ServiceResult<Expense> access = await LoadReadableAsync(user, id, cancellationToken);
        if (access.Error is not null)
        {
            return ServiceResult<IReadOnlyList<ExpenseHistoryResponse>>.Failure(access.Error);
        }

        IReadOnlyList<ExpenseHistory> entries = await _repository.ListHistoryAsync(id, cancellationToken);
        IReadOnlyList<ExpenseHistoryResponse> responses = entries.Select(ExpenseMapper.ToResponse).ToList();
        return ServiceResult<IReadOnlyList<ExpenseHistoryResponse>>.Success(responses);
    }

    /// <summary>
    /// Lista as categorias disponíveis para perfis com acesso funcional.
    /// </summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>As categorias ou a falha.</returns>
    public async Task<ServiceResult<IReadOnlyList<ExpenseCategoryResponse>>> ListCategoriesAsync(
        CurrentUser user,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (!user.IsInAnyRole(AppRoles.ExpenseReaders))
        {
            return Forbidden<IReadOnlyList<ExpenseCategoryResponse>>("O perfil do usuário não permite consultar categorias.");
        }

        IReadOnlyList<ExpenseCategory> categories = await _repository.ListCategoriesAsync(cancellationToken);
        IReadOnlyList<ExpenseCategoryResponse> responses = categories.Select(ExpenseMapper.ToResponse).ToList();
        return ServiceResult<IReadOnlyList<ExpenseCategoryResponse>>.Success(responses);
    }

    private static ServiceResult<T> Forbidden<T>(string message)
    {
        return ServiceResult<T>.Failure(ServiceErrorKind.Forbidden, message);
    }

    private static ServiceResult<T> NotFound<T>()
    {
        return ServiceResult<T>.Failure(ServiceErrorKind.NotFound, NotFoundMessage);
    }

    private static ServiceResult<T> Conflict<T>(string message)
    {
        return ServiceResult<T>.Failure(ServiceErrorKind.Conflict, message);
    }

    private static string DescribeInvalidTransition(ExpenseAction action)
    {
        return action switch
        {
            ExpenseAction.Updated => "Somente reembolsos em Draft podem ser editados.",
            ExpenseAction.Submitted => "Somente reembolsos em Draft podem ser enviados.",
            ExpenseAction.Approved => "Somente reembolsos em Submitted podem ser aprovados.",
            ExpenseAction.Rejected => "Somente reembolsos em Submitted podem ser reprovados.",
            ExpenseAction.Paid => "Somente reembolsos em Approved podem ser pagos.",
            _ => "A transição solicitada não é permitida no estado atual.",
        };
    }

    private DateTime UtcNow()
    {
        return _timeProvider.GetUtcNow().UtcDateTime;
    }

    private async Task<ServiceResult<Expense>> LoadReadableAsync(CurrentUser user, Guid id, CancellationToken cancellationToken)
    {
        if (!user.IsInAnyRole(AppRoles.ExpenseReaders))
        {
            return Forbidden<Expense>("O perfil do usuário não permite consultar reembolsos.");
        }

        Expense? expense = await _repository.FindAsync(id, ExpenseVisibility.For(user), cancellationToken);
        return expense is null ? NotFound<Expense>() : ServiceResult<Expense>.Success(expense);
    }

    private async Task<ServiceResult<Expense>> LoadForOwnerAsync(
        CurrentUser user,
        Guid id,
        ExpenseAction action,
        CancellationToken cancellationToken)
    {
        if (!user.IsInRole(AppRoles.Employee))
        {
            return Forbidden<Expense>("Somente usuários com a role Employee podem editar ou enviar reembolsos.");
        }

        Expense? expense = await _repository.FindForUpdateAsync(id, ExpenseVisibility.For(user), cancellationToken);
        if (expense is null)
        {
            return NotFound<Expense>();
        }

        if (!user.Owns(expense.OwnerId))
        {
            return Forbidden<Expense>("Somente o proprietário pode editar ou enviar o reembolso.");
        }

        return ExpenseStateMachine.CanApply(expense.Status, action)
            ? ServiceResult<Expense>.Success(expense)
            : Conflict<Expense>(DescribeInvalidTransition(action));
    }

    private async Task<ServiceResult<Expense>> LoadForWorkflowAsync(
        CurrentUser user,
        Guid id,
        string requiredRole,
        ExpenseAction action,
        CancellationToken cancellationToken)
    {
        if (!user.IsInRole(requiredRole))
        {
            return Forbidden<Expense>("O perfil do usuário não permite esta operação.");
        }

        Expression<Func<Expense, bool>> scope = ExpenseVisibility.ForWorkflow(user);
        Expense? expense = await _repository.FindForUpdateAsync(id, scope, cancellationToken);
        if (expense is null)
        {
            return NotFound<Expense>();
        }

        if (user.Owns(expense.OwnerId))
        {
            return Forbidden<Expense>("Não é permitido aprovar, reprovar ou pagar o próprio reembolso.");
        }

        return ExpenseStateMachine.CanApply(expense.Status, action)
            ? ServiceResult<Expense>.Success(expense)
            : Conflict<Expense>(DescribeInvalidTransition(action));
    }

    private async Task<ServiceResult<ExpenseDraftData>> BuildDraftAsync(
        ExpenseDraftRequest request,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        if (request.Amount is not decimal amount
            || request.ExpenseDate is not DateOnly expenseDate
            || request.CategoryId is not int categoryId)
        {
            return ServiceResult<ExpenseDraftData>.Failure(
                ServiceErrorKind.Validation,
                "Valor, data da despesa e categoria são obrigatórios.");
        }

        string description = ExpenseRules.Normalize(request.Description);
        string? validationError = ExpenseRules.ValidateDraft(description, amount, expenseDate, DateOnly.FromDateTime(nowUtc));
        if (validationError is not null)
        {
            return ServiceResult<ExpenseDraftData>.Failure(ServiceErrorKind.Validation, validationError);
        }

        if (!await _repository.CategoryExistsAsync(categoryId, cancellationToken))
        {
            return ServiceResult<ExpenseDraftData>.Failure(ServiceErrorKind.Validation, "A categoria informada não existe.");
        }

        return ServiceResult<ExpenseDraftData>.Success(new ExpenseDraftData(description, amount, expenseDate, categoryId));
    }

    private async Task<ServiceResult<ExpenseResponse>> SaveAsync(Expense expense, CancellationToken cancellationToken)
    {
        if (!await _repository.TrySaveChangesAsync(cancellationToken))
        {
            return Conflict<ExpenseResponse>(ConcurrencyMessage);
        }

        return ServiceResult<ExpenseResponse>.Success(ExpenseMapper.ToResponse(expense));
    }
}
