namespace MudBlazor.Extensions.Helper;

public static partial class MudExCss
{
    public abstract partial class Classes
    {
        /// <summary>
        /// Css classes for dialogs
        /// </summary>
        public static class General
        {
            /// <summary>
            /// Full height
            /// </summary>
            public static readonly Classes FullHeight = new CssClasses("full-height");

            /// <summary>
            /// Hidden
            /// </summary>
            public static readonly Classes Hidden = new CssClasses("mud-ex-hidden");

            /// <summary>
            /// Hidden
            /// </summary>
            public static readonly Classes Collapsed = new CssClasses("mud-ex-collapsed");
        }
    }
}