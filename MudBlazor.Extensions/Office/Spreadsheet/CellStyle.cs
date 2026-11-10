namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>Horizontal cell alignment.</summary>
public enum CellHorizontalAlignment
{
    /// <summary>Alignment depends on the value type.</summary>
    General,

    /// <summary>Left aligned.</summary>
    Left,

    /// <summary>Centered.</summary>
    Center,

    /// <summary>Right aligned.</summary>
    Right,

    /// <summary>Justified.</summary>
    Justify
}

/// <summary>Vertical cell alignment.</summary>
public enum CellVerticalAlignment
{
    /// <summary>Aligned to the top.</summary>
    Top,

    /// <summary>Centered.</summary>
    Center,

    /// <summary>Aligned to the bottom.</summary>
    Bottom
}

/// <summary>Border line style.</summary>
public enum CellBorderStyle
{
    /// <summary>No line.</summary>
    None,

    /// <summary>Thin line.</summary>
    Thin,

    /// <summary>Medium line.</summary>
    Medium,

    /// <summary>Thick line.</summary>
    Thick,

    /// <summary>Dashed line.</summary>
    Dashed,

    /// <summary>Dotted line.</summary>
    Dotted,

    /// <summary>Double line.</summary>
    Double
}

/// <summary>Font settings of a cell style.</summary>
public sealed record CellFont
{
    /// <summary>Font family name.</summary>
    public string Name { get; init; } = "Calibri";

    /// <summary>Font size in points.</summary>
    public double Size { get; init; } = 11;

    /// <summary>Bold.</summary>
    public bool Bold { get; init; }

    /// <summary>Italic.</summary>
    public bool Italic { get; init; }

    /// <summary>Underlined.</summary>
    public bool Underline { get; init; }

    /// <summary>Struck through.</summary>
    public bool Strikethrough { get; init; }

    /// <summary>Text color as #RRGGBB, or null for the theme default.</summary>
    public string Color { get; init; }
}

/// <summary>Background fill of a cell style.</summary>
public sealed record CellFill
{
    /// <summary>Fill color as #RRGGBB, or null for no fill.</summary>
    public string Color { get; init; }
}

/// <summary>One side of a cell border.</summary>
public sealed record CellBorderSide
{
    /// <summary>Line style.</summary>
    public CellBorderStyle Style { get; init; }

    /// <summary>Line color as #RRGGBB, or null for the theme default.</summary>
    public string Color { get; init; }
}

/// <summary>Borders of a cell style.</summary>
public sealed record CellBorder
{
    /// <summary>Left border.</summary>
    public CellBorderSide Left { get; init; } = new();

    /// <summary>Right border.</summary>
    public CellBorderSide Right { get; init; } = new();

    /// <summary>Top border.</summary>
    public CellBorderSide Top { get; init; } = new();

    /// <summary>Bottom border.</summary>
    public CellBorderSide Bottom { get; init; } = new();
}

/// <summary>Alignment settings of a cell style.</summary>
public sealed record CellAlignment
{
    /// <summary>Horizontal alignment.</summary>
    public CellHorizontalAlignment Horizontal { get; init; }

    /// <summary>Vertical alignment.</summary>
    public CellVerticalAlignment Vertical { get; init; } = CellVerticalAlignment.Bottom;

    /// <summary>Wrap text inside the cell.</summary>
    public bool WrapText { get; init; }

    /// <summary>Indent level.</summary>
    public int Indent { get; init; }
}

/// <summary>Complete immutable style of a cell. Equal styles share one id in the <see cref="StyleTable"/>.</summary>
public sealed record CellStyle
{
    /// <summary>The default style, always id 0.</summary>
    public static CellStyle Default { get; } = new();

    /// <summary>Font settings.</summary>
    public CellFont Font { get; init; } = new();

    /// <summary>Background fill.</summary>
    public CellFill Fill { get; init; } = new();

    /// <summary>Borders.</summary>
    public CellBorder Border { get; init; } = new();

    /// <summary>Alignment.</summary>
    public CellAlignment Alignment { get; init; } = new();

    /// <summary>Excel number format code, <c>General</c> by default.</summary>
    public string NumberFormat { get; init; } = "General";
}

/// <summary>
/// A differential format that conditional formatting lays over the cell style. Null parts leave the cell style unchanged; of the font only
/// bold, italic, underline, strikethrough and color apply.
/// </summary>
/// <param name="Font">Font changes, or null.</param>
/// <param name="Fill">Fill, or null.</param>
/// <param name="Border">Borders, or null.</param>
/// <param name="NumberFormat">Number format code, or null.</param>
public sealed record DifferentialStyle(CellFont Font = null, CellFill Fill = null, CellBorder Border = null, string NumberFormat = null);

/// <summary>Deduplicating table of cell styles; cells store only the style id.</summary>
public sealed class StyleTable
{
    private readonly List<CellStyle> _styles = [CellStyle.Default];
    private readonly Dictionary<CellStyle, int> _ids = new() { [CellStyle.Default] = 0 };
    private readonly List<DifferentialStyle> _differential = [];

    /// <summary>The differential formats (<c>dxfs</c>) of the source file by index. Formats of new rules are added to the file when it is saved.</summary>
    public IReadOnlyList<DifferentialStyle> DifferentialStyles => _differential;

    /// <summary>Number of distinct styles including the default.</summary>
    public int Count => _styles.Count;

    /// <summary>Gets the style for an id.</summary>
    /// <param name="id">The style id.</param>
    public CellStyle this[int id] => _styles[id];

    /// <summary>Returns the id of an equal style, adding the style if it is new.</summary>
    /// <param name="style">The style.</param>
    /// <returns>The style id.</returns>
    public int GetOrAdd(CellStyle style)
    {
        if (_ids.TryGetValue(style, out var id)) return id;
        id = _styles.Count;
        _styles.Add(style);
        _ids[style] = id;
        return id;
    }

    internal void AddDifferential(DifferentialStyle style) => _differential.Add(style);
}
