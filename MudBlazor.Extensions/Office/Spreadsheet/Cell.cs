namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>A stored cell: value, optional formula text and style id.</summary>
public sealed class Cell
{
    /// <summary>The current (for formulas: last calculated) value.</summary>
    public CellValue Value { get; internal set; }

    /// <summary>Formula text without the leading equals sign, or null for constants.</summary>
    public string Formula { get; internal set; }

    /// <summary>Index into <see cref="StyleTable"/>.</summary>
    public int StyleId { get; internal set; }

    internal bool HasContent => Value.Kind != CellValueKind.Empty || Formula != null;

    internal CellState State => new(Value, Formula, StyleId);
}

internal readonly record struct CellState(CellValue Value, string Formula, int StyleId)
{
    public bool IsEmpty => Value.Kind == CellValueKind.Empty && Formula == null && StyleId == 0;
}
