using System;

namespace ExpenseHub.Api.Application;

/// <summary>
/// Categorias de falha de regra de negócio, convertidas em status HTTP pelos endpoints.
/// </summary>
internal enum ServiceErrorKind
{
    Validation = 0,
    Unauthorized = 1,
    Forbidden = 2,
    NotFound = 3,
    Conflict = 4,
}

/// <summary>
/// Falha de regra de negócio com mensagem legível.
/// </summary>
/// <param name="Kind">Categoria da falha.</param>
/// <param name="Message">Mensagem para o cliente.</param>
internal sealed record ServiceError(ServiceErrorKind Kind, string Message);

/// <summary>
/// Resultado de uma operação de serviço: um valor ou uma falha.
/// </summary>
/// <typeparam name="T">Tipo do valor em caso de sucesso.</typeparam>
internal sealed class ServiceResult<T>
{
    private ServiceResult(T? value, ServiceError? error)
    {
        Value = value;
        Error = error;
    }

    /// <summary>
    /// Indica sucesso.
    /// </summary>
    public bool Succeeded => Error is null;

    /// <summary>
    /// Valor em caso de sucesso.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Falha em caso de erro.
    /// </summary>
    public ServiceError? Error { get; }

    /// <summary>
    /// Cria um resultado de sucesso.
    /// </summary>
    /// <param name="value">Valor produzido.</param>
    /// <returns>O resultado.</returns>
    public static ServiceResult<T> Success(T value)
    {
        return new ServiceResult<T>(value, null);
    }

    /// <summary>
    /// Cria um resultado de falha.
    /// </summary>
    /// <param name="kind">Categoria da falha.</param>
    /// <param name="message">Mensagem para o cliente.</param>
    /// <returns>O resultado.</returns>
    public static ServiceResult<T> Failure(ServiceErrorKind kind, string message)
    {
        return new ServiceResult<T>(default, new ServiceError(kind, message));
    }

    /// <summary>
    /// Cria um resultado de falha a partir de uma falha existente.
    /// </summary>
    /// <param name="error">Falha.</param>
    /// <returns>O resultado.</returns>
    public static ServiceResult<T> Failure(ServiceError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ServiceResult<T>(default, error);
    }

    /// <summary>
    /// Converte o resultado em outro tipo, como uma resposta HTTP.
    /// </summary>
    /// <typeparam name="TResult">Tipo produzido.</typeparam>
    /// <param name="onSuccess">Conversão do valor.</param>
    /// <param name="onFailure">Conversão da falha.</param>
    /// <returns>O valor convertido.</returns>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<ServiceError, TResult> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);

        return Error is null ? onSuccess(Value!) : onFailure(Error);
    }
}
