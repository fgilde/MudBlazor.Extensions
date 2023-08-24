using System.ComponentModel;

namespace MudBlazor.Extensions.Enums
{
    public enum ValuePresenter
    {
        [Description("none")]
        None,
        [Description("text")]
        Text,
        [Description("chip")]
        Chip,
        [Description("itemcontent")]
        ItemContent,
    }

    public enum ChipsAdditional
    {
        None,
        Above,
        Below
    }
}
