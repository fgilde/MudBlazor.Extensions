using Nextended.Core;

namespace MudBlazor.Extensions.Components;

public class BrowserContentTypePlugin
{
    public static class BrowserNames
    {
        public const string Chrome = nameof(Chrome);
        public const string Opera = nameof(Opera);
        public const string Edge = nameof(Edge);
        public const string Safari = nameof(Safari);
        public const string Firefox = nameof(Firefox);
    }

    public string Name { get; set; }
    public string[] SupportedBrowsers { get; set; }
    public string[] SupportedContentTypes { get; set; }
    public string Url { get; set; }

    public static BrowserContentTypePlugin Find(string browser, string contentType)
        => Available.FirstOrDefault(p => p.SupportedBrowsers.Contains(browser, StringComparer.InvariantCultureIgnoreCase) && MimeType.Matches(contentType, p.SupportedContentTypes));

    public static BrowserContentTypePlugin[] Available = {
        new()
        {
            Name = "Office Editor",
            SupportedBrowsers = new [] {BrowserNames.Chrome, BrowserNames.Edge, BrowserNames.Opera},
            SupportedContentTypes = new [] {
                "application/msword",
                "application/vnd.ms-excel",
                "application/vnd.ms-powerpoint*",
                "application/vnd.openxmlformats-officedocument.*"
            },
            Url = "https://chrome.google.com/webstore/detail/office-editing-for-docs-s/gbkeegbaiigmenfmjfclcdgdpimamgkj"
        }
    };
}

public interface IMudExFileDisplay
{
    public string Name { get; }
    public bool WrapInMudExFileDisplayDiv => true;
    public IMudExFileDisplayInfos FileDisplayInfos { get; set; }
    bool CanHandleFile(IMudExFileDisplayInfos fileDisplayInfos);
}

public interface IMudExFileDisplayInfos
{
    public string FileName { get; }
    public string Url { get; }
    public string ContentType { get; }
    public Stream ContentStream { get; }
}


public sealed class MudExFileDisplayContentErrorResult : IMudExFileDisplayInfos
{
    public static MudExFileDisplayContentErrorResult Unhandled => new() { IsHandled = false };
    public static MudExFileDisplayContentErrorResult Handled => new() { IsHandled = true };
    public static MudExFileDisplayContentErrorResult DisplayMessage(string message) => new MudExFileDisplayContentErrorResult().WithMessage(message);
    public static MudExFileDisplayContentErrorResult RedirectTo(string url, string contentType = "") => new() { IsHandled = true, ContentType = contentType, Url = url };
    public static MudExFileDisplayContentErrorResult RedirectTo(Stream stream, string contentType = "") => new() { IsHandled = true, ContentType = contentType, ContentStream = stream };

    public MudExFileDisplayContentErrorResult WithMessage(string message)
    {
        IsHandled = false;
        Message = message;
        return this;
    }
    public bool IsHandled { get; set; }
    public string FileName { get; set; }
    public string Url { get; set; }
    public string ContentType { get; set; }
    public string Message { get; set; }
    public Stream ContentStream { get; set; }
    public bool? FallBackInIframe { get; set; }
    public bool? SandBoxIframes { get; set; }
}