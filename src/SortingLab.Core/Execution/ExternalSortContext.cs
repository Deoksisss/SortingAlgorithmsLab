using SortingLab.Core.Abstractions;
using SortingLab.Core.Models;

namespace SortingLab.Core.Execution;

/// <summary>
/// Контекст внешней сортировки: счётчики файловых операций и нумерация шагов.
/// </summary>
public sealed class ExternalSortContext
{
    private readonly IStepSink<ExternalSortStep> _sink;
    private readonly SortStatistics _statistics = new();

    public ExternalSortContext(IStepSink<ExternalSortStep> sink)
    {
        _sink = sink;
    }

    public int StepNumber { get; private set; }

    public OperationStatistics Statistics => _statistics.Snapshot();

    public void CountComparison() => _statistics.Comparisons++;

    public void CountRead() => _statistics.Reads++;

    public void CountWrite() => _statistics.Writes++;

    public void CountMove() => _statistics.Moves++;

    public void Stop() => _statistics.Stop();

    public async ValueTask EmitAsync(ExternalSortStep step, CancellationToken cancellationToken)
    {
        var numbered = step with
        {
            Number = ++StepNumber,
            Statistics = _statistics.Snapshot()
        };
        await _sink.WriteAsync(numbered, cancellationToken);
    }
}
