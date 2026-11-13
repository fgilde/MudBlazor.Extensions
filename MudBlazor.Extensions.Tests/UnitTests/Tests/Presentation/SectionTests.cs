using MudBlazor.Extensions.Office;
using Xunit;
using static MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation.PptxBuilder;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;

using MudBlazor.Extensions.Office.Presentation;

public class SectionTests
{
    internal const string SectionsUri = "{521415D9-36F7-43E2-AB2F-B90AF26B5E84}";

    private static readonly Dictionary<string, Func<Presentation, IDocumentCommand<Presentation>>> Cases = new()
    {
        ["AddSectionFirstTime"] = p => new AddSectionCommand(p.Slides[2], "New"),
        ["AddSectionSplit"] = p => new AddSectionCommand(p.Slides[1], "New"),
        ["RenameSection"] = p => new RenameSectionCommand(p.Sections[1], "Renamed"),
        ["RemoveSectionKeep"] = p => new RemoveSectionCommand(p.Sections[1], false),
        ["RemoveSectionWithSlides"] = p => new RemoveSectionCommand(p.Sections[1], true),
        ["RemoveAllSections"] = p => new RemoveAllSectionsCommand(),
        ["MoveSection"] = p => new MoveSectionCommand(p.Sections[0], 1),
        ["AddSlideIntoSection"] = p => new AddSlideCommand(2, p.Layouts[0]),
        ["DuplicateSlidesIntoSection"] = p => new DuplicateSlidesCommand([p.Slides[0]]),
        ["DeleteSlidesFromSections"] = p => new DeleteSlidesCommand([p.Slides[1], p.Slides[2]]),
        ["MoveSlidesAcrossSections"] = p => new MoveSlidesCommand([p.Slides[0]], 4)
    };

    public static TheoryData<string> CommandNames => new(Cases.Keys);

    internal static string SectionList(params (string Name, uint[] Ids)[] sections) =>
        $"<p:extLst><p:ext uri=\"{SectionsUri}\"><p14:sectionLst xmlns:p14=\"http://schemas.microsoft.com/office/powerpoint/2010/main\">" +
        string.Concat(sections.Select((s, i) => $"<p14:section name=\"{s.Name}\" id=\"{{00000000-0000-0000-0000-00000000000{i + 1}}}\"><p14:sldIdLst>" +
                                                string.Concat(s.Ids.Select(id => $"<p14:sldId id=\"{id}\"/>")) + "</p14:sldIdLst></p14:section>")) +
        "</p14:sectionLst></p:ext></p:extLst>";

    internal static PptxBuilder Builder(int count, string sections = "")
    {
        var builder = new PptxBuilder { PresentationExt = sections };
        for (var i = 0; i < count; i++) builder.Slide(Shape(2, $"S{i + 1}", Xfrm(0, 0, 10, 10)));
        return builder;
    }

    private static Presentation Deck() => Builder(5, SectionList(("A", [256, 257]), ("B", [258, 259]), ("C", [260]))).Load();

    private static Presentation Plain(int count = 5) => Builder(count).Load();

    private static string Names(IEnumerable<Slide> slides) => string.Join(",", slides.Select(s => s.Shapes[0].Name));

    private static string Layout(Presentation presentation) => string.Join(" | ", presentation.Sections.Select(s => $"{s.Name}:{Names(s.Slides)}"));

    internal static void AssertInvariant(Presentation presentation)
    {
        if (presentation.Sections.Count == 0)
        {
            Assert.All(presentation.Slides, s => Assert.Null(presentation.SectionOf(s)));
            return;
        }

        Assert.Equal(presentation.Slides, presentation.Sections.SelectMany(s => s.Slides));
        Assert.Equal(presentation.Sections.Count, presentation.Sections.Select(s => s.Id).Distinct().Count());
        foreach (var section in presentation.Sections)
            Assert.All(section.Slides, s => Assert.Same(section, presentation.SectionOf(s)));
    }

