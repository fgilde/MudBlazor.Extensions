using System.ComponentModel;
using System.Data.SqlTypes;

namespace MudBlazor.Extensions.Options
{
    public enum AnimationType
    {
        Default,
        [Description("kf-mud-dialog-slide-{pos}")] SlideIn,
        [Description("kf-mud-dialog-fade")] FadeIn,
        [Description("scale{InOut}")] Scale,
        
        [Description("slide{InOut}{Pos}")] Slide,
        [Description("fade{InOut}{Pos?}")] Fade,
        [Description("perspective3d{InOut}{Pos?}")] Perspective3d,
        [Description("lightSpeed{InOut}{Pos?}")] LightSpeed,

        [Description("zoom{InOut}{Pos?}")] Zoom,

        [Description("roll{InOut}")] Roll, 
        [Description("jackInTheBox")] JackInTheBox,
        [Description("hinge")] Hinge,
        [Description("rotate{InOut}{Pos?}")] Rotate,
        [Description("bounce{InOut?}{Pos?}")] Bounce,
        [Description("back{InOut}{Pos}")] Back,
        [Description("jello")] Jello,
        [Description("wobble")] Wobble,
        [Description("tada")] Tada,
        [Description("swing")] Swing,
        [Description("headShake")] HeadShake,
        [Description("shake")] Shake,
        [Description("rubberBand")] RubberBand,
        [Description("pulse")] Pulse,
        [Description("flip")] Flip,
        [Description("flip{InOut}X")] FlipX,
        [Description("flip{InOut}Y")] FlipY,

    }

    public enum AnimationDirection
    {
        In,
        Out
    }

}
