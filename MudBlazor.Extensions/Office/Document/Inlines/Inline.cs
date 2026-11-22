using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Document;

/// <summary>An element of a paragraph's content.</summary>
public abstract record Inline
{
    /// <summary>The inline's own character formatting; properties not set are inherited.</summary>
    public RunFormat Format { get; init; } = new();

    /// <summary>The hyperlink the inline belongs to, or null; all inlines of one link share the same instance.</summary>
    public Hyperlink Link { get; init; }

    /// <summary>The number of characters the inline counts as in <see cref="Paragraph.Text"/>.</summary>
    public virtual int Length => 1;

    internal XElement Source { get; init; }
}

/// <summary>A run of uniformly formatted text (<c>w:t</c>).</summary>
/// <param name="Text">The text.</param>
public sealed record TextRun(string Text) : Inline
{
    /// <inheritdoc />
    public override int Length => Text.Length;
}

/// <summary>A tab character (<c>w:tab</c>).</summary>
public sealed record Tab : Inline;

/// <summary>The kind of a <see cref="Break"/>.</summary>
public enum BreakKind
{
    /// <summary>A line break within the paragraph.</summary>
    Line,

    /// <summary>A page break.</summary>
    Page,

    /// <summary>A column break.</summary>
    Column
}

/// <summary>A line, page or column break (<c>w:br</c>).</summary>
/// <param name="Kind">The kind of break.</param>
public sealed record Break(BreakKind Kind) : Inline;

/// <summary>How an image is placed relative to the text.</summary>
public enum ImagePlacement
{
    /// <summary>In line with the text.</summary>
    Inline,

    /// <summary>Floating at the left, text wraps around it.</summary>
    Left,

    /// <summary>Floating at the right, text wraps around it.</summary>
    Right,

    /// <summary>Floating in the centre on its own lines.</summary>
    Center
}

/// <summary>A picture (<c>w:drawing</c> with a <c>pic:pic</c>).</summary>
/// <param name="MediaPart">The part name of the image, for <see cref="TextDocument.GetMedia"/>.</param>
/// <param name="WidthEmu">The displayed width in EMU.</param>
/// <param name="HeightEmu">The displayed height in EMU.</param>
public sealed record InlineImage(string MediaPart, long WidthEmu, long HeightEmu) : Inline
{
    /// <summary>The alternative text, or null.</summary>
    public string AltText { get; init; }

    /// <summary>How the image is placed relative to the text.</summary>
    public ImagePlacement Placement { get; init; }
}

/// <summary>Content the editor keeps unchanged but does not model, such as a field, a formula or a chart; it counts as one character.</summary>
/// <param name="Kind">What the inline is, for example <c>field</c>.</param>
/// <param name="DisplayText">The text to display in its place.</param>
public sealed record OpaqueInline(string Kind, string DisplayText) : Inline
{
    /// <summary>The field instruction, for example <c>PAGE</c>; null for other kinds.</summary>
    public string Instruction { get; init; }

    internal IReadOnlyList<XElement> Sources { get; init; }
}

/// <summary>A zero-length element such as a bookmark boundary or a proofing mark.</summary>
/// <param name="Kind">The element name, for example <c>bookmarkStart</c> or <c>proofErr</c>.</param>
public sealed record Marker(string Kind) : Inline
{
    /// <summary>The name, for example of a bookmark, or null.</summary>
    public string Name { get; init; }

    /// <inheritdoc />
    public override int Length => 0;
}

/// <summary>A hyperlink (<c>w:hyperlink</c>) shared by the inlines it contains.</summary>
/// <param name="Target">The external target as read, or null.</param>
/// <param name="Anchor">The bookmark the link jumps to, or null.</param>
/// <param name="Tooltip">The tooltip, or null.</param>
public sealed record Hyperlink(string Target, string Anchor, string Tooltip)
{
    /// <summary>The target if it is an <c>http</c>, <c>https</c>, <c>mailto</c> or <c>#anchor</c> link, otherwise null.</summary>
    public string SafeTarget => Target != null ? IsSafe(Target) ? Target : null : Anchor != null ? "#" + Anchor : null;

    internal XElement Source { get; init; }

    private static bool IsSafe(string target) =>
        target.StartsWith('#') ||
        Uri.TryCreate(target, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeMailto);
}
