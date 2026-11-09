using Microsoft.AspNetCore.Components.Rendering;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Components.Base;
using MudBlazor.Extensions.Core;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

public class TestTextEditor : MudExFileEditorBase<TestTextEditor>
{
    public string Text { get; private set; } = "";

    public override string Name => nameof(TestTextEditor);

    public override IReadOnlyList<MudExFileFormat> SaveFormats { get; } = [new("Text", "txt", "text/plain"), new("Markdown", "md", "text/markdown")];

    public override Task<bool> CanHandleFileAsync(IMudExFileDisplayInfos fileDisplayInfos, IMudExFileService fileService) => Task.FromResult(true);

    public override Task<MudExFileContent> ExportAsync(MudExFileFormat format = null, CancellationToken cancellationToken = default)
        => Task.FromResult(new MudExFileContent
        {
            FileName = format == null ? FileName : Path.ChangeExtension(FileName, format.Extension),
            ContentType = format?.ContentType ?? "text/plain",
            Stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(Text))
        });

    protected override MudExFileContent CreateNew() => new() { FileName = "new.txt", ContentType = "text/plain", Stream = new MemoryStream() };

    protected override async Task LoadContentAsync(MudExFileContent content)
    {
        using var reader = new StreamReader(content.Stream, leaveOpen: true);
        Text = await reader.ReadToEndAsync();
    }

    public Task Edit(string text)
    {
        Text = text;
        return MarkDirtyAsync();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
    }
}
