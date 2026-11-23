using SortingLab.Core.Abstractions;

namespace SortingLab.Core.Execution;

/// <summary>
/// Синхронный сборщик шагов для модульных тестов и анализа.
/// </summary>
public sealed class CollectingStepSink<TStep> : IStepSink<TStep>
{
    private readonly List<TStep> _steps = new();

    public IReadOnlyList<TStep> Steps => _steps;

    public ValueTask WriteAsync(TStep step, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _steps.Add(step);
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Приёмник-заглушка для замеров производительности.
/// </summary>
public sealed class NullStepSink<TStep> : IStepSink<TStep>
{
    public ValueTask WriteAsync(TStep step, CancellationToken cancellationToken = default) =>
        ValueTask.CompletedTask;
}
