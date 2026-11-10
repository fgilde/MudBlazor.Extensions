using System.Xml.Linq;
using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Office.Presentation;

/// <summary>A slide master with its theme, text styles, shapes and layouts.</summary>
public sealed class SlideMaster
{
    internal SlideMaster()
    {
    }

    /// <summary>The master name, or null.</summary>
    public string Name { get; internal set; }

    /// <summary>The layouts of this master in file order.</summary>
    public IReadOnlyList<SlideLayout> Layouts => LayoutList;

    /// <summary>The master's shapes, including its placeholders.</summary>
    public IReadOnlyList<SlideShape> Shapes => ShapeList;

    internal List<SlideLayout> LayoutList { get; } = [];

    internal List<SlideShape> ShapeList { get; } = [];

    internal string PartName { get; set; }

    internal string[] Theme { get; set; } = ThemeColors.Office;

    internal IReadOnlyDictionary<string, string> ColorMap { get; set; } = new Dictionary<string, string>();

    internal string MajorFont { get; set; }

    internal string MinorFont { get; set; }

    internal XElement TextStyles { get; set; }

    internal XElement DefaultTextStyle { get; set; }
}
