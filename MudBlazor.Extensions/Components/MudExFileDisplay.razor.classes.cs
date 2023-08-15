using Nextended.Core;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Plugin information for a content type plugin
/// </summary>
public class BrowserContentTypePlugin
{
    /// <summary>
    /// Names of browsers
    /// </summary>
    public static class BrowserNames
    {
        /// <summary>
        /// Chrome
        /// </summary>
        public const string Chrome = nameof(Chrome);

        /// <summary>
        /// Opera
        /// </summary>
        public const string Opera = nameof(Opera);

        /// <summary>
        /// Edge
        /// </summary>
        public const string Edge = nameof(Edge);

        /// <summary>
        /// Safari
        /// </summary>
        public const string Safari = nameof(Safari);

        /// <summary>
        /// Firefox
        /// </summary>
        public const string Firefox = nameof(Firefox);
    }

    /// <summary>
    /// Name of content type plugin
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// By plugin supported browsers
    /// </summary>
    public string[] SupportedBrowsers { get; set; }

    /// <summary>
    /// Supported content types
    /// </summary>
    public string[] SupportedContentTypes { get; set; }

    /// <summary>
    /// Url to get the plugin
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// Tries to find a plugin for given browser and content type
    /// </summary>
    public static BrowserContentTypePlugin Find(string browser, string contentType)
        => Available.FirstOrDefault(p => p.SupportedBrowsers.Contains(browser, StringComparer.InvariantCultureIgnoreCase) && MimeType.Matches(contentType, p.SupportedContentTypes));

    /// <summary>
    /// Known available plugins
    /// </summary>
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
    /// Note: This stream should not be closed or disposed.
    /// </summary>
    public Stream ContentStream { get; }
}

/// <summary>
/// Represents a result of a file display error in the MudEx framework.
/// This class provides several static methods to create different types of error results.
/// Implements the <see cref="IMudExFileDisplayInfos"/> interface.
/// </summary>
public sealed class MudExFileDisplayContentErrorResult : IMudExFileDisplayInfos
{
    /// <summary>
    /// Returns a new instance of MudExFileDisplayContentErrorResult with IsHandled set to false.
    /// </summary>
    public static MudExFileDisplayContentErrorResult Unhandled => new() { IsHandled = false };

    /// <summary>
    /// Returns a new instance of MudExFileDisplayContentErrorResult with IsHandled set to true.
    /// </summary>
    public static MudExFileDisplayContentErrorResult Handled => new() { IsHandled = true };

    /// <summary>
    /// Returns a new instance of MudExFileDisplayContentErrorResult with a specified message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public static MudExFileDisplayContentErrorResult DisplayMessage(string message) => new MudExFileDisplayContentErrorResult().WithMessage(message);

    /// <summary>
    /// Returns a new instance of MudExFileDisplayContentErrorResult with a redirection to a specific URL and content type.
    /// </summary>
    /// <param name="url">The redirection URL.</param>
    /// <param name="contentType">The content type of the redirection.</param>
    public static MudExFileDisplayContentErrorResult RedirectTo(string url, string contentType = "") => new() { IsHandled = true, ContentType = contentType, Url = url };

    /// <summary>
    /// Returns a new instance of MudExFileDisplayContentErrorResult with a redirection to a stream and content type.
    /// </summary>
    /// <param name="stream">The stream to redirect to.</param>
    /// <param name="contentType">The content type of the stream.</param>
    public static MudExFileDisplayContentErrorResult RedirectTo(Stream stream, string contentType = "") => new() { IsHandled = true, ContentType = contentType, ContentStream = stream };

    /// <summary>
    /// Sets the Message property and returns the current MudExFileDisplayContentErrorResult instance.
    /// </summary>
    /// <param name="message">The error message.</param>
    public MudExFileDisplayContentErrorResult WithMessage(string message)
    {
        IsHandled = false;
        Message = message;
        return this;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the error is handled.
    /// </summary>
    public bool IsHandled { get; set; }

    /// <summary>
    /// Gets or sets the name of the file.
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// Gets or sets the redirection URL.
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// Gets or sets the content type.
    /// </summary>
    public string ContentType { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Gets or sets the content stream for redirection.
    /// Note: This stream should not be closed or disposed.
    /// </summary>
    public Stream ContentStream { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to fall back in iframe.
    /// </summary>
    public bool? FallBackInIframe { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to sandbox iframes.
    /// </summary>
    public bool? SandBoxIframes { get; set; }
}
