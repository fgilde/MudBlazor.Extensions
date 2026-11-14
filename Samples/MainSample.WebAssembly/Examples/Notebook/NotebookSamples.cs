using MudBlazor.Extensions.Office.Notebook;
using NotebookModel = MudBlazor.Extensions.Office.Notebook.Notebook;

namespace MainSample.WebAssembly.Examples.Notebook;

public static class NotebookSamples
{
    private const string PictureBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4nGP4z8DwHwAFAAH/q842iQAAAABJRU5ErkJggg==";

    public static NotebookModel CreateNotebook() => NotebookModel.Load(new MemoryStream(CreateFile()));

    public static byte[] CreateFile()
    {
        var notebook = NotebookModel.Create("Sample notebook");
        var welcome = notebook.Sections[0];
        notebook.Execute(new RenameSectionCommand(welcome, "Welcome"));
        notebook.Execute(new SetSectionColorCommand(welcome, "#7719AA"));
        var addSketches = new AddSectionCommand(1, "Sketches", "#2B579A");
        notebook.Execute(addSketches);
        var sketches = addSketches.Section;
        IReadOnlyList<NotebookPage> starters = [welcome.Pages[0], sketches.Pages[0]];

        var hello = Page(notebook, welcome, "Hello notebook", 0);
        Text(notebook, hello, 40, 100, 520, Paragraph("Click anywhere on the page to type. Pick a pen to draw, add shapes, tables and pictures, and zoom with the status bar."));
        Text(notebook, hello, 40, 200, 520, Rich(Run("This notebook was built in code with the notebook commands and saved as a "), Run("mxnb", new NoteRunFormat(Bold: true)),
            Run(" file: a zip with one JSON file per page.")));
        Shape(notebook, hello, NoteShapeKind.Rectangle, new NoteBounds(600, 40, 160, 90), "#7719AA", "#EBD9F5");
        Shape(notebook, hello, NoteShapeKind.Ellipse, new NoteBounds(620, 160, 120, 120), "#2B579A", "#D6E4F7");

        var text = Page(notebook, welcome, "Rich text", 1);
        Text(notebook, text, 40, 100, 560,
            Rich(Run("Bold", new NoteRunFormat(Bold: true)), Run(", "), Run("italic", new NoteRunFormat(Italic: true)), Run(", "), Run("underlined", new NoteRunFormat(Underline: true)),
                Run(", "), Run("struck through", new NoteRunFormat(Strike: true)), Run(", "), Run("colored", new NoteRunFormat(Color: "#C43E1C")), Run(", "),
                Run("highlighted", new NoteRunFormat(Highlight: "#FFF176")), Run(" and a "), Run("link", new NoteRunFormat(Link: "https://www.mudex.org")), Run(".")),
            Paragraph("Centered text") with { Align = NoteAlign.Center }, Paragraph("Right aligned text") with { Align = NoteAlign.Right });
        Text(notebook, text, 40, 260, 560, Paragraph("Heading 2") with { Heading = NoteHeading.H2 }, Paragraph("Heading 3") with { Heading = NoteHeading.H3 },
            Paragraph("Normal text again."));

        var lists = Page(notebook, welcome, "Lists and tables", 1);
        Text(notebook, lists, 40, 100, 300,
            List("Bullet one", NoteListKind.Bullet), List("Nested bullet", NoteListKind.Bullet, 1), List("Bullet two", NoteListKind.Bullet),
            List("First step", NoteListKind.Number), List("Sub step", NoteListKind.Number, 1), List("Second step", NoteListKind.Number));
        Text(notebook, lists, 40, 330, 300, List("Write the notes", NoteListKind.Todo) with { Checked = true }, List("Draw a sketch", NoteListKind.Todo),
            List("Share the file", NoteListKind.Todo));
        Table(notebook, lists, 400, 100, [["Quarter", "Revenue", "Growth"], ["Q1", "1.2 M", "+4 %"], ["Q2", "1.5 M", "+25 %"], ["Q3", "1.4 M", "-7 %"]]);

        var shapes = Page(notebook, sketches, "Shapes and pictures", 0);
        Shape(notebook, shapes, NoteShapeKind.Rectangle, new NoteBounds(40, 100, 160, 100), "#C43E1C", "#F8D9D0");
        Shape(notebook, shapes, NoteShapeKind.Ellipse, new NoteBounds(240, 100, 120, 120), "#2B579A", "#D6E4F7");
        Shape(notebook, shapes, NoteShapeKind.Triangle, new NoteBounds(400, 100, 120, 110), "#217346", "#D4ECDD");
        Shape(notebook, shapes, NoteShapeKind.Diamond, new NoteBounds(560, 100, 120, 120), "#7719AA", null);
        Shape(notebook, shapes, NoteShapeKind.Line, new NoteBounds(40, 260, 200, 0), "#333333", null);
        Shape(notebook, shapes, NoteShapeKind.Arrow, new NoteBounds(300, 260, 200, 60), "#C43E1C", null);
        var picture = new AddImageCommand(shapes, Convert.FromBase64String(PictureBase64), "image/png", 40, 360, 160);
        notebook.Execute(picture);
        notebook.Execute(new ResizeElementsCommand(shapes, new Dictionary<string, NoteBounds> { [picture.Image.Id] = new(40, 360, 160, 100) }));
        notebook.Execute(new SetImageAltTextCommand(shapes, picture.Image.Id, "A single blue pixel stretched into a picture"));

        var ink = Page(notebook, sketches, "Handwriting", 0);
        Stroke(notebook, ink, 40, 120, Wave(300, 80, 40), "#2B579A", 3, InkTool.Pen);
        Stroke(notebook, ink, 40, 230, Wave(300, 40, 12), "#C43E1C", 4, InkTool.Pen);
        Stroke(notebook, ink, 60, 100, Wave(240, 28, 0), "#FFEB3B", 18, InkTool.Highlighter);
        Stroke(notebook, ink, 400, 160, [new InkPoint(0, 0, 0.5)], "#000000", 6, InkTool.Pen);

        notebook.Execute(new DeletePagesCommand(starters));
        using var stream = new MemoryStream();
        notebook.Save(stream);
        return stream.ToArray();
    }

