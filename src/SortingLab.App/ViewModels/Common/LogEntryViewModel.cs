namespace SortingLab.App.ViewModels.Common;

public sealed record LogEntryViewModel(
    int Number,
    string Kind,
    string Details,
    string Explanation,
    string Title);
