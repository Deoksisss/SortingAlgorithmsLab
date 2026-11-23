using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using SortingLab.App.Visualization;

namespace SortingLab.App.Views.Charts;

public sealed class PerformanceChartControl : Control
{
    public static readonly StyledProperty<IEnumerable<BenchmarkPoint>?> ResultsProperty =
        AvaloniaProperty.Register<PerformanceChartControl, IEnumerable<BenchmarkPoint>?>(nameof(Results));

    public static readonly StyledProperty<int> RevisionProperty =
        AvaloniaProperty.Register<PerformanceChartControl, int>(nameof(Revision));

    static PerformanceChartControl()
    {
        AffectsRender<PerformanceChartControl>(ResultsProperty, RevisionProperty);
    }

    public IEnumerable<BenchmarkPoint>? Results
    {
        get => GetValue(ResultsProperty);
        set => SetValue(ResultsProperty, value);
    }

    public int Revision
    {
        get => GetValue(RevisionProperty);
        set => SetValue(RevisionProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var points = Results?.OrderBy(point => point.WordCount).ToArray() ?? Array.Empty<BenchmarkPoint>();
        if (points.Length == 0)
        {
            DrawCenteredText(context, "Запустите эксперименты, чтобы построить график", 12, ChartPalette.TextMuted);
            return;
        }

        const double left = 62;
        const double right = 18;
        const double top = 34;
        const double bottom = 46;
        var plot = new Rect(
            left,
            top,
            Math.Max(1, Bounds.Width - left - right),
            Math.Max(1, Bounds.Height - top - bottom));

        var maxValue = points.Max(point => Math.Max(point.QuickSortMs, point.RadixMs));
        var positive = points
            .SelectMany(point => new[] { point.QuickSortMs, point.RadixMs })
            .Where(value => value > 0)
            .ToArray();
        var minPositive = positive.Length > 0 ? positive.Min() : 0;
        var useLog = positive.Length >= 2 && maxValue / Math.Max(0.0001, minPositive) > 20;

        var maxAxis = Math.Max(1, maxValue);
        double Transform(double value) => useLog ? Math.Log10(value + 1) : value;
        var maxTransformed = Transform(maxAxis);
        var tickCount = 4;

        var gridPen = new Pen(ChartPalette.Grid, 1);
        for (var tick = 0; tick <= tickCount; tick++)
        {
            var ratio = tick / (double)tickCount;
            var transformed = maxTransformed * ratio;
            var value = useLog ? Math.Pow(10, transformed) - 1 : transformed;
            var y = plot.Bottom - ratio * plot.Height;
            context.DrawLine(gridPen, new Point(plot.Left, y), new Point(plot.Right, y));
            var label = CreateText(FormatValue(value), 10, ChartPalette.TextMuted);
            context.DrawText(label, new Point(plot.Left - label.Width - 8, y - label.Height / 2));
        }

        context.DrawLine(new Pen(ChartPalette.Axis, 1), new Point(plot.Left, plot.Top), new Point(plot.Left, plot.Bottom));
        context.DrawLine(new Pen(ChartPalette.Axis, 1), new Point(plot.Left, plot.Bottom), new Point(plot.Right, plot.Bottom));

        var xStep = points.Length > 1 ? plot.Width / (points.Length - 1) : 0;
        var xPositions = new double[points.Length];
        for (var i = 0; i < points.Length; i++)
        {
            xPositions[i] = points.Length > 1 ? plot.Left + xStep * i : plot.Left + plot.Width / 2;
            var label = CreateText(points[i].WordCount.ToString("N0", CultureInfo.InvariantCulture), 10, ChartPalette.TextMuted);
            if (points.Length > 1 || i == 0)
            {
                context.DrawText(label, new Point(xPositions[i] - label.Width / 2, plot.Bottom + 8));
            }
        }

        DrawSeries(context, points, xPositions, plot, useLog, maxTransformed, point => point.QuickSortMs, ChartPalette.Quick);
        DrawSeries(context, points, xPositions, plot, useLog, maxTransformed, point => point.RadixMs, ChartPalette.Radix);

        var legendX = plot.Right - 178;
        var legendY = 8;
        DrawLegend(context, legendX, legendY, ChartPalette.Quick, "QuickSort");
        DrawLegend(context, legendX + 88, legendY, ChartPalette.Radix, "Radix Sort");

        var axisTitle = CreateText("Время, мс", 10, ChartPalette.TextMuted);
        context.DrawText(axisTitle, new Point(12, 10));
        var xTitle = CreateText("Количество слов", 10, ChartPalette.TextMuted);
        context.DrawText(xTitle, new Point(plot.Left + (plot.Width - xTitle.Width) / 2, Bounds.Height - xTitle.Height - 2));

        if (useLog)
        {
            var note = CreateText("логарифмическая шкала", 9, ChartPalette.TextMuted);
            context.DrawText(note, new Point(plot.Right - note.Width, Bounds.Height - note.Height - 2));
        }
    }

    private static void DrawSeries(
        DrawingContext context,
        IReadOnlyList<BenchmarkPoint> points,
        IReadOnlyList<double> xPositions,
        Rect plot,
        bool useLog,
        double maxTransformed,
        Func<BenchmarkPoint, double> selector,
        IBrush brush)
    {
        double YOf(double value)
        {
            var transformed = useLog ? Math.Log10(value + 1) : value;
            return plot.Bottom - transformed / maxTransformed * plot.Height;
        }

        var pen = new Pen(brush, 2.2);
        for (var i = 1; i < points.Count; i++)
        {
            context.DrawLine(
                pen,
                new Point(xPositions[i - 1], YOf(selector(points[i - 1]))),
                new Point(xPositions[i], YOf(selector(points[i]))));
        }

        for (var i = 0; i < points.Count; i++)
        {
            var center = new Point(xPositions[i], YOf(selector(points[i])));
            context.DrawEllipse(Brushes.White, new Pen(brush, 2), center, 4.5, 4.5);
        }
    }

    private static void DrawLegend(DrawingContext context, double x, double y, IBrush brush, string text)
    {
        context.DrawLine(new Pen(brush, 2.4), new Point(x, y + 7), new Point(x + 16, y + 7));
        context.DrawEllipse(Brushes.White, new Pen(brush, 2), new Point(x + 8, y + 7), 3.5, 3.5);
        var label = CreateText(text, 10, ChartPalette.TextSecondary);
        context.DrawText(label, new Point(x + 22, y));
    }

    private static string FormatValue(double value) => value switch
    {
        >= 1000 => $"{value:0}",
        >= 100 => $"{value:0.0}",
        >= 1 => $"{value:0.00}",
        > 0 => $"{value:0.000}",
        _ => "0"
    };

    private void DrawCenteredText(DrawingContext context, string value, double size, IBrush brush)
    {
        var text = CreateText(value, size, brush);
        context.DrawText(
            text,
            new Point(
                Math.Max(0, (Bounds.Width - text.Width) / 2),
                Math.Max(0, (Bounds.Height - text.Height) / 2)));
    }

    private static FormattedText CreateText(
        string text,
        double size,
        IBrush brush,
        FontWeight weight = FontWeight.Normal) =>
        new(
            text,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyle.Normal, weight),
            size,
            brush);
}
