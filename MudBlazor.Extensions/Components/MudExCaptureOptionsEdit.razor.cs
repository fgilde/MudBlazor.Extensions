using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Core;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Component edit capture options
/// </summary>
public partial class MudExCaptureOptionsEdit : IObjectEditorWithCustomRenderDataFor<CaptureOptions>
{
    [Parameter]
    public CaptureOptions Value { get; set; }

    [Parameter]
    public EventCallback<CaptureOptions> ValueChanged { get; set; }

    [Parameter]
    public bool ReadOnly { get; set; }

    public IRenderData GetRenderData(ObjectEditPropertyMeta meta)
    {
        return RenderData.For<MudExCaptureOptionsEdit, CaptureOptions>(edit => edit.Value, edit =>
        {
            edit.Style = "background-color:pink;";
        });
        //return RenderData.For<MudExObjectEditPicker<CaptureOptions>, CaptureOptions>(edit => edit.Value, edit =>
        //{
        //    edit.Style = "background-color:pink;";
        //});
    }
}