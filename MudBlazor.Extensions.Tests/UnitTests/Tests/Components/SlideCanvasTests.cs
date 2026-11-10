using Bunit;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;
using Xunit;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation.PptxBuilder;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Presentation;

public class SlideCanvasTests : IDisposable
{
    private const string Solid = "<a:solidFill><a:srgbClr val=\"FF0000\"/></a:solidFill>";
    private const long Inch = 914400;

    private const string Chart = "<p:graphicFrame><p:nvGraphicFramePr><p:cNvPr id=\"7\" name=\"Sales chart\"/><p:cNvGraphicFramePr/><p:nvPr/></p:nvGraphicFramePr>" +
                                 "<p:xfrm><a:off x=\"100\" y=\"200\"/><a:ext cx=\"3000000\" cy=\"2000000\"/></p:xfrm>" +
                                 "<a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/chart\"/></a:graphic></p:graphicFrame>";

    private const string Group = "<p:grpSp><p:nvGrpSpPr><p:cNvPr id=\"10\" name=\"Group\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr>" +
                                 "<a:xfrm><a:off x=\"1219200\" y=\"685800\"/><a:ext cx=\"2438400\" cy=\"1371600\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"1219200\" cy=\"685800\"/></a:xfrm>" +
                                 "</p:grpSpPr><p:grpSp><p:nvGrpSpPr><p:cNvPr id=\"20\" name=\"Inner\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr><p:grpSpPr>" +
                                 "<a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"609600\" cy=\"342900\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"609600\" cy=\"342900\"/></a:xfrm>" +
                                 "</p:grpSpPr><p:sp><p:nvSpPr><p:cNvPr id=\"21\" name=\"Deep\"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr><p:spPr>" +
                                 "<a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"100\" cy=\"100\"/></a:xfrm></p:spPr></p:sp></p:grpSp>" +
                                 "<p:sp><p:nvSpPr><p:cNvPr id=\"11\" name=\"Child\"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr><p:spPr>" +
                                 "<a:xfrm><a:off x=\"609600\" y=\"342900\"/><a:ext cx=\"609600\" cy=\"342900\"/></a:xfrm>" + Solid + "</p:spPr></p:sp></p:grpSp>";

    private readonly BunitContext _context = new();

