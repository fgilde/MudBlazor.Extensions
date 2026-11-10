namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>The outline of a shape.</summary>
/// <param name="Color">The colour in the format of <see cref="RunFormat.Color"/>, or null for no line.</param>
/// <param name="Width">The width in EMU.</param>
/// <param name="Dash">The preset dash name, for example <c>solid</c> or <c>dash</c>.</param>
public sealed record ShapeLine(string Color, long Width, string Dash = "solid");
