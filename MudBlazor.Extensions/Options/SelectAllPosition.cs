using System.ComponentModel;

namespace MudBlazor.Extensions.Options
{
    /// <summary>
    /// Position of the select all button
    /// </summary>
    public enum SelectAllPosition
    {
        /// <summary>
        /// Before the search box
        /// </summary>
        [Description("Upper line")]
        BeforeSearchBox,
        
        /// <summary>
        /// Next to the search box
        /// </summary>
        [Description("Start of the searchbox in the same line")]
        NextToSearchBox,
        
        /// <summary>
        /// After the search box
        /// </summary>
        [Description("Below line")]
        AfterSearchBox
    }
}
