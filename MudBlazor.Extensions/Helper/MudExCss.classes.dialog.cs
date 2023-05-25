using System.ComponentModel;

namespace MudBlazor.Extensions.Helper;

public static partial class MudExCss
{
    public abstract partial class Classes
    {
        public static class Dialog
        {
            public static readonly Classes _Initial = new CssClasses("mud-ex-dialog-initial");
            public static readonly Classes Glass = new CssClasses("mud-ex-glass-dialog");
            public static readonly Classes FullHeightContent = new CssClasses("dialog-content-full-height");
            public static readonly Classes FullHeightWithMargin = new CssClasses("mud-dialog-height-full");
            public static readonly Classes FullHeightWithoutMargin = new CssClasses("mud-dialog-height-full-no-margin");
            public static readonly Classes ColorfullGlass = new CssClasses(Glass, Backgrounds.LightBulb);

            internal static readonly Classes PositionFixedNoMargin = new CssClasses("mud-dialog-position-fixed mud-ex-dialog-no-margin");
        }
    }
}