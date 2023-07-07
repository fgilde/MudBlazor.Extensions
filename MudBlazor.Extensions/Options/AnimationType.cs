using System.ComponentModel;

namespace MudBlazor.Extensions.Options
{
    /// <summary>
    /// Animation Type enum specifies all supported animations
    /// </summary>
    public enum AnimationType
    {
        /// <summary>
        /// Default animation
        /// </summary>
        Default,

        /// <summary>
        /// Slide in animation
        /// </summary>
        [Description("kf-mud-dialog-slide-{pos}")] SlideIn,

        /// <summary>
        /// Fade in animation
        /// </summary>
        [Description("kf-mud-dialog-fade")] FadeIn,

        /// <summary>
        /// Scale animation
        /// </summary>
        [Description("scale{InOut}")] Scale,

        /// <summary>
        /// Slide animation
        /// </summary>
        [Description("slide{InOut}{Pos}")] Slide,

        /// <summary>
        /// Fade animation
        /// </summary>        
        [Description("fade{InOut}{Pos?}")] Fade,

        /// <summary>
        /// 3D Perspective animation
        /// </summary>        
        [Description("perspective3d{InOut}{Pos?}")] Perspective3d,

        /// <summary>
        /// Light speed animation
        /// </summary>        
        [Description("lightSpeed{InOut}{Pos?}")] LightSpeed,
        
        /// <summary>
        /// Zoom animation
        /// </summary>
        [Description("zoom{InOut}{Pos?}")] Zoom,

        /// <summary>
        /// Roll animation
        /// </summary>        
        /// <summary>
        /// Jack in the box animation
        /// </summary>
        [Description("jackInTheBox")] JackInTheBox,

        /// <summary>
        /// Hinge animation
        /// </summary>
        [Description("hinge")] Hinge,

        /// <summary>
        /// Rotate animation
        /// </summary>
        [Description("rotate{InOut}{Pos?}")] Rotate,

        /// <summary>
        /// Bounce animation
        /// </summary>
        [Description("bounce{InOut?}{Pos?}")] Bounce,

        /// <summary>
        /// Back animation
        /// </summary>
        [Description("back{InOut}{Pos}")] Back,

        /// <summary>
        /// Jello animation
        /// </summary>
        [Description("jello")] Jello,

        /// <summary>
        /// Wobble animation
        /// </summary>
        [Description("wobble")] Wobble,

        /// <summary>
        /// Tada animation
        /// </summary>
        [Description("tada")] Tada,

        /// <summary>
        /// Swing animation
        /// </summary>
        [Description("swing")] Swing,

        /// <summary>
        /// Head shake animation
        /// </summary>
        [Description("headShake")] HeadShake,

        /// <summary>
        /// Shake animation
        /// </summary>
        [Description("shake")] Shake,

        /// <summary>
        /// Rubber band animation
        /// </summary>
        [Description("rubberBand")] RubberBand,

        /// <summary>
        /// Pulse animation
        /// </summary>
        [Description("pulse")] Pulse,

        /// <summary>
        /// Flip animation
        /// </summary>
        [Description("flip")] Flip,

        /// <summary>
        /// Flip in X axis animation
        /// </summary>
        [Description("flip{InOut}X")] FlipX,

        /// <summary>
        /// Flip in Y axis animation
        /// </summary>
        [Description("flip{InOut}Y")] FlipY,

    }


    /// <summary>
    /// Direction of animation
    /// </summary>
    public enum AnimationDirection
    {
        /// <summary>
        /// Animation direction inwards
        /// </summary>
        In,

        /// <summary>
        /// Animation direction outwards
        /// </summary>
        Out
    }
    
}
