using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Presentation;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// The Header and Footer dialog of <see cref="MudExSlideEditor"/>: date and time, slide number and footer of the slides. Its result is the chosen
/// <see cref="HeaderFooterOptions"/> and whether they apply to all slides (Apply to All) or only to the selected ones (Apply).
/// </summary>
public partial class MudExSlideHeaderFooterDialog
{
    private bool _showDate;
    private bool _automaticDate = true;
    private string _fixedDate = "";
    private bool _showSlideNumber;
    private bool _showFooter;
    private string _footerText = "";
    private bool _hideOnTitleSlides;

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; }

    /// <summary>The options the dialog starts with, usually those of the current slide.</summary>
    [Parameter] public HeaderFooterOptions Options { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Options is not { } o)
            return;
        (_showDate, _automaticDate, _fixedDate, _showSlideNumber) = (o.ShowDate, o.AutomaticDate, o.FixedDate ?? "", o.ShowSlideNumber);
        (_showFooter, _footerText, _hideOnTitleSlides) = (o.ShowFooter, o.FooterText ?? "", o.HideOnTitleSlides);
    }

    private void Close(bool all) =>
        MudDialog.Close(DialogResult.Ok((new HeaderFooterOptions(_showDate, _automaticDate, _fixedDate ?? "", _showSlideNumber, _showFooter, _footerText ?? "", _hideOnTitleSlides), all)));

    private void Cancel() => MudDialog.Cancel();
}
