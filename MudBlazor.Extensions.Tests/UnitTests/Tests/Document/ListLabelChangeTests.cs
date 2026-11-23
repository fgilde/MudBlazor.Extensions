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
