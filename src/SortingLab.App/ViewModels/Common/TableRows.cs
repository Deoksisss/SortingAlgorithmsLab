namespace SortingLab.App.ViewModels.Common;

public sealed record FrequencyRowViewModel(string Word, int Count, double Percent)
{
    public string CountText => Count.ToString();
    public string PercentText => $"{Percent:0.0}%";
}

public sealed record BenchmarkRowViewModel(
    int WordCount,
    double QuickSortMs,
    double RadixSortMs,
    double QuickSortMinMs,
    double RadixSortMinMs)
{
    public string WordCountText => WordCount.ToString("N0");

    public string QuickSortText => QuickSortMs < 1
        ? $"{QuickSortMs * 1000:0} мкс"
        : $"{QuickSortMs:0.000} мс";

    public string RadixSortText => RadixSortMs < 1
        ? $"{RadixSortMs * 1000:0} мкс"
        : $"{RadixSortMs:0.000} мс";

    public string RatioText => QuickSortMs <= 0.000001 || RadixSortMs <= 0.000001
        ? "—"
        : $"×{QuickSortMs / RadixSortMs:0.00}";
}
