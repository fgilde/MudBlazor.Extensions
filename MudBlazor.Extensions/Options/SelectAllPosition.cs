using System.ComponentModel;

namespace MudBlazor.Extensions.Options
{
    public enum SelectAllPosition
    {
        [Description("Upper line")]
        BeforeSearchBox,
        [Description("Start of the searchbox in the same line")]
        NextToSearchBox,
        [Description("Below line")]
        AfterSearchBox
    }
}
