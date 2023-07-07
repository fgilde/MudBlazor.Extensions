using System.Globalization;

namespace MudBlazor.Extensions.Options
{
    /// <summary>
    /// Represents the animation timing function.
    /// </summary>
    public class AnimationTimingFunction
    {
        private readonly string _timingFn;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnimationTimingFunction"/> class.
        /// </summary>
        /// <param name="timingFn">The timing function string.</param>
        public AnimationTimingFunction(string timingFn)
        {
            _timingFn = timingFn;
        }

        /// <summary>
        /// Gets the 'ease-in-out' timing function.
        /// </summary>
        public static AnimationTimingFunction EaseInOut = new("ease-in-out");

        /// <summary>
        /// Gets the 'ease' timing function.
        /// </summary>
        public static AnimationTimingFunction Ease = new("ease");

        /// <summary>
        /// Gets the 'ease-in' timing function.
        /// </summary>
        public static AnimationTimingFunction EaseIn = new("ease-in");

        /// <summary>
        /// Gets the 'ease-out' timing function.
        /// </summary>
        public static AnimationTimingFunction EaseOut = new("ease-out");

        /// <summary>
        /// Gets the 'inherit' timing function.
        /// </summary>
        public static AnimationTimingFunction Inherit = new("inherit");

        /// <summary>
        /// Gets the 'initial' timing function.
        /// </summary>
        public static AnimationTimingFunction Initial = new("initial");

        /// <summary>
        /// Gets the 'linear' timing function.
        /// </summary>
        public static AnimationTimingFunction Linear = new("linear");

        /// <summary>
        /// Gets the 'step-end' timing function.
        /// </summary>
        public static AnimationTimingFunction StepEnd = new("step-end");

        /// <summary>
        /// Gets the 'step-start' timing function.
        /// </summary>
        public static AnimationTimingFunction StepStart = new("step-start");

        /// <summary>
        /// Creates a 'cubic-bezier' timing function.
        /// </summary>
        public static AnimationTimingFunction CubicBezier(double p1, double p2, double p3, double p4) => new(string.Format(CultureInfo.InvariantCulture, "cubic-bezier({0}, {1}, {2}, {3})", p1, p2, p3, p4));

        /// <summary>
        /// Creates a 'steps' timing function.
        /// </summary>
        public static AnimationTimingFunction Steps(int number, string fn) => new($"steps({number}, {fn})");

        /// <summary>
        /// Converts the value of the current AnimationTimingFunction object to its equivalent string representation.
        /// </summary>
        public static implicit operator string(AnimationTimingFunction t) => t?.ToString();

        /// <summary>
        /// Converts the specified string to its equivalent AnimationTimingFunction representation.
        /// </summary>
        public static implicit operator AnimationTimingFunction(string s) => new(s);

        /// <summary>
        /// Returns a string that represents the current timing function.
        /// </summary>
        /// <returns>A string that represents the current timing function.</returns>
        public override string ToString()
        {
            return _timingFn;
        }
    }

}