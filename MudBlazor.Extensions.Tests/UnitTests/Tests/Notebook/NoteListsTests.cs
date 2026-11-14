using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Notebook;

using MudBlazor.Extensions.Office.Notebook;

public class NoteListsTests
{
    private static NoteParagraph P(NoteListKind list, int indent = 0) => new() { Runs = [new NoteRun("x")], List = list, Indent = indent };

    [Fact]
    public void Numbers_CountPerIndentLevel_ResetOnPlainParagraph()
    {
        const NoteListKind n = NoteListKind.Number;
        NoteParagraph[] paragraphs =
        [
            P(n), P(n), P(n, 1), P(n, 1), P(n, 2), P(n, 2), P(n), P(n, 1), P(NoteListKind.None), P(n), P(n, 3)
        ];

        Assert.Equal(["1.", "2.", "a.", "b.", "i.", "ii.", "3.", "a.", "", "1.", "1."], NoteLists.Numbers(paragraphs));
    }

    [Fact]
    public void Numbers_OtherListKindResetsItsLevelOnly()
    {
        const NoteListKind n = NoteListKind.Number;

        Assert.Equal(["1.", "", "2.", "", "1."], NoteLists.Numbers([P(n), P(NoteListKind.Bullet, 1), P(n), P(NoteListKind.Todo), P(n)]));
    }

    [Fact]
    public void Numbers_LettersAndRomanBeyondSimpleRange()
    {
        var letters = NoteLists.Numbers(Enumerable.Repeat(P(NoteListKind.Number, 1), 28).ToList());
        var roman = NoteLists.Numbers(Enumerable.Repeat(P(NoteListKind.Number, 2), 14).ToList());

        Assert.Equal(["z.", "aa.", "ab."], letters.Skip(25));
        Assert.Equal(["iv.", "ix.", "xiv."], new[] { roman[3], roman[8], roman[13] });
    }
}
