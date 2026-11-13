namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>The built-in slide designs.</summary>
public static class SlideDesigns
{
    private static readonly SlideBackground Plain = new(ShapeFillKind.Solid, "scheme:bg1", null);

    /// <summary>The six built-in designs: Paper, Slate, Ocean, Forest, Sunset and Mono.</summary>
    public static IReadOnlyList<SlideDesign> BuiltIn { get; } =
    [
        new("Paper", ["#1F1B16", "#FBF8F1", "#3B3A36", "#EDE6D6", "#2E5E8C", "#B5482E", "#3F7A4F", "#8A4F9E", "#946510", "#2D7F86", "#2457A6", "#7A4E8C"],
            "Georgia", "Calibri", Plain),
        new("Slate", ["#F1F5F9", "#1E2530", "#CBD5E1", "#2C3542", "#6CB4FF", "#F59E6B", "#7DD87F", "#F2CC60", "#C79BF2", "#5FD4D0", "#8CC8FF", "#C9A7F0"],
            "Segoe UI", "Segoe UI", Plain),
        new("Ocean", ["#0B2233", "#F4FAFC", "#12415E", "#D6EBF2", "#0B6E99", "#00808A", "#2F5DA8", "#C2551F", "#5E6E2A", "#7A4FA0", "#0A63A8", "#6A4C93"],
            "Aptos", "Aptos", Plain),
        new("Forest", ["#1A2418", "#F6F8F2", "#2F4A2B", "#DDE7D3", "#2F6B3A", "#7A5C1E", "#A0442C", "#3D6E7A", "#6B5B95", "#5A7A1F", "#2B6A8F", "#6E5A8A"],
            "Cambria", "Calibri", Plain),
        new("Sunset", ["#2B1A1F", "#FFF6EC", "#5A2E3A", "#FDE1D3", "#B83D0B", "#A8204F", "#6D35C9", "#8A5300", "#1D4ED8", "#0F6E66", "#1E50B8", "#7D3C98"],
            "Georgia", "Aptos", new SlideBackground(ShapeFillKind.Gradient, "scheme:bg1", null) { Stops = [new(0, "scheme:bg1"), new(1, "scheme:bg2")], Angle = 90 }),
        new("Mono", ["#111111", "#FFFFFF", "#333333", "#EEEEEE", "#222222", "#444444", "#5A5A5A", "#6E6E6E", "#3A3A3A", "#767676", "#1A1A1A", "#555555"],
            "Consolas", "Segoe UI", Plain)
    ];

    internal static Slide Preview(Slide slide, SlideDesign design)
    {
        if (slide.Layout is not { Master: { } source } layout) return null;
        var master = new SlideMaster
        {
            Name = source.Name, Theme = source.Theme.With(PresentationTheme.Normalize(design.Colors), design.MajorFont, design.MinorFont), Background = design.Background,
            FormatScheme = source.FormatScheme, ColorMap = source.ColorMap, TextStyles = source.TextStyles, DefaultTextStyle = source.DefaultTextStyle
        };
        master.ShapeList.AddRange(Retarget(source.Shapes, master));
        var preview = new SlideLayout
        {
            Name = layout.Name, Type = layout.Type, Master = master, Background = layout.Background, ShowMasterShapes = layout.ShowMasterShapes, ColorMap = layout.ColorMap
        };
        preview.ShapeList.AddRange(Retarget(layout.Shapes, master));
        var copy = DuplicateSlidesCommand.Snapshot(slide);
        copy.Layout = preview;
        foreach (var shape in SlideShape.Flatten(copy.Shapes)) shape.Master = master;
        return copy;
    }

    private static List<SlideShape> Retarget(IEnumerable<SlideShape> shapes, SlideMaster master)
    {
        var copies = shapes.Select(s => s.Clone(null)).ToList();
        foreach (var shape in SlideShape.Flatten(copies)) shape.Master = master;
        return copies;
    }
}
