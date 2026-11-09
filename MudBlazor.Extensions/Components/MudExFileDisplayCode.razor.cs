using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Components.Base;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Helper.Internal;
using MudBlazor.Extensions.Options;
using MudBlazor.Extensions.Services;
using Nextended.Core.Extensions;
using System.ComponentModel;
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
    private string _text = string.Empty;
    private int _textKey;
    private bool _edited;
    private ElementReference _area;
    private string _jsArea;
    private IJSObjectReference _module;
    private IJSObjectReference _js;
    private DotNetObjectReference<MudExFileDisplayCode> _dotnet;

    /// <summary>
    /// The name of the component
    /// </summary>
    public override string Name => nameof(MudExFileDisplayCode);

    /// <summary>
    /// The code string of the file. While editing it is updated when the content is exported.
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
        ResetText();
    }

    private void ResetText()
    {
        _edited = false;
        _text = Value;
        _textKey++;
    }

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        if (_edited && !ReadOnly && parameters.TryGetValue<bool>(nameof(ReadOnly), out var readOnly) && readOnly)
        {
            await SyncValueAsync();
            ResetText();
        }
        await base.SetParametersAsync(parameters);
    }

    /// <summary>
    /// Called from JS on the first input after the content was loaded or read
    /// </summary>
    [JSInvokable, EditorBrowsable(EditorBrowsableState.Never)]
    public async Task OnCodeInput()
    {
        _edited = true;
        await MarkDirtyAsync();
    }

    private async Task SyncValueAsync()
    {
        if (!_edited || _js == null)
            return;
        var reference = await _js.InvokeAsync<IJSStreamReference>("readValue");
        await using (reference)
        {
            await using var stream = await reference.OpenReadStreamAsync(long.MaxValue);
            using var reader = new StreamReader(stream, new UTF8Encoding(false), false);
            Value = await reader.ReadToEndAsync();
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        var area = ReadOnly || LoadFailed ? null : _area.Id;
        if (area == _jsArea)
            return;
        await DisposeJsAsync();
        _jsArea = area;
        if (area == null)
            return;
        _dotnet ??= DotNetObjectReference.Create(this);
        _module ??= await JsRuntime.ImportModuleAsync<MudExFileDisplayCode>();
        _js = await _module.InvokeAsync<IJSObjectReference>("initializeMudExFileDisplayCode", _area, _dotnet);
    }

    private async Task DisposeJsAsync()
    {
        var js = _js;
        _js = null;
        if (js == null)
            return;
        try
        {
            await js.InvokeVoidAsync("dispose");
            await js.DisposeAsync();
        }
        catch (JSDisconnectedException) { }
        catch (JSException) { }
        catch (TaskCanceledException) { }
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        await DisposeJsAsync();
        try
        {
            if (_module != null)
                await _module.DisposeAsync();
        }
        catch (JSDisconnectedException) { }
        _dotnet?.Dispose();
        await base.DisposeAsync();
    }

    /// <inheritdoc />
    public override async Task<MudExFileContent> ExportAsync(MudExFileFormat format = null, CancellationToken cancellationToken = default)
    {
        await SyncValueAsync();
        var text = (Value ?? string.Empty).NormalizeLineEndings().Replace("\n", _lineEnding);
        var body = _encoding.GetBytes(text);
        var bytes = _hadBom ? _encoding.GetPreamble().Concat(body).ToArray() : body;
        return new MudExFileContent
        {
            FileName = FileName,
            ContentType = ContentType,
            Stream = new MemoryStream(bytes)
        };
    }
}
