using System.ComponentModel;

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
            public static readonly Classes FullHeight = new CssClasses("full-height");
        }
    }
}