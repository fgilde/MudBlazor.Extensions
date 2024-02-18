using System.ComponentModel;

namespace MudBlazor.Extensions.Options
{
    /// <summary>
    /// Component to use for multi selection
    /// </summary>
    public enum MultiSelectionComponent
    {
        /// <summary>
        /// No component is used item is selected by clicking
        /// </summary>
        [Description("none")]
        None,
        
        /// <summary>
        /// A checkbox is used to select the item
        /// </summary>
        [Description("checkbox")]
        CheckBox,
        
        /// <summary>
        /// A switch is used to select the item
        /// </summary>
        [Description("switch")]
        Switch
    }
}
