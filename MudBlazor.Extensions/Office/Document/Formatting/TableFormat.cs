namespace MudBlazor.Extensions.Office.Document;

/// <summary>Horizontal alignment of a table between the page margins.</summary>
public enum TableAlignment
{
    /// <summary>Left aligned.</summary>
    Left,

    /// <summary>Centred.</summary>
    Center,

    /// <summary>Right aligned.</summary>
    Right
}

/// <summary>Which parts of the table style apply (<c>w:tblLook</c>).</summary>
[Flags]
public enum TableLook
{
    /// <summary>No conditional formatting.</summary>
    None = 0,

    /// <summary>The first row is formatted as header.</summary>
    FirstRow = 1,

    /// <summary>The last row is formatted as total row.</summary>
    LastRow = 2,

    /// <summary>The first column is formatted specially.</summary>
    FirstColumn = 4,

    /// <summary>The last column is formatted specially.</summary>
    LastColumn = 8,

    /// <summary>Rows are banded.</summary>
    BandedRows = 16,

    /// <summary>Columns are banded.</summary>
    BandedColumns = 32
}

/// <summary>A border line.</summary>
/// <param name="Style">The line style as the <c>w:val</c> value, for example <c>single</c> or <c>nil</c>.</param>
/// <param name="WidthPt">The line width in points.</param>
/// <param name="Color">The colour (<c>#RRGGBB</c>, <c>auto</c> or <c>theme:&lt;name&gt;</c>).</param>
public sealed record Border(string Style, double WidthPt, string Color);

/// <summary>The borders of a table or a cell; a null side is inherited.</summary>
/// <param name="Top">The top border.</param>
/// <param name="Bottom">The bottom border.</param>
/// <param name="Left">The left border.</param>
/// <param name="Right">The right border.</param>
/// <param name="InsideH">The horizontal borders between rows.</param>
/// <param name="InsideV">The vertical borders between columns.</param>
public sealed record TableBorders(Border Top, Border Bottom, Border Left, Border Right, Border InsideH, Border InsideV);

/// <summary>Table formatting (<c>w:tblPr</c>). Null properties are inherited.</summary>
public sealed record TableFormat
{
    /// <summary>The table style id; null for none.</summary>
    public string StyleId { get; init; }

    /// <summary>The preferred table width in twips; null for automatic.</summary>
    public int? Width { get; init; }

    /// <summary>The horizontal alignment; null inherits it.</summary>
    public TableAlignment? Alignment { get; init; }

    /// <summary>The table borders; null inherits them.</summary>
    public TableBorders Borders { get; init; }

    /// <summary>Which parts of the table style apply.</summary>
    public TableLook Look { get; init; }
}
