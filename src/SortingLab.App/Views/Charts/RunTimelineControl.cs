using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using SortingLab.App.ViewModels.Common;

namespace SortingLab.App.Views.Charts;

public sealed class RunTimelineControl : Control
{
    private static readonly IBrush[] SegmentBrushes =
    {
        new SolidColorBrush(Color.Parse("#DBEAFE")),
        new SolidColorBrush(Color.Parse("#CCFBF1")),
        new SolidColorBrush(Color.Parse("#FEF3C7")),
        new SolidColorBrush(Color.Parse("#EDE9FE")),
        new SolidColorBrush(Color.Parse("#FCE7F3"))
    };

    public static readonly StyledProperty<IEnumerable<RunSegmentViewModel>?> RunsProperty =
        AvaloniaProperty.Register<RunTimelineControl, IEnumerable<RunSegmentViewModel>?>(nameof(Runs));

    public static readonly StyledProperty<int> RevisionProperty =
        AvaloniaProperty.Register<RunTimelineControl, int>(nameof(Revision));

    static RunTimelineControl()
    {
        AffectsRender<RunTimelineControl>(RunsProperty, RevisionProperty);
    }

    public IEnumerable<RunSegmentViewModel>? Runs
    {
        get => GetValue(RunsProperty);
        set => SetValue(RunsProperty, value);
    }

    public int Revision
    {
        get => GetValue(RevisionProperty);
        set => SetValue(RevisionProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var runs = Runs?.ToArray() ?? Array.Empty<RunSegmentViewModel>();
        if (runs.Length == 0)
        {
            var empty = CreateText("Естественные серии ещё не найдены", 11, ChartPalette.TextMuted);
            context.DrawText(empty, new Point(12, Math.Max(0, (Bounds.Height - empty.Height) / 2)));
            return;
        }

        const double padding = 10;
        var width = Math.Max(1, Bounds.Width - padding * 2);
        var total = runs.Sum(run => Math.Max(1, run.Count));
        var strip = new Rect(padding, 8, width, Math.Max(16, Bounds.Height - 28));
        var offset = strip.Left;

        for (var index = 0; index < runs.Length; index++)
        {
            var run = runs[index];
            var segmentWidth = Math.Max(2, width * Math.Max(1, run.Count) / total);
            var rect = new Rect(offset, strip.Top, segmentWidth - 1, strip.Height);
            var brush = SegmentBrushes[index % SegmentBrushes.Length];
            context.DrawRectangle(brush, new Pen(ChartPalette.Border, 0.8), new RoundedRect(rect, 4));

            if (segmentWidth >= 16)
            {
                var label = CreateText(run.Number.ToString(CultureInfo.InvariantCulture), 9, ChartPalette.TextSecondary, FontWeight.SemiBold);
                if (label.Width <= segmentWidth - 4)
                {
                    context.DrawText(label, new Point(rect.Center.X - label.Width / 2, rect.Center.Y - label.Height / 2));
                }
            }

            offset += segmentWidth;
        }

        var summary = CreateText(
            $"Серий: {runs.Length}   ·   записей: {runs.Sum(run => run.Count)}",
            9,
            ChartPalette.TextMuted);
        context.DrawText(summary, new Point(padding, Bounds.Height - summary.Height - 2));
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
