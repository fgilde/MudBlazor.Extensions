using System.Globalization;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Office.Notebook;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Shows the paragraphs of a notebook text element or table cell. Text is rendered as text nodes, never as markup; list markers and check boxes
/// are separate elements that cannot be edited, and only <c>http</c>, <c>https</c> and <c>mailto</c> links become anchors.
/// </summary>
public partial class MudExNotebookText
{
    private const string CheckedBox = "☑";
    private const string EmptyBox = "☐";

    /// <summary>The paragraphs to show.</summary>
    [Parameter] public IReadOnlyList<NoteParagraph> Paragraphs { get; set; } = [];

    private static string HeadingLevel(NoteParagraph paragraph) =>
        paragraph.Heading == NoteHeading.None ? null : ((int)paragraph.Heading).ToString(CultureInfo.InvariantCulture);
}
