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
            public static readonly Classes MainClass = MainPrefix;

            /// <summary>
            /// Full height
            /// </summary>
            public static readonly Classes FullHeight = new CssClasses("full-height");

            /// <summary>
            /// Hidden
            /// </summary>
            public static readonly Classes Hidden = new CssClasses(true, "hidden");

            /// <summary>
            /// Hidden
            /// </summary>
            public static readonly Classes Collapsed = new CssClasses(true, "collapsed");
        }
    }
}