namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>Character formatting of a text run; null properties are inherited.</summary>
/// <param name="Bold">Bold.</param>
/// <param name="Italic">Italic.</param>
/// <param name="Underline">Underlined.</param>
/// <param name="Strike">Struck through.</param>
/// <param name="SizePt">Font size in points.</param>
/// <param name="Color">
/// Text colour: <c>#RRGGBB</c> or a theme colour like <c>scheme:accent1</c>, optionally followed by space separated DrawingML modifiers
/// such as <c>lumMod=75000</c>.
/// </param>
/// <param name="Font">Latin typeface name; <c>+mj-lt</c> and <c>+mn-lt</c> stand for the theme heading and body fonts.</param>
public sealed record RunFormat(bool? Bold = null, bool? Italic = null, bool? Underline = null, bool? Strike = null, double? SizePt = null, string Color = null,
    string Font = null);
