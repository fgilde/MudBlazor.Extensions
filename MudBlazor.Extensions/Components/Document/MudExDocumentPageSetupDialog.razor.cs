using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Document;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// The page setup dialog of <see cref="MudExDocumentEditor"/>: orientation, paper size and margins in centimetres or inches. Closes with the new
/// <see cref="PageSetup"/>.
/// </summary>
public partial class MudExDocumentPageSetupDialog
{
    internal static readonly IReadOnlyDictionary<string, (int Width, int Height)> Sizes = new Dictionary<string, (int, int)>
    {
        ["A4"] = (11906, 16838), ["Letter"] = (12240, 15840), ["A5"] = (8392, 11906), ["Legal"] = (12240, 20160)
    };

    private PageOrientation _orientation;
    private double[] _values = new double[6];

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }

    /// <summary>The current page setup.</summary>
    [Parameter] public PageSetup Page { get; set; } = PageSetup.A4;

    /// <summary>True to show lengths in centimetres, false for inches.</summary>
    [Parameter] public bool Metric { get; set; } = true;

    private string Unit => DocumentUnits.Unit(Metric);

    private double Step => Metric ? 0.1 : 0.05;

    private double MaxLength => DocumentUnits.FromTwips(DocumentCss.MaxTwips, Metric);

    private string SizeName => Sizes.FirstOrDefault(s => Matches(s.Value)).Key;

    private bool Valid => Twips(0) > 0 && Twips(1) > 0 && Twips(0) - Twips(4) - Twips(5) - Page.Gutter >= 720 && Twips(1) - Twips(2) - Twips(3) >= 720;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        _orientation = Page.Orientation;
        _values = [.. new[] { Page.Width, Page.Height, Page.MarginTop, Page.MarginBottom, Page.MarginLeft, Page.MarginRight }.Select(t => DocumentUnits.FromTwips(t, Metric))];
    }

    private int Twips(int index) => DocumentUnits.ToTwips(_values[index], Metric);

    private bool Matches((int Width, int Height) size)
    {
        var (a, b) = (Math.Min(Twips(0), Twips(1)), Math.Max(Twips(0), Twips(1)));
        return Math.Abs(a - size.Width) <= 20 && Math.Abs(b - size.Height) <= 20;
    }

    private void SetOrientation(PageOrientation orientation)
    {
        _orientation = orientation;
        if ((_values[0] > _values[1]) != (orientation == PageOrientation.Landscape))
            (_values[0], _values[1]) = (_values[1], _values[0]);
    }

    private void SetSize(string name)
    {
        if (!Sizes.TryGetValue(name ?? "", out var size))
            return;
        var (w, h) = (DocumentUnits.FromTwips(size.Width, Metric), DocumentUnits.FromTwips(size.Height, Metric));
        (_values[0], _values[1]) = _orientation == PageOrientation.Landscape ? (h, w) : (w, h);
    }

    private void Ok() => MudDialog.Close(DialogResult.Ok(Page with
    {
        Width = Twips(0), Height = Twips(1), Orientation = Twips(0) > Twips(1) ? PageOrientation.Landscape : PageOrientation.Portrait, MarginTop = Twips(2), MarginBottom = Twips(3), MarginLeft = Twips(4), MarginRight = Twips(5)
    }));

    private void Cancel() => MudDialog.Cancel();
}
