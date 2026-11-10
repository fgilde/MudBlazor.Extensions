using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Shared file menu of the file editors with New, Open, Save, Save as, Export and Info.
/// Meant as <see cref="MudExRibbon.FileMenu"/> of a <see cref="MudExRibbon"/>, whose backstage it closes after a successful action.
/// </summary>
public partial class MudExFileEditorBackstage
{
    private IMudExFileEditor _observed;

    /// <summary>
    /// The editor the menu acts on
    /// </summary>
    [Parameter] public IMudExFileEditor Editor { get; set; }

    /// <summary>Additional actions of the editor, rendered after Save as.</summary>
    [Parameter] public RenderFragment ChildContent { get; set; }

    [CascadingParameter] private MudExRibbon Ribbon { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (ReferenceEquals(_observed, Editor))
            return;
        if (_observed != null)
            _observed.DocumentChanged -= OnDocumentChanged;
        _observed = Editor;
        if (_observed != null)
            _observed.DocumentChanged += OnDocumentChanged;
    }

    private void OnDocumentChanged() => InvokeAsync(StateHasChanged);

    private async Task RunAsync(Func<Task<bool>> action)
    {
        if (await action() && Ribbon != null)
            await Ribbon.SetFileMenuOpenAsync(false);
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        if (_observed != null)
            _observed.DocumentChanged -= OnDocumentChanged;
        await base.DisposeAsync();
    }
}
