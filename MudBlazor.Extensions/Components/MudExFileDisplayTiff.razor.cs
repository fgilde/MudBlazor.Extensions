using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Helper.Internal;
using MudBlazor.Extensions.Services;
using SixLabors.ImageSharp;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Simple component to display markdown files with MudExFileDisplay
/// </summary>
public partial class MudExFileDisplayTiff : IMudExFileDisplay
{

    private string _url;

    [Inject] private MudExFileService FileService { get; set; }

    /// <summary>
    /// The name of the component
    /// </summary>
    public string Name => nameof(MudExFileDisplayTiff);

    /// <summary>
    /// Parent display control
    /// </summary>
    [CascadingParameter] public MudExFileDisplay MudExFileDisplay { get; set; }

    /// <summary>
    /// File display infos
    /// </summary>
    [Parameter] public IMudExFileDisplayInfos FileDisplayInfos { get; set; }

    /// <summary>
    /// Returns true if its a markdown file and we can handle it
    /// </summary>
    public bool CanHandleFile(IMudExFileDisplayInfos fileDisplayInfos)
        => fileDisplayInfos?.FileName?.EndsWith(".tiff") == true || fileDisplayInfos?.FileName?.EndsWith(".tif") == true || fileDisplayInfos?.ContentType == "image/tiff";

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var updateRequired = (parameters.TryGetValue<IMudExFileDisplayInfos>(nameof(FileDisplayInfos), out var fileDisplayInfos) && FileDisplayInfos != fileDisplayInfos);
        await base.SetParametersAsync(parameters);
        if (updateRequired && string.IsNullOrEmpty(_url))
        {
            await ConvertImageAsync(fileDisplayInfos);
        }
    }


    private async Task ConvertImageAsync(IMudExFileDisplayInfos fileDisplayInfos)
    {
        if ((fileDisplayInfos?.ContentStream == null && string.IsNullOrEmpty(fileDisplayInfos?.Url)) || !string.IsNullOrEmpty(_url))
            return;

        var stream = fileDisplayInfos.ContentStream != null
            ? await fileDisplayInfos.ContentStream.CopyStreamAsync()
            : await FileService.ReadStreamAsync(fileDisplayInfos.Url);
        if (stream == null)
            return;
        await MudExFileDisplay.SetStatusTextAsync("Please wait while the image is being converted");
        using var image = await Image.LoadAsync(stream);
        var pngStream = new MemoryStream();

        await image.SaveAsPngAsync(pngStream);
        pngStream.Position = 0;
        _url = await FileService.CreateDataUrlAsync(pngStream.ToArray(), "image/png", MudExFileDisplay.StreamUrlHandling == StreamUrlHandling.BlobUrl);
        await MudExFileDisplay.RemoveStatusTextAsync();
    }

    //public IEnumerable<Task<MemoryStream>> ResizeImage(Image image)
    //{
    //    var sizes = new[] { 1920, 1080, 720, 480 };
    //    foreach (var size in sizes)
    //    {
    //        yield return Task.Run(() =>
    //        {
    //            image.Mutate(x => x.Resize(size, 0));
    //            var stream = new MemoryStream();
    //            image.Save(stream, PngFormat.Instance);
    //            stream.Position = 0;
    //            return stream;
    //        });
    //    }
    //}

    public override async ValueTask DisposeAsync()
    {
        await FileService.DisposeAsync();
        await base.DisposeAsync();
    }
}