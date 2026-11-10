using System.Globalization;
using System.Text;

namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>PowerPoint preset shapes (<c>a:prstGeom</c>) as SVG path data.</summary>
public static class PresetGeometry
{
    private const double Full = 100000;

    private static readonly Dictionary<string, Action<ShapePath>> Builders = new()
    {
        ["rect"] = p => p.Poly((0, 0), (p.W, 0), (p.W, p.H), (0, p.H)),
        ["roundRect"] = p => { var r = p.Radius("adj", 16667); p.RoundedRect(r, r, r, r); },
        ["round2SameRect"] = p => { var (a, b) = (p.Radius("adj1", 16667), p.Radius("adj2", 0)); p.RoundedRect(a, a, b, b); },
        ["ellipse"] = p => p.Ellipse(p.W / 2, p.H / 2, p.W / 2, p.H / 2, true),
        ["triangle"] = p => p.Poly((0, p.H), (p.W * p.Adj("adj", 50000) / Full, 0), (p.W, p.H)),
        ["rtTriangle"] = p => p.Poly((0, 0), (0, p.H), (p.W, p.H)),
        ["diamond"] = p => p.Poly((0, p.H / 2), (p.W / 2, 0), (p.W, p.H / 2), (p.W / 2, p.H)),
        ["parallelogram"] = p => { var x = p.Ss * p.Adj("adj", 25000, Full * p.W / p.Ss) / Full; p.Poly((0, p.H), (x, 0), (p.W, 0), (p.W - x, p.H)); },
        ["trapezoid"] = p => { var x = p.Ss * p.Adj("adj", 25000, 50000 * p.W / p.Ss) / Full; p.Poly((0, p.H), (x, 0), (p.W - x, 0), (p.W, p.H)); },
        ["pentagon"] = p => p.Star(5, 1),
        ["hexagon"] = Hexagon,
        ["octagon"] = p => { var x = p.Radius("adj", 29289); p.Poly((0, x), (x, 0), (p.W - x, 0), (p.W, x), (p.W, p.H - x), (p.W - x, p.H), (x, p.H), (0, p.H - x)); },
        ["star5"] = p => p.Star(5, p.Adj("adj", 19098) / 50000),
        ["star6"] = p => p.Star(6, p.Adj("adj", 28868) / 50000),
        ["plus"] = p => { var x = p.Radius("adj", 25000); var (x2, y2) = (p.W - x, p.H - x); p.Poly((0, x), (x, x), (x, 0), (x2, 0), (x2, x), (p.W, x), (p.W, y2), (x2, y2), (x2, p.H), (x, p.H), (x, y2), (0, y2)); },
        ["rightArrow"] = p => p.Poly(Arrow(p, p.W, p.H).ToArray()),
        ["leftArrow"] = p => p.Poly(Arrow(p, p.W, p.H).Select(q => (p.W - q.X, q.Y)).ToArray()),
        ["downArrow"] = p => p.Poly(Arrow(p, p.H, p.W).Select(q => (q.Y, q.X)).ToArray()),
        ["upArrow"] = p => p.Poly(Arrow(p, p.H, p.W).Select(q => (q.Y, p.H - q.X)).ToArray()),
        ["leftRightArrow"] = p =>
        {
            var dy = p.H * p.Adj("adj1", 50000) / 200000;
            var d = p.Ss * p.Adj("adj2", 50000, 50000 * p.W / p.Ss) / Full;
            var (y1, y2, vc, x3) = (p.H / 2 - dy, p.H / 2 + dy, p.H / 2, p.W - d);
            p.Poly((0, vc), (d, 0), (d, y1), (x3, y1), (x3, 0), (p.W, vc), (x3, p.H), (x3, y2), (d, y2), (d, p.H));
        },
        ["chevron"] = p => { var x = p.Ss * p.Adj("adj", 50000, Full * p.W / p.Ss) / Full; p.Poly((0, 0), (p.W - x, 0), (p.W, p.H / 2), (p.W - x, p.H), (0, p.H), (x, p.H / 2)); },
        ["homePlate"] = p => { var x = p.W - p.Ss * p.Adj("adj", 50000, Full * p.W / p.Ss) / Full; p.Poly((0, 0), (x, 0), (p.W, p.H / 2), (x, p.H), (0, p.H)); },
        ["line"] = Line,
        ["straightConnector1"] = Line,
        ["wedgeRectCallout"] = p => Callout(p, 0),
        ["wedgeRoundRectCallout"] = p => Callout(p, p.Radius("adj3", 16667)),
        ["cloud"] = Cloud,
        ["heart"] = Heart,
        ["donut"] = p =>
        {
            var d = p.Radius("adj", 25000);
            p.Ellipse(p.W / 2, p.H / 2, p.W / 2, p.H / 2, true);
            p.Ellipse(p.W / 2, p.H / 2, p.W / 2 - d, p.H / 2 - d, false);
        },
        ["frame"] = p =>
        {
            var x = p.Radius("adj1", 12500);
            p.Poly((0, 0), (p.W, 0), (p.W, p.H), (0, p.H));
            p.Poly((x, x), (x, p.H - x), (p.W - x, p.H - x), (p.W - x, x));
        },
        ["snip1Rect"] = p => { var d = p.Radius("adj", 16667); p.Poly((0, 0), (p.W - d, 0), (p.W, d), (p.W, p.H), (0, p.H)); }
    };

