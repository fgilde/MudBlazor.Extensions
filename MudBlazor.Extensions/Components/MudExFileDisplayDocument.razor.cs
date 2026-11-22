using System.Text;
using BlazorJS;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Helper.Internal;
using MudBlazor.Extensions.Services;
using Nextended.Core;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Document file viewer for DOCX, RTF, MSG, EML and MHTML files.
/// Uses docx-preview for DOCX, RtfPipe for RTF, MsgReader for MSG (Outlook) and a lightweight MIME parser for EML/MHTML files.
/// </summary>
public partial class MudExFileDisplayDocument : IMudExFileDisplay
{
    private enum DocFormat
    {
        Docx,
        Rtf,
        Msg,
        Eml,
        Mhtml,
        Unsupported
    }

    private readonly string _containerId = $"docview-{Guid.NewGuid().ToFormattedId()}";
    private bool _isLoading;
    private string _errorMessage;
    private bool _isUnsupported;
    private byte[] _pendingDocxBytes;
    private string _pendingHtmlContent;
    private List<MailAttachment> _attachments = new();
    private MailAttachment _openAttachment;
    private Stream _openAttachmentStream;

    private sealed record MailAttachment(string Name, string ContentType, byte[] Data);

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
            "text/rtf",
            "application/rtf",
            "application/vnd.ms-outlook",
            "message/rfc822",
            "application/x-mimearchive",
            "multipart/related");

        var extensionMatch = !string.IsNullOrEmpty(fileName) && (
            fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".rtf", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".msg", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".eml", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".mht", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".mhtml", StringComparison.OrdinalIgnoreCase));

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
        if (JsReference == null)
            return;

        if (_pendingDocxBytes != null)
        {
            await RenderDocxInternalAsync(_pendingDocxBytes);
            _pendingDocxBytes = null;
        }
        else if (_pendingHtmlContent != null)
        {
            await RenderHtmlInternalAsync(_pendingHtmlContent);
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
                _attachments = new();
                CloseAttachment();
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
                    case DocFormat.Docx:
                        await HandleDocxAsync(bytes);
                        break;
                    case DocFormat.Rtf:
                        await HandleRtfAsync(bytes);
                        break;
                    case DocFormat.Msg:
                        await HandleMsgAsync(bytes);
                        break;
                    case DocFormat.Eml:
                        await HandleEmlAsync(bytes);
                        break;
                    case DocFormat.Mhtml:
                        await HandleMhtmlAsync(bytes);
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

    private static DocFormat DetectFormat(IMudExFileDisplayInfos fileInfos, byte[] bytes)
    {
        var contentType = fileInfos?.ContentType ?? string.Empty;
        var fileName = fileInfos?.FileName ?? string.Empty;

        if (contentType.Contains("x-mimearchive", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".mht", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".mhtml", StringComparison.OrdinalIgnoreCase))
            return DocFormat.Mhtml;

        // MSG detection (MIME or extension)
        if (contentType.Contains("vnd.ms-outlook", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".msg", StringComparison.OrdinalIgnoreCase))
            return DocFormat.Msg;

        // EML detection (MIME or extension)
        if (contentType.Contains("rfc822", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".eml", StringComparison.OrdinalIgnoreCase))
            return DocFormat.Eml;

        // RTF detection (MIME or extension)
        if (contentType.Contains("rtf", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".rtf", StringComparison.OrdinalIgnoreCase))
            return DocFormat.Rtf;

        // Magic bytes: ZIP/PK header → DOCX
        if (bytes.Length >= 2 && bytes[0] == 0x50 && bytes[1] == 0x4B)
            return DocFormat.Docx;

        // MIME/Extension fallback for docx
        if (contentType.Contains("wordprocessingml", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            return DocFormat.Docx;

        return DocFormat.Unsupported;
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
            await RenderHtmlInternalAsync(html);
        }
        else
        {
            _pendingHtmlContent = html;
        }
    }

    private async Task HandleEmlAsync(byte[] bytes)
    {
        // Latin1 keeps every byte intact through the string, so MimeMessage can decode each part with the
        // charset that part declares (a UTF-8 read here would already have destroyed non ASCII bytes).
        var text = Encoding.Latin1.GetString(bytes);
        var message = MimeMessage.Parse(text);

        var html = message.HtmlBody ?? (message.TextBody != null ? $"<pre>{System.Net.WebUtility.HtmlEncode(message.TextBody)}</pre>" : "<p>No message body available.</p>");
        html = message.ResolveCidImages(html);
        html = message.PrependHeaders(html);
        _attachments = message.Attachments.Select(a => new MailAttachment(a.Name, a.MimeType, a.Data)).ToList();

        if (JsReference != null)
        {
            await RenderHtmlInternalAsync(html);
        }
        else
        {
            _pendingHtmlContent = html;
        }
    }

    private async Task HandleMhtmlAsync(byte[] bytes)
    {
        // MHTML is a multipart/related MIME message. Reusing the mail parser also gives us charset,
        // quoted-printable/base64 and Content-Location/CID handling without executing document scripts.
        var message = MimeMessage.Parse(Encoding.Latin1.GetString(bytes));
        var html = message.HtmlBody ?? (message.TextBody != null
            ? $"<pre>{System.Net.WebUtility.HtmlEncode(message.TextBody)}</pre>"
            : "<p>No document body available.</p>");
        html = message.ResolveCidImages(html);

        if (JsReference != null)
            await RenderHtmlInternalAsync(html);
        else
            _pendingHtmlContent = html;
    }

    private async Task HandleMsgAsync(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var msg = new MsgReader.Outlook.Storage.Message(stream);

        var html = ExtractMsgHtml(msg);
        _attachments = ReadMsgAttachments(msg, html);
        html = ResolveCidImages(html, msg);
        html = PrependEmailHeaders(html, msg);

        if (JsReference != null)
        {
            await RenderHtmlInternalAsync(html);
        }
        else
        {
            _pendingHtmlContent = html;
        }
    }

    // Outlook gives every attachment a content id, so only the ones the body really references count as inline images.
    private static List<MailAttachment> ReadMsgAttachments(MsgReader.Outlook.Storage.Message msg, string html)
    {
        var result = new List<MailAttachment>();
        foreach (var attachment in msg.Attachments ?? new List<object>())
        {
            if (attachment is MsgReader.Outlook.Storage.Attachment file)
            {
                var inline = !string.IsNullOrEmpty(file.ContentId) && html?.Contains($"cid:{file.ContentId}", StringComparison.OrdinalIgnoreCase) == true;
                if (file.Data != null && !file.Hidden && !inline)
                    result.Add(new MailAttachment(file.FileName, file.MimeType ?? MimeType.GetMimeType(file.FileName), file.Data));
            }
            else if (attachment is MsgReader.Outlook.Storage.Message mail)
            {
                using var stream = new MemoryStream();
                mail.Save(stream);
                var name = string.IsNullOrWhiteSpace(mail.FileName) ? $"{mail.Subject}.msg" : mail.FileName;
                result.Add(new MailAttachment(name.EndsWith(".msg", StringComparison.OrdinalIgnoreCase) ? name : name + ".msg", "application/vnd.ms-outlook", stream.ToArray()));
            }
        }

        return result;
    }

    // Same hand-over as MudExFileDisplayZip: the attachment viewer gets the settings of the surrounding display,
    // and the shared ElementId lets it place its menu into that display's toolbar.
    private Dictionary<string, object> AttachmentParameters()
    {
        var parameters = new Dictionary<string, object>
        {
            [nameof(MudExFileDisplay.FileName)] = _openAttachment.Name,
            [nameof(MudExFileDisplay.ContentType)] = _openAttachment.ContentType,
            [nameof(MudExFileDisplay.ContentStream)] = _openAttachmentStream,
            [nameof(MudExFileDisplay.Localizer)] = Localizer,
            [nameof(MudExFileDisplay.CanClose)] = true,
            [nameof(MudExFileDisplay.OnCloseClick)] = EventCallback.Factory.Create(this, CloseAttachment)
        };
        if (MudExFileDisplay is not { } parent)
            return parameters;

        parameters[nameof(parent.ElementId)] = parent.ElementId;
        parameters[nameof(parent.FallBackInIframe)] = parent.FallBackInIframe;
        parameters[nameof(parent.ShowContentError)] = parent.ShowContentError;
        parameters[nameof(parent.SandBoxIframes)] = parent.SandBoxIframes;
        parameters[nameof(parent.ImageAsBackgroundImage)] = parent.ImageAsBackgroundImage;
        parameters[nameof(parent.ViewDependsOnContentType)] = parent.ViewDependsOnContentType;
        parameters[nameof(parent.HandleContentErrorFunc)] = parent.HandleContentErrorFunc;
        parameters[nameof(parent.CustomContentErrorMessage)] = parent.CustomContentErrorMessage;
        parameters[nameof(parent.StreamUrlHandling)] = parent.StreamUrlHandling;
        parameters[nameof(parent.ColorizeIcons)] = parent.ColorizeIcons;
        parameters[nameof(parent.IconColor)] = parent.IconColor;
        parameters[nameof(parent.Dense)] = parent.Dense;
        parameters[nameof(parent.ForceNativeRender)] = parent.ForceNativeRender;
        parameters[nameof(parent.IgnoredRenderControls)] = parent.IgnoredRenderControls;
        parameters[nameof(parent.ParametersForSubControls)] = parent.ParametersForSubControls;
        parameters[nameof(parent.AllowDownload)] = parent.AllowDownload;
        parameters[nameof(parent.ShowFileMeta)] = parent.ShowFileMeta;
        parameters[nameof(parent.FileMetaPlacement)] = parent.FileMetaPlacement;
        return parameters;
    }

    private void OpenAttachment(MailAttachment attachment)
    {
        CloseAttachment();
        _openAttachmentStream = new MemoryStream(attachment.Data);
        _openAttachment = attachment;
    }

    private void CloseAttachment()
    {
        _openAttachment = null;
        _openAttachmentStream?.Dispose();
        _openAttachmentStream = null;
    }

    private static string ExtractMsgHtml(MsgReader.Outlook.Storage.Message msg)
    {
        if (!string.IsNullOrWhiteSpace(msg.BodyHtml))
            return msg.BodyHtml;

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

    private async Task RenderHtmlInternalAsync(string html)
    {
        try
        {
            await JsReference.InvokeVoidAsync("renderHtml", html);
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
        // the js init can fail after another file replaced this viewer; its error must not land on the next file
        if (IsDisposed)
            return;
        _isLoading = false;
        _errorMessage = message;
        MudExFileDisplay?.ShowError(message);
        CallStateHasChanged();
    }
}
