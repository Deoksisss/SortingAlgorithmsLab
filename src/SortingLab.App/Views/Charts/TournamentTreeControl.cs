using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using SortingLab.Core.Models;

namespace SortingLab.App.Views.Charts;

public sealed class TournamentTreeControl : Control
{
    public static readonly StyledProperty<TournamentSnapshot?> SnapshotProperty =
        AvaloniaProperty.Register<TournamentTreeControl, TournamentSnapshot?>(nameof(Snapshot));

    static TournamentTreeControl()
    {
        AffectsRender<TournamentTreeControl>(SnapshotProperty);
    }

    public TournamentSnapshot? Snapshot
    {
        get => GetValue(SnapshotProperty);
        set => SetValue(SnapshotProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var snapshot = Snapshot;
        if (snapshot is null || snapshot.LeafCount < 1)
        {
            DrawCenteredText(context, "Дерево появится после запуска Tournament Sort", 12, ChartPalette.TextMuted);
            return;
        }

        var leafCount = snapshot.LeafCount;
        var levels = (int)Math.Log2(leafCount) + 1;
        const double padding = 18;
        var width = Math.Max(1, Bounds.Width - padding * 2);
        var height = Math.Max(1, Bounds.Height - padding * 2 - 18);
        var horizontal = width / leafCount;
        var vertical = levels > 1 ? height / (levels - 1) : height;
        var radius = Math.Clamp(Math.Min(horizontal * 0.34, vertical * 0.28), 7, 17);

        var x = new double[snapshot.Nodes.Count];
        for (var leaf = 0; leaf < leafCount; leaf++)
        {
            x[leafCount + leaf] = padding + horizontal * (leaf + 0.5);
        }

        for (var node = leafCount - 1; node >= 1; node--)
        {
            x[node] = (x[node * 2] + x[node * 2 + 1]) / 2;
        }

        var y = new double[snapshot.Nodes.Count];
        for (var node = 1; node < snapshot.Nodes.Count; node++)
        {
            var level = (int)Math.Floor(Math.Log2(node));
            y[node] = padding + level * vertical;
        }

        var pathSet = new HashSet<int>(snapshot.ActivePath);
        var linkPen = new Pen(ChartPalette.Grid, 1.4);
        var activePen = new Pen(ChartPalette.Active, 2.2);

        for (var node = 1; node < snapshot.Nodes.Count; node++)
        {
            var left = node * 2;
            var right = left + 1;
            if (right >= snapshot.Nodes.Count)
            {
                continue;
            }

            var isActive = pathSet.Contains(node) && (pathSet.Contains(left) || pathSet.Contains(right));
            context.DrawLine(
                isActive ? activePen : linkPen,
                new Point(x[node], y[node]),
                new Point(x[left], y[left]));
            context.DrawLine(
                isActive ? activePen : linkPen,
                new Point(x[node], y[node]),
                new Point(x[right], y[right]));
        }

        for (var node = 1; node < snapshot.Nodes.Count; node++)
        {
            var item = snapshot.Nodes[node];
            var center = new Point(x[node], y[node]);
            var isPath = pathSet.Contains(node);
            var isRoot = node == 1;
            var fill = item.IsActive
                ? isPath || isRoot ? ChartPalette.AccentSoft : Brushes.White
                : new SolidColorBrush(Color.Parse("#F1F5F9"));
            var border = isPath || isRoot
                ? new Pen(ChartPalette.Active, isRoot ? 2.4 : 1.8)
                : new Pen(ChartPalette.Border, 1.2);

            context.DrawEllipse(fill, border, center, radius, radius);

            if (radius >= 9)
            {
                var label = item.WinnerIndex?.ToString(CultureInfo.InvariantCulture) ?? "—";
                var text = CreateText(label, Math.Max(8, radius * 0.85), ChartPalette.TextPrimary, FontWeight.SemiBold);
                context.DrawText(text, new Point(center.X - text.Width / 2, center.Y - text.Height / 2));
            }
        }

        if (leafCount <= 32)
        {
            for (var leaf = 0; leaf < leafCount; leaf++)
            {
                var node = snapshot.Nodes[leafCount + leaf];
                if (node.Value is null)
                {
                    continue;
                }

                var text = CreateText(node.Value.Value.ToString(CultureInfo.InvariantCulture), 9, ChartPalette.TextMuted);
                context.DrawText(
                    text,
                    new Point(x[leafCount + leaf] - text.Width / 2, y[leafCount + leaf] + radius + 4));
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
