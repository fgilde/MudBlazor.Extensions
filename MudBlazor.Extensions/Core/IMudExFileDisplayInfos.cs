namespace MudBlazor.Extensions.Core;

public interface IMudExFileDisplayInfos
{
    public string FileName { get; }
    public string Url { get; }
    public string ContentType { get; }
    public Stream ContentStream { get; }
}
