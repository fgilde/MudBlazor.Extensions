using System.ComponentModel;

namespace MudBlazor.Extensions.Options
{
    /// <summary>
    /// Value presenter for the MudExField
    /// </summary>
    public enum ValuePresenter
    {
        /// <summary>
        /// No value will be presented
        /// </summary>
        [Description("none")]
        None,
        
        /// <summary>
        /// Value will be presented as text
        /// </summary>
        [Description("text")]
        Text,
        
        /// <summary>
        /// Value will be presented as chip
        /// </summary>
        [Description("chip")]
        Chip,
        
        /// <summary>
        /// Value will be presented as chip without the item template
        /// </summary>
        [Description("chips-only")]
        ChipWithoutItemTemplate,
        
        /// <summary>
        /// Value will be presented as item content based on given item template
        /// </summary>
        [Description("itemcontent")]
        ItemContent,
    }

}
