using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Data;
using SortingLab.App.ViewModels.Sections;

namespace SortingLab.App.Views.Sections;

public partial class ExternalSortView : UserControl
{
    private ExternalSortViewModel? _viewModel;
    private INotifyCollectionChanged? _resultRows;

    public ExternalSortView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (_resultRows is not null)
        {
            _resultRows.CollectionChanged -= OnResultRowsChanged;
            _resultRows = null;
        }

        _viewModel = DataContext as ExternalSortViewModel;
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _resultRows = _viewModel.ResultRows;
        _resultRows.CollectionChanged += OnResultRowsChanged;
        RebuildColumns();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ExternalSortViewModel.Headers))
        {
            RebuildColumns();
        }
    }

    private void RebuildColumns()
    {
        var headers = _viewModel?.Headers ?? Array.Empty<string>();
        RebuildGrid(SourceGrid, headers);
        RebuildGrid(ResultGrid, headers);
    }

    private static void RebuildGrid(DataGrid grid, IReadOnlyList<string> headers)
    {
        grid.Columns.Clear();
        for (var index = 0; index < headers.Count; index++)
        {
            grid.Columns.Add(new DataGridTextColumn
            {
                Header = headers[index],
                Binding = new Binding($"Fields[{index}]"),
                Width = new DataGridLength(index == 0 ? 200 : 160, DataGridLengthUnitType.Pixel),
                IsReadOnly = true
            });
        }
    }

    private void OnResultRowsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is { Count: > 0 } items)
        {
            ResultGrid.ScrollIntoView(items[^1], null);
        }
    }
}
