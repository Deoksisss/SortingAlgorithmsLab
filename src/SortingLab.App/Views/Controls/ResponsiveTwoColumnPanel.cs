using Avalonia;
using Avalonia.Controls;

namespace SortingLab.App.Views.Controls;

/// <summary>
/// Две колонки на широком окне и вертикальное расположение на узком.
/// Убирает горизонтальное переполнение при изменении размера окна.
/// </summary>
public sealed class ResponsiveTwoColumnPanel : Panel
{
    public static readonly StyledProperty<double> BreakpointProperty =
        AvaloniaProperty.Register<ResponsiveTwoColumnPanel, double>(nameof(Breakpoint), 1050);

    public static readonly StyledProperty<double> SideWidthProperty =
        AvaloniaProperty.Register<ResponsiveTwoColumnPanel, double>(nameof(SideWidth), 370);

    public static readonly StyledProperty<double> ColumnSpacingProperty =
        AvaloniaProperty.Register<ResponsiveTwoColumnPanel, double>(nameof(ColumnSpacing), 12);

    public static readonly StyledProperty<double> RowSpacingProperty =
        AvaloniaProperty.Register<ResponsiveTwoColumnPanel, double>(nameof(RowSpacing), 12);

    static ResponsiveTwoColumnPanel()
    {
        AffectsMeasure<ResponsiveTwoColumnPanel>(
            BreakpointProperty,
            SideWidthProperty,
            ColumnSpacingProperty,
            RowSpacingProperty);
    }

    public double Breakpoint
    {
        get => GetValue(BreakpointProperty);
        set => SetValue(BreakpointProperty, value);
    }

    public double SideWidth
    {
        get => GetValue(SideWidthProperty);
        set => SetValue(SideWidthProperty, value);
    }

    public double ColumnSpacing
    {
        get => GetValue(ColumnSpacingProperty);
        set => SetValue(ColumnSpacingProperty, value);
    }

    public double RowSpacing
    {
        get => GetValue(RowSpacingProperty);
        set => SetValue(RowSpacingProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count == 0)
        {
            return default;
        }

        if (Children.Count == 1)
        {
            Children[0].Measure(availableSize);
            return Children[0].DesiredSize;
        }

        var width = double.IsInfinity(availableSize.Width) ? 1200 : availableSize.Width;
        if (width >= Breakpoint)
        {
            var mainWidth = Math.Max(0, width - SideWidth - ColumnSpacing);
            Children[0].Measure(new Size(mainWidth, availableSize.Height));
            Children[1].Measure(new Size(SideWidth, availableSize.Height));
            return new Size(
                width,
                Math.Max(Children[0].DesiredSize.Height, Children[1].DesiredSize.Height));
        }

        Children[0].Measure(new Size(width, double.PositiveInfinity));
        Children[1].Measure(new Size(width, double.PositiveInfinity));
        return new Size(
            width,
            Children[0].DesiredSize.Height + RowSpacing + Children[1].DesiredSize.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count == 0)
        {
            return finalSize;
        }

        if (Children.Count == 1)
        {
            Children[0].Arrange(new Rect(finalSize));
            return finalSize;
        }

        if (finalSize.Width >= Breakpoint)
        {
            var mainWidth = Math.Max(0, finalSize.Width - SideWidth - ColumnSpacing);
            Children[0].Arrange(new Rect(0, 0, mainWidth, finalSize.Height));
            Children[1].Arrange(new Rect(mainWidth + ColumnSpacing, 0, SideWidth, finalSize.Height));
            return finalSize;
        }

        var mainHeight = Children[0].DesiredSize.Height;
        Children[0].Arrange(new Rect(0, 0, finalSize.Width, mainHeight));
        Children[1].Arrange(new Rect(0, mainHeight + RowSpacing, finalSize.Width, Children[1].DesiredSize.Height));
        return new Size(finalSize.Width, mainHeight + RowSpacing + Children[1].DesiredSize.Height);
    }
}
