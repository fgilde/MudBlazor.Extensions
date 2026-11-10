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
        $"aspect-ratio:{Presentation.SlideWidth.ToString(CultureInfo.InvariantCulture)}/{Presentation.SlideHeight.ToString(CultureInfo.InvariantCulture)};{Style}";

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

    private void Remember() => (_rendered, _renderedVersion, _renderedInteractive) = (Slide, Slide?.Version ?? 0, Interactive);
}