    public SlideCanvasTests()
    {
        _context.Services.AddMudServicesWithExtensions();
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public void Dispose() => _context.Dispose();

    private static Presentation TwoBoxes() => new PptxBuilder()
        .Slide(Shape(2, "One", Xfrm(Inch, Inch, Inch, Inch) + Solid) + Shape(3, "Two", Xfrm(3 * Inch, Inch, Inch, Inch) + Solid) + Chart + Group)
        .Load();

    private IRenderedComponent<MudExSlideCanvas> Render(Presentation presentation, bool readOnly = false) =>
        _context.Render<MudExSlideCanvas>(p => p
            .Add(c => c.Presentation, presentation)
            .Add(c => c.Slide, presentation.Slides[0])
            .Add(c => c.Images, new SlideImageUrls(presentation))
            .Add(c => c.ReadOnly, readOnly));

    private static ShapeTransform Transform(Presentation presentation, uint id) =>
        SlideShape.Flatten(presentation.Slides[0].Shapes).Single(s => s.Id == id).ResolvedTransform;

    private static Task Select(IRenderedComponent<MudExSlideCanvas> cut, params uint[] ids) => cut.InvokeAsync(() => cut.Instance.OnSelectionAsync(ids));

    private static Task<uint[]> Click(IRenderedComponent<MudExSlideCanvas> cut, uint id, bool toggle) => cut.InvokeAsync(() => cut.Instance.OnShapeClickAsync(id, toggle));

    private static Task Key(IRenderedComponent<MudExSlideCanvas> cut, string key, bool ctrl = false, bool shift = false) =>
        cut.Find(".mud-ex-slide-canvas").TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = key, CtrlKey = ctrl, ShiftKey = shift });

    [Fact]
    public async Task Canvas_TransformsCallback_IsOneUndoStep()
    {
        var presentation = TwoBoxes();
        var cut = Render(presentation);
        await Select(cut, 2, 3);

        await cut.InvokeAsync(() => cut.Instance.OnTransformsAsync(new Dictionary<uint, ShapeTransformDto>
        {
            [2] = new() { X = 2 * Inch, Y = Inch, Width = Inch, Height = Inch, Rotation = 30 },
            [3] = new() { X = 4 * Inch + 0.4, Y = 2 * Inch, Width = 2 * Inch, Height = Inch, Rotation = -90 }
        }));

        Assert.Equal(new ShapeTransform(2 * Inch, Inch, Inch, Inch, 30), Transform(presentation, 2));
        Assert.Equal(new ShapeTransform(4 * Inch, 2 * Inch, 2 * Inch, Inch, 270), Transform(presentation, 3));
        Assert.Equal([2u, 3u], cut.Instance.Selection);
        cut.WaitForAssertion(() => Assert.Contains("rotate(30deg)", cut.Find(".mud-ex-slide-selection[data-id='2']").GetAttribute("style")));

        presentation.Undo.Undo();

        Assert.Equal(new ShapeTransform(Inch, Inch, Inch, Inch), Transform(presentation, 2));
        Assert.Equal(new ShapeTransform(3 * Inch, Inch, Inch, Inch), Transform(presentation, 3));
        Assert.False(presentation.Undo.CanUndo);
    }

    [Fact]
    public async Task Canvas_TransformsCallback_WithoutChange_RecordsNothing_AndOpaqueKeepsRotation()
    {
        var presentation = TwoBoxes();
        var cut = Render(presentation);

        await cut.InvokeAsync(() => cut.Instance.OnTransformsAsync(new() { [2] = new() { X = Inch, Y = Inch, Width = Inch, Height = Inch }, [3] = new() { X = 3 * Inch, Y = Inch, Width = Inch, Height = Inch, Rotation = 359.99995 }, [99] = new() { X = 1 } }));
        Assert.False(presentation.Undo.CanUndo);

        await cut.InvokeAsync(() => cut.Instance.OnTransformsAsync(new() { [7] = new() { X = 100, Y = 200, Width = 3000000, Height = 2000000, Rotation = 45 } }));
        Assert.False(presentation.Undo.CanUndo);
    }

    [Fact]
    public async Task Canvas_Nudge_MovesByOneEighthInch()
    {
        var presentation = TwoBoxes();
        var cut = Render(presentation);

        await Key(cut, "ArrowRight");
        Assert.False(presentation.Undo.CanUndo);

        await Select(cut, 2, 3);
        await Key(cut, "ArrowRight");
        Assert.Equal(Inch + Inch / 8, Transform(presentation, 2).X);
        Assert.Equal(3 * Inch + Inch / 8, Transform(presentation, 3).X);

        await Key(cut, "ArrowUp", ctrl: true);
        Assert.Equal(Inch - Inch / 80, Transform(presentation, 2).Y);

        presentation.Undo.Undo();
        presentation.Undo.Undo();
        Assert.Equal(Inch, Transform(presentation, 2).X);
        Assert.False(presentation.Undo.CanUndo);
    }

    [Fact]
    public async Task Canvas_Delete_RemovesSelected()
    {
        var presentation = TwoBoxes();
        var cut = Render(presentation);
        IReadOnlyList<uint>? raised = null;
        cut.Render(p => p.Add(c => c.SelectionChanged, ids => raised = ids));
        await Select(cut, 2);
        Assert.Equal([2u], raised);

        await Key(cut, "Delete");

        Assert.DoesNotContain(presentation.Slides[0].Shapes, s => s.Id == 2);
        Assert.Contains(presentation.Slides[0].Shapes, s => s.Id == 3);
        Assert.Empty(cut.Instance.Selection);
        Assert.Empty(raised!);
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".mud-ex-slide-selection")));
    }

    [Fact]
    public async Task Canvas_DuplicateCtrlD_SelectsCopies()
    {
        var presentation = TwoBoxes();
        var cut = Render(presentation);
        await Select(cut, 2, 3);

        await Key(cut, "d", ctrl: true);

        var shapes = presentation.Slides[0].Shapes;
        Assert.Equal(6, shapes.Count);
        var copies = shapes.Skip(4).Select(s => s.Id).ToArray();
        Assert.Equal(copies, cut.Instance.Selection);
        Assert.DoesNotContain(2u, copies);
        cut.WaitForAssertion(() => Assert.Equal(copies.Select(id => id.ToString()), cut.FindAll(".mud-ex-slide-selection").Select(e => e.GetAttribute("data-id"))));
        Assert.Equal(Transform(presentation, 2).X + Inch / 8, Transform(presentation, copies[0]).X);
    }

    [Fact]
    public async Task Canvas_GroupClickTwice_SelectsChild()
    {
        var presentation = TwoBoxes();
        var cut = Render(presentation);

        Assert.Equal(new[] { 10u }, await Click(cut, 11, false));
        Assert.Equal(new[] { 11u }, await Click(cut, 11, false));
        Assert.Equal(new[] { 11u }, await Click(cut, 11, false));

        Assert.Equal(new[] { 10u }, await Click(cut, 21, false));
        Assert.Equal(new[] { 20u }, await Click(cut, 21, false));
        Assert.Equal(new[] { 21u }, await Click(cut, 21, false));

        Assert.Equal(new[] { 21u, 2u }, await Click(cut, 2, true));
        Assert.Equal(new[] { 2u }, await Click(cut, 21, true));
        Assert.Equal("10", cut.Find("[data-shape-id='11']").GetAttribute("data-group-id"));

        await Select(cut, 10, 11, 3);
        Assert.Equal([10u, 3u], cut.Instance.Selection);
    }

    [Fact]
    public async Task Canvas_SelectAll_Tab_Escape()
    {
        var presentation = TwoBoxes();
        var cut = Render(presentation);

        await Key(cut, "a", ctrl: true);
        Assert.Equal([2u, 3u, 7u, 10u], cut.Instance.Selection);

        await Key(cut, "Escape");
        Assert.Empty(cut.Instance.Selection);

        await Key(cut, "Tab");
        Assert.Equal([2u], cut.Instance.Selection);
        await Key(cut, "Tab", shift: true);
        Assert.Equal([10u], cut.Instance.Selection);
        await Key(cut, "Tab");
        Assert.Equal([2u], cut.Instance.Selection);
    }

    [Fact]
    public async Task Canvas_Handles_OpaqueHasNoRotation()
    {
        var presentation = TwoBoxes();
        var cut = Render(presentation);
        await Select(cut, 2, 7);

        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".mud-ex-slide-selection").Count));
        Assert.Equal(9, cut.FindAll(".mud-ex-slide-selection[data-id='2'] [data-handle]").Count);
        Assert.Equal(8, cut.FindAll(".mud-ex-slide-selection[data-id='7'] [data-handle]").Count);
        Assert.Empty(cut.FindAll(".mud-ex-slide-selection[data-id='7'] [data-handle='rotate']"));
        Assert.Equal("2,7", cut.Find(".mud-ex-slide-canvas").GetAttribute("data-selection"));
    }

    [Fact]
    public async Task Canvas_ReadOnly_HasNoHandles()
    {
        var presentation = TwoBoxes();
        var cut = Render(presentation, readOnly: true);

        await Select(cut, 2);
        Assert.Empty(await Click(cut, 2, false));
        await cut.InvokeAsync(() => cut.Instance.OnTransformsAsync(new() { [2] = new() { X = 0, Y = 0, Width = Inch, Height = Inch } }));
        await Key(cut, "a", ctrl: true);
        await Key(cut, "Delete");

        Assert.Empty(cut.FindAll("[data-handle]"));
        Assert.Empty(cut.FindAll(".mud-ex-slide-selection"));
        Assert.Equal("true", cut.Find(".mud-ex-slide-canvas").GetAttribute("data-readonly"));
        Assert.False(presentation.Undo.CanUndo);
        Assert.Equal(4, presentation.Slides[0].Shapes.Count);
    }

    [Fact]
    public async Task Canvas_UnhandledKeys_AreForwarded_SlideSwitchClearsSelection()
    {
        var presentation = TwoBoxes();
        presentation.Execute(new AddSlideCommand(1, presentation.Layouts[0]));
        var cut = Render(presentation);
        var forwarded = new List<string>();
        cut.Render(p => p.Add(c => c.KeyDown, e => forwarded.Add(e.Key)));

        await Key(cut, "PageDown");
        await Key(cut, "ArrowDown");
        await Select(cut, 2);
        await Key(cut, "ArrowDown");
        Assert.Equal(["PageDown", "ArrowDown"], forwarded);

        cut.Render(p => p.Add(c => c.Slide, presentation.Slides[1]));
        Assert.Empty(cut.Instance.Selection);
    }
}
