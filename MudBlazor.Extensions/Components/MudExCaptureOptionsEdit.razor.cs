using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Core;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Component edit capture options
/// </summary>
public partial class MudExCaptureOptionsEdit : IObjectEditorWithCustomPropertyRenderDataFor<CaptureOptions>
{
    /// <inheritdoc />
    [Parameter]
    public CaptureOptions Value { get; set; }

    /// <inheritdoc />
    [Parameter]
    public EventCallback<CaptureOptions> ValueChanged { get; set; }

    /// <summary>
    /// Set to true to have a read only mode
    /// </summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    /// <inheritdoc />
    public IRenderData GetRenderData(ObjectEditPropertyMeta meta)
    {
        return RenderData.For<MudExObjectEditPicker<CaptureOptions>, CaptureOptions>(edit => edit.Value, edit =>
        {
            edit.ToStringFunc = options => "MIX: " + options.AudioContentType;
            edit.AllowOpenOnReadOnly = true;
        });
    }
}