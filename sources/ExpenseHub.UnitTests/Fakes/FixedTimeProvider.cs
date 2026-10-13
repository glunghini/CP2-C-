using System;

namespace ExpenseHub.UnitTests.Fakes;

/// <summary>
/// Relógio fixo para tornar os testes determinísticos.
/// </summary>
internal sealed class FixedTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    /// <summary>
    /// Cria o relógio no instante informado.
    /// </summary>
    /// <param name="utcNow">Instante atual simulado.</param>
    public FixedTimeProvider(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow()
    {
        return _utcNow;
    }

    /// <summary>
    /// Avança o relógio.
    /// </summary>
    /// <param name="interval">Intervalo a avançar.</param>
    public void Advance(TimeSpan interval)
    {
        _utcNow = _utcNow.Add(interval);
    }
}
