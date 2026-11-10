using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>A run of uniformly formatted text. A line break (<c>a:br</c>) is a run whose text is a single newline.</summary>
/// <param name="Text">The text.</param>
/// <param name="Format">The run's own formatting; null means none.</param>
public sealed record TextRun(string Text, RunFormat Format = null)
{
    /// <summary>The run's own formatting; properties not set are inherited.</summary>
    public RunFormat Format { get; init; } = Format ?? new RunFormat();

    internal XElement Source { get; init; }
}
