namespace SortingLab.App.ViewModels.Common;

public sealed record RunSegmentViewModel(
    int Number,
    int StartIndex,
    int EndIndex,
    int Count,
    string FirstKey,
    string LastKey)
{
    public string RangeText => StartIndex == EndIndex ? StartIndex.ToString() : $"{StartIndex}–{EndIndex}";

    public string Summary => $"#{Number}: {Count} зап., ключи {FirstKey} … {LastKey}";
}
