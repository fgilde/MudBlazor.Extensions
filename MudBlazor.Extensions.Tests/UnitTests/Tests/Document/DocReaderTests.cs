using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using MudBlazor.Extensions.Office;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

public class DocReaderTests
{
    [Fact]
    public void Doc_Read_Sample_TextMatchesWordExport()
    {
        var expected = File.ReadAllLines(Data("WordDokument.expected.txt")).Select(l => l.TrimEnd()).Where(l => l.Length > 0);

        var document = Load(File.ReadAllBytes(Data("WordDokument.doc")));

        Assert.Equal(expected, Paragraphs(document).Select(p => p.Text.TrimEnd()).Where(t => t.Length > 0));
    }

    [Fact]
    public void Doc_Read_Sample_CharacterFormats()
    {
        var document = Load(File.ReadAllBytes(Data("WordDokument.doc")));

        var run = Assert.IsType<TextRun>(Assert.Single(Assert.Single(Paragraphs(document)).Inlines));
        Assert.Equal(("Sample Word Document", "Verdana", 8.5, "#333333"), (run.Text, run.Format.Font, run.Format.SizePt, run.Format.Color));
        Assert.NotEqual(true, run.Format.Bold);
        Assert.NotEqual(true, run.Format.Italic);
        Assert.Null(Assert.Single(Paragraphs(document)).Format.StyleId);
    }

    [Fact]
    public void Doc_Read_RichWordFixture_TextFormatsAndParagraphs()
    {
        var document = Load(File.ReadAllBytes(Data("Rich.doc")));
        var paragraphs = Paragraphs(document);

        Assert.Equal([
            "\u00DCberschrift \u00C4\u00F6\u00FC \u00DF", "Fett normal kursiv unterstrichen durch rot Arial 14", "Zentriert \u4E2D\u6587 \u65E5\u672C\u8A9E \uD55C\uAD6D\uC5B4", "Rechts",
            "Blocksatz mit genug Text, damit er umbricht und als Blocksatz zu sehen ist, noch ein wenig mehr Text hier.", "Feld: Feldergebnis Link: Beispiel",
            "Zeile1\vZeile2\tnach Tab", "Gesch\u00FCtzt\u00A0Leer und Bindestrich\u2011Strich", "A1", "B1", "A2", "B2", "Fu\u00DFnote hier\uFFFC", "\f",
            "Nach Seitenumbruch", "Zweite Ebene"
        ], paragraphs.Select(p => p.Text));
        Assert.Equal(["Heading1", .. Enumerable.Repeat<string>(null, 14), "Heading2"], paragraphs.Select(p => p.Format.StyleId));
        Assert.Equal([ParagraphAlignment.Center, ParagraphAlignment.Right, ParagraphAlignment.Justify], paragraphs.Skip(2).Take(3).Select(p => p.Format.Alignment!.Value));
        Assert.All(paragraphs.Skip(5), p => Assert.NotEqual(ParagraphAlignment.Center, p.Format.Alignment));
        Assert.NotNull(document.Styles.Find("Heading2"));

        var runs = paragraphs[1].Inlines.OfType<TextRun>().Where(r => r.Text.Trim().Length > 0).ToDictionary(r => r.Text.Trim(), r => r.Format);
        Assert.Equal(true, runs["Fett"].Bold);
        Assert.Equal(true, runs["kursiv"].Italic);
        Assert.Equal("single", runs["unterstrichen"].Underline);
        Assert.Equal(true, runs["durch"].Strike);
        Assert.Equal("#FF0000", runs["rot"].Color);
        Assert.Equal(("Arial", 14.0), (runs["Arial 14"].Font, runs["Arial 14"].SizePt));
        Assert.NotEqual(true, runs["normal"].Bold);
        Assert.Equal(("footnote", "1"), paragraphs[12].Inlines.OfType<OpaqueInline>().Select(t => (t.Kind, t.DisplayText)).Single());
        Assert.Equal(BreakKind.Line, paragraphs[6].Inlines.OfType<Break>().Single().Kind);
        using var docx = new MemoryStream();
        document.Save(docx);
        DocxWriterTests.AssertConsistent(docx.ToArray());
    }

    [Fact]
    public void Doc_Read_AnsiWordFixture_DecodesWindows1252()
    {
        var paragraphs = Paragraphs(Load(File.ReadAllBytes(Data("Ansi.doc"))));

        Assert.Equal(["Gr\u00FC\u00DFe \u00E4\u00F6\u00FC \u00C4\u00D6\u00DC \u00DF \u20AC \u201EZitat\u201C \u2013 Ende", "Zweiter Absatz"], paragraphs.Select(p => p.Text));
        Assert.Equal(true, Assert.Single(paragraphs[1].Inlines).Format.Bold);
    }

