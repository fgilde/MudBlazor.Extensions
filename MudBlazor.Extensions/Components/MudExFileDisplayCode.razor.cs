using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Components.Base;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper.Internal;
using MudBlazor.Extensions.Options;
using MudBlazor.Extensions.Services;
using Nextended.Core.Extensions;
using System.Text;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Component to display and edit code and text files with MudExFileDisplay
/// </summary>
public partial class MudExFileDisplayCode
{
    private Encoding _encoding = new UTF8Encoding(false);
    private bool _hadBom;
    private string _lineEnding = "\n";

    /// <summary>
    /// The name of the component
    /// </summary>
    public override string Name => nameof(MudExFileDisplayCode);

    /// <summary>
    /// The Current code string provided from file
    /// </summary>
    public string Value { get; private set; }

    /// <summary>
    /// Language
    /// </summary>
    public MudExCodeLanguage Language { get; private set; }

    /// <summary>
    /// The theme of the code block
    /// </summary>
    [Parameter]
    public CodeBlockTheme Theme { get; set; }

    /// <inheritdoc />
    public override IReadOnlyList<MudExFileFormat> SaveFormats =>
        [new MudExFileFormat(Language == MudExCodeLanguage.Unknown ? "Text" : Language.ToString(), Path.GetExtension(FileName)?.TrimStart('.'), ContentType)];

    /// <inheritdoc />
    protected override IReadOnlyList<MudExFileFormat> OpenFormats => [];

    /// <inheritdoc />
    protected override MudExFileContent CreateNew() => new() { FileName = "new.txt", ContentType = "text/plain", Stream = new MemoryStream() };

    /// <summary>
    /// Returns true if it's a code or text file and we can handle it
    /// </summary>
    public override Task<bool> CanHandleFileAsync(IMudExFileDisplayInfos fileDisplayInfos, IMudExFileService fileService)
    {
        var language = Language = MudExCodeLanguageExtensionsMapping.GetCodeLanguageForFile(fileDisplayInfos?.FileName);
        return Task.FromResult(language != MudExCodeLanguage.Unknown && language != MudExCodeLanguage.Markdown);
    }

    /// <inheritdoc />
    public override Task<IDictionary<string, object>> FileMetaInformationAsync(IMudExFileDisplayInfos fileDisplayInfos)
    {
        return Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object>()
        {
            {"Lines", Value.GetLines().Count()},
            {"Language", Language.ToString()}
        });
    }

    /// <inheritdoc />
    protected override async Task LoadContentAsync(MudExFileContent content)
    {
        Language = MudExCodeLanguageExtensionsMapping.GetCodeLanguageForFile(content.FileName);
        Value = string.Empty;
        _encoding = new UTF8Encoding(false);
        _hadBom = false;
        _lineEnding = "\n";
        using var buffer = new MemoryStream();
        await content.Stream.CopyToAsync(buffer);
        var bytes = buffer.ToArray();
        string text;
        using (var reader = new StreamReader(new MemoryStream(bytes), new UTF8Encoding(false, true), true))
        {
            try
            {
                text = await reader.ReadToEndAsync();
                _encoding = reader.CurrentEncoding;
            }
            catch (DecoderFallbackException)
            {
                _encoding = Encoding.Latin1;
                text = _encoding.GetString(bytes);
            }
        }

        var preamble = _encoding.GetPreamble();
        _hadBom = preamble.Length > 0 && bytes.AsSpan().StartsWith(preamble);
        _lineEnding = text.DetectLineEnding();
        Value = text.NormalizeLineEndings();
    }

    /// <inheritdoc />
    public override Task<MudExFileContent> ExportAsync(MudExFileFormat format = null, CancellationToken cancellationToken = default)
    {
        if (LoadFailed)
            throw new InvalidOperationException("The file could not be loaded and cannot be saved.");
        var text = (Value ?? string.Empty).NormalizeLineEndings().Replace("\n", _lineEnding);
        var body = _encoding.GetBytes(text);
        var bytes = _hadBom ? _encoding.GetPreamble().Concat(body).ToArray() : body;
        return Task.FromResult(new MudExFileContent
        {
            FileName = FileName,
            ContentType = ContentType,
            Stream = new MemoryStream(bytes)
        });
    }
}