    [Theory]
    [MemberData(nameof(CommandNames))]
    public void Command_ApplyThenRevert_RestoresModel(string name)
    {
        var presentation = name == "AddSectionFirstTime" ? Plain() : Deck();
        var before = SlideCommandTests.Snapshot(presentation);
        var command = Cases[name](presentation);

        command.Apply(presentation);
        var applied = SlideCommandTests.Snapshot(presentation);
        Assert.NotEqual(before, applied);
        AssertInvariant(presentation);
        command.Revert(presentation);
        Assert.Equal(before, SlideCommandTests.Snapshot(presentation));
        command.Apply(presentation);
        Assert.Equal(applied, SlideCommandTests.Snapshot(presentation));
    }

    [Fact]
    public void Reader_Sections_FollowSlideOrder()
    {
        var presentation = Deck();

        Assert.Equal("A:S1,S2 | B:S3,S4 | C:S5", Layout(presentation));
        Assert.Equal("{00000000-0000-0000-0000-000000000002}", presentation.Sections[1].Id);
        Assert.Same(presentation.Sections[1], presentation.SectionOf(presentation.Slides[3]));
        Assert.Empty(Plain().Sections);
    }

    [Fact]
    public void AddSection_FirstTime_CreatesDefaultSectionBefore()
    {
        var presentation = Plain();
        var command = new AddSectionCommand(presentation.Slides[2], "Second");

        presentation.Execute(command);

        Assert.Equal("Default Section:S1,S2 | Second:S3,S4,S5", Layout(presentation));
        Assert.Same(command.Section, presentation.Sections[1]);
        Assert.Matches("^\\{[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}\\}$", command.Section.Id);
        presentation.Undo.Undo();
        Assert.Empty(presentation.Sections);

        presentation.Execute(new AddSectionCommand(presentation.Slides[0], "All"));
        Assert.Equal("All:S1,S2,S3,S4,S5", Layout(presentation));
        presentation.Execute(new AddSectionCommand(presentation.Slides[3], "Tail"));
        Assert.Equal("All:S1,S2,S3 | Tail:S4,S5", Layout(presentation));
    }

    [Fact]
    public void RemoveSection_KeepSlides_MergesIntoPrevious()
    {
        var presentation = Deck();

        presentation.Execute(new RemoveSectionCommand(presentation.Sections[1], false));

        Assert.Equal("A:S1,S2,S3,S4 | C:S5", Layout(presentation));
        Assert.Equal(5, presentation.Slides.Count);
    }

    [Fact]
    public void RemoveSection_First_MergesIntoNext()
    {
        var presentation = Deck();

        presentation.Execute(new RemoveSectionCommand(presentation.Sections[0], false));

        Assert.Equal("B:S1,S2,S3,S4 | C:S5", Layout(presentation));
    }

    [Fact]
    public void RemoveSection_WithSlides_DeletesThem()
    {
        var presentation = Deck();
        var changes = new List<PresentationChangedEventArgs>();
        presentation.Changed += (_, e) => changes.Add(e);

        presentation.Execute(new RemoveSectionCommand(presentation.Sections[1], true));

        Assert.Equal("A:S1,S2 | C:S5", Layout(presentation));
        Assert.Equal("S1,S2,S5", Names(presentation.Slides));
        Assert.True(changes.Single().Kind.HasFlag(PresentationChangeKind.SlidesRemoved));
        Assert.True(changes.Single().Kind.HasFlag(PresentationChangeKind.Sections));
        presentation.Undo.Undo();
        Assert.Equal("A:S1,S2 | B:S3,S4 | C:S5", Layout(presentation));
    }

