namespace MudBlazor.Extensions.Office.Document;

/// <summary>Orientation of a page.</summary>
public enum PageOrientation
{
    /// <summary>Taller than wide.</summary>
    Portrait,

    /// <summary>Wider than tall.</summary>
    Landscape
}

/// <summary>Page size and margins of a section (<c>w:pgSz</c>, <c>w:pgMar</c>), all in twips.</summary>
/// <param name="Width">The page width.</param>
/// <param name="Height">The page height.</param>
/// <param name="Orientation">The orientation.</param>
/// <param name="MarginTop">The top margin.</param>
/// <param name="MarginRight">The right margin.</param>
/// <param name="MarginBottom">The bottom margin.</param>
/// <param name="MarginLeft">The left margin.</param>
/// <param name="HeaderDistance">The distance of the header from the top edge.</param>
/// <param name="FooterDistance">The distance of the footer from the bottom edge.</param>
/// <param name="Gutter">The extra binding margin.</param>
public sealed record PageSetup(int Width, int Height, PageOrientation Orientation, int MarginTop, int MarginRight, int MarginBottom, int MarginLeft,
    int HeaderDistance, int FooterDistance, int Gutter)
{
    /// <summary>A4 portrait with 2.54 cm margins.</summary>
    public static PageSetup A4 { get; } = new(11906, 16838, PageOrientation.Portrait, 1440, 1440, 1440, 1440, 708, 708, 0);

    /// <summary>US Letter portrait with 1 inch margins.</summary>
    public static PageSetup Letter { get; } = new(12240, 15840, PageOrientation.Portrait, 1440, 1440, 1440, 1440, 708, 708, 0);

    /// <summary>The width between the margins.</summary>
    public int TextWidth => Width - MarginLeft - MarginRight - Gutter;
}
