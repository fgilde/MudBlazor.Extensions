using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Office;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

using MudBlazor.Extensions.Office.Document;

public class DocumentEditActionsTests
{
    private static Paragraph P(string text) => new() { Inlines = text.Length == 0 ? [] : [new TextRun(text)] };

    private static TextDocument Doc(params Block[] blocks) => TextDocument.FromBlocks(blocks);

    private static DocumentRange R(TextDocument document, string id, int start, int end) => new(document.PositionOf(id, start), document.PositionOf(id, end));

    private static DocumentRange At(TextDocument document, string id, int offset) => R(document, id, offset, offset);

    private static string[] Texts(TextDocument document) => document.Blocks.OfType<Paragraph>().Select(p => p.Text).ToArray();

    private static IDocumentCommand<TextDocument> Run(TextDocument document, string name, DocumentRange range, string text = null)
    {
        var command = DocumentEditActions.For(document, name, range, text);
        Assert.NotNull(command);
        document.Execute(command, range);
        return command;
    }

    private static TextDocument ListDoc(params string[] items)
    {
        var document = Doc(items.Select(P).ToArray<Block>());
        document.Execute(new ApplyListCommand(document.Blocks.Select(b => b.Id).ToList(), ListTemplate.Decimal));
        return document;
    }

    private static int? Level(TextDocument document, int index) => document.EffectiveParagraphFormat((Paragraph)document.Blocks[index]).Numbering is { NumId: not 0 } n ? n.Level : null;

    private static Table Table() => new() { Grid = [1000], Rows = [new TableRow([new TableCell([P("cell")])])] };

    [Fact]
    public void Split_AtCaret_SplitsTheParagraph()
    {
        var document = Doc(P("Hello"));
        var id = document.Blocks[0].Id;

        Assert.IsType<SplitParagraphCommand>(Run(document, "split", At(document, id, 2)));

        Assert.Equal(["He", "llo"], Texts(document));
    }

    [Fact]
    public void Split_WithSelection_ReplacesWithNewParagraph()
    {
        var document = Doc(P("Hello"));
        var id = document.Blocks[0].Id;

        Assert.IsType<ReplaceTextCommand>(Run(document, "split", R(document, id, 1, 3)));

        Assert.Equal(["H", "lo"], Texts(document));
    }

    [Fact]
    public void PageBreak_AtCaret_InsertsAPageBreak()
    {
        var document = Doc(P("Hello"));
        var id = document.Blocks[0].Id;

        Assert.IsType<InsertBreakCommand>(Run(document, "pageBreak", At(document, id, 2)));

        Assert.Contains(((Paragraph)document.Blocks[0]).Inlines, i => i is Break { Kind: BreakKind.Page });
        Assert.Equal(6, ((Paragraph)document.Blocks[0]).Length);
    }

    [Fact]
    public void PageBreak_WithSelection_ReplacesWithAPageBreak()
    {
        var document = Doc(P("Hello"));
        var id = document.Blocks[0].Id;

        Assert.IsType<ReplaceTextCommand>(Run(document, "pageBreak", R(document, id, 1, 4)));

        var paragraph = (Paragraph)document.Blocks[0];
        Assert.Contains(paragraph.Inlines, i => i is Break { Kind: BreakKind.Page });
        Assert.Equal(3, paragraph.Length);
    }

    [Fact]
    public void DeleteBackward_WithSelection_DeletesIt()
    {
        var document = Doc(P("Hello"));
        var id = document.Blocks[0].Id;

        Run(document, "deleteBackward", R(document, id, 4, 1));

        Assert.Equal(["Ho"], Texts(document));
    }

    [Fact]
    public void DeleteBackward_InText_DeletesThePreviousCharacter()
    {
        var document = Doc(P("Hello"));
        var id = document.Blocks[0].Id;

        Run(document, "deleteBackward", At(document, id, 2));

        Assert.Equal(["Hllo"], Texts(document));
    }

    [Fact]
    public void DeleteBackward_AfterSurrogatePair_DeletesBoth()
    {
        var document = Doc(P("a\U0001F600b"));
        var id = document.Blocks[0].Id;

        Run(document, "deleteBackward", At(document, id, 3));

        Assert.Equal(["ab"], Texts(document));
    }

    [Fact]
    public void DeleteBackward_AtStartOfListItem_RemovesList()
    {
        var document = ListDoc("one", "two");
        var id = document.Blocks[1].Id;

        Assert.IsType<RemoveListCommand>(Run(document, "deleteBackward", At(document, id, 0)));

        Assert.Equal(["one", "two"], Texts(document));
        Assert.Equal(0, Level(document, 0));
        Assert.Null(Level(document, 1));
    }

    [Fact]
    public void DeleteBackward_AtStartAfterParagraph_MergesWithIt()
    {
        var document = Doc(P("one"), P("two"));
        var id = document.Blocks[1].Id;

        Assert.IsType<MergeParagraphsCommand>(Run(document, "deleteBackward", At(document, id, 0)));

        Assert.Equal(["onetwo"], Texts(document));
    }

    [Fact]
    public void DeleteBackward_AtStartAfterTable_IsNull()
    {
        var document = Doc(Table(), P("two"));
        var id = document.Blocks[1].Id;

        Assert.Null(DocumentEditActions.For(document, "deleteBackward", At(document, id, 0), null));
    }

