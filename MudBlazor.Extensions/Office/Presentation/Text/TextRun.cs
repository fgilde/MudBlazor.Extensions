using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>A run of uniformly formatted text. A line break (<c>a:br</c>) is a run whose text is a single newline.</summary>
/// <param name="Text">The text.</param>
/// <param name="Format">The run's own formatting; null means none.</param>
public sealed record TextRun(string Text, RunFormat Format = null)
{
    /// <summary>The run's own formatting; properties not set are inherited.</summary>
    public RunFormat Format { get; init; } = Format ?? new RunFormat();

    /// <summary>The field type of a field run (<c>a:fld</c>), for example <c>slidenum</c> or <c>datetime1</c>; null for a normal run.</summary>
    public string FieldType { get; init; }

    internal XElement Source { get; init; }
}
