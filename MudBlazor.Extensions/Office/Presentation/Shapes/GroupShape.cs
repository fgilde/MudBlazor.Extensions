namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>A group of shapes. Its children are positioned in the child coordinate space <see cref="ChildTransform"/>.</summary>
public sealed class GroupShape : SlideShape
{
    internal GroupShape()
    {
    }

    /// <summary>The child shapes in z-order.</summary>
    public IReadOnlyList<SlideShape> Children => ChildList;

    /// <summary>The child coordinate space (<c>a:chOff</c>, <c>a:chExt</c>) that maps onto the group's own box.</summary>
    public ShapeTransform ChildTransform { get; internal set; }

    internal List<SlideShape> ChildList { get; } = [];

    internal ShapeTransform ToParentSpace(ShapeTransform child)
    {
        var frame = Transform ?? default;
        var space = ChildTransform;
        var sx = space.Width == 0 ? 1 : (double)frame.Width / space.Width;
        var sy = space.Height == 0 ? 1 : (double)frame.Height / space.Height;
        double width = child.Width * sx, height = child.Height * sy;
        var dx = frame.X + (child.X - space.X) * sx + width / 2 - (frame.X + frame.Width / 2.0);
        var dy = frame.Y + (child.Y - space.Y) * sy + height / 2 - (frame.Y + frame.Height / 2.0);
        if (frame.FlipH) dx = -dx;
        if (frame.FlipV) dy = -dy;
        var angle = frame.Rotation * Math.PI / 180;
        var cx = frame.X + frame.Width / 2.0 + dx * Math.Cos(angle) - dy * Math.Sin(angle);
        var cy = frame.Y + frame.Height / 2.0 + dx * Math.Sin(angle) + dy * Math.Cos(angle);
        var rotation = ((frame.FlipH ^ frame.FlipV ? -child.Rotation : child.Rotation) + frame.Rotation) % 360;
        return new ShapeTransform((long)Math.Round(cx - width / 2), (long)Math.Round(cy - height / 2), (long)Math.Round(width), (long)Math.Round(height),
            rotation < 0 ? rotation + 360 : rotation, child.FlipH ^ frame.FlipH, child.FlipV ^ frame.FlipV);
    }
}
