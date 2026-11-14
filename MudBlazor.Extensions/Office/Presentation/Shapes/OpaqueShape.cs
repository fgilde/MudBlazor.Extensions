namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>Content the editor does not model, such as a chart or SmartArt. It renders as a named frame and can only be moved and resized.</summary>
public sealed class OpaqueShape : SlideShape
{
    internal OpaqueShape()
    {
    }

    /// <summary>The kind of content: <c>chart</c>, <c>table</c>, <c>smartArt</c>, <c>media</c>, <c>ole</c> or <c>other</c>.</summary>
    public string Kind { get; internal set; } = "other";

    /// <summary>The chart read from its cached values when <see cref="Kind"/> is <c>chart</c> and the chart can be drawn; otherwise null.</summary>
    public ChartSnapshot Chart { get; internal set; }
}
