using System.Buffers.Binary;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Notebook;

using MudBlazor.Extensions.Office.Notebook;

public class OneNoteReaderTests
{
    private static readonly DateTime Created = new(2025, 2, 7, 8, 53, 39, DateTimeKind.Utc);

    private static byte[] Sample() => OneNoteFileBuilder.Build(
        new OnePage("Meeting notes", Created,
            new OneOutline(1, 2.5f, 15, new OneParagraph("Größe und Maße"), new OneParagraph("Agenda", 0, Bold: true), new OneParagraph("First point", 1),
                new OneParagraph("Detail", 2), new OneParagraph("Second point", 1)),
            new OneOutline(6.5f, 2.5f, 13, new OneParagraph("Costs: 12 €"))),
        new OnePage("Ideas", Created.AddDays(17), new OneOutline(1.5f, 5.4f, 13, new OneParagraph("Line one\vLine two"))));

    private static Notebook Load(byte[] data, string fileName = "Quick Notes.one", NotebookLoadOptions options = null) =>
        Notebook.LoadOneNote(new MemoryStream(data), fileName, options);

    private static string Text(NoteParagraph paragraph) => string.Concat(paragraph.Runs.Select(r => r.Text));

    [Fact]
    public void LoadOneNote_ReadsPagesTitlesAndTextBlocks()
    {
        var notebook = Load(Sample());

        Assert.Equal("Quick Notes", notebook.Name);
        var section = Assert.Single(notebook.Sections);
        Assert.Equal("Quick Notes", section.Name);
        Assert.Equal(["Meeting notes", "Ideas"], section.Pages.Select(p => p.Title));
        var first = section.Pages[0];
        Assert.Equal(new DateTimeOffset(Created), first.Created);
        Assert.Equal(0, first.Level);
        Assert.False(notebook.Modified);

        var blocks = first.Elements.Cast<TextElement>().ToList();
        Assert.Equal(2, blocks.Count);
        Assert.Equal((48d, 120d, 720d), (blocks[0].X, blocks[0].Y, blocks[0].Width));
        Assert.Equal((312d, 120d, 624d), (blocks[1].X, blocks[1].Y, blocks[1].Width));
        Assert.Equal(["Größe und Maße", "Agenda", "First point", "Detail", "Second point"], blocks[0].Paragraphs.Select(Text));
        Assert.Equal([0, 0, 1, 2, 1], blocks[0].Paragraphs.Select(p => p.Indent));
        Assert.True(blocks[0].Paragraphs[1].Runs[0].Format.Bold);
        Assert.False(blocks[0].Paragraphs[0].Runs[0].Format.Bold);
        Assert.Equal("Costs: 12 €", Text(Assert.Single(blocks[1].Paragraphs)));

        var second = Assert.IsType<TextElement>(Assert.Single(section.Pages[1].Elements));
        Assert.Equal(["Line one", "Line two"], second.Paragraphs.Select(Text));
        Assert.Equal(259.2, second.Y, 3);
    }

    [Fact]
    public void LoadOneNote_WithoutFileName_NamesTheSectionSection1_AndSavesAsMxnb()
    {
        var notebook = Load(Sample(), null);

        Assert.Equal("Section 1", notebook.Sections[0].Name);
        var saved = new MemoryStream();
        notebook.Save(saved);
        var reloaded = Notebook.Load(new MemoryStream(saved.ToArray()));
        Assert.Equal(["Meeting notes", "Ideas"], reloaded.Sections[0].Pages.Select(p => p.Title));
        Assert.Equal("Costs: 12 €", Text(reloaded.Sections[0].Pages[0].Elements.OfType<TextElement>().Last().Paragraphs[0]));
    }

    [Fact]
    public void LoadOneNote_SectionWithoutPages_HasOneEmptyPage()
    {
        var page = Assert.Single(Load(OneNoteFileBuilder.Build()).Sections[0].Pages);

        Assert.Empty(page.Elements);
    }

    [Theory]
    [InlineData(500)]
    [InlineData(2000)]
    [InlineData(-8)]
    public void LoadOneNote_TruncatedFile_Rejected(int length)
    {
        var data = Sample();

        Assert.Throws<InvalidDataException>(() => Load(data[..(length > 0 ? length : data.Length + length)]));
    }

    [Fact]
    public void LoadOneNote_RootListOffsetOutsideTheFile_Rejected()
    {
        var data = Sample();
        BinaryPrimitives.WriteUInt64LittleEndian(data.AsSpan(172), 0x7FFF_FFFF_0000);

        var error = Assert.Throws<InvalidDataException>(() => Load(data));
        Assert.Contains("damaged", error.Message);
    }

    [Fact]
    public void LoadOneNote_FragmentPointingToItself_Rejected()
    {
        var data = Sample();
        var root = (long)BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(172));
        var length = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(180));
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(96), 0);
        BinaryPrimitives.WriteUInt64LittleEndian(data.AsSpan((int)(root + length - 20)), (ulong)root);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan((int)(root + length - 12)), length);

        Assert.Throws<InvalidDataException>(() => Load(data));
    }

    [Fact]
    public void LoadOneNote_RandomBytesWithTheRightGuids_Rejected()
    {
        var data = new byte[20_000];
        new Random(7).NextBytes(data);
        OneNoteFileBuilder.SectionFile.ToByteArray().CopyTo(data, 0);
        OneNoteFileBuilder.StoreFormat.ToByteArray().CopyTo(data, 48);

        Assert.Throws<InvalidDataException>(() => Load(data));
    }

    [Fact]
    public void LoadOneNote_FlippedBytes_OpenOrRejectedButNeverCrash()
    {
        var sample = Sample();
        var random = new Random(11);
        for (var round = 0; round < 400; round++)
        {
            var data = (byte[])sample.Clone();
            for (var i = random.Next(1, 6); i > 0; i--) data[random.Next(1024, data.Length)] = (byte)random.Next(256);
            try
            {
                Load(data);
            }
            catch (InvalidDataException)
            {
            }
        }
    }

    [Fact]
    public void LoadOneNote_PackagedFssHttpFile_RejectedWithClearMessage()
    {
        var data = Sample();
        new Guid("638DE92F-A6D4-4BC1-9A36-B3FC2511A5B7").ToByteArray().CopyTo(data, 48);

        var error = Assert.Throws<InvalidDataException>(() => Load(data));
        Assert.Contains("FSSHTTP", error.Message);
    }

    [Fact]
    public void LoadOneNote_TableOfContentsOrOtherFile_Rejected()
    {
        var toc = Sample();
        new Guid("43FF2FA1-EFD9-4C76-9EE2-10EA5722765F").ToByteArray().CopyTo(toc, 0);

        Assert.Contains(".onetoc2", Assert.Throws<InvalidDataException>(() => Load(toc)).Message);
        Assert.Throws<InvalidDataException>(() => Load(new byte[4096]));
        Assert.Throws<InvalidDataException>(() => Load([]));
    }

    [Fact]
    public void LoadOneNote_OverTheSizeLimit_Rejected()
    {
        var data = Sample();

        Assert.Throws<InvalidDataException>(() => Load(data, options: new NotebookLoadOptions { MaxEntryBytes = data.Length - 1 }));
        Assert.NotNull(Load(data, options: new NotebookLoadOptions { MaxEntryBytes = data.Length, MaxTotalBytes = data.Length }));
    }
}
