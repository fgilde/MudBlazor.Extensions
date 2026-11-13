using System.Xml;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Presentation;

namespace MudBlazor.Extensions.Components;

/// <summary>The built-in designs of <see cref="SlideDesigns.BuiltIn"/>, each shown as a preview of the current slide in the colours, fonts and background of the design.</summary>
public partial class MudExSlideDesignGallery
{
    private (Presentation Presentation, Slide Slide, int Version) _source;
    private IReadOnlyList<(SlideDesign Design, Presentation Presentation, Slide Slide)> _previews = [];

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
        _previews = Previews();
    }

    private IReadOnlyList<(SlideDesign, Presentation, Slide)> Previews()
    {
        var index = Slide == null || Presentation == null ? -1 : Presentation.SlideList.IndexOf(Slide);
        if (index < 0)
            return SlideDesigns.BuiltIn.Select(d => (d, (Presentation)null, (Slide)null)).ToList();
        try
        {
            using var buffer = new MemoryStream();
            Presentation.Save(buffer);
            var bytes = buffer.ToArray();
            return SlideDesigns.BuiltIn.Select(design =>
            {
                var copy = Office.Presentation.Presentation.Load(new MemoryStream(bytes));
                new ApplyDesignCommand(design).Apply(copy);
                return (design, copy, copy.Slides[index]);
            }).ToList();
        }
        catch (Exception e) when (e is InvalidOperationException or InvalidDataException or IOException or XmlException)
        {
            return SlideDesigns.BuiltIn.Select(d => (d, (Presentation)null, (Slide)null)).ToList();
        }
    }
}
