using System.Text;
using MudBlazor.Extensions.Office.Notebook;

namespace MudBlazor.Extensions.Components;

internal static class NotebookStyles
{
    internal const double PageMargin = 400;
    internal const double DefaultSizePt = 11;
    internal const double LineHeight = 1.4;
    internal const double IndentStep = 24;
    internal const double CellPadding = 4;
    private const double MarkerWidth = 24;
    private const double AverageCharWidth = 0.5;

    private static readonly string[] Bullets = ["•", "◦", "▪"];

    internal static string Num(double value) => MudExSlideShapeView.Num(double.IsFinite(value) ? value : 0);

    internal static string BoxStyle(NoteElement element) =>
        $"left:{Num(element.X)}px;top:{Num(element.Y)}px;width:{Num(element.Width)}px;" +
        (element.Height > 0 || element is not (TextElement or TableElement) ? $"height:{Num(element.Height)}px;" : "");

    internal static string ParagraphStyle(NoteParagraph paragraph) =>
        $"text-align:{paragraph.Align.ToString().ToLowerInvariant()};" + (Indent(paragraph) > 0 ? $"padding-left:{Num(Indent(paragraph) * IndentStep)}px;" : "");

    internal static string RunStyle(NoteParagraph paragraph, NoteRun run)
    {
        var format = run.Format;
        var decoration = string.Join(' ', new[] { format.Underline ? "underline" : null, format.Strike ? "line-through" : null }.Where(d => d != null));
        return $"font-size:{Num(Px(SizePt(paragraph, run)))}px;" +
               (NoteValues.IsColor(format.Color) ? $"color:{format.Color};" : "") +
               (NoteValues.IsColor(format.Highlight) ? $"background-color:{format.Highlight};" : "") +
               (NoteValues.IsFont(format.Font) ? MudExSlideText.FontStyle(format.Font) : "") +
               (format.Bold ? "font-weight:bold;" : "") +
               (format.Italic ? "font-style:italic;" : "") +
               (decoration.Length > 0 ? $"text-decoration:{decoration};" : "");
    }

    internal static string EmptyRunStyle(NoteParagraph paragraph) => $"font-size:{Num(Px(HeadingSizePt(paragraph.Heading)))}px;";

    internal static string Bullet(NoteParagraph paragraph) => Bullets[Indent(paragraph) % Bullets.Length];

    internal static string InkPath(InkElement ink)
    {
        var points = ink.Points;
        if (points.Count == 0) return null;
        var path = new StringBuilder($"M{Num(points[0].X)} {Num(points[0].Y)}");
        if (points.Count == 1) path.Append($"L{Num(points[0].X)} {Num(points[0].Y)}");
        for (var i = 1; i < points.Count; i++)
            path.Append($"L{Num(points[i].X)} {Num(points[i].Y)}");
        return path.ToString();
    }

    internal static string ShapePath(ShapeElement shape)
    {
        var (w, h, i) = (shape.Width, shape.Height, Math.Min(Math.Max(0, shape.StrokeWidth) / 2, Math.Min(shape.Width, shape.Height) / 2));
        switch (shape.Kind)
        {
            case NoteShapeKind.Line or NoteShapeKind.Arrow:
                var (x1, y1, x2, y2) = LineOf(shape);
                return $"M{Num(x1)} {Num(y1)}L{Num(x2)} {Num(y2)}";
            case NoteShapeKind.Ellipse:
                var (rx, ry) = (w / 2 - i, h / 2 - i);
                return $"M{Num(i)} {Num(h / 2)}A{Num(rx)} {Num(ry)} 0 1 0 {Num(w - i)} {Num(h / 2)}A{Num(rx)} {Num(ry)} 0 1 0 {Num(i)} {Num(h / 2)}Z";
            case NoteShapeKind.Triangle:
                return $"M{Num(w / 2)} {Num(i)}L{Num(w - i)} {Num(h - i)}L{Num(i)} {Num(h - i)}Z";
            case NoteShapeKind.Diamond:
                return $"M{Num(w / 2)} {Num(i)}L{Num(w - i)} {Num(h / 2)}L{Num(w / 2)} {Num(h - i)}L{Num(i)} {Num(h / 2)}Z";
            default:
                return $"M{Num(i)} {Num(i)}H{Num(w - i)}V{Num(h - i)}H{Num(i)}Z";
        }
    }

    internal static string ArrowHead(ShapeElement shape)
    {
        var (x1, y1, x2, y2) = LineOf(shape);
        var length = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
        if (shape.Kind != NoteShapeKind.Arrow || length == 0) return null;
        var size = Math.Min(length, Math.Max(8, Math.Max(0, shape.StrokeWidth) * 3));
        var (dx, dy) = ((x2 - x1) / length, (y2 - y1) / length);
        var (bx, by) = (x2 - dx * size, y2 - dy * size);
        var (px, py) = (-dy * size / 2, dx * size / 2);
        return $"M{Num(x2)} {Num(y2)}L{Num(bx + px)} {Num(by + py)}L{Num(bx - px)} {Num(by - py)}Z";
    }

    internal static double EstimateHeight(NoteElement element) => element switch
    {
        TextElement text => EstimateHeight(text.Paragraphs, element.Width),
        TableElement table => table.Rows.Sum(row =>
            row.Select((cell, c) => EstimateHeight(cell.Paragraphs, (c < table.ColumnWidths.Count ? table.ColumnWidths[c] : 0) - 2 * CellPadding)).DefaultIfEmpty(0).Max()
            + 2 * CellPadding + 1),
        _ => 0
    };

    private static double EstimateHeight(IReadOnlyList<NoteParagraph> paragraphs, double width)
    {
        var height = 0d;
        foreach (var paragraph in paragraphs)
        {
            var size = Px(paragraph.Runs.Select(r => SizePt(paragraph, r)).DefaultIfEmpty(HeadingSizePt(paragraph.Heading)).Max());
            var text = string.Concat(paragraph.Runs.Select(r => r.Text));
            var available = width - Indent(paragraph) * IndentStep - (paragraph.List == NoteListKind.None ? 0 : MarkerWidth);
            var wrapped = available > 0 ? Math.Ceiling(text.Length * AverageCharWidth * size / available) : 0;
            height += Math.Max(1 + text.Count(c => c == '\n'), wrapped) * size * LineHeight;
        }

        return height;
    }

    private static (double X1, double Y1, double X2, double Y2) LineOf(ShapeElement shape) =>
        (shape.FlipX ? shape.Width : 0, shape.FlipY ? shape.Height : 0, shape.FlipX ? 0 : shape.Width, shape.FlipY ? 0 : shape.Height);

    private static int Indent(NoteParagraph paragraph) => Math.Clamp(paragraph.Indent, 0, NoteParagraph.MaxIndent);

    private static double SizePt(NoteParagraph paragraph, NoteRun run) =>
        run.Format.SizePt is { } size && double.IsFinite(size) && size > 0 && size <= 1000 ? size : HeadingSizePt(paragraph.Heading);

    private static double HeadingSizePt(NoteHeading heading) => heading switch
    {
        NoteHeading.H1 => 20,
        NoteHeading.H2 => 16,
        NoteHeading.H3 => 14,
        _ => DefaultSizePt
    };

    private static double Px(double points) => points * 4 / 3;
}
