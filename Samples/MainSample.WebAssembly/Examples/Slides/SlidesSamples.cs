using MudBlazor.Extensions.Office.Presentation;

namespace MainSample.WebAssembly.Examples.Slides;

public static class SlidesSamples
{
    private const long Emu = 914400;
    private const string PictureBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4nGP4z8DwHwAFAAH/q842iQAAAABJRU5ErkJggg==";

    public static Presentation CreateDeck(SlideDesign design = null)
    {
        var deck = Presentation.Create(design);
        var title = deck.Slides[0];
        SetText(deck, title, 2, "Slide Editor");
        SetText(deck, title, 3, "Built in code with MudBlazor.Extensions");
        deck.Execute(new SetNotesCommand(title, "Welcome. This deck was created with Presentation.Create() and a few commands."));

        var bullets = Add(deck, 1, "obj");
        SetText(deck, bullets, 2, "What the editor does");
        deck.Execute(new SetTextCommand(bullets, 3, new TextBody
        {
            Paragraphs = new[] { "Edit text, shapes and pictures on the canvas", "Reorder, duplicate and hide slides", "Undo and redo every step", "Save the deck as pptx" }
                .Select(t => new TextParagraph { Bullet = true, Runs = [new TextRun(t)] }).ToList()
        }));
        deck.Execute(new SetNotesCommand(bullets, "Each bullet is a paragraph of the content placeholder."));

        var shapes = Add(deck, 2, "obj");
        SetText(deck, shapes, 2, "Shapes");
        deck.Execute(new SetGeometryCommand(shapes, 3, "roundRect"));
        deck.Execute(new SetTransformsCommand(shapes, new Dictionary<uint, ShapeTransform> { [3] = new(Emu, 2 * Emu, 3 * Emu, 2 * Emu) }));
        deck.Execute(new SetShapeStyleCommand(shapes, [3], new ShapeFill(ShapeFillKind.Solid, "#C43E1C"), new ShapeLine("#7A2610", 25400)));
        SetText(deck, shapes, 3, "Rounded rectangle");
        var copies = new[] { ("ellipse", 5 * Emu, "#2B579A"), ("rightArrow", 9 * Emu, "#217346") };
        foreach (var (geometry, x, color) in copies)
        {
            var copy = new DuplicateShapesCommand(shapes, [3], 0);
            deck.Execute(copy);
            var id = copy.Copies[0].Id;
            deck.Execute(new SetGeometryCommand(shapes, id, geometry));
            deck.Execute(new SetTransformsCommand(shapes, new Dictionary<uint, ShapeTransform> { [id] = new(x, 2 * Emu, 2 * Emu, 2 * Emu) }));
            deck.Execute(new SetShapeStyleCommand(shapes, [id], new ShapeFill(ShapeFillKind.Solid, color), new ShapeLine(color, 12700)));
            SetText(deck, shapes, id, geometry);
        }

        deck.Execute(new SetNotesCommand(shapes, "Three shapes with different geometry and fill."));

        var picture = Add(deck, 3, "titleOnly");
        SetText(deck, picture, 2, "Pictures");
        deck.Execute(new InsertPictureCommand(picture, Convert.FromBase64String(PictureBase64), "image/png", new ShapeTransform(3 * Emu, 2 * Emu, 6 * Emu, 3 * Emu)));
        deck.Execute(new SetNotesCommand(picture, "A picture inserted from bytes."));
        AddTable(deck);
        AddGroup(deck);
        AddAnimated(deck);
        deck.Execute(new AddSectionCommand(deck.Slides[1], "Content"));
        deck.Execute(new AddSectionCommand(deck.Slides[4], "Tables and motion"));
        var fade = new SlideTransition(TransitionKind.Fade, TransitionDirection.None, 0.75, true, null);
        deck.Execute(new SetTransitionCommand(deck.Slides.Skip(1).ToList(), fade));
        deck.Undo.Clear();
        return deck;
    }