    private static readonly (double X, double Y)[] CloudPoints =
        [(0.10, 0.62), (0.20, 0.28), (0.42, 0.14), (0.66, 0.16), (0.86, 0.30), (0.92, 0.58), (0.78, 0.84), (0.50, 0.86), (0.24, 0.84)];

    /// <summary>The supported preset names, using the OOXML spelling.</summary>
    public static IReadOnlyList<string> Supported { get; } = Builders.Keys.ToArray();

    /// <summary>Tells whether a preset has a geometry.</summary>
    /// <param name="preset">The preset name, such as <c>roundRect</c>.</param>
    /// <returns>True if <see cref="Path"/> draws the preset itself and does not fall back to <c>rect</c>.</returns>
    public static bool IsSupported(string preset) => preset != null && Builders.ContainsKey(preset);

    /// <summary>Builds the outline of a preset shape.</summary>
    /// <param name="preset">The preset name; an unknown name draws <c>rect</c>.</param>
    /// <param name="width">The width of the box.</param>
    /// <param name="height">The height of the box.</param>
    /// <param name="adjustments">Adjust values by name (<c>adj</c>, <c>adj1</c>, ...) in 100000ths; missing ones use the defaults of the preset.</param>
    /// <returns>SVG path data in the box <c>0..width</c> by <c>0..height</c>.</returns>
    public static string Path(string preset, double width, double height, IReadOnlyDictionary<string, long> adjustments = null)
    {
        var path = new ShapePath(width, height, adjustments);
        Builders[IsSupported(preset) ? preset : "rect"](path);
        return path.ToString();
    }

    private static IEnumerable<(double X, double Y)> Arrow(ShapePath p, double length, double breadth)
    {
        var dy = breadth * p.Adj("adj1", 50000) / 200000;
        var x1 = length - p.Ss * p.Adj("adj2", 50000, Full * length / p.Ss) / Full;
        var (y1, y2) = (breadth / 2 - dy, breadth / 2 + dy);
        return [(0, y1), (x1, y1), (x1, 0), (length, breadth / 2), (x1, breadth), (x1, y2), (0, y2)];
    }

    private static void Hexagon(ShapePath p)
    {
        var x1 = p.Ss * p.Adj("adj", 25000, 50000 * p.W / p.Ss) / Full;
        var dy = p.H / 2 * p.Adj("vf", 115470, 200000) / Full * Math.Sin(Math.PI / 3);
        var (y1, y2, vc) = (p.H / 2 - dy, p.H / 2 + dy, p.H / 2);
        p.Poly((0, vc), (x1, y1), (p.W - x1, y1), (p.W, vc), (p.W - x1, y2), (x1, y2));
    }

    private static void Line(ShapePath p) => p.Raw($"M0,0 L{ShapePath.Num(p.W)},{ShapePath.Num(p.H)}");

    private static void Callout(ShapePath p, double r)
    {
        var dx = p.W * p.Adj("adj1", -20833, double.MaxValue, double.MinValue) / Full;
        var dy = p.H * p.Adj("adj2", 62500, double.MaxValue, double.MinValue) / Full;
        var (tx, ty) = (p.W / 2 + dx, p.H / 2 + dy);
        var vertical = Math.Abs(dy) > Math.Abs(dx * p.H / p.W);
        var (x1, x2) = (p.W * (dx > 0 ? 7 : 2) / 12, p.W * (dx > 0 ? 10 : 5) / 12);
        var (y1, y2) = (p.H * (dy > 0 ? 7 : 2) / 12, p.H * (dy > 0 ? 10 : 5) / 12);
        p.Begin(0, r);
        p.Corner(r, r, 0);
        p.Line(x1, 0);
        if (vertical && dy < 0) p.Line(tx, ty);
        p.Line(x2, 0);
        p.Line(p.W - r, 0);
        p.Corner(r, p.W, r);
        p.Line(p.W, y1);
        if (!vertical && dx > 0) p.Line(tx, ty);
        p.Line(p.W, y2);
        p.Line(p.W, p.H - r);
        p.Corner(r, p.W - r, p.H);
        p.Line(x2, p.H);
        if (vertical && dy > 0) p.Line(tx, ty);
        p.Line(x1, p.H);
        p.Line(r, p.H);
        p.Corner(r, 0, p.H - r);
        p.Line(0, y2);
        if (!vertical && dx < 0) p.Line(tx, ty);
        p.Line(0, y1);
        p.Close();
    }

