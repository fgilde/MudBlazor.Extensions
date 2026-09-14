using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Core.EInvoice;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Services;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Renders an electronic invoice - XRechnung, ZUGFeRD or Factur-X - as the document it describes.
/// </summary>
/// <remarks>
/// Reading, not checking: the viewer shows what the file says and never claims it is a valid invoice.
/// Validating against the EN 16931 rule set is a job for a validator, not for a file display.
/// </remarks>
public partial class MudExFileDisplayEInvoice : IMudExFileDisplay
{
    private object _loadedSource;
    private MudExEInvoice _invoice;
    private MudExEInvoiceValidationResult _validation;
    private int _activeTab;

    // Built once per file: handing a viewer a new infos object on every render restarts it, and pdf.js then
    // paints two renders onto the same canvas.
    private IMudExFileDisplayInfos _pdfInfos;
    private IMudExFileDisplayInfos _xmlInfos;

    // A content stream is read once and is empty afterwards, and the viewer is asked twice: first whether it
    // can handle the file, then to show it. So the bytes of the answer are what gets kept.
    private static object _cachedSource;
    private static byte[] _cachedBytes;

    [Inject] private MudExFileService FileService { get; set; }

    /// <inheritdoc />
    public string Name => nameof(MudExFileDisplayEInvoice);

    /// <summary>
    /// Ahead of the code and pdf views: those can show the file too, but only this one shows the invoice.
    /// </summary>
    public int RenderPriority => 100;

    /// <inheritdoc />
    [Parameter]
    public IMudExFileDisplayInfos FileDisplayInfos { get; set; }

    /// <summary>Reference to the parent MudExFileDisplay when the component is used inside one.</summary>
    [CascadingParameter] public MudExFileDisplay MudExFileDisplay { get; set; }

    /// <inheritdoc />
    public async Task<bool> CanHandleFileAsync(IMudExFileDisplayInfos fileDisplayInfos, IMudExFileService fileService)
    {
        if (!MudExEInvoiceReader.LooksLikeEInvoice(fileDisplayInfos?.FileName, fileDisplayInfos?.ContentType))
            return false;

        // Only the content decides: the extension of an invoice says xml or pdf like any other file of those
        // kinds, so the file has to be read before this viewer may claim it.
        var bytes = await ReadBytesAsync(fileDisplayInfos, fileService);
        return bytes is { Length: > 0 } && MudExEInvoiceReader.Read(bytes) != null;
    }

    /// <inheritdoc />
    public Task<IDictionary<string, object>> FileMetaInformationAsync(IMudExFileDisplayInfos fileDisplayInfos)
    {
        var result = new Dictionary<string, object>();
        if (_invoice == null)
            return Task.FromResult<IDictionary<string, object>>(result);

        Add("Invoice number", _invoice.Number);
        Add("Invoice date", _invoice.IssueDate?.ToShortDateString());
        Add("Due date", _invoice.DueDate?.ToShortDateString());
        Add("Seller", _invoice.Seller?.Name);
        Add("Buyer", _invoice.Buyer?.Name);
        Add("Amount due", Money(_invoice.DuePayable ?? _invoice.GrandTotal));
        Add("Total without tax", Money(_invoice.TaxBasisTotal));
        Add("Tax", Money(_invoice.TaxTotal));
        Add("Line items", _invoice.Lines.Count);
        Add("Syntax", _invoice.Syntax.ToString().ToUpperInvariant());
        Add("Specification", _invoice.Profile);
        Add("Embedded file", _invoice.EmbeddedFileName);
        Add("Buyer reference", _invoice.BuyerReference);

        return Task.FromResult<IDictionary<string, object>>(result);

        void Add(string key, object value)
        {
            if (value is string text ? !string.IsNullOrWhiteSpace(text) : value != null)
                result[TryLocalize(key)] = value;
        }
    }

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        // The same viewer instance serves every file of its kind, and FileDisplayInfos is always the same
        // object - so only the values tell one file from the next.
        parameters.TryGetValue<IMudExFileDisplayInfos>(nameof(FileDisplayInfos), out var infos);
        var sourceChanged = infos.SourceChanged(ref _loadedSource);

        await base.SetParametersAsync(parameters);

        if (!sourceChanged)
            return;

        _invoice = null;
        _validation = null;
        _pdfInfos = null;
        _xmlInfos = null;
        // A new file starts on the invoice again, not on whatever tab the last one was left on.
        _activeTab = 0;
        await LoadAsync();

        // A viewer knows its own values only now, and its renders do not reach the host.
        await FileDisplayInfos.NotifyMetaChangedAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var bytes = await ReadBytesAsync(FileDisplayInfos, FileService);
            _invoice = MudExEInvoiceReader.Read(bytes);

