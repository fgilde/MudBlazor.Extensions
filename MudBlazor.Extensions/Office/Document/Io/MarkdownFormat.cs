using Markdig;

namespace MudBlazor.Extensions.Office.Document;

internal static class MarkdownFormat
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UsePipeTables().UseEmphasisExtras().UseAutoLinks().Build();

    internal static TextDocument Read(Stream stream)
    {
        var text = TextFormat.Decode(stream);
        string html;
        try
        {
            html = Markdig.Markdown.ToHtml(text, Pipeline);
        }
        catch (ArgumentException)
        {
            return TextFormat.FromText(text);
        }

        return HtmlReader.Read(html);
    }
}