    private static void Cloud(ShapePath p)
    {
        var pts = CloudPoints.Select(q => (X: q.X * p.W, Y: q.Y * p.H)).ToArray();
        p.Begin(pts[0].X, pts[0].Y);
        for (var i = 0; i < pts.Length; i++)
        {
            var (a, b) = (pts[i], pts[(i + 1) % pts.Length]);
            var (mx, my) = ((a.X + b.X) / 2, (a.Y + b.Y) / 2);
            p.Quad(mx + (mx - p.W / 2) * 0.55, my + (my - p.H / 2) * 0.55, b.X, b.Y);
        }
        p.Close();
    }

    private static void Heart(ShapePath p)
    {
        var (w, h, hc) = (p.W, p.H, p.W / 2);
        p.Begin(hc, h);
        p.Cubic(w * 0.1, h * 0.75, 0, h * 0.5, 0, h * 0.3);
        p.Cubic(0, h * 0.1, w * 0.18, 0, w * 0.3, 0);
        p.Cubic(w * 0.4, 0, w * 0.47, h * 0.08, hc, h * 0.22);
        p.Cubic(w * 0.53, h * 0.08, w * 0.6, 0, w * 0.7, 0);
        p.Cubic(w * 0.82, 0, w, h * 0.1, w, h * 0.3);
        p.Cubic(w, h * 0.5, w * 0.9, h * 0.75, hc, h);
        p.Close();
    }

    private sealed class ShapePath(double width, double height, IReadOnlyDictionary<string, long> adjustments)
    {
        private readonly StringBuilder _sb = new();

        public double W { get; } = width;

        public double H { get; } = height;

        public double Ss { get; } = Math.Min(width, height);

        public static string Num(double v)
        {
            var r = Math.Round(v, 2);
            return (r == 0 ? 0 : r).ToString("0.##", CultureInfo.InvariantCulture);
        }

        public double Adj(string name, double fallback, double max = Full, double min = 0) =>
            Math.Clamp(adjustments != null && adjustments.TryGetValue(name, out var v) ? v : fallback, min, max);

        public double Radius(string name, double fallback) => Ss * Adj(name, fallback, 50000) / Full;

        public override string ToString() => _sb.ToString();

        public void Raw(string s) => _sb.Append(s);

        public void Begin(double x, double y) => Add($"M{Num(x)},{Num(y)}");

        public void Line(double x, double y) => Add($"L{Num(x)},{Num(y)}");

        public void Quad(double cx, double cy, double x, double y) => Add($"Q{Num(cx)},{Num(cy)} {Num(x)},{Num(y)}");

        public void Cubic(double x1, double y1, double x2, double y2, double x, double y) =>
            Add($"C{Num(x1)},{Num(y1)} {Num(x2)},{Num(y2)} {Num(x)},{Num(y)}");

        public void Close() => Add("Z");

        public void Corner(double r, double x, double y)
        {
            if (r > 0) Add($"A{Num(r)},{Num(r)} 0 0 1 {Num(x)},{Num(y)}");
            else Line(x, y);
        }

        public void Poly(params (double X, double Y)[] points)
        {
            Begin(points[0].X, points[0].Y);
            foreach (var q in points.Skip(1)) Line(q.X, q.Y);
            Close();
        }

        public void Ellipse(double cx, double cy, double rx, double ry, bool clockwise)
        {
            var sweep = clockwise ? 1 : 0;
            Begin(cx - rx, cy);
            Add($"A{Num(rx)},{Num(ry)} 0 1 {sweep} {Num(cx + rx)},{Num(cy)}");
            Add($"A{Num(rx)},{Num(ry)} 0 1 {sweep} {Num(cx - rx)},{Num(cy)}");
            Close();
        }

        public void RoundedRect(double tl, double tr, double br, double bl)
        {
            Begin(0, tl);
            Corner(tl, tl, 0);
            Line(W - tr, 0);
            Corner(tr, W, tr);
            Line(W, H - br);
            Corner(br, W - br, H);
            Line(bl, H);
            Corner(bl, 0, H - bl);
            Close();
        }

        public void Star(int points, double innerRatio)
        {
            var outer = Enumerable.Range(0, points).Select(i => Polar(i * 2 * Math.PI / points, 1)).ToArray();
            var (minX, maxX, minY, maxY) = (outer.Min(q => q.X), outer.Max(q => q.X), outer.Min(q => q.Y), outer.Max(q => q.Y));
            (double X, double Y) Fit((double X, double Y) q) => ((q.X - minX) / (maxX - minX) * W, (q.Y - minY) / (maxY - minY) * H);
            var all = new List<(double X, double Y)>();
            for (var i = 0; i < points; i++)
            {
                all.Add(Fit(outer[i]));
                if (innerRatio < 1) all.Add(Fit(Polar((i + 0.5) * 2 * Math.PI / points, innerRatio)));
            }
            Poly(all.ToArray());
        }

        private static (double X, double Y) Polar(double angle, double radius) =>
            (radius * Math.Sin(angle), -radius * Math.Cos(angle));

        private void Add(string s)
        {
            if (_sb.Length > 0) _sb.Append(' ');
            _sb.Append(s);
        }
    }
}
