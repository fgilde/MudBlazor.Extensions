using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Attribute
{
    /// <summary>
    /// Attribute to mark and link a documentation for a class
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class HasDocumentationAttribute: System.Attribute
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="markdownFile"></param>
        public HasDocumentationAttribute(string markdownFile)
        {
            MarkdownFile = markdownFile.EnsureEndsWith(".md");
        }

        /// <summary>
        /// Documentation file
        /// </summary>
        public string MarkdownFile { get; set; }
    }
}