    private static NotebookPage Page(NotebookModel notebook, NotebookSection section, string title, int level)
    {
        var add = new AddPageCommand(section, section.Pages.Count, title);
        notebook.Execute(add);
        if (level > 0)
            notebook.Execute(new SetPageLevelCommand([add.Page], level));
        Text(notebook, add.Page, 40, 30, 520, Paragraph(title) with { Heading = NoteHeading.H1 });
        return add.Page;
    }

    private static void Text(NotebookModel notebook, NotebookPage page, double x, double y, double width, params NoteParagraph[] paragraphs) =>
        notebook.Execute(new AddTextCommand(page, x, y, width, paragraphs));

    private static void Shape(NotebookModel notebook, NotebookPage page, NoteShapeKind kind, NoteBounds bounds, string stroke, string fill) =>
        notebook.Execute(new AddShapeCommand(page, kind, bounds, stroke, 2, fill));

    private static void Table(NotebookModel notebook, NotebookPage page, double x, double y, string[][] cells)
    {
        var add = new AddTableCommand(page, cells.Length, cells[0].Length, x, y);
        notebook.Execute(add);
        notebook.Execute(new SetTableHeaderRowCommand(page, add.Table.Id, true));
        for (var row = 0; row < cells.Length; row++)
            for (var column = 0; column < cells[row].Length; column++)
                notebook.Execute(new SetTableCellCommand(page, add.Table.Id, row, column, [Paragraph(cells[row][column])]));
    }

    private static void Stroke(NotebookModel notebook, NotebookPage page, double x, double y, IReadOnlyList<InkPoint> points, string color, double width, InkTool tool) =>
        notebook.Execute(new AddStrokeCommand(page, points.Select(p => p with { X = x + p.X, Y = y + p.Y }).ToList(), color, width, tool));

    private static NoteRun Run(string text, NoteRunFormat format = null) => new(text, format);

    private static NoteParagraph Rich(params NoteRun[] runs) => new() { Runs = runs };

    private static NoteParagraph Paragraph(string text) => Rich(Run(text));

    private static NoteParagraph List(string text, NoteListKind list, int indent = 0) => Paragraph(text) with { List = list, Indent = indent };

    private static List<InkPoint> Wave(double width, double height, double amplitude) =>
        Enumerable.Range(0, 31).Select(i => i / 30.0)
            .Select(t => new InkPoint(Math.Round(t * width, 2), Math.Round(height / 2 + Math.Sin(t * Math.PI * 4) * amplitude * 0.5, 2), 0.6)).ToList();
}
