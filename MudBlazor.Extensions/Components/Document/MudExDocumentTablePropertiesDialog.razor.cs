using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Document;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// The table properties dialog of <see cref="MudExDocumentEditor"/>: alignment and the width of every column in centimetres or inches. Closes with a
/// <c>(TableAlignment? Alignment, List&lt;int&gt; Widths)</c> tuple, the widths in twips.
/// </summary>
public partial class MudExDocumentTablePropertiesDialog
{
    private const int MinTwips = 144;

    private TableAlignment? _alignment;
    private double[] _widths = [];

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }

    /// <summary>The current alignment of the table.</summary>
    [Parameter] public TableAlignment? Alignment { get; set; }

    /// <summary>The current widths of the grid columns in twips.</summary>
    [Parameter] public IReadOnlyList<int> Widths { get; set; }

    /// <summary>True to show lengths in centimetres, false for inches.</summary>
    [Parameter] public bool Metric { get; set; } = true;

    private double MaxLength => DocumentUnits.FromTwips(DocumentCss.MaxTwips, Metric);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        _alignment = Alignment;
        _widths = [.. (Widths ?? []).Select(w => DocumentUnits.FromTwips(w, Metric))];
    }

    private void Ok() => MudDialog.Close(DialogResult.Ok((_alignment, _widths.Select(w => Math.Max(MinTwips, DocumentUnits.ToTwips(w, Metric))).ToList())));

    private void Cancel() => MudDialog.Cancel();
}
