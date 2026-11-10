using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Presentation;

namespace MudBlazor.Extensions.Components;

/// <summary>The speaker notes pane of the slide editor. The text is committed when the field loses the focus.</summary>
public partial class MudExSlideNotes
{
    private Slide _slide;
    private string _committed;
    private string _draft;

    /// <summary>The slide whose notes are shown.</summary>
    [Parameter] public Slide Slide { get; set; }

    /// <summary>If true the notes cannot be changed.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>False if the slide cannot get notes because the presentation has no notes master; the pane is then read-only with a hint.</summary>
    [Parameter] public bool CanHaveNotes { get; set; } = true;

    /// <summary>Raised with the new text when the user leaves the field after changing the notes.</summary>
    [Parameter] public EventCallback<string> NotesChanged { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (ReferenceEquals(Slide, _slide) && Slide?.Notes == _committed)
            return;
        _slide = Slide;
        _committed = _draft = Slide?.Notes;
    }

    internal async Task CommitAsync()
    {
        if (_slide == null || ReadOnly || !CanHaveNotes)
            return;
        var text = (_draft ?? "").ReplaceLineEndings("\n");
        if (text == _committed)
            return;
        _committed = text;
        await NotesChanged.InvokeAsync(text);
    }
}
