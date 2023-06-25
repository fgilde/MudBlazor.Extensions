using Nextended.Core;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Plugin information for a content type plugin
/// </summary>
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

/// <summary>
/// Interface to register an own file display component
/// </summary>
public interface IMudExFileDisplay
{
    /// <summary>
    /// Name of component
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// If this is true the component will wrap the display inside a div
    /// </summary>
    public bool WrapInMudExFileDisplayDiv => true;

    /// <summary>
    /// FileDisplayInfos for file. Will be dynamically set
    /// Important. The property for FileDisplayInfos from interface IMudExFileDisplayInfos needs to have the [Parameter] attribute
    /// </summary>
    public IMudExFileDisplayInfos FileDisplayInfos { get; set; }

    /// <summary>
    /// Should return true if this component can handle a file with thw given informations
    /// </summary>
    bool CanHandleFile(IMudExFileDisplayInfos fileDisplayInfos);
}

/// <summary>
/// Interface containing infos for a file
/// </summary>
public interface IMudExFileDisplayInfos
{
    /// <summary>
    /// Filename
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Url. (Can also be a data uri)
    /// </summary>
    public string Url { get; }

    /// <summary>
    /// Content type of file
    /// </summary>
    public string ContentType { get; }

    /// <summary>
    /// Stream of file if its already loaded or dynamically created on client
    /// </summary>
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