    [Fact]
    public void Doc_Read_SpecialCharacters_MappedToTheModel()
    {
        var text = "a\u0013 PAGE \u0014r1\u0013 INNER \u0014r2\u0015\u0015b\u0013 NORESULT \u0015\v1\f2\t3\u0001\u0008\u0002\u001E\u001F\u00A0\u0003\uD800x\r" +
                   "c1\u0007c2\u0007\u0007end\r";

        var paragraphs = Paragraphs(Load(Doc(text)));

        Assert.Equal(["ar1r2b\v1\f2\t3\uFFFC\u2011\u00AD\u00A0x", "c1", "c2", "", "end"], paragraphs.Select(p => p.Text));
        Assert.Equal(("footnote", "1"), paragraphs[0].Inlines.OfType<OpaqueInline>().Select(t => (t.Kind, t.DisplayText)).Single());
        Assert.Equal(["Gr\u00FC\u00DFe \u20AC x", "y"], Paragraphs(Load(Doc("Gr\u00FC\u00DFe \u20AC x\ry", compressed: true))).Select(p => p.Text));
        Assert.Equal(["\uD83D\uDE00"], Paragraphs(Load(Doc("\uD83D\uDE00"))).Select(p => p.Text));
    }

    [Fact]
    public void Doc_Encrypted_ThrowsNotSupported()
    {
        var encrypted = File.ReadAllBytes(Data("WordDokument.doc"));
        encrypted[Fib(encrypted) + 0x0B] |= 0x01;
        var word95 = File.ReadAllBytes(Data("WordDokument.doc"));
        BinaryPrimitives.WriteUInt16LittleEndian(word95.AsSpan(Fib(word95) + 2), 0x0068);

        Assert.Throws<NotSupportedException>(() => Load(encrypted));
        Assert.Throws<NotSupportedException>(() => Load(word95));
        Assert.Throws<NotSupportedException>(() => Load(Doc("x", flags: 0x0300)));
    }

    [Fact(Timeout = 5000)]
    public async Task Doc_TruncatedOrCyclicFat_ThrowsInvalidData()
    {
        await Task.Run(() =>
        {
            var sample = File.ReadAllBytes(Data("WordDokument.doc"));
            var directoryCycle = (byte[])sample.Clone();
            var directory = BinaryPrimitives.ReadUInt32LittleEndian(sample.AsSpan(0x30));
            SetFat(directoryCycle, directory, directory);
            var streamCycle = (byte[])sample.Clone();
            var start = StreamStart(sample, "WordDocument");
            SetFat(streamCycle, start + 2, start + 1);
            var freeSector = (byte[])sample.Clone();
            SetFat(freeSector, start, 0xFFFFFFFF);
            var badShift = (byte[])sample.Clone();
            badShift[0x1E] = 10;

            foreach (var bytes in new[] { sample[..300], sample[..600], sample[..3000], sample[..(sample.Length / 2)], directoryCycle, streamCycle, freeSector, badShift, [] })
                Assert.Throws<InvalidDataException>(() => Load(bytes));
        });
    }

    [Fact(Timeout = 5000)]
    public async Task Doc_BrokenPieceTable_ThrowsInvalidData()
    {
        await Task.Run(() =>
        {
            Assert.Throws<InvalidDataException>(() => Load(Doc("x", fib: f => BinaryPrimitives.WriteInt32LittleEndian(f.AsSpan(0x9A + 33 * 8), 0x7FFFFFF0))));
            Assert.Throws<InvalidDataException>(() => Load(Doc("x", fib: f => BinaryPrimitives.WriteInt32LittleEndian(f.AsSpan(0x9A + 33 * 8 + 4), int.MaxValue))));
            Assert.Throws<InvalidDataException>(() => Load(Doc("abc", pieces: [(0, 0x800), (2, 0x800), (1, 0x800)])));
            Assert.Throws<InvalidDataException>(() => Load(Doc("abc", pieces: [(0, 0x800), (0, 0x800)])));
            Assert.Throws<InvalidDataException>(() => Load(Doc("abc", pieces: [(0, 0x7FFFFF00)])));
            Assert.Throws<InvalidDataException>(() => Load(Doc("abc", pieces: [(0, 0x800), (2, 0x800)], end: int.MaxValue)));
            Assert.Throws<InvalidDataException>(() => Load(Doc("abc", pieces: [(1, 0x800)])));
            Assert.Throws<InvalidDataException>(() => Load(Doc("abc", end: 2)));
            Assert.Throws<InvalidDataException>(() => Load(Doc("abc", fib: f => BinaryPrimitives.WriteInt32LittleEndian(f.AsSpan(0x4C), 40 * 1024 * 1024))));
            Assert.Throws<InvalidDataException>(() => Load(Doc("abc", fib: f => BinaryPrimitives.WriteUInt16LittleEndian(f, 0x1234))));
        });
        Assert.Equal(["bca"], Paragraphs(Load(Doc("abc", pieces: [(0, 0x802), (2, 0x800)]))).Select(p => p.Text));
    }

