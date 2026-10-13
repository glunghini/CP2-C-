using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Application;
using ExpenseHub.Api.Application.Expenses;
using ExpenseHub.Api.Dtos;
using ExpenseHub.Api.Responses;
using ExpenseHub.Api.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ExpenseHub.Api.Endpoints;

/// <summary>
/// Rotas de reembolsos. Cada rota exige a role correspondente; o serviço completa a
/// verificação com ownership, estado e visibilidade.
/// </summary>
internal static class ExpenseEndpoints
{
    /// <summary>
    /// Mapeia as rotas de <c>/api/expenses</c> e <c>/api/expense-categories</c>.
    /// </summary>
    /// <param name="endpoints">Construtor de rotas.</param>
    /// <returns>O mesmo construtor, para encadeamento.</returns>
    public static IEndpointRouteBuilder MapExpenseEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder group = endpoints.MapGroup("/api/expenses").WithTags("Expenses");

        group.MapPost(string.Empty, CreateAsync)
            .RequireAuthorization(AuthorizationPolicies.EmployeeOnly)
            .WithName("CreateExpense");

        group.MapPut("/{id:guid}", UpdateAsync)
            .RequireAuthorization(AuthorizationPolicies.EmployeeOnly)
            .WithName("UpdateExpense");

        group.MapGet(string.Empty, ListAsync)
            .RequireAuthorization(AuthorizationPolicies.ExpenseReader)
            .WithName("ListExpenses");

        group.MapGet("/{id:guid}", GetAsync)
            .RequireAuthorization(AuthorizationPolicies.ExpenseReader)
            .WithName("GetExpense");

        group.MapPost("/{id:guid}/submit", SubmitAsync)
            .RequireAuthorization(AuthorizationPolicies.EmployeeOnly)
            .WithName("SubmitExpense");

        group.MapPost("/{id:guid}/approve", ApproveAsync)
            .RequireAuthorization(AuthorizationPolicies.ApproverOnly)
            .WithName("ApproveExpense");

        group.MapPost("/{id:guid}/reject", RejectAsync)
            .RequireAuthorization(AuthorizationPolicies.ApproverOnly)
            .WithName("RejectExpense");

        group.MapPost("/{id:guid}/pay", PayAsync)
            .RequireAuthorization(AuthorizationPolicies.FinanceOnly)
            .WithName("PayExpense");

        group.MapGet("/{id:guid}/history", GetHistoryAsync)
            .RequireAuthorization(AuthorizationPolicies.ExpenseReader)
            .WithName("GetExpenseHistory");

