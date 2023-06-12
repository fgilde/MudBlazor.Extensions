using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Attribute
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class HasDocumentationAttribute: System.Attribute
    {
        public HasDocumentationAttribute(string markdownFile)
        {
            MarkdownFile = markdownFile.EnsureEndsWith(".md");
        }
        public string MarkdownFile { get; set; }
    }
}
