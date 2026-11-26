namespace MudBlazor.Extensions.Office.Document;

/// <summary>A file format a <see cref="TextDocument"/> can be read from or written to.</summary>
public enum DocumentFileFormat
{
    /// <summary>Word document (.docx); a macro-enabled source is written as a macro-enabled document (.docm), see <see cref="TextDocument.HasMacros"/>.</summary>
    Docx,

    /// <summary>Word template (.dotx); a macro-enabled source is written as a macro-enabled template (.dotm), see <see cref="TextDocument.HasMacros"/>.</summary>
    Dotx,

    /// <summary>Rich Text Format (.rtf).</summary>
    Rtf,

    /// <summary>HTML (.html).</summary>
    Html,

    /// <summary>Plain text (.txt).</summary>
    Text,

    /// <summary>Markdown (.md).</summary>
    Markdown,

    /// <summary>OpenDocument text (.odt).</summary>
    Odt,

    /// <summary>Word 97-2003 document (.doc).</summary>
    Doc
}
