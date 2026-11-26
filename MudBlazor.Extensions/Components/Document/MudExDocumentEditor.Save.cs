using MudBlazor.Extensions.Components.Base;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Office.Document;

namespace MudBlazor.Extensions.Components;

public partial class MudExDocumentEditor
{
    private static readonly MudExFileFormat Docx = FileFormat("Word Document", DocumentFileFormat.Docx);
    private static readonly MudExFileFormat Docm = new("Word Macro-Enabled Document", "docm", "application/vnd.ms-word.document.macroEnabled.12");
    private static readonly MudExFileFormat Dotx = FileFormat("Word Template", DocumentFileFormat.Dotx);
    private static readonly MudExFileFormat Dotm = new("Word Macro-Enabled Template", "dotm", "application/vnd.ms-word.template.macroEnabled.12");
    private static readonly MudExFileFormat Rtf = FileFormat("Rich Text Format", DocumentFileFormat.Rtf);
    private static readonly MudExFileFormat Html = FileFormat("Web Page", DocumentFileFormat.Html);
    private static readonly MudExFileFormat Markdown = FileFormat("Markdown", DocumentFileFormat.Markdown);
    private static readonly MudExFileFormat Text = FileFormat("Plain Text", DocumentFileFormat.Text);
    private static readonly MudExFileFormat[] Writable = [Docx, Docm, Dotx, Dotm, Rtf, Html, Html with { Extension = "htm" }, Markdown, Markdown with { Extension = "markdown" }, Text];
    private static readonly MudExFileFormat[] Readable =
        [Docx, Docm, Dotx, Dotm, FileFormat("Word 97-2003 Document", DocumentFileFormat.Doc), Rtf, FileFormat("OpenDocument Text", DocumentFileFormat.Odt), .. Writable[5..]];

    private readonly HashSet<DocumentFileFormat> _kept = [];
    private (TextDocument Document, DocumentFileFormat Format)? _pendingKeep;
    private DocumentFileFormat? _loaded;
    private string _loadedExtension;

    /// <summary>
    /// The formats the document can be saved in: the format it was loaded from if it can be written (a macro-enabled document or template keeps its
    /// type), else Word Document; then the remaining of Word Document, Rich Text Format, Web Page, Markdown and Plain Text.
    /// </summary>
    public override IReadOnlyList<MudExFileFormat> SaveFormats
    {
        get
        {
            var word = _document?.HasMacros == true ? Docm : Docx;
            var first = Original ?? word;
            return [first, .. new[] { word, Rtf, Html, Markdown, Text }.Where(f => Kind(f) != Kind(first))];
        }
    }

    /// <inheritdoc />
    protected override IReadOnlyList<MudExFileFormat> OpenFormats => Readable;

    private MudExFileFormat Original => _loaded switch
    {
        null or DocumentFileFormat.Docx or DocumentFileFormat.Doc or DocumentFileFormat.Odt => null,
        DocumentFileFormat.Dotx => _document?.HasMacros == true ? Dotm : Dotx,
        var format => Writable.FirstOrDefault(f => f.Extension == _loadedExtension && Kind(f) == format) ?? Writable.First(f => Kind(f) == format)
    };

    /// <inheritdoc />
    protected override MudExFileContent CreateNew()
    {
        var stream = new MemoryStream();
        Office.Document.TextDocument.Create().Save(stream);
        stream.Position = 0;
        return new() { FileName = "Document1.docx", ContentType = Docx.ContentType, Stream = stream };
    }

    /// <inheritdoc />
    protected override async Task LoadContentAsync(MudExFileContent content)
    {
        Attach(null, null, null);
        var source = content.Stream ?? Stream.Null;
        using var copy = source is MemoryStream { CanSeek: true } ? null : new MemoryStream();
        if (copy != null)
        {
            await source.CopyToAsync(copy);
            copy.Position = 0;
            source = copy;
        }

        if (source.Position >= source.Length)
            throw new InvalidDataException(TryLocalize("The file is empty."));
        var format = DocumentFileFormats.DetectContent(source, content.FileName, content.ContentType)
                     ?? throw new InvalidDataException(TryLocalize("The file is not a supported document."));
        var document = Office.Document.TextDocument.Load(source, format);
        Attach(document, format, Path.GetExtension(content.FileName ?? "").TrimStart('.').ToLowerInvariant());
        await TextDocumentChanged.InvokeAsync(document);
    }

