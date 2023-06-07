using Nextended.Core.Types;
using System.ComponentModel;

namespace MudBlazor.Extensions.Helper;

public static partial class MudExCss
{
    public abstract partial class Classes
    {
        public static class Backgrounds
        {
            public static readonly Classes Blur = new CssClasses("mud-ex-blur");
            public static readonly Classes LightBulb = new CssClasses("mud-ex-bg-gradient");
            public static readonly Classes MovingDots = new CssClasses("mud-ex-bg-dot");
            public static readonly Classes TransparentIndicator = new CssClasses("mud-ex-transparent-indicator-bg");
            public static readonly Classes EmptyIndicator = new CssClasses("mud-ex-empty-indicator-bg");
            internal static readonly Classes NoModal = new CssClasses("mud-dialog-container-no-modal");
        }
    }
}