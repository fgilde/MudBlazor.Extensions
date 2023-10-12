using System.ComponentModel;

namespace MudBlazor.Extensions.Options
{
    public enum ValuePresenter
    {
        [Description("none")]
        None,
        [Description("text")]
        Text,
        [Description("chip")]
        Chip,
        [Description("chips-only")]
        ChipWithoutItemTemplate,
        [Description("itemcontent")]
        ItemContent,
    }

}