    public static Presentation CreateTablesDeck()
    {
        var deck = Presentation.Create();
        SetText(deck, deck.Slides[0], 2, "Tables");
        SetText(deck, deck.Slides[0], 3, "Select a cell, then use the Table Design and Layout tabs");
        deck.Execute(new SetNotesCommand(deck.Slides[0], "Click into the table on the next slide."));
        var (slide, table) = AddTable(deck);
        deck.Execute(new InsertTableRowsCommand(slide, table, 4, 1));
        deck.Execute(new MergeTableCellsCommand(slide, table, 4, 0, 1, 3));
        deck.Execute(new SetTableCellTextCommand(slide, table, 4, 0, new TextBody { Paragraphs = [new TextParagraph { Runs = [new TextRun("Total: 4.1 M")] }] }));
        deck.Undo.Clear();
        return deck;
    }

    private static (Slide Slide, uint Table) AddTable(Presentation deck)
    {
        var slide = Add(deck, deck.Slides.Count, "titleOnly");
        SetText(deck, slide, 2, "Tables");
        var insert = new InsertTableCommand(slide, 4, 3, new ShapeTransform(Emu, 2 * Emu, 10 * Emu, 2 * Emu));
        deck.Execute(insert);
        string[][] cells = [["Quarter", "Revenue", "Growth"], ["Q1", "1.2 M", "+4 %"], ["Q2", "1.5 M", "+25 %"], ["Q3", "1.4 M", "-7 %"]];
        for (var row = 0; row < cells.Length; row++)
            for (var column = 0; column < cells[row].Length; column++)
                deck.Execute(new SetTableCellTextCommand(slide, insert.Table.Id, row, column, new TextBody { Paragraphs = [new TextParagraph { Runs = [new TextRun(cells[row][column])] }] }));
        deck.Execute(new SetNotesCommand(slide, "A table with a header row. Use the Table Design and Layout tabs to style it, merge cells and add rows."));
        return (slide, insert.Table.Id);
    }

    private static void AddGroup(Presentation deck)
    {
        var slide = Add(deck, deck.Slides.Count, "titleOnly");
        SetText(deck, slide, 2, "Groups");
        var first = AutoShape.CreateStyled("ellipse", new ShapeTransform(2 * Emu, 2 * Emu, 2 * Emu, 2 * Emu));
        var second = AutoShape.CreateStyled("rightArrow", new ShapeTransform(4 * Emu, 2 * Emu, 3 * Emu, 2 * Emu));
        deck.Execute(new InsertShapeCommand(slide, first));
        deck.Execute(new InsertShapeCommand(slide, second));
        deck.Execute(new GroupShapesCommand(slide, [first.Id, second.Id]));
        deck.Execute(new SetNotesCommand(slide, "Both shapes form one group; move, resize or rotate it as a unit and use Ungroup to split it."));
    }

    private static void AddAnimated(Presentation deck)
    {
        var slide = Add(deck, deck.Slides.Count, "titleOnly");
        SetText(deck, slide, 2, "Build steps");
        var ids = new List<uint>();
        foreach (var (text, x) in new[] { ("First", Emu), ("Second", 5 * Emu), ("Third", 9 * Emu) })
        {
            var shape = AutoShape.CreateStyled("roundRect", new ShapeTransform(x, 2 * Emu, 3 * Emu, 2 * Emu));
            deck.Execute(new InsertShapeCommand(slide, shape));
            SetText(deck, slide, shape.Id, text);
            ids.Add(shape.Id);
        }

        var effects = new[] { (AnimationPreset.Fade, AnimationDirection.None), (AnimationPreset.FlyIn, AnimationDirection.FromLeft), (AnimationPreset.Zoom, AnimationDirection.None) };
        for (var i = 0; i < ids.Count; i++)
            deck.Execute(new AddAnimationCommand(slide, new AnimationEffect(ids[i], AnimationClass.Entrance, effects[i].Item1, AnimationTrigger.OnClick, 0, 0.5, effects[i].Item2, null)));
        deck.Execute(new SetNotesCommand(slide, "Each shape appears on a click. Press F5 to see the build steps in the slide show."));
    }

    private static Slide Add(Presentation deck, int index, string layoutType)
    {
        var command = new AddSlideCommand(index, deck.Layouts.First(l => l.Type == layoutType));
        deck.Execute(command);
        return command.Slide;
    }

    private static void SetText(Presentation deck, Slide slide, uint shape, string text) =>
        deck.Execute(new SetTextCommand(slide, shape, new TextBody { Paragraphs = [new TextParagraph { Runs = [new TextRun(text)] }] }));
}
