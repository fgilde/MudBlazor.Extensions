namespace MudBlazor.Extensions.Core;

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