    [Fact]
    public void CompoundFile_ReadsMiniAndRegularStreams_512And4096ByteSectors()
    {
        var sample = CompoundFile.Open(File.ReadAllBytes(Data("WordDokument.doc")));
        var streams = sample.StreamNames.ToDictionary(n => n, sample.ReadStream);
        var large = Enumerable.Range(0, 70000).Select(i => (byte)i).ToArray();

        foreach (var shift in new[] { 9, 12 })
        {
            var file = CompoundFile.Open(Cfb(shift, ("small", [1, 2, 3]), ("large", large), ("empty", [])));
            Assert.Equal(["empty", "large", "small"], file.StreamNames.Order());
            Assert.Equal((byte[])[1, 2, 3], file.ReadStream("small"));
            Assert.Equal(large, file.ReadStream("large"));
            Assert.Empty(file.ReadStream("empty"));
            Assert.Throws<InvalidDataException>(() => file.ReadStream("missing"));

            var repacked = Load(Cfb(shift, streams.Select(s => (s.Key, s.Value)).ToArray()));
            Assert.Equal(["Sample Word Document"], Paragraphs(repacked).Select(p => p.Text));
        }

        Assert.Contains("WordDocument", streams.Keys);
        Assert.Contains("1Table", streams.Keys);
    }

    private static string Data(string name) => Path.Combine(AppContext.BaseDirectory, "TestData", "Document", name);

    private static TextDocument Load(byte[] bytes) => TextDocument.Load(new MemoryStream(bytes), DocumentFileFormat.Doc);

    private static List<Paragraph> Paragraphs(TextDocument document) => document.Blocks.OfType<Paragraph>().ToList();

    private static int Fib(byte[] file)
    {
        for (var at = 512; at + 2 <= file.Length; at += 512)
            if (file[at] == 0xEC && file[at + 1] == 0xA5) return at;
        throw new InvalidOperationException("No FIB.");
    }

