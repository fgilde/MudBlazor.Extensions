using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Presentation;

namespace MudBlazor.Extensions.Components;

/// <summary>Shows the text of a shape. The text is rendered as text nodes, never as markup.</summary>
public partial class MudExSlideText
{
    /// <summary>The shape whose text is shown.</summary>
    [Parameter] public AutoShape Shape { get; set; }

    /// <summary>The presentation that defines the slide size.</summary>
    [Parameter] public Presentation Presentation { get; set; }

    internal static string BodyStyle(AutoShape shape, Presentation presentation)
    {
        var body = shape.EffectiveBody();
        var justify = body.Anchor switch
        {
            TextAnchor.Middle => "center",
            TextAnchor.Bottom => "flex-end",
            _ => "flex-start"
        };
        return $"justify-content:{justify};padding:{Cqw(body.InsetTop, presentation)}cqw {Cqw(body.InsetRight, presentation)}cqw " +
               $"{Cqw(body.InsetBottom, presentation)}cqw {Cqw(body.InsetLeft, presentation)}cqw;" +
               (body.Wrap == false ? "white-space:pre;" : "");
    }

    internal static string ParagraphStyle(AutoShape shape, TextParagraph paragraph) => $"text-align:{shape.EffectiveAlign(paragraph).ToString().ToLowerInvariant()};";

    internal static string RunStyle(AutoShape shape, Presentation presentation, TextRun run)
    {
        var format = shape.EffectiveFormat(run);
        var decoration = string.Join(' ', new[] { format.Underline == true ? "underline" : null, format.Strike == true ? "line-through" : null }.Where(d => d != null));
        return $"font-size:{Cqw((long)((format.SizePt ?? 18) * 12700), presentation)}cqw;color:{format.Color};" +
               (CssFont(format.Font) is { Length: > 0 } font ? $"font-family:\"{font}\",sans-serif;" : "") +
               (format.Bold == true ? "font-weight:bold;" : "") +
               (format.Italic == true ? "font-style:italic;" : "") +
               (decoration.Length > 0 ? $"text-decoration:{decoration};" : "");
    }

    internal static TextRun EmptyRun(TextParagraph paragraph) => new("", paragraph.EndFormat);

    private static string CssFont(string font) => font == null ? null : string.Concat(font.Where(c => char.IsLetterOrDigit(c) || c is ' ' or '-' or '_' or '.')).Trim();

    private static string Cqw(long? emu, Presentation presentation) => MudExSlideShapeView.Num((emu ?? 0) / (double)presentation.SlideWidth * 100);
}
