using System.ComponentModel;

namespace MudBlazor.Extensions.Options
{
    public enum MultiSelectionComponent
    {
        [Description("none")]
        None,
        [Description("checkbox")]
        CheckBox,
        [Description("switch")]
        Switch
    }
}
