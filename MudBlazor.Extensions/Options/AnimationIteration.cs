using System.Globalization;

namespace MudBlazor.Extensions.Options
{
    /// <summary>
    /// Represents the animation timing function.
    /// </summary>
    public class AnimationIteration
    {
        private readonly string _iteration;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnimationIteration"/> class.
        /// </summary>
        public AnimationIteration(string iteration)
        {
            _iteration = iteration;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AnimationIteration"/> class.
        /// </summary>
        public AnimationIteration(int iteration): this(iteration.ToString())
        {}

        /// <summary>
        /// infinite
        /// </summary>
        public static AnimationIteration Infinite = new("infinite");

        /// <summary>
        /// Converts the value of the current AnimationIteration object to its equivalent string representation.
        /// </summary>
        public static implicit operator string(AnimationIteration t) => t?.ToString();

        /// <summary>
        /// Converts the specified string to its equivalent AnimationIteration representation.
        /// </summary>
        public static implicit operator AnimationIteration(string s) => new(s);

        /// <summary>
        /// Converts the specified string to its equivalent AnimationIteration representation.
        /// </summary>
        public static implicit operator AnimationIteration(int s) => new(s);

        /// <summary>
        /// Returns a string that represents the current timing function.
        /// </summary>
        /// <returns>A string that represents the current timing function.</returns>
        public override string ToString()
        {
            return _iteration;
        }
    }

}