    private static void SetFat(byte[] file, uint sector, uint next) =>
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan((int)(BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(0x4C)) + 1) * 512 + 4 * (int)sector), next);

    private static uint StreamStart(byte[] file, string name)
    {
        var directory = (int)(BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(0x30)) + 1) * 512;
        for (var at = directory; at < directory + 512; at += 128)
            if (Encoding.Unicode.GetString(file, at, BinaryPrimitives.ReadUInt16LittleEndian(file.AsSpan(at + 0x40)) - 2) == name)
                return BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(at + 0x74));
        throw new InvalidOperationException("No stream " + name);
    }

    private static byte[] Doc(string text, bool compressed = false, ushort flags = 0x0200, (int Cp, int Fc)[] pieces = null, int? end = null, Action<byte[]> fib = null)
    {
        var main = new byte[Math.Max(4096, 0x800 + text.Length * 2)];
        BinaryPrimitives.WriteUInt16LittleEndian(main, 0xA5EC);
        BinaryPrimitives.WriteUInt16LittleEndian(main.AsSpan(2), 0xC1);
        BinaryPrimitives.WriteUInt16LittleEndian(main.AsSpan(0x0A), flags);
        BinaryPrimitives.WriteUInt16LittleEndian(main.AsSpan(0x20), 14);
        BinaryPrimitives.WriteUInt16LittleEndian(main.AsSpan(0x3E), 22);
        BinaryPrimitives.WriteInt32LittleEndian(main.AsSpan(0x4C), text.Length);
        BinaryPrimitives.WriteUInt16LittleEndian(main.AsSpan(0x98), 93);
        var encoded = compressed ? CodePagesEncodingProvider.Instance.GetEncoding(1252)!.GetBytes(text) : MemoryMarshal.AsBytes(text.AsSpan()).ToArray();
        encoded.CopyTo(main, 0x800);
        pieces ??= [(0, compressed ? 0x1000 | 0x40000000 : 0x800)];
        var table = new byte[5 + 4 * (pieces.Length + 1) + 8 * pieces.Length];
        table[0] = 2;
        BinaryPrimitives.WriteInt32LittleEndian(table.AsSpan(1), table.Length - 5);
        for (var i = 0; i < pieces.Length; i++)
        {
            BinaryPrimitives.WriteInt32LittleEndian(table.AsSpan(5 + 4 * i), pieces[i].Cp);
            BinaryPrimitives.WriteInt32LittleEndian(table.AsSpan(5 + 4 * (pieces.Length + 1) + 8 * i + 2), pieces[i].Fc);
        }

        BinaryPrimitives.WriteInt32LittleEndian(table.AsSpan(5 + 4 * pieces.Length), end ?? text.Length);
        BinaryPrimitives.WriteInt32LittleEndian(main.AsSpan(0x9A + 33 * 8 + 4), table.Length);
        fib?.Invoke(main);
        return Cfb(9, ("WordDocument", main), ((flags & 0x0200) != 0 ? "1Table" : "0Table", table));
    }

    internal static byte[] Cfb(int shift, params (string Name, byte[] Data)[] streams)
    {
        var size = 1 << shift;
        var sectors = new List<byte[]>();
        var fat = new List<uint>();
        var mini = new MemoryStream();
        var miniFat = new List<uint>();
        var entries = new List<(string Name, byte Type, uint Start, long Length)>();
        foreach (var (name, data) in streams)
            if (data.Length >= 4096) entries.Add((name, 2, Chain(data), data.Length));
            else
            {
                var start = data.Length == 0 ? 0xFFFFFFFE : (uint)miniFat.Count;
                for (var at = 0; at < data.Length; at += 64) miniFat.Add((uint)miniFat.Count + 1);
                if (data.Length > 0) miniFat[^1] = 0xFFFFFFFE;
                mini.Write(data);
                mini.Write(new byte[(64 - data.Length % 64) % 64]);
                entries.Add((name, 2, start, data.Length));
            }

        var miniStart = Chain(mini.ToArray());
        var miniFatStart = Chain(miniFat.SelectMany(BitConverter.GetBytes).ToArray());
        entries.Insert(0, ("Root Entry", 5, miniStart, mini.Length));
        var directory = new byte[entries.Count * 128];
        for (var i = 0; i < entries.Count; i++)
        {
            var at = 128 * i;
            var name = Encoding.Unicode.GetBytes(entries[i].Name);
            name.CopyTo(directory, at);
            BinaryPrimitives.WriteUInt16LittleEndian(directory.AsSpan(at + 0x40), (ushort)(name.Length + 2));
            directory[at + 0x42] = entries[i].Type;
            BinaryPrimitives.WriteUInt32LittleEndian(directory.AsSpan(at + 0x44), 0xFFFFFFFF);
            BinaryPrimitives.WriteUInt32LittleEndian(directory.AsSpan(at + 0x48), i > 0 && i + 1 < entries.Count ? (uint)i + 1 : 0xFFFFFFFF);
            BinaryPrimitives.WriteUInt32LittleEndian(directory.AsSpan(at + 0x4C), i == 0 && entries.Count > 1 ? 1u : 0xFFFFFFFF);
            BinaryPrimitives.WriteUInt32LittleEndian(directory.AsSpan(at + 0x74), entries[i].Start);
            BinaryPrimitives.WriteInt64LittleEndian(directory.AsSpan(at + 0x78), entries[i].Length);
        }

        var directoryStart = Chain(directory);
        var fatSectors = 1;
        while ((sectors.Count + fatSectors) * 4 > fatSectors * size) fatSectors++;
        var firstFat = sectors.Count;
        for (var i = 0; i < fatSectors; i++)
        {
            sectors.Add(new byte[size]);
            fat.Add(0xFFFFFFFD);
        }

        var fatBytes = fat.SelectMany(BitConverter.GetBytes).Concat(Enumerable.Repeat((byte)0xFF, fatSectors * size - fat.Count * 4)).ToArray();
        for (var i = 0; i < fatSectors; i++) Array.Copy(fatBytes, i * size, sectors[firstFat + i], 0, size);
        var header = new byte[size];
        byte[] signature = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];
        signature.CopyTo(header, 0);
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(0x18), 0x3E);
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(0x1A), (ushort)(shift == 9 ? 3 : 4));
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(0x1C), 0xFFFE);
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(0x1E), (ushort)shift);
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(0x20), 6);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(0x2C), fatSectors);
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(0x30), directoryStart);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(0x38), 4096);
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(0x3C), miniFatStart);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(0x40), (miniFat.Count * 4 + size - 1) / size);
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(0x44), 0xFFFFFFFE);
        for (var i = 0; i < 109; i++) BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(0x4C + 4 * i), i < fatSectors ? (uint)(firstFat + i) : 0xFFFFFFFF);
        return [.. header, .. sectors.SelectMany(s => s)];

        uint Chain(byte[] data)
        {
            if (data.Length == 0) return 0xFFFFFFFE;
            var start = (uint)sectors.Count;
            for (var at = 0; at < data.Length; at += size)
            {
                var sector = new byte[size];
                Array.Copy(data, at, sector, 0, Math.Min(size, data.Length - at));
                sectors.Add(sector);
                fat.Add((uint)sectors.Count);
            }

            fat[^1] = 0xFFFFFFFE;
            return start;
        }
    }
}