    [Fact]
    public void RemoveLastSection_ClearsSections()
    {
        var presentation = Deck();
        presentation.Execute(new RemoveSectionCommand(presentation.Sections[2], false));
        presentation.Execute(new RemoveSectionCommand(presentation.Sections[1], false));

        presentation.Execute(new RemoveSectionCommand(presentation.Sections[0], false));

        Assert.Empty(presentation.Sections);
        Assert.Equal(5, presentation.Slides.Count);

        var other = Deck();
        other.Execute(new RemoveAllSectionsCommand());
        Assert.Empty(other.Sections);
        Assert.Equal(5, other.Slides.Count);
    }

    [Fact]
    public void MoveSection_MovesSlides()
    {
        var presentation = Deck();

        presentation.Execute(new MoveSectionCommand(presentation.Sections[0], 2));

        Assert.Equal("B:S3,S4 | C:S5 | A:S1,S2", Layout(presentation));
        Assert.Equal("S3,S4,S5,S1,S2", Names(presentation.Slides));
        presentation.Execute(new MoveSectionCommand(presentation.Sections[1], 0));
        Assert.Equal("C:S5 | B:S3,S4 | A:S1,S2", Layout(presentation));
    }

    [Fact]
    public void MoveSlide_JoinsSectionOfPredecessor()
    {
        var presentation = Deck();

        presentation.Execute(new MoveSlidesCommand([presentation.Slides[0]], 4));
        Assert.Equal("A:S2 | B:S3,S4,S1 | C:S5", Layout(presentation));

        presentation.Execute(new MoveSlidesCommand([presentation.Slides[4]], 0));
        Assert.Equal("A:S5,S2 | B:S3,S4,S1 | C:", Layout(presentation));

        presentation.Execute(new AddSlideCommand(0, presentation.Layouts[0]));
        Assert.Equal(3, presentation.Sections[0].Slides.Count);
        presentation.Execute(new DuplicateSlidesCommand([presentation.Slides[5]]));
        Assert.Equal("S3,S4,S1,S1", Names(presentation.Sections[1].Slides));
    }

    [Fact]
    public void MoveSection_UndoRedo_RaiseSlideOrder()
    {
        var presentation = Deck();
        var changes = new List<PresentationChangedEventArgs>();
        presentation.Changed += (_, e) => changes.Add(e);

        presentation.Execute(new MoveSectionCommand(presentation.Sections[0], 2));
        presentation.Undo.Undo();
        presentation.Undo.Redo();

        Assert.Equal(3, changes.Count);
        Assert.All(changes, c =>
        {
            Assert.True(c.Kind.HasFlag(PresentationChangeKind.SlideOrder));
            Assert.True(c.Kind.HasFlag(PresentationChangeKind.Sections));
            Assert.Equal(5, c.Slides.Count);
        });
    }

    [Fact]
    public void MoveSlide_ToSamePlace_IsNoOp()
    {
        var presentation = Deck();
        var before = SlideCommandTests.Snapshot(presentation);

        presentation.Execute(new MoveSlidesCommand([presentation.Slides[2]], 2));
        presentation.Execute(new MoveSlidesCommand([presentation.Slides[2]], 3));
        presentation.Execute(new MoveSlidesCommand([presentation.Slides[2], presentation.Slides[3]], 4));

        Assert.Equal(before, SlideCommandTests.Snapshot(presentation));
        Assert.False(presentation.Undo.CanUndo);
    }

    [Fact]
    public void SlideCommands_ChangingSectionMembership_RaiseSections()
    {
        var presentation = Deck();
        var plain = Plain();
        var kinds = new List<PresentationChangeKind>();
        presentation.Changed += (_, e) => kinds.Add(e.Kind);
        plain.Changed += (_, e) => kinds.Add(e.Kind);

        presentation.Execute(new AddSlideCommand(2, presentation.Layouts[0]));
        presentation.Undo.Undo();
        plain.Execute(new AddSlideCommand(2, plain.Layouts[0]));

        Assert.Equal([true, true, false], kinds.Select(k => k.HasFlag(PresentationChangeKind.Sections)));
    }

