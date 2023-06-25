using Nextended.Core.Types;
using System.ComponentModel;

namespace MudBlazor.Extensions.Helper;

public static partial class MudExCss
{
    public abstract partial class Classes
    {
        /// <summary>
        /// Css classes for backgrounds
        /// </summary>
        public static class Backgrounds
        {
            /// <summary>
            /// Blur background
            /// </summary>
            public static readonly Classes Blur = new CssClasses("mud-ex-blur");

            /// <summary>
            /// Light bulb background
            /// </summary>
            public static readonly Classes LightBulb = new CssClasses("mud-ex-bg-gradient");

            /// <summary>
            /// Background with animated moving color dots
            /// </summary>
            public static readonly Classes MovingDots = new CssClasses("mud-ex-bg-dot");
            
            /// <summary>
            /// Background with Transparent indicator style
            /// </summary>
            public static readonly Classes TransparentIndicator = new CssClasses("mud-ex-transparent-indicator-bg");

            /// <summary>
            /// Background used for Empty content
            /// </summary>
            public static readonly Classes EmptyIndicator = new CssClasses("mud-ex-empty-indicator-bg");

            /// <summary>
            /// Class used for Background if no modal dialogs appear
            /// </summary>
            internal static readonly Classes NoModal = new CssClasses("mud-dialog-container-no-modal");
        }
    }
}