
namespace MudBlazor.Extensions.Helper;

public static partial class MudExCss
{
    public abstract partial class Classes
    {
        /// <summary>
        /// Css classes for dialogs
        /// </summary>
        public static class Dialog
        {
            /// <summary>
            /// The mud ex initial class should always applied to dialogs if you use dialogServiceExt
            /// </summary>
            public static readonly Classes _Initial = new CssClasses("mud-ex-dialog-initial");

            /// <summary>
            /// Glass dialog
            /// </summary>
            public static readonly Classes Glass = new CssClasses("mud-ex-glass-dialog");
            

            /// <summary>
            /// Class for full height content
            /// </summary>
            public static readonly Classes FullHeightContent = new CssClasses("dialog-content-full-height");
            
            /// <summary>
            /// Full height with margin
            /// </summary>
            public static readonly Classes FullHeightWithMargin = new CssClasses("mud-dialog-height-full");
            
            /// <summary>
            /// Full height without margin
            /// </summary>
            public static readonly Classes FullHeightWithoutMargin = new CssClasses("mud-dialog-height-full-no-margin");
            
            /// <summary>
            /// Class for color full glass dialog
            /// </summary>
            public static readonly Classes ColorfullGlass = new CssClasses(Glass, Backgrounds.LightBulb);
            
            /// <summary>
            /// Class for sticky dialog actions
            /// </summary>
            public static readonly Classes DialogActionsSticky = new CssClasses("mud-ex-dialog-actions-sticky");
            
            /// <summary>
            /// Class for absolute dialog actions
            /// </summary>
            public static readonly Classes DialogActionsAbsolute = new CssClasses("mud-ex-dialog-actions-absolute");
            
            /// <summary>
            /// No margin dialog
            /// </summary>
            internal static readonly Classes PositionFixedNoMargin = new CssClasses("mud-dialog-position-fixed mud-ex-dialog-no-margin");
        }
    }
}