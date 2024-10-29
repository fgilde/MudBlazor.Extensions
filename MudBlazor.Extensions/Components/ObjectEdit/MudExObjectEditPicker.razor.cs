using System.IO;
using MudBlazor.Extensions.Components.ObjectEdit.Options;

namespace MudBlazor.Extensions.Components.ObjectEdit;

public partial class MudExObjectEditPicker<T>
{
    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (!IsOverwritten(nameof(AdornmentIcon)))
            AdornmentIcon = ReadOnly ? Icons.Material.Filled.Search : Icons.Material.Filled.Edit;
        if (!IsOverwritten(nameof(IconSize)))
            IconSize = Size.Medium;
        base.OnInitialized();
    }

    protected override void AfterValueChanged(T from, T to)
    {
        Text = to?.ToString();
        base.AfterValueChanged(from, to);
    }

    private bool? ReadOnlyOverwrite()
    {
        return ReadOnly ? true : null;
    }
}