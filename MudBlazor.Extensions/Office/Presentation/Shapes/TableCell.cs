using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>A row of a <see cref="TableShape"/>.</summary>
public sealed record TableRow
{
    /// <summary>The row height in EMU.</summary>
    public long Height { get; init; }

    /// <summary>The cells, one per column.</summary>
    public IReadOnlyList<TableCell> Cells { get; init; } = [];

    internal XElement Source { get; init; }
}

/// <summary>A cell of a <see cref="TableShape"/>. Margins and anchor hold the cell's own values; null takes the default.</summary>
public sealed record TableCell
{
    /// <summary>The text, or null if the cell has no text body.</summary>
    public TextBody Text { get; init; } = new() { Paragraphs = [new TextParagraph()] };

    /// <summary>The fill, or null to take it from the table style.</summary>
    public ShapeFill Fill { get; init; }

    /// <summary>The number of columns a merged range spans from this cell, at least 1.</summary>
    public int ColumnSpan { get; init; } = 1;

    /// <summary>The number of rows a merged range spans from this cell, at least 1.</summary>
    public int RowSpan { get; init; } = 1;

    /// <summary>True if the cell lies under a merged range that starts in another cell.</summary>
    public bool IsMergedAway { get; init; }

    /// <summary>The left margin in EMU, or null for the default of 91440.</summary>
    public long? MarginLeft { get; init; }

    /// <summary>The top margin in EMU, or null for the default of 45720.</summary>
    public long? MarginTop { get; init; }

    /// <summary>The right margin in EMU, or null for the default of 91440.</summary>
    public long? MarginRight { get; init; }

    /// <summary>The bottom margin in EMU, or null for the default of 45720.</summary>
    public long? MarginBottom { get; init; }

    /// <summary>The vertical anchoring of the text, or null for the default (top).</summary>
    public TextAnchor? Anchor { get; init; }

    internal XElement Source { get; init; }

    internal bool HasText => Text?.Paragraphs.Any(p => p.Runs.Any(r => r.Text.Length > 0)) == true;
}
