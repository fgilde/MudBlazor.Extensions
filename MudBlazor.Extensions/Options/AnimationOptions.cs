using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Options
{
    public class AnimationOptions
    {
        public AnimationType AnimationType { get; set; }
        public DialogPosition? From { get; set; }
        public string[] FromPositionNames => From.GetPositionNames();
    }

    public enum AnimationType
    {
        SlideIn
    }
}