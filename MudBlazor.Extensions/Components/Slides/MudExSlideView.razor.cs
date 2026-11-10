using System.Globalization;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Presentation;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Shows one slide as DOM that scales with its container: shapes in percent of the slide, font sizes in container query units. When
/// <see cref="Interactive"/> is set, empty title and text placeholders show a dashed outline and their prompt text.
/// </summary>
public partial class MudExSlideView
{
    private Slide _rendered;
    private int _renderedVersion;
    private bool _renderedInteractive;
    private SlideShape _renderedHiddenText;

    /// <summary>The presentation the slide belongs to.</summary>
    [Parameter] public Presentation Presentation { get; set; }

    /// <summary>The slide to show. It renders again only when its <see cref="Slide.Version"/> changes.</summary>
    [Parameter] public Slide Slide { get; set; }

    /// <summary>The image URLs of the presentation; pictures stay empty if null.</summary>
    [Parameter] public SlideImageUrls Images { get; set; }

    /// <summary>True if the shapes take pointer events, as in the editor; false for thumbnails.</summary>
    [Parameter] public bool Interactive { get; set; }

    /// <summary>A shape whose text is not shown, because it is being edited above the slide.</summary>
    [Parameter] public SlideShape HiddenText { get; set; }

    private string RootStyle =>
        $"aspect-ratio:{Presentation.SlideWidth.ToString(CultureInfo.InvariantCulture)}/{Presentation.SlideHeight.ToString(CultureInfo.InvariantCulture)};{BackgroundStyle}{Style}";

    private string BackgroundStyle
    {
        get
        {
            var background = SlideBackground.EffectiveBackground(Slide);
            return background.Kind switch
            {
                ShapeFillKind.Picture when Images?.UrlFor(background.ImagePart) is { } url =>
                    $"background-image:url(\"{CssString(url)}\");background-size:cover;background-position:center;",
                ShapeFillKind.Solid or ShapeFillKind.Other when ColorResolver.Resolve(background.Color, Slide.Layout?.Master) is { } color => $"background-color:{color};",
                _ => ""
            };
        }
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        Remember();
    }

    /// <inheritdoc />
    protected override bool ShouldRender()
    {
        if (ReferenceEquals(_rendered, Slide) && _renderedVersion == Slide?.Version && _renderedInteractive == Interactive && ReferenceEquals(_renderedHiddenText, HiddenText)) return false;
        Remember();
        return true;
    }

    private string PromptOf(SlideShape shape)
    {
        if (!Interactive || ReferenceEquals(shape, HiddenText) || shape is not AutoShape { Placeholder: { } placeholder } auto
            || auto.Text?.Paragraphs.Any(p => p.Runs.Any(r => r.Text.Length > 0)) == true)
            return null;
        if (auto.InheritsFrom is AutoShape { HasCustomPrompt: true, Text: { } prompt })
            return string.Join("\n", prompt.Paragraphs.Select(p => string.Concat(p.Runs.Select(r => r.Text))));
        return placeholder.Type switch
        {
            "title" or "ctrTitle" => TryLocalize("Click to add title"),
            "subTitle" => TryLocalize("Click to add subtitle"),
            "body" or "obj" => TryLocalize("Click to add text"),
            _ => null
        };
    }

    private static string CssString(string value) =>
        string.Concat(value.Where(c => !char.IsControl(c))).Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

    private void Remember() => (_rendered, _renderedVersion, _renderedInteractive, _renderedHiddenText) = (Slide, Slide?.Version ?? 0, Interactive, HiddenText);
}
