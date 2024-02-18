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

        /// <summary>
        /// Constructor
        /// </summary>
        protected Classes(string @class)
        {
            Class = @class;
        }

        /// <summary>
        /// ToString
        /// </summary>
        public override string ToString() => Class;
        
        /// <summary>
        /// Operator to convert to string
        /// </summary>
        public static implicit operator string(Classes cssClasses) => cssClasses.ToString();
        
        /// <summary>
        /// Operator to convert to CssClasses
        /// </summary>
        public static implicit operator Classes(string name) => new CssClasses(name);
    }

    private class CssClasses : Classes
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public CssClasses(string description, params string[] other) : base(string.Join(" ", new[] { description }.Concat(other))) { }
    }

}