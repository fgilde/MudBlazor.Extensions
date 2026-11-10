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

    private string BodyStyle
    {
        get
        {
            var body = Shape.EffectiveBody();
            var justify = body.Anchor switch
            {
                TextAnchor.Middle => "center",
                TextAnchor.Bottom => "flex-end",
                _ => "flex-start"
            };
            return $"justify-content:{justify};padding:{Cqw(body.InsetTop)}cqw {Cqw(body.InsetRight)}cqw {Cqw(body.InsetBottom)}cqw {Cqw(body.InsetLeft)}cqw;" +
                   (body.Wrap == false ? "white-space:pre;" : "");
        }
    }

    private string ParagraphStyle(TextParagraph paragraph) => $"text-align:{Shape.EffectiveAlign(paragraph).ToString().ToLowerInvariant()};";

    private static TextRun EmptyRun(TextParagraph paragraph) => new("", paragraph.EndFormat);

    private string RunStyle(TextRun run)
    {
        var format = Shape.EffectiveFormat(run);
        var decoration = string.Join(' ', new[] { format.Underline == true ? "underline" : null, format.Strike == true ? "line-through" : null }.Where(d => d != null));
        return $"font-size:{Cqw((long)((format.SizePt ?? 18) * 12700))}cqw;color:{format.Color};" +
               (format.Font != null ? $"font-family:'{format.Font.Replace("'", "")}',sans-serif;" : "") +
               (format.Bold == true ? "font-weight:bold;" : "") +
               (format.Italic == true ? "font-style:italic;" : "") +
               (decoration.Length > 0 ? $"text-decoration:{decoration};" : "");
    }

    private string Cqw(long? emu) => MudExSlideShapeView.Num((emu ?? 0) / (double)Presentation.SlideWidth * 100);
}
