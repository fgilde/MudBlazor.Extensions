namespace MudBlazor.Extensions.Office.Notebook;

/// <summary>Hit tests and simplification for freehand strokes. All coordinates are page coordinates unless stated otherwise.</summary>
public static class InkGeometry
{
    /// <summary>The default tolerance of <see cref="Simplify"/> in pixels.</summary>
    public const double DefaultTolerance = 0.5;

    /// <summary>Tells whether an eraser path touches a stroke, taking the stroke width into account.</summary>
    /// <param name="stroke">The stroke.</param>
    /// <param name="path">The eraser path; a single point is a tap.</param>
    /// <param name="radius">The eraser radius in pixels.</param>
    /// <returns>True if the eraser touches the stroke.</returns>
    public static bool HitByEraser(InkElement stroke, IReadOnlyList<(double X, double Y)> path, double radius)
    {
        ArgumentNullException.ThrowIfNull(stroke);
        if (path is not { Count: > 0 } || stroke.Points.Count == 0) return false;
        var reach = radius + stroke.StrokeWidth / 2;
        var (left, top, right, bottom) = (double.MaxValue, double.MaxValue, double.MinValue, double.MinValue);
        foreach (var p in stroke.Points) (left, top, right, bottom) = (Math.Min(left, p.X), Math.Min(top, p.Y), Math.Max(right, p.X), Math.Max(bottom, p.Y));
        if (path.Max(p => p.X) < stroke.X + left - reach || path.Min(p => p.X) > stroke.X + right + reach
            || path.Max(p => p.Y) < stroke.Y + top - reach || path.Min(p => p.Y) > stroke.Y + bottom + reach)
            return false;
        var points = stroke.Points.Select(p => (X: stroke.X + p.X, Y: stroke.Y + p.Y)).ToList();
        for (var i = 0; i < path.Count; i++)
        {
            var (a, b) = (path[i], path[Math.Min(i + 1, path.Count - 1)]);
            for (var j = 0; j < points.Count; j++)
                if (SegmentDistance(a, b, points[j], points[Math.Min(j + 1, points.Count - 1)]) <= reach)
                    return true;
        }

        return false;
    }

    /// <summary>Tells whether a lasso selects an element: a stroke if more than half of its points lie inside, any other element if its centre does.</summary>
    /// <param name="element">The element.</param>
    /// <param name="polygon">The closed lasso line.</param>
    /// <returns>True if the element is selected.</returns>
    public static bool InsideLasso(NoteElement element, IReadOnlyList<(double X, double Y)> polygon)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (polygon is not { Count: >= 3 }) return false;
        if (element is InkElement { Points.Count: > 0 } ink)
            return ink.Points.Count(p => Contains(polygon, (ink.X + p.X, ink.Y + p.Y))) * 2 > ink.Points.Count;
        return Contains(polygon, (element.X + element.Width / 2, element.Y + element.Height / 2));
    }

    /// <summary>Reduces the points of a stroke with the Ramer-Douglas-Peucker algorithm. The first and last point are always kept.</summary>
    /// <param name="points">The points.</param>
    /// <param name="tolerance">The largest distance in pixels a dropped point may have from the simplified line.</param>
    /// <returns>The kept points in order.</returns>
    public static IReadOnlyList<InkPoint> Simplify(IReadOnlyList<InkPoint> points, double tolerance = DefaultTolerance)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count < 3) return points.ToList();
        var keep = new bool[points.Count];
        keep[0] = keep[^1] = true;
        var ranges = new Stack<(int First, int Last)>();
        ranges.Push((0, points.Count - 1));
        while (ranges.Count > 0)
        {
            var (first, last) = ranges.Pop();
            var (farthest, distance) = (-1, tolerance);
            for (var i = first + 1; i < last; i++)
            {
                var d = PointDistance((points[i].X, points[i].Y), (points[first].X, points[first].Y), (points[last].X, points[last].Y));
                if (d > distance) (farthest, distance) = (i, d);
            }

            if (farthest < 0) continue;
            keep[farthest] = true;
            ranges.Push((first, farthest));
            ranges.Push((farthest, last));
        }

        return points.Where((_, i) => keep[i]).ToList();
    }

    private static bool Contains(IReadOnlyList<(double X, double Y)> polygon, (double X, double Y) point)
    {
        var inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            var (a, b) = (polygon[i], polygon[j]);
            if (a.Y > point.Y != b.Y > point.Y && point.X < (b.X - a.X) * (point.Y - a.Y) / (b.Y - a.Y) + a.X) inside = !inside;
        }

        return inside;
    }

    private static double SegmentDistance((double X, double Y) a, (double X, double Y) b, (double X, double Y) c, (double X, double Y) d)
    {
        if (Intersect(a, b, c, d)) return 0;
        return Math.Min(Math.Min(PointDistance(a, c, d), PointDistance(b, c, d)), Math.Min(PointDistance(c, a, b), PointDistance(d, a, b)));
    }

    private static double PointDistance((double X, double Y) p, (double X, double Y) a, (double X, double Y) b)
    {
        var (dx, dy) = (b.X - a.X, b.Y - a.Y);
        var length = dx * dx + dy * dy;
        var t = length == 0 ? 0 : Math.Clamp(((p.X - a.X) * dx + (p.Y - a.Y) * dy) / length, 0, 1);
        return Math.Sqrt(Math.Pow(p.X - a.X - t * dx, 2) + Math.Pow(p.Y - a.Y - t * dy, 2));
    }

    private static bool Intersect((double X, double Y) a, (double X, double Y) b, (double X, double Y) c, (double X, double Y) d)
    {
        static double Cross((double X, double Y) o, (double X, double Y) p, (double X, double Y) q) => (p.X - o.X) * (q.Y - o.Y) - (p.Y - o.Y) * (q.X - o.X);
        var (d1, d2, d3, d4) = (Cross(c, d, a), Cross(c, d, b), Cross(a, b, c), Cross(a, b, d));
        return (d1 > 0 && d2 < 0 || d1 < 0 && d2 > 0) && (d3 > 0 && d4 < 0 || d3 < 0 && d4 > 0);
    }
}
