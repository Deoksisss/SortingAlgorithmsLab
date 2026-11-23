using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using SortingLab.App.Visualization;

namespace SortingLab.App.Views.Charts;

public sealed class ArrayBarChart : Control
{
    public static readonly StyledProperty<IEnumerable<BarDatum>?> ItemsSourceProperty =
        AvaloniaProperty.Register<ArrayBarChart, IEnumerable<BarDatum>?>(nameof(ItemsSource));

    public static readonly StyledProperty<int> RevisionProperty =
        AvaloniaProperty.Register<ArrayBarChart, int>(nameof(Revision));

    static ArrayBarChart()
    {
        AffectsRender<ArrayBarChart>(ItemsSourceProperty, RevisionProperty);
    }

    public IEnumerable<BarDatum>? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public int Revision
    {
        get => GetValue(RevisionProperty);
        set => SetValue(RevisionProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var items = ItemsSource?.ToArray() ?? Array.Empty<BarDatum>();
        if (items.Length == 0 || Bounds.Width < 60 || Bounds.Height < 60)
        {
            DrawCenteredText(context, "Нет данных для отображения", 12, ChartPalette.TextMuted);
            return;
        }

        const double left = 48;
        const double right = 14;
        const double top = 18;
        const double bottom = 28;
        var plot = new Rect(left, top, Math.Max(1, Bounds.Width - left - right), Math.Max(1, Bounds.Height - top - bottom));

        var min = 0;
        var max = 0;
        foreach (var item in items)
        {
            min = Math.Min(min, item.Value);
            max = Math.Max(max, item.Value);
        }

        if (min == max)
        {
            max = min + 1;
        }

        var range = max - min;
        double YOf(int value) => plot.Bottom - (value - min) / (double)range * plot.Height;
        var zeroY = YOf(0);

        var gridPen = new Pen(ChartPalette.Grid, 1);
        for (var tick = 0; tick <= 4; tick++)
        {
            var value = min + (int)Math.Round(range * tick / 4.0);
            var y = YOf(value);
            context.DrawLine(gridPen, new Point(plot.Left, y), new Point(plot.Right, y));
            var label = CreateText(value.ToString(CultureInfo.InvariantCulture), 10, ChartPalette.TextMuted);
            context.DrawText(label, new Point(plot.Left - label.Width - 8, y - label.Height / 2));
        }

        context.DrawLine(new Pen(ChartPalette.Axis, 1), new Point(plot.Left, zeroY), new Point(plot.Right, zeroY));

        var slot = plot.Width / items.Length;
        var barWidth = Math.Max(1.0, slot * 0.72);
        var showValues = items.Length <= 28 && slot >= 18;
        var showIndexes = items.Length <= 40 && slot >= 12;

        for (var index = 0; index < items.Length; index++)
        {
            var item = items[index];
            var centerX = plot.Left + slot * (index + 0.5);
            var valueY = YOf(item.Value);
            var barTop = Math.Min(valueY, zeroY);
            var barBottom = Math.Max(valueY, zeroY);
            var rect = new Rect(centerX - barWidth / 2, barTop, barWidth, Math.Max(1, barBottom - barTop));
            var brush = ChartPalette.ForBar(item.State);

            if (barWidth >= 4)
            {
                context.DrawRectangle(brush, null, new RoundedRect(rect, 2));
            }
            else
            {
                context.DrawRectangle(brush, null, rect);
            }

            if (showValues)
            {
                var text = CreateText(item.Value.ToString(CultureInfo.InvariantCulture), 10, ChartPalette.TextSecondary);
                var y = item.Value >= 0 ? valueY - text.Height - 3 : valueY + 3;
                context.DrawText(text, new Point(centerX - text.Width / 2, y));
            }

            if (showIndexes)
            {
                var text = CreateText(index.ToString(CultureInfo.InvariantCulture), 9, ChartPalette.TextMuted);
                context.DrawText(text, new Point(centerX - text.Width / 2, plot.Bottom + 7));
            }
        }
    }

    private void DrawCenteredText(DrawingContext context, string value, double size, IBrush brush)
    {
        var text = CreateText(value, size, brush);
        context.DrawText(
            text,
            new Point(
                Math.Max(0, (Bounds.Width - text.Width) / 2),
                Math.Max(0, (Bounds.Height - text.Height) / 2)));
    }

    private static FormattedText CreateText(string text, double size, IBrush brush, FontWeight weight = FontWeight.Normal) =>
        new(
            text,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyle.Normal, weight),
            size,
            brush);
}
