namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>A design that sets the theme colours, the theme fonts and the background of slide masters.</summary>
/// <param name="Name">The design name.</param>
/// <param name="Colors">The twelve theme colours as <c>#RRGGBB</c> in the order of <see cref="PresentationTheme.Colors"/>.</param>
/// <param name="MajorFont">The heading font.</param>
/// <param name="MinorFont">The body font.</param>
/// <param name="Background">The master background: a solid colour, a gradient or no fill.</param>
public sealed record SlideDesign(string Name, IReadOnlyList<string> Colors, string MajorFont, string MinorFont, SlideBackground Background);
