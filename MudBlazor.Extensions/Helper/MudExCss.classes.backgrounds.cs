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
        }
    }
}