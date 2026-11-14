namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>
/// Sets the slide size. With <c>scaleContent</c> every shape of the slides, layouts and masters that has its own position is scaled by the same factor in both
/// directions, so that the content fits the new size, and centred on it; table columns and rows scale with their table. Font sizes stay.
/// </summary>
/// <param name="width">The slide width in EMU, from 914400 to 51206400.</param>
/// <param name="height">The slide height in EMU, from 914400 to 51206400.</param>
/// <param name="scaleContent">True to scale the content to the new size, false to change only the size.</param>
public sealed class SetSlideSizeCommand(long width, long height, bool scaleContent) : IDocumentCommand<Presentation>
{
    private const long MinSize = 914400;
    private const long MaxSize = 51206400;
    private readonly List<(SlideShape Shape, ShapeTransform Transform, IReadOnlyList<long> Columns, IReadOnlyList<TableRow> Rows)> _before = [];
    private (long Width, long Height)? _size;

    /// <inheritdoc />
    public string Description => "Slide size";

    /// <inheritdoc />
    public void Apply(Presentation document)
    {
        if (width is < MinSize or > MaxSize) throw new ArgumentOutOfRangeException(nameof(width), width, $"The width must be from {MinSize} to {MaxSize} EMU.");
        if (height is < MinSize or > MaxSize) throw new ArgumentOutOfRangeException(nameof(height), height, $"The height must be from {MinSize} to {MaxSize} EMU.");
        _before.Clear();
        _size = null;
        if (width == document.SlideWidth && height == document.SlideHeight) return;
        _size = (document.SlideWidth, document.SlideHeight);
        if (scaleContent) Scale(document);
        (document.SlideWidth, document.SlideHeight) = (width, height);
        Notify(document);
    }

    /// <inheritdoc />
    public void Revert(Presentation document)
    {
        if (_size is not { } size) return;
        foreach (var (shape, transform, columns, rows) in _before)
        {
            shape.Transform = transform;
            if (shape is TableShape table) (table.ColumnWidths, table.Rows) = (columns, rows);
        }

        (document.SlideWidth, document.SlideHeight) = size;
        Notify(document);
    }

    private void Scale(Presentation document)
    {
        var factor = Math.Min((double)width / document.SlideWidth, (double)height / document.SlideHeight);
        var (dx, dy) = ((width - document.SlideWidth * factor) / 2, (height - document.SlideHeight * factor) / 2);
        var shapes = document.SlideList.SelectMany(s => s.ShapeList).Concat(document.LayoutList.SelectMany(l => l.ShapeList)).Concat(document.MasterList.SelectMany(m => m.ShapeList));
        foreach (var shape in shapes)
        {
            if (shape.Transform is not { } t) continue;
            var table = shape as TableShape;
            _before.Add((shape, t, table?.ColumnWidths, table?.Rows));
            var scaled = t with { X = Round(t.X * factor + dx), Y = Round(t.Y * factor + dy), Width = Round(t.Width * factor), Height = Round(t.Height * factor) };
            if (table != null)
            {
                table.ColumnWidths = table.ColumnWidths.Select(w => Round(w * factor)).ToList();
                table.Rows = table.Rows.Select(r => r with { Height = Round(r.Height * factor) }).ToList();
                scaled = scaled with { Width = table.ColumnWidths.Sum(), Height = table.Rows.Sum(r => r.Height) };
            }

            shape.Transform = scaled;
        }
    }

    private static long Round(double value) => (long)Math.Round(value);

    private static void Notify(Presentation document)
    {
        document.Notify(null, PresentationChangeKind.Content);
        foreach (var slide in document.SlideList) document.Notify(slide, PresentationChangeKind.Content);
    }
}