        endpoints.MapGet("/api/expense-categories", ListCategoriesAsync)
            .RequireAuthorization(AuthorizationPolicies.ExpenseReader)
            .WithName("ListExpenseCategories")
            .WithTags("Expenses");

        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        ExpenseDraftRequest request,
        ClaimsPrincipal principal,
        ExpenseService expenseService,
        CancellationToken cancellationToken)
    {
        if (!CurrentUser.TryCreate(principal, out CurrentUser? user))
        {
            return ProblemResults.MissingIdentity();
        }

        if (!RequestValidation.TryValidate(request, out IResult? problem))
        {
            return problem;
        }

        ServiceResult<ExpenseResponse> result = await expenseService.CreateAsync(user, request, cancellationToken);
        return result.Match(
            expense => Results.Created(LocationOf(expense), expense),
            ProblemResults.FromError);
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        ExpenseDraftRequest request,
        ClaimsPrincipal principal,
        ExpenseService expenseService,
        CancellationToken cancellationToken)
    {
        if (!CurrentUser.TryCreate(principal, out CurrentUser? user))
        {
            return ProblemResults.MissingIdentity();
        }

        if (!RequestValidation.TryValidate(request, out IResult? problem))
        {
            return problem;
        }

        ServiceResult<ExpenseResponse> result = await expenseService.UpdateAsync(user, id, request, cancellationToken);
        return ToOk(result);
    }

    private static async Task<IResult> ListAsync(
        ClaimsPrincipal principal,
        ExpenseService expenseService,
        CancellationToken cancellationToken)
    {
        if (!CurrentUser.TryCreate(principal, out CurrentUser? user))
        {
            return ProblemResults.MissingIdentity();
        }

        ServiceResult<IReadOnlyList<ExpenseResponse>> result = await expenseService.ListAsync(user, cancellationToken);
        return ToOk(result);
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        ClaimsPrincipal principal,
        ExpenseService expenseService,
        CancellationToken cancellationToken)
    {
        if (!CurrentUser.TryCreate(principal, out CurrentUser? user))
        {
            return ProblemResults.MissingIdentity();
        }

        ServiceResult<ExpenseResponse> result = await expenseService.GetAsync(user, id, cancellationToken);
        return ToOk(result);
    }

    private static async Task<IResult> SubmitAsync(
        Guid id,
        ClaimsPrincipal principal,
        ExpenseService expenseService,
        CancellationToken cancellationToken)
    {
        if (!CurrentUser.TryCreate(principal, out CurrentUser? user))
        {
            return ProblemResults.MissingIdentity();
        }

        ServiceResult<ExpenseResponse> result = await expenseService.SubmitAsync(user, id, cancellationToken);
        return ToOk(result);
    }

    private static async Task<IResult> ApproveAsync(
        Guid id,
        ClaimsPrincipal principal,
        ExpenseService expenseService,
        CancellationToken cancellationToken)
    {
        if (!CurrentUser.TryCreate(principal, out CurrentUser? user))
        {
            return ProblemResults.MissingIdentity();
        }

        ServiceResult<ExpenseResponse> result = await expenseService.ApproveAsync(user, id, cancellationToken);
        return ToOk(result);
    }

    private static async Task<IResult> RejectAsync(
        Guid id,
        RejectExpenseRequest request,
        ClaimsPrincipal principal,
        ExpenseService expenseService,
        CancellationToken cancellationToken)
    {
        if (!CurrentUser.TryCreate(principal, out CurrentUser? user))
        {
            return ProblemResults.MissingIdentity();
        }

        if (!RequestValidation.TryValidate(request, out IResult? problem))
        {
            return problem;
        }

        ServiceResult<ExpenseResponse> result = await expenseService.RejectAsync(user, id, request, cancellationToken);
        return ToOk(result);
    }

    private static async Task<IResult> PayAsync(
        Guid id,
        ClaimsPrincipal principal,
        ExpenseService expenseService,
        CancellationToken cancellationToken)
    {
        if (!CurrentUser.TryCreate(principal, out CurrentUser? user))
        {
            return ProblemResults.MissingIdentity();
        }

        ServiceResult<ExpenseResponse> result = await expenseService.PayAsync(user, id, cancellationToken);
        return ToOk(result);
    }

    private static async Task<IResult> GetHistoryAsync(
        Guid id,
        ClaimsPrincipal principal,
        ExpenseService expenseService,
        CancellationToken cancellationToken)
    {
        if (!CurrentUser.TryCreate(principal, out CurrentUser? user))
        {
            return ProblemResults.MissingIdentity();
        }

        ServiceResult<IReadOnlyList<ExpenseHistoryResponse>> result = await expenseService.GetHistoryAsync(user, id, cancellationToken);
        return ToOk(result);
    }

    private static async Task<IResult> ListCategoriesAsync(
        ClaimsPrincipal principal,
        ExpenseService expenseService,
        CancellationToken cancellationToken)
    {
        if (!CurrentUser.TryCreate(principal, out CurrentUser? user))
        {
            return ProblemResults.MissingIdentity();
        }

        ServiceResult<IReadOnlyList<ExpenseCategoryResponse>> result = await expenseService.ListCategoriesAsync(user, cancellationToken);
        return ToOk(result);
    }

    private static Uri LocationOf(ExpenseResponse expense)
    {
        string path = "/api/expenses/" + expense.Id.ToString("D", CultureInfo.InvariantCulture);
        return new Uri(path, UriKind.Relative);
    }

    private static IResult ToOk<T>(ServiceResult<T> result)
    {
        return result.Match(value => Results.Ok(value), ProblemResults.FromError);
    }
}
