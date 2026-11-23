using CommunityToolkit.Mvvm.ComponentModel;

namespace SortingLab.App.ViewModels.Common;

public sealed partial class RecordRowViewModel : ObservableObject
{
    public RecordRowViewModel(int id, IReadOnlyList<string> fields, RowHighlight highlight = RowHighlight.None)
    {
        Id = id;
        Fields = fields;
        _highlight = highlight;
    }

    public int Id { get; }

    public IReadOnlyList<string> Fields { get; }

    [ObservableProperty]
    private RowHighlight _highlight;

    public string FirstField => Fields.Count > 0 ? Fields[0] : string.Empty;
}
