using System.Globalization;

namespace MudBlazor.Extensions.Options
{
    public class AnimationTimingFunction
    {
        private readonly string _timingFn;

        public AnimationTimingFunction(string timingFn)
        {
            _timingFn = timingFn;
        }

        public static AnimationTimingFunction EaseInOut = new("ease-in-out");
        public static AnimationTimingFunction Ease = new("ease");
        public static AnimationTimingFunction EaseIn = new("ease-in");
        public static AnimationTimingFunction EaseOut = new("ease-out");
        public static AnimationTimingFunction Inherit = new("inherit");
        public static AnimationTimingFunction Initial = new("initial");
        public static AnimationTimingFunction Linear = new("linear");
        public static AnimationTimingFunction StepEnd = new("step-end");
        public static AnimationTimingFunction StepStart = new("step-start");
        public static AnimationTimingFunction CubicBezier(double p1, double p2, double p3, double p4) => new(string.Format(CultureInfo.InvariantCulture, "cubic-bezier({0}, {1}, {2}, {3})", p1, p2, p3, p4));
        public static AnimationTimingFunction Steps(int number, string fn) => new($"steps({number}, {fn})");

        public static implicit operator string(AnimationTimingFunction t) => t?.ToString();
        public static implicit operator AnimationTimingFunction(string s) => new(s);

        public override string ToString()
        {
            return _timingFn;
        }
    }
}