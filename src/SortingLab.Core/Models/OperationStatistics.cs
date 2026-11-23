namespace SortingLab.Core.Models;

/// <summary>
/// Неизменяемый снимок счётчиков и времени выполнения алгоритма.
/// </summary>
public sealed record OperationStatistics(
    int Comparisons = 0,
    int Swaps = 0,
    int Moves = 0,
    int Writes = 0,
    int Reads = 0,
    double ElapsedMilliseconds = 0)
{
    public static OperationStatistics Empty { get; } = new();

    public int TotalOperations => Comparisons + Swaps + Moves + Writes + Reads;

    public string ElapsedText => ElapsedMilliseconds < 1
        ? $"{ElapsedMilliseconds * 1000:0} мкс"
        : $"{ElapsedMilliseconds:0.000} мс";
}
