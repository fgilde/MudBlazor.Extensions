using System.Text;
using BlazorJS;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Services;
using Nextended.Core;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Document file viewer for DOCX, RTF and MSG files.
/// Uses docx-preview (CDN) for DOCX, RtfPipe for RTF, and MsgReader for MSG files.
/// </summary>
public partial class MudExFileDisplayDocument : IMudExFileDisplay
{
    private enum DocumentFormat
    {
        Docx,
        Rtf,
        Msg,
        Unsupported
    }

    private readonly string _containerId = $"docview-{Guid.NewGuid().ToFormattedId()}";
    private bool _isLoading;
    private string _errorMessage;
    private bool _isUnsupported;
    private byte[] _pendingDocxBytes;
    private string _pendingHtmlContent;
    private bool _pendingHtmlApplyTheme;

    [Inject] private MudExFileService FileService { get; set; }

    /// <summary>
    /// The name of the component
    /// </summary>
    public string Name => "Document Viewer";

    /// <summary>
    /// This viewer starts active by default
    /// </summary>
    public bool StartsActive => true;

    /// <summary>
    /// The file display infos
    /// </summary>
    [Parameter]
    public IMudExFileDisplayInfos FileDisplayInfos { get; set; }

    /// <summary>
    /// Reference to the parent MudExFileDisplay if the component is used inside a MudExFileDisplay
    /// </summary>
    [CascadingParameter] public MudExFileDisplay MudExFileDisplay { get; set; }

    /// <inheritdoc />
    public Task<bool> CanHandleFileAsync(IMudExFileDisplayInfos fileDisplayInfos, IMudExFileService fileService)
    {
        var contentType = fileDisplayInfos?.ContentType;
        var fileName = fileDisplayInfos?.FileName;

        var mimeMatch = MimeType.Matches(contentType,
            "application/vnd.openxmlformats-officedocument.wordprocessingml*",
            "application/msword",
            "text/rtf",
            "application/rtf",
            "application/vnd.ms-outlook");

        var extensionMatch = !string.IsNullOrEmpty(fileName) && (
            fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".doc", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".rtf", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".msg", StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(mimeMatch || extensionMatch);
    }

    /// <inheritdoc />
    public Task<IDictionary<string, object>> FileMetaInformationAsync(IMudExFileDisplayInfos fileDisplayInfos)
        => Task.FromResult<IDictionary<string, object>>(null);

    /// <inheritdoc />
    public override object[] GetJsArguments() => new object[] { ElementReference, CreateDotNetObjectReference(), _containerId };

    /// <inheritdoc />
    public override async Task ImportModuleAndCreateJsAsync()
    {
        await JsRuntime.InvokeVoidAsync("eval", new object[] { "window.__mudex_saved_define = window.define; window.define = undefined;" });

        try
        {
            await JsRuntime.LoadFilesAsync(
                "https://cdn.jsdelivr.net/npm/jszip@3.10.1/dist/jszip.min.js"
            );
            await JsRuntime.WaitForNamespaceAsync("JSZip", TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(200));

            await JsRuntime.LoadFilesAsync(
                "https://cdn.jsdelivr.net/npm/docx-preview@0.3.3/dist/docx-preview.min.js"
            );
            await JsRuntime.WaitForNamespaceAsync("docx", TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(200));
        }
        finally
        {
            await JsRuntime.InvokeVoidAsync("eval", new object[] { "window.define = window.__mudex_saved_define; delete window.__mudex_saved_define;" });
        }

        await base.ImportModuleAndCreateJsAsync();

        if (_pendingDocxBytes != null)
        {
            await RenderDocxInternalAsync(_pendingDocxBytes);
            _pendingDocxBytes = null;
        }
        else if (_pendingHtmlContent != null)
        {
            await RenderHtmlInternalAsync(_pendingHtmlContent, _pendingHtmlApplyTheme);
            _pendingHtmlContent = null;
        }
    }

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var fileInfosUpdated = parameters.TryGetValue<IMudExFileDisplayInfos>(nameof(FileDisplayInfos), out var fileDisplayInfos) && FileDisplayInfos != fileDisplayInfos;
        await base.SetParametersAsync(parameters);

        if (fileInfosUpdated && fileDisplayInfos != null)
        {
            try
            {
                _errorMessage = null;
                _isUnsupported = false;
                _isLoading = true;
                StateHasChanged();

                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                var stream = await FileService.ReadStreamAsync(fileDisplayInfos);
                if (stream == null)
                    throw new ArgumentException("No stream and no url available");

                var bytes = stream.ToByteArray();
                var format = DetectFormat(fileDisplayInfos, bytes);

                switch (format)
                {
                    case DocumentFormat.Docx:
                        await HandleDocxAsync(bytes);
                        break;
                    case DocumentFormat.Rtf:
                        await HandleRtfAsync(bytes);
                        break;
                    case DocumentFormat.Msg:
                        await HandleMsgAsync(bytes);
                        break;
                    default:
                        _isLoading = false;
                        _isUnsupported = true;
                        StateHasChanged();
                        break;
                }
            }
            catch (Exception e)
            {
                _isLoading = false;
                _errorMessage = e.Message;
                MudExFileDisplay?.ShowError(e.Message);
                Console.WriteLine(e);
                StateHasChanged();
            }
        }
    }

