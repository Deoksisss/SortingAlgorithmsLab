using Avalonia;
using Avalonia.Controls;
using SortingLab.Core.Models;

namespace SortingLab.App.Views.Controls;

public partial class InfoCard : UserControl
{
    public static readonly StyledProperty<AlgorithmDescriptor?> DescriptorProperty =
        AvaloniaProperty.Register<InfoCard, AlgorithmDescriptor?>(nameof(Descriptor));

    public InfoCard()
    {
        InitializeComponent();
    }

    public AlgorithmDescriptor? Descriptor
    {
        get => GetValue(DescriptorProperty);
        set => SetValue(DescriptorProperty, value);
    }
}
