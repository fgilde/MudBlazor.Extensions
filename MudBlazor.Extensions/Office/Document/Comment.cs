namespace MudBlazor.Extensions.Office.Document;

/// <summary>A comment of the document.</summary>
/// <param name="Id">The comment id as stored in the file.</param>
/// <param name="Author">The author.</param>
/// <param name="Initials">The author's initials.</param>
/// <param name="Date">When the comment was written, or null.</param>
/// <param name="Text">The comment text, paragraphs separated by newlines.</param>
/// <param name="ParagraphId">The id of the paragraph the comment is anchored in, or null.</param>
public sealed record Comment(string Id, string Author, string Initials, DateTimeOffset? Date, string Text, string ParagraphId);