    private static DocumentFormat DetectFormat(IMudExFileDisplayInfos fileInfos, byte[] bytes)
    {
        var contentType = fileInfos?.ContentType ?? string.Empty;
        var fileName = fileInfos?.FileName ?? string.Empty;

        // MSG detection (MIME or extension)
        if (contentType.Contains("vnd.ms-outlook", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".msg", StringComparison.OrdinalIgnoreCase))
            return DocumentFormat.Msg;

        // RTF detection (MIME or extension)
        if (contentType.Contains("rtf", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".rtf", StringComparison.OrdinalIgnoreCase))
            return DocumentFormat.Rtf;

        // Magic bytes: ZIP/PK header → DOCX (even if extension is .doc)
        if (bytes.Length >= 2 && bytes[0] == 0x50 && bytes[1] == 0x4B)
            return DocumentFormat.Docx;

        // Magic bytes: OLE2 compound document → unsupported legacy .doc
        if (bytes.Length >= 4 && bytes[0] == 0xD0 && bytes[1] == 0xCF && bytes[2] == 0x11 && bytes[3] == 0xE0)
            return DocumentFormat.Unsupported;

        // MIME/Extension fallback for docx
        if (contentType.Contains("wordprocessingml", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            return DocumentFormat.Docx;

        return DocumentFormat.Unsupported;
    }

    private async Task HandleDocxAsync(byte[] bytes)
    {
        if (JsReference != null)
        {
            await RenderDocxInternalAsync(bytes);
        }
        else
        {
            _pendingDocxBytes = bytes;
        }
    }

    private async Task HandleRtfAsync(byte[] bytes)
    {
        var rtfString = Encoding.UTF8.GetString(bytes);

        // Try UTF-8 first; if it doesn't look like valid RTF, try default encoding
        if (!rtfString.TrimStart().StartsWith("{\\rtf", StringComparison.Ordinal))
            rtfString = Encoding.Default.GetString(bytes);

        string html;
        using (var reader = new StringReader(rtfString))
        {
            html = RtfPipe.Rtf.ToHtml(new RtfPipe.RtfSource(reader));
        }

        if (JsReference != null)
        {
            await RenderHtmlInternalAsync(html, false);
        }
        else
        {
            _pendingHtmlContent = html;
            _pendingHtmlApplyTheme = false;
        }
    }

    private async Task HandleMsgAsync(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var msg = new MsgReader.Outlook.Storage.Message(stream);

        var html = ExtractMsgHtml(msg);
        html = ResolveCidImages(html, msg);
        html = PrependEmailHeaders(html, msg);

        if (JsReference != null)
        {
            await RenderHtmlInternalAsync(html, true);
        }
        else
        {
            _pendingHtmlContent = html;
            _pendingHtmlApplyTheme = true;
        }
    }

    private static string ExtractMsgHtml(MsgReader.Outlook.Storage.Message msg)
    {
        // Prefer HTML body
        if (!string.IsNullOrWhiteSpace(msg.BodyHtml))
            return msg.BodyHtml;

        // Fall back to RTF body converted to HTML
        if (!string.IsNullOrWhiteSpace(msg.BodyRtf))
        {
            try
            {
                using var reader = new StringReader(msg.BodyRtf);
                return RtfPipe.Rtf.ToHtml(new RtfPipe.RtfSource(reader));
            }
            catch
            {
                // If RTF conversion fails, fall through to plain text
            }
        }

        // Fall back to plain text
        if (!string.IsNullOrWhiteSpace(msg.BodyText))
            return $"<pre>{System.Net.WebUtility.HtmlEncode(msg.BodyText)}</pre>";

        return "<p>No message body available.</p>";
    }

    private static string ResolveCidImages(string html, MsgReader.Outlook.Storage.Message msg)
    {
        if (string.IsNullOrEmpty(html) || msg.Attachments == null)
            return html;

        foreach (var attachment in msg.Attachments)
        {
            if (attachment is not MsgReader.Outlook.Storage.Attachment att)
                continue;
            if (string.IsNullOrEmpty(att.ContentId) || att.Data == null)
                continue;

            var mimeType = att.MimeType ?? "application/octet-stream";
            var dataUri = $"data:{mimeType};base64,{Convert.ToBase64String(att.Data)}";
            html = html.Replace($"cid:{att.ContentId}", dataUri, StringComparison.OrdinalIgnoreCase);
        }

        return html;
    }

    private static string PrependEmailHeaders(string html, MsgReader.Outlook.Storage.Message msg)
    {
        var headers = new StringBuilder();
        headers.Append("<div style=\"border-bottom: 1px solid #ccc; padding-bottom: 12px; margin-bottom: 16px; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;\">");

        if (!string.IsNullOrWhiteSpace(msg.Sender?.Email))
            headers.Append($"<div><strong>From:</strong> {System.Net.WebUtility.HtmlEncode(msg.Sender.Email)}</div>");

        var toRecipients = msg.GetEmailRecipients(MsgReader.Outlook.RecipientType.To, false, false);
        if (!string.IsNullOrWhiteSpace(toRecipients))
            headers.Append($"<div><strong>To:</strong> {System.Net.WebUtility.HtmlEncode(toRecipients)}</div>");

        var ccRecipients = msg.GetEmailRecipients(MsgReader.Outlook.RecipientType.Cc, false, false);
        if (!string.IsNullOrWhiteSpace(ccRecipients))
            headers.Append($"<div><strong>CC:</strong> {System.Net.WebUtility.HtmlEncode(ccRecipients)}</div>");

        if (!string.IsNullOrWhiteSpace(msg.Subject))
            headers.Append($"<div><strong>Subject:</strong> {System.Net.WebUtility.HtmlEncode(msg.Subject)}</div>");

        if (msg.SentOn.HasValue)
            headers.Append($"<div><strong>Sent:</strong> {msg.SentOn.Value:yyyy-MM-dd HH:mm}</div>");

        headers.Append("</div>");

        return headers + html;
    }

    private async Task RenderDocxInternalAsync(byte[] bytes)
    {
        try
        {
            await JsReference.InvokeVoidAsync("renderDocx", bytes);
        }
        catch (Exception e)
        {
            _isLoading = false;
            _errorMessage = e.Message;
            MudExFileDisplay?.ShowError(e.Message);
            Console.WriteLine(e);
            StateHasChanged();
        }
    }

    private async Task RenderHtmlInternalAsync(string html, bool applyTheme)
    {
        try
        {
            var themeColors = applyTheme ? await GetThemeColorsAsync() : null;
            await JsReference.InvokeVoidAsync("renderHtml", html, themeColors);
        }
        catch (Exception e)
        {
            _isLoading = false;
            _errorMessage = e.Message;
            MudExFileDisplay?.ShowError(e.Message);
            Console.WriteLine(e);
            StateHasChanged();
        }
    }

    private async Task<object> GetThemeColorsAsync()
    {
        try
        {
            var primary = await GetCssVariableAsync("--mud-palette-primary");
            var surface = await GetCssVariableAsync("--mud-palette-surface");
            var background = await GetCssVariableAsync("--mud-palette-background");
            var textPrimary = await GetCssVariableAsync("--mud-palette-text-primary");
            var lines = await GetCssVariableAsync("--mud-palette-lines-default");

            return new { primary, surface, background, textPrimary, lines };
        }
        catch
        {
            return null;
        }
    }

    private async Task<string> GetCssVariableAsync(string variableName)
    {
        return await JsRuntime.InvokeAsync<string>("eval",
            new object[] { $"getComputedStyle(document.documentElement).getPropertyValue('{variableName}').trim()" });
    }

    /// <summary>
    /// Called from JS when the document is rendered successfully
    /// </summary>
    [JSInvokable]
    public void OnDocumentRendered()
    {
        _isLoading = false;
        _errorMessage = null;
        CallStateHasChanged();
    }

    /// <summary>
    /// Called from JS when an error occurs
    /// </summary>
    [JSInvokable]
    public void OnError(string message)
    {
        _isLoading = false;
        _errorMessage = message;
        MudExFileDisplay?.ShowError(message);
        CallStateHasChanged();
    }
}
