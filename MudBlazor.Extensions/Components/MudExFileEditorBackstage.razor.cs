using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Shared file menu of the file editors with New, Open, Save, Save as, Export and Info.
/// Meant as <see cref="MudExRibbon.FileMenu"/> of a <see cref="MudExRibbon"/>, whose backstage it closes after a successful action.
/// </summary>
public partial class MudExFileEditorBackstage
{
    /// <summary>
    /// The editor the menu acts on
    /// </summary>
    [Parameter] public IMudExFileEditor Editor { get; set; }

    [CascadingParameter] private MudExRibbon Ribbon { get; set; }

    private async Task RunAsync(Func<Task<bool>> action)
    {
        if (await action() && Ribbon != null)
            await Ribbon.SetFileMenuOpenAsync(false);
    }
}
