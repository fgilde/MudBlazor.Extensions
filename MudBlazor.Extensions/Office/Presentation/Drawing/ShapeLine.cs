namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>The outline of a shape.</summary>
/// <param name="Color">The colour in the format of <see cref="RunFormat.Color"/>, or null for no line.</param>
/// <param name="Width">The width in EMU.</param>
/// <param name="Dash">The preset dash name, for example <c>solid</c> or <c>dash</c>.</param>
public sealed record ShapeLine(string Color, long Width, string Dash = "solid")
{
    private const long DefaultStyleWidth = 9525;

    /// <summary>
    /// True if the outline has no fill of its own (<c>a:ln</c> without a fill element): its colour comes from the shape style, which is then in
    /// <see cref="Color"/>, or from the placeholder; a null <see cref="Color"/> does not mean no line.
    /// </summary>
    public bool ColorInherited { get; init; }

    internal static ShapeLine EffectiveLine(AutoShape shape)
    {
        var inherited = shape.InheritsFrom as AutoShape;
        if (shape.Line == null) return StyleLine(shape) ?? (inherited == null ? null : EffectiveLine(inherited));
        if (!shape.Line.ColorInherited) return shape.Line;
        return shape.Line with { Color = shape.Line.Color ?? (inherited == null ? null : EffectiveLine(inherited)?.Color), ColorInherited = false };
    }

    private static ShapeLine StyleLine(AutoShape shape)
    {
        var style = shape.Source?.Element(PptxReader.P + "style");
        if (ShapeReader.StyleColor(style, "lnRef") is not { } color) return null;
        var index = PptxReader.Long((string)style.Element(PptxReader.A + "lnRef").Attribute("idx")) ?? 0;
        var width = shape.Master?.FormatScheme?.Element(PptxReader.A + "lnStyleLst")?.Elements(PptxReader.A + "ln").ElementAtOrDefault((int)index - 1)?.Attribute("w");
        return new ShapeLine(color, PptxReader.Long((string)width) ?? DefaultStyleWidth);
    }
}
