using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Office.Presentation;
using MudBlazor.Utilities;

namespace MudBlazor.Extensions.Components;

public partial class MudExSlideEditor
{
    private const long EmuPerPixel = 9525;
    private const long DefaultOutline = 9525;

    private static readonly Dictionary<string, string> PictureTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png", [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".gif"] = "image/gif", [".svg"] = "image/svg+xml", [".webp"] = "image/webp",
        [".bmp"] = "image/bmp"
    };

    private readonly string _pictureInput = $"mud-ex-slide-picture-{Guid.NewGuid():N}";

    private static string PictureAccept => string.Join(",", PictureTypes.Keys);

    private IReadOnlyList<AutoShape> SelectedAutoShapes => _current == null
        ? []
        : SlideShape.Flatten(_current.Shapes).Where(s => SelectedShapes.Contains(s.Id)).SelectMany(s => SlideShape.Flatten([s])).OfType<AutoShape>().ToList();

    private Task InsertShapeAsync(string preset) => IsReadOnly || _canvas == null ? Task.CompletedTask : _canvas.InsertShapeAsync(preset);

    private Task ChangeShapeAsync(string preset) => RunAsync(() =>
    {
        var shapes = SelectedAutoShapes.Where(s => s.Geometry != preset).ToList();
        if (shapes.Count == 0)
            return;
        using (_presentation.Undo.BeginTransaction("Change shape"))
            foreach (var shape in shapes)
                _presentation.Execute(new SetGeometryCommand(_current, shape.Id, preset));
    });

    private Task SetShapeFillAsync(MudExColor color) =>
        Hex(color) is { } hex ? StyleAsync(new ShapeFill(ShapeFillKind.Solid, hex), null) : Task.CompletedTask;

    private Task SetShapeOutlineAsync(MudExColor color)
    {
        if (Hex(color) is not { } hex)
            return Task.CompletedTask;
        var current = SelectedAutoShapes.Select(s => s.Line).FirstOrDefault(l => l != null);
        return StyleAsync(null, new ShapeLine(hex, current?.Width is > 0 and var width ? width : DefaultOutline, current?.Dash ?? "solid"));
    }

    private Task StyleAsync(ShapeFill fill, ShapeLine line) => RunAsync(() =>
    {
        var ids = SelectedAutoShapes.Select(s => s.Id).ToList();
        if (ids.Count > 0)
            _presentation.Execute(new SetShapeStyleCommand(_current, ids, fill, line));
    });

    private static string Hex(MudExColor color) => color.IsSet() && color.ToMudColor() is { } c ? c.ToString(MudColorOutputFormats.Hex).ToUpperInvariant() : null;

    private Task PickPictureAsync() =>
        IsReadOnly || _current == null ? Task.CompletedTask : JsRuntime.InvokeVoidAsync("MudExEventHelper.clickElementById", _pictureInput).AsTask();

    private async Task OnPictureChosenAsync(InputFileChangeEventArgs e)
    {
        try
        {
            var file = e.File;
            await using var stream = file.OpenReadStream(MaxPasteBytes);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            await InsertPictureAsync(buffer.ToArray(), PictureTypes.GetValueOrDefault(Path.GetExtension(file.Name)) ?? file.ContentType);
        }
        catch (Exception ex)
        {
            await ShowErrorAsync(ex.Message);
        }
    }

    internal async Task InsertPictureAsync(byte[] data, string contentType)
    {
        if (IsReadOnly || _current == null || data is not { Length: > 0 })
            return;
        if (!PictureTypes.ContainsValue(contentType ?? ""))
        {
            await ShowErrorAsync(TryLocalize("The picture format is not supported."));
            return;
        }

        if (_canvas != null)
            await _canvas.CommitTextAsync();
        var slide = _current;
        var url = await RegisterImageAsync(data, contentType);
        var part = InsertPictureCommand.MediaPart(_presentation, contentType);
        if (url != null)
            _images.Register(part, url);
        var command = new InsertPictureCommand(slide, data, contentType, FitPicture(SlideImageSize.Read(data, contentType)));
        await RunAsync(() => _presentation.Execute(command));
        if (command.Picture == null)
            return;
        if (url != null && command.Picture.ImagePart != part)
            _images.Register(command.Picture.ImagePart, url);
        if (_canvas != null)
            await _canvas.SelectAsync(slide, [command.Picture.Id]);
        StateHasChanged();
    }

    private async Task<string> RegisterImageAsync(byte[] data, string contentType)
    {
        if (_js == null || _images == null)
            return null;
        try { return await _js.InvokeAsync<string>("register", data, contentType); }
        catch (Exception e) when (e is JSException or JSDisconnectedException) { return null; }
    }

    private ShapeTransform FitPicture((double Width, double Height)? pixels)
    {
        var (width, height) = pixels is { Width: > 0, Height: > 0 } size
            ? (size.Width * EmuPerPixel, size.Height * EmuPerPixel)
            : (_presentation.SlideHeight / 2d, _presentation.SlideHeight / 2d);
        var scale = Math.Min(1, Math.Min(_presentation.SlideWidth * 0.8 / width, _presentation.SlideHeight * 0.8 / height));
        var (cx, cy) = (Math.Max(1, (long)Math.Round(width * scale)), Math.Max(1, (long)Math.Round(height * scale)));
        return new ShapeTransform((_presentation.SlideWidth - cx) / 2, (_presentation.SlideHeight - cy) / 2, cx, cy);
    }

    private async Task FindReplaceAsync(bool replace)
    {
        if (_presentation == null)
            return;
        if (_notes != null)
            await _notes.CommitAsync();
        if (_canvas != null)
            await _canvas.CommitTextAsync();
        await DialogService.ShowAsync<MudExSlideFindReplaceDialog>(TryLocalize("Find and Replace"), new DialogParameters
        {
            { nameof(MudExSlideFindReplaceDialog.Presentation), _presentation },
            { nameof(MudExSlideFindReplaceDialog.Start), _current },
            { nameof(MudExSlideFindReplaceDialog.Replace), replace && !IsReadOnly },
            { nameof(MudExSlideFindReplaceDialog.CanReplace), !IsReadOnly },
            { nameof(MudExSlideFindReplaceDialog.Select), (Func<Slide, uint?, Task>)SelectFoundAsync },
            { nameof(MudExSlideFindReplaceDialog.Localizer), Localizer }
        }, new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseButton = true });
    }

    private async Task SelectFoundAsync(Slide slide, uint? shape)
    {
        if (!ReferenceEquals(slide, _current))
            Show(slide);
        if (_canvas != null)
            await _canvas.SelectAsync(slide, shape is { } id ? [id] : []);
        StateHasChanged();
    }
}
