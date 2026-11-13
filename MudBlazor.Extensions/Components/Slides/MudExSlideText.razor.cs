using System.Globalization;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Presentation;

namespace MudBlazor.Extensions.Components;

/// <summary>Shows the text of a shape. The text is rendered as text nodes, never as markup.</summary>
public partial class MudExSlideText
{
    private const double SingleLineHeight = 1.2;

    private static readonly HashSet<string> SerifFonts = new(StringComparer.OrdinalIgnoreCase)
    {
        "Georgia", "Cambria", "Times New Roman", "Times", "Garamond", "Book Antiqua", "Palatino Linotype", "Palatino", "Bookman Old Style", "Century Schoolbook",
        "Constantia", "Baskerville Old Face", "Bodoni MT", "Didot", "Perpetua", "Sylfaen"
    };

    private static readonly HashSet<string> MonospaceFonts = new(StringComparer.OrdinalIgnoreCase)
    {
        "Consolas", "Courier New", "Courier", "Lucida Console", "Lucida Sans Typewriter", "Cascadia Code", "Cascadia Mono", "Menlo", "Monaco", "Source Code Pro",
        "Fira Code", "JetBrains Mono", "Andale Mono"
    };

    /// <summary>The shape whose text is shown.</summary>
    [Parameter] public AutoShape Shape { get; set; }

    /// <summary>The presentation that defines the slide size.</summary>
    [Parameter] public Presentation Presentation { get; set; }

    /// <summary>The number shown by slide number fields.</summary>
    [Parameter] public int SlideNumber { get; set; }

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

    internal static string ParagraphStyle(AutoShape shape, TextParagraph paragraph, Presentation presentation)
    {
        var p = shape.EffectiveParagraph(paragraph);
        var lineHeight = SingleLineHeight * Math.Max(0, (p.LineSpacingPercent ?? 100) - (shape.Text?.LineSpaceReductionPercent ?? 0)) / 100;
        return $"text-align:{(p.Align ?? TextAlign.Left).ToString().ToLowerInvariant()};padding-left:{Cqw(p.MarginLeft, presentation)}cqw;" +
               $"text-indent:{Cqw(p.Indent, presentation)}cqw;--mud-ex-slide-hang:{Cqw(Math.Max(0, -(p.Indent ?? 0)), presentation)}cqw;" +
               $"line-height:{MudExSlideShapeView.Num(lineHeight)};" +
               $"margin-top:{Cqw(Emu(p.SpaceBeforePt), presentation)}cqw;margin-bottom:{Cqw(Emu(p.SpaceAfterPt), presentation)}cqw;";
    }

    internal static string RunStyle(AutoShape shape, Presentation presentation, TextRun run)
    {
        var format = shape.EffectiveFormat(run);
        var decoration = string.Join(' ', new[] { format.Underline == true ? "underline" : null, format.Strike == true ? "line-through" : null }.Where(d => d != null));
        var scale = (shape.Text?.FontScalePercent ?? 100) / 100;
        return $"font-size:{Cqw(Emu((format.SizePt ?? 18) * scale), presentation)}cqw;color:{format.Color};" +
               FontStyle(format.Font) +
               (format.Bold == true ? "font-weight:bold;" : "") +
               (format.Italic == true ? "font-style:italic;" : "") +
               (decoration.Length > 0 ? $"text-decoration:{decoration};" : "");
    }

    internal static string GenericFamily(string font) =>
        font == null ? "sans-serif" : SerifFonts.Contains(font) ? "serif" : MonospaceFonts.Contains(font) ? "monospace" : "sans-serif";

    internal static string FontStyle(string font) => CssFont(font) is { Length: > 0 } css ? $"font-family:\"{css}\",{GenericFamily(css)};" : "";

    internal static TextRun EmptyRun(TextParagraph paragraph) => new("", paragraph.EndFormat);

    private string TextOf(TextRun run) => run.FieldType switch
    {
        "slidenum" => SlideNumber.ToString(CultureInfo.CurrentCulture),
        { } type when type.StartsWith("datetime", StringComparison.Ordinal) => DateTime.Today.ToString("d", CultureInfo.CurrentCulture),
        _ => run.Text
    };

    private static long Emu(double? points) => (long)((points ?? 0) * 12700);

    private static string CssFont(string font) => font == null ? null : string.Concat(font.Where(c => char.IsLetterOrDigit(c) || c is ' ' or '-' or '_' or '.')).Trim();

    private static string Cqw(long? emu, Presentation presentation) => MudExSlideShapeView.Num((emu ?? 0) / (double)presentation.SlideWidth * 100);
}
