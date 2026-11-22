namespace MudBlazor.Extensions.Office.Document;

/// <summary>The core properties of a document; null values are not set.</summary>
/// <param name="Title">The title.</param>
/// <param name="Subject">The subject.</param>
/// <param name="Author">The author.</param>
/// <param name="Keywords">The keywords.</param>
/// <param name="Description">The description.</param>
/// <param name="LastModifiedBy">Who saved the document last.</param>
/// <param name="Created">When the document was created.</param>
/// <param name="Modified">When the document was saved last.</param>
public sealed record DocumentProperties(string Title, string Subject, string Author, string Keywords, string Description, string LastModifiedBy,
    DateTimeOffset? Created, DateTimeOffset? Modified);
