using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public class ListLabelChangeTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 23, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void InsertItemInMiddle_ReportsTheNewAndFollowingItems()
    {
        var document = CreateList();
        ReadLabels(document);
        var events = Record(document);
        var item2 = document.Blocks[1].Id;

        document.Execute(new SplitParagraphCommand(document.PositionOf(item2, ((Paragraph)document.Blocks[1]).Length)));

        Assert.Equal(6, document.Blocks.Count);
        Assert.Equal(Ids(document, 2, 3, 4, 5), Assert.Single(events).ListLabelChangedIds.Order());
        Assert.Equal("6.", document.ListLabel((Paragraph)document.Blocks[5]));
    }

    [Fact]
    public void Typing_ReportsNoneAndComputesNoLabels()
    {
        var document = CreateList();
        ReadLabels(document);
        var computations = document.LabelComputations;
        var events = Record(document);
        var item1 = document.Blocks[0].Id;

        for (var i = 1; i <= 10; i++)
            document.ApplyTyping([new TypedParagraph(item1, "1" + new string('a', i), 1 + i)], T0.AddMilliseconds(200 * i));

        Assert.Equal(10, events.Count);
        Assert.All(events, e => Assert.Empty(e.ListLabelChangedIds));
        Assert.Equal(computations, document.LabelComputations);
    }

    [Fact]
    public void RestartNumbering_ReportsRestartedItems()
    {
        var document = CreateList();
        ReadLabels(document);
        var events = Record(document);

        document.Execute(new RestartNumberingCommand(document.Blocks[2].Id));

        Assert.Equal(Ids(document, 2, 3, 4), Assert.Single(events).ListLabelChangedIds.Order());
        Assert.Equal(["1.", "2.", "1.", "2.", "3."], ReadLabels(document));
    }

    [Fact]
    public void RemoveList_ReportsItemAndFollowers()
    {
        var document = CreateList();
        ReadLabels(document);
        var events = Record(document);

        document.Execute(new RemoveListCommand([document.Blocks[1].Id]));

        Assert.Equal(Ids(document, 1, 2, 3, 4), Assert.Single(events).ListLabelChangedIds.Order());
        Assert.Equal(["1.", null, "2.", "3.", "4."], ReadLabels(document));
    }

    [Fact]
    public void LabelsNeverRead_ReportsEmptyWithoutComputing()
    {
        var document = CreateList();
        var events = Record(document);

        document.Execute(new SplitParagraphCommand(document.PositionOf(document.Blocks[1].Id, 1)));
        document.Execute(new RemoveListCommand([document.Blocks[0].Id]));

        Assert.Equal(2, events.Count);
        Assert.All(events, e => Assert.Empty(e.ListLabelChangedIds));
        Assert.Equal(0, document.LabelComputations);
    }

    [Fact]
    public void LabelsReadInsideTransaction_StillReportsAgainstTheStateBefore()
    {
        var document = CreateList();
        ReadLabels(document);
        var events = Record(document);

        using (document.Undo.BeginTransaction("Split"))
        {
            document.Execute(new SplitParagraphCommand(document.PositionOf(document.Blocks[1].Id, 1)));
            ReadLabels(document);
        }

        Assert.Equal(Ids(document, 2, 3, 4, 5), Assert.Single(events).ListLabelChangedIds.Order());
    }

    [Fact]
    public void Undo_ReportsTheRestoredLabels()
    {
        var document = CreateList();
        document.Execute(new RemoveListCommand([document.Blocks[1].Id]));
        ReadLabels(document);
        var events = Record(document);

        document.Undo.Undo();

        Assert.Equal(Ids(document, 1, 2, 3, 4), Assert.Single(events).ListLabelChangedIds.Order());
    }

    [Fact]
    public void Split_ThenTyping_TypingReportsNothingAndComputesNothing()
    {
        var document = CreateList();
        ReadLabels(document);
        var events = Record(document);

        document.Execute(new SplitParagraphCommand(document.PositionOf(document.Blocks[1].Id, 1)));
        var computations = document.LabelComputations;
        document.ApplyTyping([new TypedParagraph(document.Blocks[0].Id, "1x", 2)], T0);

        Assert.Equal(2, events.Count);
        Assert.Equal(4, events[0].ListLabelChangedIds.Count);
        Assert.Empty(events[1].ListLabelChangedIds);
        Assert.Equal(computations, document.LabelComputations);
    }

    [Fact]
    public void ChangeThenRevert_WithoutReadBetween_ReportsTheRevertedItems()
    {
        foreach (var revert in new Action<TextDocument>[]
                 {
                     d => d.Undo.Undo(),
                     d => d.Execute(new ApplyListCommand([d.Blocks[1].Id], ListTemplate.Decimal))
                 })
        {
            var document = CreateList();
            var before = ReadLabels(document);
            var events = Record(document);

            document.Execute(new RemoveListCommand([document.Blocks[1].Id]));
            revert(document);

            Assert.Equal(2, events.Count);
            Assert.Equal(Ids(document, 1, 2, 3, 4), events[0].ListLabelChangedIds.Order());
            Assert.Equal(Ids(document, 1, 2, 3, 4), events[1].ListLabelChangedIds.Order());
            Assert.Equal(before, ReadLabels(document));
        }
    }

    [Fact]
    public void Read_Change_Read_Change_ReportsAgainstTheStateAfterTheFirst()
    {
        var document = CreateList();
        ReadLabels(document);
        var events = Record(document);

        document.Execute(new RemoveListCommand([document.Blocks[1].Id]));
        ReadLabels(document);
        document.Execute(new RemoveListCommand([document.Blocks[4].Id]));

        Assert.Equal(Ids(document, 4), events[1].ListLabelChangedIds);
    }

    [Fact]
    public void TransactionWithTwoListCommands_OneEventWithAllChangedIds()
    {
        var document = CreateList();
        ReadLabels(document);
        var events = Record(document);

        using (document.Undo.BeginTransaction("Both"))
        {
            document.Execute(new RemoveListCommand([document.Blocks[1].Id]));
            document.Execute(new RestartNumberingCommand(document.Blocks[3].Id));
        }

        Assert.Equal(["1.", null, "2.", "1.", "2."], ReadLabels(document));
        Assert.Equal(Ids(document, 1, 2, 3, 4), Assert.Single(events).ListLabelChangedIds.Order());
    }

    [Fact]
    public void ReplacingNumberingOrStyles_ReportsAffectedParagraphs()
    {
        const string decimalList =
            "<w:abstractNum w:abstractNumId=\"0\"><w:lvl w:ilvl=\"0\"><w:start w:val=\"1\"/><w:numFmt w:val=\"decimal\"/><w:lvlText w:val=\"%1.\"/></w:lvl></w:abstractNum>" +
            "<w:num w:numId=\"1\"><w:abstractNumId w:val=\"0\"/></w:num>";
        const string listStyle = "<w:pPr><w:pStyle w:val=\"ListNum\"/></w:pPr>";
        var source = new DocxBuilder()
            .Styles("<w:style w:type=\"paragraph\" w:default=\"1\" w:styleId=\"Normal\"><w:name w:val=\"Normal\"/></w:style>" +
                    "<w:style w:type=\"paragraph\" w:styleId=\"ListNum\"><w:name w:val=\"List Num\"/><w:pPr><w:numPr><w:numId w:val=\"1\"/></w:numPr></w:pPr></w:style>")
            .Numbering(decimalList)
            .Body($"<w:p>{listStyle}<w:r><w:t>a</w:t></w:r></w:p><w:p>{listStyle}<w:r><w:t>b</w:t></w:r></w:p><w:p>{listStyle}<w:r><w:t>c</w:t></w:r></w:p>" +
                  "<w:p><w:r><w:t>d</w:t></w:r></w:p><w:p><w:r><w:t>e</w:t></w:r></w:p>")
            .Build();
        var document = TextDocument.Load(new MemoryStream(source));
        Assert.Equal(["1.", "2.", "3.", null, null], ReadLabels(document));
        var events = Record(document);
        var numbering = document.Numbering;

        document.Execute(new ApplyListCommand([document.Blocks[3].Id, document.Blocks[4].Id], ListTemplate.Bullet));

        Assert.NotSame(numbering, document.Numbering);
        Assert.True(events[0].Kind.HasFlag(DocumentChangeKind.Numbering));
        Assert.Equal(Ids(document, 3, 4), events[0].ListLabelChangedIds.Order());

        document.Execute(new ApplyParagraphStyleCommand([document.Blocks[0].Id], "Normal"));

        Assert.Equal([null, "1.", "2."], ReadLabels(document).Take(3));
        Assert.Equal(Ids(document, 0, 1, 2), events[1].ListLabelChangedIds.Order());
    }

    private static TextDocument CreateList()
    {
        var document = TextDocument.FromBlocks(Enumerable.Range(1, 5).Select(i => new Paragraph { Inlines = [new TextRun(i.ToString())] }));
        document.Execute(new ApplyListCommand(document.Blocks.Select(b => b.Id).ToList(), ListTemplate.Decimal));
        return document;
    }

    private static List<string> ReadLabels(TextDocument document) => document.Blocks.Select(b => document.ListLabel((Paragraph)b)).ToList();

    private static IEnumerable<string> Ids(TextDocument document, params int[] indexes) => indexes.Select(i => document.Blocks[i].Id).Order();

    private static List<TextDocumentChangedEventArgs> Record(TextDocument document)
    {
        var events = new List<TextDocumentChangedEventArgs>();
        document.Changed += (_, e) => events.Add(e);
        return events;
    }
}
