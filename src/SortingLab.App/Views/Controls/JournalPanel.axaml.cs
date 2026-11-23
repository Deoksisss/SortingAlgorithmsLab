using System.Collections;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;

namespace SortingLab.App.Views.Controls;

public partial class JournalPanel : UserControl
{
    public static readonly StyledProperty<IEnumerable?> ItemsProperty =
        AvaloniaProperty.Register<JournalPanel, IEnumerable?>(nameof(Items));

    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<JournalPanel, string>(nameof(Title), "Журнал операций");

    private INotifyCollectionChanged? _notifyCollectionChanged;

    public JournalPanel()
    {
        InitializeComponent();
    }

    public IEnumerable? Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ItemsProperty)
        {
            if (_notifyCollectionChanged is not null)
            {
                _notifyCollectionChanged.CollectionChanged -= OnCollectionChanged;
            }

            _notifyCollectionChanged = change.NewValue as INotifyCollectionChanged;
            if (_notifyCollectionChanged is not null)
            {
                _notifyCollectionChanged.CollectionChanged += OnCollectionChanged;
            }
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action is NotifyCollectionChangedAction.Add && Items is { } items)
        {
            var last = items.Cast<object?>().LastOrDefault();
            if (last is not null)
            {
                LogGrid.ScrollIntoView(last, null);
            }
        }
    }
}
