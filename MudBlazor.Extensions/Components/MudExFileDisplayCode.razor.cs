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
    private bool _loadFailed;
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
        [new MudExFileFormat(Language.ToString(), Path.GetExtension(FileDisplayInfos?.FileName)?.TrimStart('.'), FileDisplayInfos?.ContentType)];

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
    protected override async Task LoadAsync(IMudExFileDisplayInfos fileDisplayInfos)
    {
        Language = MudExCodeLanguageExtensionsMapping.GetCodeLanguageForFile(fileDisplayInfos.FileName);
        Value = string.Empty;
        _loadFailed = false;
        try
        {
            await using var stream = await FileService.ReadStreamAsync(fileDisplayInfos);
            if (stream == null)
                return;
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
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
        catch
        {
            _loadFailed = true;
            throw;
        }
    }

    /// <inheritdoc />
    public override Task<MudExFileContent> ExportAsync(MudExFileFormat format = null, CancellationToken cancellationToken = default)
    {
        if (_loadFailed)
            throw new InvalidOperationException("The file could not be loaded and cannot be saved.");
        var text = (Value ?? string.Empty).NormalizeLineEndings().Replace("\n", _lineEnding);
        var body = _encoding.GetBytes(text);
        var bytes = _hadBom ? _encoding.GetPreamble().Concat(body).ToArray() : body;
        return Task.FromResult(new MudExFileContent
        {
            FileName = FileDisplayInfos?.FileName,
            ContentType = FileDisplayInfos?.ContentType,
            Stream = new MemoryStream(bytes)
        });
    }
}