            if (_invoice != null)
                _validation = MudExEInvoiceValidator.Validate(_invoice);

            if (bytes != null && MudExPdfAttachments.IsPdf(bytes))
            {
                var url = FileDisplayInfos?.Url ?? await FileService.ReadDataUrlForStreamAsync(
                    new MemoryStream(bytes), "application/pdf", useBlob: true);
                _pdfInfos = new FileSource(FileDisplayInfos?.FileName, url, "application/pdf");
            }

            if (_invoice?.Xml is { Length: > 0 } xml)
            {
                // A url, not the stream: the code view may load it again whenever it re-renders.
                var url = await FileService.ReadDataUrlForStreamAsync(
                    new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml)), "text/xml", useBlob: true);
                _xmlInfos = new FileSource(_invoice.EmbeddedFileName ?? FileDisplayInfos?.FileName ?? "invoice.xml", url, "text/xml");
            }
        }
        catch (Exception)
        {
            // A file that cannot be read is simply not an invoice - the empty state says so.
            _invoice = null;
        }

        StateHasChanged();
    }

    /// <summary>Points another viewer at one part of this file, without handing it this viewer's infos.</summary>
    private sealed record FileSource(string FileName, string Url, string ContentType) : IMudExFileDisplayInfos
    {
        public Stream ContentStream => null;
    }

    private static async Task<byte[]> ReadBytesAsync(IMudExFileDisplayInfos infos, IMudExFileService fileService)
    {
        if (infos == null || fileService == null)
            return null;

        var source = infos.SourceKey();
        if (_cachedBytes != null && Equals(_cachedSource, source))
            return _cachedBytes;

        try
        {
            var stream = await fileService.ReadStreamAsync(infos);
            if (stream == null)
                return null;

            using var buffer = new MemoryStream();
            if (stream.CanSeek)
                stream.Position = 0;
            await stream.CopyToAsync(buffer);
            if (stream.CanSeek)
                stream.Position = 0;

            _cachedSource = source;
            _cachedBytes = buffer.ToArray();
            return _cachedBytes;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private string Money(decimal? value)
        => value == null ? null : $"{value.Value:N2} {_invoice?.Currency}".TrimEnd();

    private static string Number(decimal? value) => value?.ToString("0.###");

    private static string Percent(decimal? value) => value == null ? null : $"{value.Value:0.##} %";

    private string ValidationSummary() => _validation switch
    {
        null => null,
        { Violations.Count: 0 } => TryLocalize("Core rules passed"),
        { ErrorCount: 0 } v => TryLocalize("{0} warnings", v.WarningCount),
        { WarningCount: 0 } v => TryLocalize("{0} errors", v.ErrorCount),
        var v => TryLocalize("{0} errors, {1} warnings", v.ErrorCount, v.WarningCount)
    };

    private IEnumerable<(string Label, decimal? Amount)> Totals()
    {
        if (_invoice.LineTotal != null)
            yield return (TryLocalize("Net total"), _invoice.LineTotal);

        foreach (var tax in _invoice.Taxes.Where(t => t.TaxAmount != null))
            yield return (TryLocalize("VAT") + (tax.Percent == null ? null : $" {Percent(tax.Percent)}"), tax.TaxAmount);

        if (!_invoice.Taxes.Any() && _invoice.TaxTotal != null)
            yield return (TryLocalize("VAT"), _invoice.TaxTotal);

        if (_invoice.GrandTotal != null)
            yield return (TryLocalize("Gross total"), _invoice.GrandTotal);

        if (_invoice.PrepaidAmount is > 0)
            yield return (TryLocalize("Prepaid"), _invoice.PrepaidAmount);
    }

    private IEnumerable<string> Facts()
    {
        if (_invoice.IssueDate != null)
            yield return $"{TryLocalize("Invoice date")}: {_invoice.IssueDate.Value.ToShortDateString()}";

        if (_invoice.DueDate != null)
            yield return $"{TryLocalize("Due")}: {_invoice.DueDate.Value.ToShortDateString()}";

        if (!string.IsNullOrWhiteSpace(_invoice.BuyerReference))
            yield return $"{TryLocalize("Buyer reference")}: {_invoice.BuyerReference}";

        if (!string.IsNullOrWhiteSpace(_invoice.OrderReference))
            yield return $"{TryLocalize("Order")}: {_invoice.OrderReference}";

        if (!string.IsNullOrWhiteSpace(_invoice.Profile))
            yield return _invoice.Profile;
    }
}