    [Fact]
    public void NoOpSectionCommands_RecordNoUndoStep()
    {
        var presentation = Deck();

        presentation.Execute(new RenameSectionCommand(presentation.Sections[0], "A"));
        presentation.Execute(new MoveSectionCommand(presentation.Sections[1], 1));
        Plain().Execute(new RemoveAllSectionsCommand());

        Assert.False(presentation.Undo.CanUndo);
    }

    [Fact]
    public void SectionCommands_ForeignSection_Throw()
    {
        var presentation = Deck();
        var foreign = Deck().Sections[0];

        Assert.Throws<ArgumentException>(() => presentation.Execute(new RenameSectionCommand(foreign, "X")));
        Assert.Throws<ArgumentException>(() => presentation.Execute(new RemoveSectionCommand(foreign, false)));
        Assert.Throws<ArgumentException>(() => presentation.Execute(new MoveSectionCommand(foreign, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => presentation.Execute(new MoveSectionCommand(presentation.Sections[0], 3)));
        Assert.Throws<ArgumentException>(() => presentation.Execute(new AddSectionCommand(Plain().Slides[0], "X")));
        Assert.False(presentation.Undo.CanUndo);
    }

    [Fact]
    public void Sections_EverySlideExactlyOnce_AfterMixedOperations()
    {
        var presentation = Plain(6);
        presentation.Undo.MaxDepth = 1000;
        var random = new Random(4711);
        var states = new List<string> { SlideCommandTests.Snapshot(presentation) };
        var executed = 0;
        var changes = 0;
        presentation.Changed += (_, _) => changes++;

        IReadOnlyList<Slide> Some()
        {
            var slides = presentation.Slides.Where(_ => random.Next(3) == 0).ToList();
            return slides.Count > 0 ? slides : [presentation.Slides[random.Next(presentation.Slides.Count)]];
        }

        for (var step = 0; step < 200; step++)
        {
            var slides = presentation.Slides.Count;
            var sections = presentation.Sections.Count;
            IDocumentCommand<Presentation> command = random.Next(11) switch
            {
                0 or 1 when slides > 0 => new AddSectionCommand(presentation.Slides[random.Next(slides)], $"Section {step}"),
                2 when sections > 0 => new RemoveSectionCommand(presentation.Sections[random.Next(sections)], random.Next(4) == 0 && slides > 3),
                3 when sections > 0 => new MoveSectionCommand(presentation.Sections[random.Next(sections)], random.Next(sections)),
                4 when sections > 0 => new RenameSectionCommand(presentation.Sections[random.Next(sections)], $"Renamed {step}"),
                5 when slides > 0 => new MoveSlidesCommand(Some(), random.Next(slides + 1)),
                6 => new AddSlideCommand(random.Next(slides + 1), presentation.Layouts[0]),
                7 when slides > 0 => new DuplicateSlidesCommand(Some()),
                8 when slides > 3 => new DeleteSlidesCommand([presentation.Slides[random.Next(slides)]]),
                9 when random.Next(8) == 0 => new RemoveAllSectionsCommand(),
                _ => null
            };
            if (command == null) continue;
            var before = changes;
            presentation.Execute(command);
            AssertInvariant(presentation);
            if (changes == before) continue;
            states.Add(SlideCommandTests.Snapshot(presentation));
            executed++;
        }

        Assert.True(executed > 100);
        Assert.True(presentation.Sections.Count > 0);
        for (var i = states.Count - 2; i >= 0; i--)
        {
            presentation.Undo.Undo();
            AssertInvariant(presentation);
            Assert.Equal(states[i], SlideCommandTests.Snapshot(presentation));
        }

        Assert.False(presentation.Undo.CanUndo);
        for (var i = 1; i < states.Count; i++)
        {
            presentation.Undo.Redo();
            AssertInvariant(presentation);
            Assert.Equal(states[i], SlideCommandTests.Snapshot(presentation));
        }
    }
}