    /// <inheritdoc />
    protected override async Task FlushPendingEditsAsync()
    {
        if (_surface != null)
            await _surface.FlushAsync();
        await _changeNotification;
    }

    /// <summary>
    /// Exports the document. A format that cannot keep everything the document uses (see <see cref="DocumentExport.Losses"/>) asks once per format
    /// and document whether to save as a Word document instead; the choice to keep the format is remembered after the save succeeded.
    /// </summary>
    /// <param name="format">Target format, or null for the first of <see cref="SaveFormats"/></param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The exported content</returns>
    /// <exception cref="InvalidOperationException">No document is loaded.</exception>
    /// <exception cref="OperationCanceledException">The token was cancelled.</exception>
    public override async Task<MudExFileContent> ExportAsync(MudExFileFormat format = null, CancellationToken cancellationToken = default)
    {
        var document = _document ?? throw new InvalidOperationException(TryLocalize("There is no document to export."));
        cancellationToken.ThrowIfCancellationRequested();
        await FlushPendingEditsAsync();
        format ??= SaveFormats[0];
        var target = Kind(format);
        _pendingKeep = null;
        if (target is not (DocumentFileFormat.Docx or DocumentFileFormat.Dotx) && !_kept.Contains(target) &&
            DocumentExport.Losses(document, target) is { Count: > 0 } losses)
            (format, target) = await ConfirmLossesAsync(document, format, target, losses);

        cancellationToken.ThrowIfCancellationRequested();
        var stream = new MemoryStream();
        document.Save(stream, target);
        stream.Position = 0;
        return new MudExFileContent
        {
            FileName = Path.ChangeExtension(string.IsNullOrEmpty(FileName) ? "Document1.docx" : FileName, format.Extension),
            ContentType = format.ContentType,
            Stream = stream
        };
    }

    /// <inheritdoc />
    protected override void OnSaveSucceeded()
    {
        if (_pendingKeep is ({ } document, var format) && ReferenceEquals(document, _document))
            _kept.Add(format);
        _pendingKeep = null;
    }

    private async Task<(MudExFileFormat Format, DocumentFileFormat Target)> ConfirmLossesAsync(TextDocument document, MudExFileFormat format, DocumentFileFormat target,
        IReadOnlyList<DocumentFeature> losses)
    {
        var message = TryLocalize("{0} cannot keep everything in this document: {1}.", format.Name, string.Join(", ", losses.Select(f => TryLocalize(FeatureName(f)))));
        if (DialogService == null)
        {
            MudExFileDisplay?.ShowError(message);
            _pendingKeep = (document, target);
            return (format, target);
        }

        var word = document.HasMacros ? Docm : Docx;
        var answer = await DialogService.ShowMessageBoxAsync(TryLocalize("Save as {0}", format.Name), message + " " + TryLocalize("Save it as a Word document to keep them?"),
            TryLocalize("Save as {0}", "." + word.Extension), TryLocalize("Keep {0}", format.Name), TryLocalize("Cancel"));
        if (answer == null)
            throw new SaveCancelledException();
        if (answer == true)
            return (word, DocumentFileFormat.Docx);
        _pendingKeep = (document, target);
        return (format, target);
    }

    private static MudExFileFormat FileFormat(string name, DocumentFileFormat format) =>
        new(name, DocumentFileFormats.Extension(format).TrimStart('.'), DocumentFileFormats.ContentType(format));

    private static DocumentFileFormat Kind(MudExFileFormat format) => DocumentFileFormats.Detect("." + format.Extension) ?? DocumentFileFormat.Docx;

    private static string FeatureName(DocumentFeature feature) => feature switch
    {
        DocumentFeature.CharacterFormatting => "character formatting",
        DocumentFeature.ParagraphFormatting => "paragraph formatting",
        DocumentFeature.Styles => "styles",
        DocumentFeature.Lists => "lists",
        DocumentFeature.Tables => "tables",
        DocumentFeature.MergedCells => "merged cells",
        DocumentFeature.NestedTables => "nested tables",
        DocumentFeature.Images => "pictures",
        DocumentFeature.Links => "links and bookmarks",
        DocumentFeature.HeadersFooters => "headers and footers",
        DocumentFeature.Sections => "sections",
        DocumentFeature.Comments => "comments",
        DocumentFeature.Fields => "fields",
        _ => "other content"
    };
}
