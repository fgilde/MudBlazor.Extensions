using System.ComponentModel;

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
            public static readonly Classes FullHeightWithMargin = new CssClasses("mud-dialog-height-full");
            public static readonly Classes FullHeightWithoutMargin = new CssClasses("mud-dialog-height-full-no-margin");
            public static readonly Classes ColorfullGlass = new CssClasses(Glass, Backgrounds.LightBulb);

            internal static readonly Classes PositionFixedNoMargin = new CssClasses("mud-dialog-position-fixed mud-ex-dialog-no-margin");
        }
    }
}