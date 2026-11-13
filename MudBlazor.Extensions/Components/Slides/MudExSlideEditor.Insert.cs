using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Office;
using MudBlazor.Extensions.Office.Presentation;
using MudBlazor.Utilities;

namespace MudBlazor.Extensions.Components;

public partial class MudExSlideEditor
{
    private const long EmuPerPixel = 9525;
    private const long DefaultOutline = 9525;
    private const string OutlineColor = "scheme:tx1";

    private static readonly long[] OutlineWeights = [3175, 6350, 9525, 12700, 19050, 28575, 38100, 57150, 76200];

    private static readonly (string Dash, string Text)[] OutlineDashes =
    [
        ("solid", "Solid"), ("sysDot", "Round Dot"), ("sysDash", "Square Dot"), ("dash", "Dash"), ("dashDot", "Dash Dot"), ("lgDash", "Long Dash"),
        ("lgDashDot", "Long Dash Dot"), ("lgDashDotDot", "Long Dash Dot Dot")
    ];

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

    private bool HasNoFill => SelectedAutoShapes is { Count: > 0 } shapes && shapes.All(s => ShapeFill.EffectiveFill(s).Kind == ShapeFillKind.None);

    private ShapeLine CurrentOutline => SelectedAutoShapes.Select(ShapeLine.EffectiveLine).FirstOrDefault(l => l?.Color != null);

    private Task SetShapeFillAsync(MudExColor color) =>
        Hex(color) is { } hex ? FillAsync(new ShapeFill(ShapeFillKind.Solid, hex)) : Task.CompletedTask;

    private Task FillAsync(ShapeFill fill) => RunAsync(() =>
    {
        var ids = SelectedAutoShapes.Select(s => s.Id).ToList();
        if (ids.Count > 0)
            _presentation.Execute(new SetShapeStyleCommand(_current, ids, fill, null));
    });

    private Task SetShapeOutlineAsync(MudExColor color) => Hex(color) is { } hex ? OutlineAsync(l => l with { Color = hex }) : Task.CompletedTask;

    private Task SetOutlineWeightAsync(long width) => OutlineAsync(l => l with { Width = width, Color = l.Color ?? OutlineColor });

    private Task SetOutlineDashAsync(string dash) => OutlineAsync(l => l with { Dash = dash, Color = l.Color ?? OutlineColor });

    private Task OutlineAsync(Func<ShapeLine, ShapeLine> change) => RunAsync(() =>
    {
        using (_presentation.Undo.BeginTransaction("Format shape"))
            foreach (var shape in SelectedAutoShapes)
            {
                var effective = ShapeLine.EffectiveLine(shape);
                var line = change(effective is { Width: > 0 } e ? e with { ColorInherited = false } : new ShapeLine(null, DefaultOutline, effective?.Dash ?? "solid"));
                if (line != effective && (effective?.Color != null || line.Color != null))
                    _presentation.Execute(new SetShapeStyleCommand(_current, [shape.Id], null, line));
            }
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
        var command = new InsertPictureCommand(slide, data, contentType, FitPicture(OfficeImageSize.Read(data, contentType)));
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
