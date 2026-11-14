using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Presentation;

namespace MudBlazor.Extensions.Components;

/// <summary>The built-in designs of <see cref="SlideDesigns.BuiltIn"/>, each shown as a preview of the current slide in the colours, fonts and background of the design.</summary>
public partial class MudExSlideDesignGallery
{
    private (Presentation Presentation, Slide Slide, int Version) _source;
    private IReadOnlyList<(SlideDesign Design, Slide Slide)> _previews = [];

    /// <summary>The presentation the designs are previewed with.</summary>
    [Parameter] public Presentation Presentation { get; set; }

    /// <summary>The slide shown in the previews.</summary>
    [Parameter] public Slide Slide { get; set; }

    /// <summary>The image URLs of the presentation.</summary>
    [Parameter] public SlideImageUrls Images { get; set; }

    /// <summary>The design the presentation has, highlighted in the gallery, or null.</summary>
    [Parameter] public SlideDesign Current { get; set; }

    /// <summary>Raised when the user picks a design.</summary>
    [Parameter] public EventCallback<SlideDesign> DesignChosen { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        var source = (Presentation, Slide, Slide?.Version ?? 0);
        if (source == _source)
            return;
        _source = source;
        _previews = SlideDesigns.BuiltIn.Select(d => (d, Slide == null || Presentation == null ? null : SlideDesigns.Preview(Slide, d))).ToList();
    }
}
