using System.Globalization;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Presentation;

namespace MudBlazor.Extensions.Components;

/// <summary>Shows one slide as DOM that scales with its container: shapes in percent of the slide, font sizes in container query units.</summary>
public partial class MudExSlideView
{
    private Slide _rendered;
    private int _renderedVersion;
    private bool _renderedInteractive;

    /// <summary>The presentation the slide belongs to.</summary>
    [Parameter] public Presentation Presentation { get; set; }

    /// <summary>The slide to show. It renders again only when its <see cref="Slide.Version"/> changes.</summary>
    [Parameter] public Slide Slide { get; set; }

    /// <summary>The image URLs of the presentation; pictures stay empty if null.</summary>
    [Parameter] public SlideImageUrls Images { get; set; }

    /// <summary>True if the shapes take pointer events, as in the editor; false for thumbnails.</summary>
    [Parameter] public bool Interactive { get; set; }

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
        if (ReferenceEquals(_rendered, Slide) && _renderedVersion == Slide?.Version && _renderedInteractive == Interactive) return false;
        Remember();
        return true;
    }

    private static string CssString(string value) =>
        string.Concat(value.Where(c => !char.IsControl(c))).Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

    private void Remember() => (_rendered, _renderedVersion, _renderedInteractive) = (Slide, Slide?.Version ?? 0, Interactive);
}
