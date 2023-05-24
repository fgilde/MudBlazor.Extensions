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
            public static readonly Classes ColorfullGlass = new CssClasses(Glass, Backgrounds.LightBulb);
        }
    }
}