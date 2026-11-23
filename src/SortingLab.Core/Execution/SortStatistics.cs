using System.Diagnostics;
using SortingLab.Core.Models;

namespace SortingLab.Core.Execution;

internal sealed class SortStatistics
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    public int Comparisons { get; set; }

    public int Swaps { get; set; }

    public int Moves { get; set; }

    public int Writes { get; set; }

    public int Reads { get; set; }

    public OperationStatistics Snapshot() =>
        new(Comparisons, Swaps, Moves, Writes, Reads, _stopwatch.Elapsed.TotalMilliseconds);

    public void Stop()
    {
        if (_stopwatch.IsRunning)
        {
            _stopwatch.Stop();
        }
    }
}
