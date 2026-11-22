using System.Text;

namespace MudBlazor.Extensions.Office;

internal enum PastedList { None, Bullet, Number }

internal readonly record struct PastedFormat(bool Bold, bool Italic, bool Underline, string Color, string Link)
{
    public bool Strike { get; init; }

    public double? SizePt { get; init; }

    public string Font { get; init; }

    public string Highlight { get; init; }

    public string VerticalAlign { get; init; }
}

internal sealed class PastedRun(string text, PastedFormat format)
{
    private readonly StringBuilder _text = new(text);

    public string Text
    {
        get => _text.ToString();
        set => _text.Clear().Append(value);
    }

    public PastedFormat Format { get; } = format;

    public byte[] ImageData { get; init; }

    public string ImageType { get; init; }

    public string Alt { get; init; }

    public int? Width { get; init; }

    public int? Height { get; init; }

    internal int Length => _text.Length;

    internal void Append(string text) => _text.Append(text);
}

internal abstract class PastedBlock;

internal sealed class PastedParagraph : PastedBlock
{
    public List<PastedRun> Runs { get; } = [];

    public PastedList List { get; set; }

    public int Depth { get; set; }

    public int Heading { get; set; }

    public string Align { get; set; }

    public bool Preformatted { get; set; }
}

internal sealed class PastedTable : PastedBlock
{
    public List<List<PastedCell>> Rows { get; } = [];
}

internal sealed class PastedCell
{
    public List<PastedBlock> Blocks { get; } = [];

    public int ColSpan { get; init; } = 1;

    public int RowSpan { get; init; } = 1;
}
