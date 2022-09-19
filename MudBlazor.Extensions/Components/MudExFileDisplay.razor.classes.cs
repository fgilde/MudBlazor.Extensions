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