    [Fact]
    public void DeleteBackward_AtStartOfDocument_IsNull()
    {
        var document = Doc(P("one"));

        Assert.Null(DocumentEditActions.For(document, "deleteBackward", At(document, document.Blocks[0].Id, 0), null));
    }

    [Fact]
    public void DeleteForward_WithSelection_DeletesIt()
    {
        var document = Doc(P("Hello"));
        var id = document.Blocks[0].Id;

        Run(document, "deleteForward", R(document, id, 1, 4));

        Assert.Equal(["Ho"], Texts(document));
    }

    [Fact]
    public void DeleteForward_InText_DeletesTheNextCharacter()
    {
        var document = Doc(P("Hello"));
        var id = document.Blocks[0].Id;

        Run(document, "deleteForward", At(document, id, 1));

        Assert.Equal(["Hllo"], Texts(document));
    }

    [Fact]
    public void DeleteForward_BeforeSurrogatePair_DeletesBoth()
    {
        var document = Doc(P("a\U0001F600b"));
        var id = document.Blocks[0].Id;

        Run(document, "deleteForward", At(document, id, 1));

        Assert.Equal(["ab"], Texts(document));
    }

    [Fact]
    public void DeleteForward_AtEndBeforeParagraph_MergesWithIt()
    {
        var document = Doc(P("one"), P("two"));
        var id = document.Blocks[0].Id;

        Assert.IsType<MergeParagraphsCommand>(Run(document, "deleteForward", At(document, id, 3)));

        Assert.Equal(["onetwo"], Texts(document));
    }

    [Fact]
    public void DeleteForward_AtEndBeforeTable_IsNull()
    {
        var document = Doc(P("one"), Table(), P(""));

        Assert.Null(DocumentEditActions.For(document, "deleteForward", At(document, document.Blocks[0].Id, 3), null));
    }

    [Fact]
    public void DeleteForward_AtEndOfDocument_IsNull()
    {
        var document = Doc(P("one"));

        Assert.Null(DocumentEditActions.For(document, "deleteForward", At(document, document.Blocks[0].Id, 3), null));
    }

    [Fact]
    public void Replace_WithSelection_ReplacesIt()
    {
        var document = Doc(P("Hello"));
        var id = document.Blocks[0].Id;

        Run(document, "replace", R(document, id, 1, 5), "ey");

        Assert.Equal(["Hey"], Texts(document));
    }

    [Fact]
    public void Replace_AtCaret_Inserts()
    {
        var document = Doc(P("Hello"));
        var id = document.Blocks[0].Id;

        Run(document, "replace", At(document, id, 5), "!");

        Assert.Equal(["Hello!"], Texts(document));
    }

    [Fact]
    public void Tab_AtStartOfListItem_Indents()
    {
        var document = ListDoc("one", "two");
        var id = document.Blocks[1].Id;

        Assert.IsType<ChangeListLevelCommand>(Run(document, "tab", At(document, id, 0)));

        Assert.Equal(["one", "two"], Texts(document));
        Assert.Equal(1, Level(document, 1));
    }

    [Fact]
    public void Tab_InsideListItem_InsertsTab()
    {
        var document = ListDoc("one");
        var id = document.Blocks[0].Id;

        Run(document, "tab", At(document, id, 1));

        Assert.Equal(["o\tne"], Texts(document));
        Assert.Equal(0, Level(document, 0));
    }

    [Fact]
    public void Tab_RangeOverListItems_IndentsThemAll()
    {
        var document = ListDoc("one", "two");

        Run(document, "tab", new DocumentRange(document.PositionOf(document.Blocks[0].Id, 2), document.PositionOf(document.Blocks[1].Id, 1)));

        Assert.Equal(["one", "two"], Texts(document));
        Assert.Equal([1, 1], [Level(document, 0), Level(document, 1)]);
    }

    [Fact]
    public void Tab_RangeOverPlainParagraphs_ReplacesWithTab()
    {
        var document = Doc(P("one"), P("two"));

        Run(document, "tab", new DocumentRange(document.PositionOf(document.Blocks[0].Id, 2), document.PositionOf(document.Blocks[1].Id, 1)));

        Assert.Equal(["on\two"], Texts(document));
    }

    [Fact]
    public void Tab_InText_InsertsTab()
    {
        var document = Doc(P("Hello"));
        var id = document.Blocks[0].Id;

        Assert.IsType<ReplaceTextCommand>(Run(document, "tab", At(document, id, 0)));

        Assert.Equal(["\tHello"], Texts(document));
    }

    [Fact]
    public void Untab_ListItem_Outdents()
    {
        var document = ListDoc("one");
        var id = document.Blocks[0].Id;
        document.Execute(new ChangeListLevelCommand([id], 2));

        Assert.IsType<ChangeListLevelCommand>(Run(document, "untab", At(document, id, 2)));

        Assert.Equal(1, Level(document, 0));
    }

    [Fact]
    public void UnknownName_IsNull()
    {
        var document = Doc(P("Hello"));

        Assert.Null(DocumentEditActions.For(document, "explode", At(document, document.Blocks[0].Id, 0), null));
        Assert.Null(DocumentEditActions.For(document, null, At(document, document.Blocks[0].Id, 0), null));
    }
}
