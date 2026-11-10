using MudBlazor.Extensions.Office.Presentation;

namespace MainSample.WebAssembly.Examples.Slides;

public static class SlidesSamples
{
    private const long Emu = 914400;
    private const string PictureBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4nGP4z8DwHwAFAAH/q842iQAAAABJRU5ErkJggg==";

    public static Presentation CreateDeck()
    {
        var deck = Presentation.Create();
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
        deck.Undo.Clear();
        return deck;
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
