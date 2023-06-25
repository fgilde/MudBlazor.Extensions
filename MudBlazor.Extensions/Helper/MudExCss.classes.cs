using MudBlazor.Extensions.Core;

namespace MudBlazor.Extensions.Helper;

public static partial class MudExCss
{
    /// <summary>
    /// MudExCss classes
    /// </summary>
    public abstract partial class Classes: IMudExClassAppearance
    {
        /// <summary>
        /// Class to apply
        /// </summary>
        public string Class { get; }

        protected Classes(string @class)
        {
            Class = @class;
        }

        public override string ToString() => Class;
        public static implicit operator string(Classes cssClasses) => cssClasses.ToString();
        public static implicit operator Classes(string name) => new CssClasses(name);
    }

    private class CssClasses : Classes
    {
        public CssClasses(string description, params string[] other) : base(string.Join(" ", new[] { description }.Concat(other))) { }
    }

}