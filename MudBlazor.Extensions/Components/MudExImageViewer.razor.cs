using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Services;
using BlazorJS;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Core.Css;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Helper.Internal;
using MudBlazor.Extensions.Options;
using Nextended.Core;
using Nextended.Core.Extensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Simple component to display markdown files with MudExFileDisplay
/// </summary>
public partial class MudExImageViewer : IMudExFileDisplay
{
    private string _id = Guid.NewGuid().ToFormattedId();
    private string _src;
    private string _navigatorClass = MudExCss.Classes.Backgrounds.TransparentIndicator;
    private bool _showNavigator = true;
    private double _maxZoomPixelRatio = 50.0;
    private double _minZoomLevel = 0.1;
    private double _animationTime = 0.3;
    private double _navigatorSizeRatio = 0.15;
    private Origin _navigatorPosition = Origin.BottomRight;
    private MudExColor _navigatorRectangleColor = MudExColor.Primary;
    private ConcurrentDictionary<(string FormatName, string Url), string> _convertedUrlMapping = new();


    [Inject] private MudExFileService FileService { get; set; }
    
    /// <summary>
    /// Returns the current status text
    /// </summary>
    public string StatusText { get; private set; }

    #region Parameters

    /// <summary>
    /// The icon for the ZoomIn button
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Appearance)]
    public string ZoomInButtonIcon { get; set; } = Icons.Material.Filled.ZoomIn;

    /// <summary>
    /// The icon for the ZoomOut button
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Appearance)]
    public string ZoomOutButtonIcon { get; set; } = Icons.Material.Filled.ZoomOut;

    /// <summary>
    /// The icon for the Print button
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Appearance)]
    public string PrintButtonIcon { get; set; } = Icons.Material.Filled.Print;

    /// <summary>
    /// The icon for the Reset button
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Appearance)]
    public string ResetButtonIcon { get; set; } = Icons.Material.Filled.Home;

    /// <summary>
    /// The icon for the Fullscreen button
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Appearance)]
    public string FullScreenButtonIcon { get; set; } = Icons.Material.Filled.Fullscreen;

    /// <summary>
    /// The icon for the Save button
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Appearance)]
    public string SaveButtonIcon { get; set; } = Icons.Material.Filled.SaveAs;

    /// <summary>
    /// The dialog options for the save dialog
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Appearance), IgnoreOnObjectEdit]
    public DialogOptionsEx SaveDialogOptions { get; set; } = null;

    /// <summary>
    /// If true a ZoomIn button is shown in the toolbar
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Behavior)]
    public bool ShowZoomInButton { get; set; } = true;

    /// <summary>
    /// If true a ZoomOut button is shown in the toolbar
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Behavior)]
    public bool ShowZoomOutButton { get; set; } = true;

    /// <summary>
    /// If true a Print button is shown in the toolbar
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Behavior)]
    public bool ShowPrintButton { get; set; } = true;

    /// <summary>
    /// If true a Save button is shown in the toolbar
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Behavior)]
    public bool ShowSaveButton { get; set; } = true;

    /// <summary>
    /// If true a Reset button is shown in the toolbar
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Behavior)]
    public bool ShowResetButton { get; set; } = true;

    /// <summary>
    /// If true a FullScreen button is shown in the toolbar
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Behavior)]
    public bool ShowFullScreenButton { get; set; } = true;

    /// <summary>
    /// Variant for toolbar buttons only if <see cref="ShowTools"/> is true
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Appearance)]
    public Variant ToolbarButtonVariant { get; set; } = Variant.Filled;

    /// <summary>
    /// Size for toolbar buttons only if <see cref="ShowTools"/> is true
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Appearance)]
    public Size ToolbarButtonSize { get; set; } = MudBlazor.Size.Small;

    /// <summary>
    /// 
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Appearance)]
    public Origin ToolbarButtonPosition { get; set; }

    /// <summary>
    /// Size of the viewer
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Behavior)]
    public MudExDimension Size { get; set; } = "100%";

    /// <summary>
    /// This method returns true when at least one button is used
    /// </summary>
    public bool ShowTools() => ShowZoomInButton || ShowZoomOutButton || ShowPrintButton || ShowResetButton || ShowFullScreenButton;

    /// <summary>
    /// Style for toolbar only if <see cref="ShowTools"/> is true
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Appearance)]
    public string ToolbarStyle { get; set; }

    /// <summary>
    /// Dense toolbar
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Appearance)]
    public bool Dense { get; set; } = true;

    /// <summary>
    /// Toolbar background color only if <see cref="ShowTools"/> is true
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Appearance)]
    public MudExColor ToolbarBackgroundColor { get; set; }

    /// <summary>
    /// Border color
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Appearance)]
    public MudExColor BorderColor { get; set; } = MudExColor.Transparent;

    /// <summary>
    /// Border style
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Appearance)]
    public BorderStyle BorderStyle { get; set; } = BorderStyle.Solid;

    /// <summary>
    /// Border size
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Appearance)]
    public MudExSize<double> BorderSize { get; set; } = "1px";

    /// <summary>
    /// Color of the navigator rectangle only if <see cref="ShowNavigator"/> is true
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Appearance)]
    public MudExColor NavigatorRectangleColor
    {
        get => _navigatorRectangleColor;
        set => Set(ref _navigatorRectangleColor, value, _ => Update().AndForget());
    }

    /// <summary>
    /// Toolbar button color only if <see cref="ShowTools"/> is true
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Appearance)]
    public Color ToolbarButtonColor { get; set; }

    /// <summary>
    /// Image source
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Data)]
    public string Src
    {
        get => _src;
        set => Set(ref _src, value, _ => Update().AndForget());
    }

    /// <summary>
    /// Position of the navigator if <see cref="ShowNavigator"/> is true
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Behavior)]
    public Origin NavigatorPosition
    {
        get => _navigatorPosition;
        set => Set(ref _navigatorPosition, value, _ => Update().AndForget());
    }

    /// <summary>
    /// Css class for the navigator if <see cref="ShowNavigator"/> is true
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Appearance)]
    public string NavigatorClass
    {
        get => _navigatorClass;
        set => Set(ref _navigatorClass, value, _ => Update().AndForget());
    }

    /// <summary>
    /// The size ratio of the navigator in relation to the viewer size
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Appearance)]
    public double NavigatorSizeRatio
    {
        get => _navigatorSizeRatio;
        set => Set(ref _navigatorSizeRatio, value, _ => Update().AndForget());
    }

    /// <summary>
    /// The maximum zoom pixel ratio
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Behavior)]
    public double MaxZoomPixelRatio
    {
        get => _maxZoomPixelRatio;
        set => Set(ref _maxZoomPixelRatio, value, _ => Update().AndForget());
    }

    /// <summary>
    /// The minimum zoom level
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Behavior)]
    public double MinZoomLevel
    {
        get => _minZoomLevel;
        set => Set(ref _minZoomLevel, value, _ => Update().AndForget());
    }

    /// <summary>
    /// Animation time in seconds
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Behavior)]
    public double AnimationTime
    {
        get => _animationTime;
        set => Set(ref _animationTime, value, _ => Update().AndForget());
    }

    /// <summary>
    /// Show the navigator
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Behavior)]
    public bool ShowNavigator
    {
        get => _showNavigator;
        set => Set(ref _showNavigator, value, _ => Update().AndForget());
    }

    #endregion

    private async Task Update()
    {
        await WaitReferenceCreatedAsync();
        await JsReference.InvokeVoidAsync("createViewer", await Options());
    }

    #region Interface Implementation

    /// <summary>
    /// The name of the component
    /// </summary>
    public string Name => nameof(MudExImageViewer);

    /// <summary>
    /// The file display infos
    /// </summary>
    [Parameter, IgnoreOnObjectEdit]
    public IMudExFileDisplayInfos FileDisplayInfos { get; set; }

    /// <summary>
    /// Reference to the MudExFileDisplay if this component is used inside a MudExFileDisplay
    /// </summary>
    [CascadingParameter] public MudExFileDisplay MudExFileDisplay { get; set; }

    #endregion

    /// <summary>
    /// Called when the viewer is created
    /// </summary>
    [JSInvokable]
    public void OnViewerCreated()
    {
    }
    
    /// <inheritdoc />
    public override async Task ImportModuleAndCreateJsAsync()
    {
        await JsRuntime.LoadFilesAsync(JsImportHelper.JsPath("/js/libs/MudExImageView.min.js"));
        await JsRuntime.WaitForNamespaceAsync("MudExImageView", TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(200));
        await base.ImportModuleAndCreateJsAsync();
    }

    /// <summary>
    /// Returns true if it's a markdown file and we can handle it
    /// </summary>
    public bool CanHandleFile(IMudExFileDisplayInfos fileDisplayInfos)
    {
        return fileDisplayInfos?.FileName?.EndsWith(".png") == true
               || fileDisplayInfos?.FileName?.EndsWith(".jpg") == true
               || fileDisplayInfos?.FileName?.EndsWith(".jpeg") == true
               || fileDisplayInfos?.FileName?.EndsWith(".webp") == true
               || fileDisplayInfos?.FileName?.EndsWith(".bmp") == true
               || fileDisplayInfos?.FileName?.EndsWith(".gif") == true
               || MimeType.Matches(fileDisplayInfos?.ContentType, "image/*");
    }

    private bool IsTiff(IMudExFileDisplayInfos fileDisplayInfos)
    {
        return fileDisplayInfos != null && (fileDisplayInfos.FileName?.EndsWith(".tiff") == true
                                            || fileDisplayInfos.FileName?.EndsWith(".tif") == true
                                            || IsTiff(fileDisplayInfos.ContentType));
    }


    private bool IsTiff(string mime) => mime == "image/tiff";

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var updateRequired = (parameters.TryGetValue<IMudExFileDisplayInfos>(nameof(FileDisplayInfos), out var fileDisplayInfos) && (FileDisplayInfos != fileDisplayInfos || (!string.IsNullOrEmpty(fileDisplayInfos.Url) && fileDisplayInfos.Url != Src)));
        await base.SetParametersAsync(parameters);
        if (updateRequired || Src == null)
        {
            try
            {
                Src = fileDisplayInfos?.Url ?? await FileService.CreateDataUrlAsync(fileDisplayInfos?.ContentStream?.ToByteArray() ?? throw new ArgumentException("No stream and no url available"), fileDisplayInfos.ContentType, MudExFileDisplay == null || MudExFileDisplay.StreamUrlHandling == StreamUrlHandling.BlobUrl);
            }
            catch (Exception e)
            {
                MudExFileDisplay?.ShowError(e.Message);
            }
            StateHasChanged();
        }
    }

    /// <summary>
    /// Sets the status text
    /// </summary>
    public async Task SetStatusTextAsync(string text)
    {
        if (MudExFileDisplay is { IsRendered: true })
            await MudExFileDisplay.SetStatusTextAsync(text);
        else
        {
            StatusText = text;
            CallStateHasChanged();
            await Task.Delay(100);
        }
    }

    /// <summary>
    /// removes the status text
    /// </summary>
    public Task RemoveStatusTextAsync() => SetStatusTextAsync(null);


    /// <summary>
    /// Zooms to the given factor
    /// </summary>
    public Task ZoomBy(double factor) => JsReference.InvokeVoidAsync("zoomBy", factor).AsTask();

    /// <summary>
    /// Zooms to the given factor
    /// </summary>
    public Task ResetZoom() => JsReference.InvokeVoidAsync("reset").AsTask();

    /// <summary>
    /// Toggles the fullscreen mode
    /// </summary>
    public Task ToggleFullScreen() => JsReference.InvokeVoidAsync("toggleFullScreen").AsTask();

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        _convertedUrlMapping.Clear();
        await FileService.DisposeAsync();
        await base.DisposeAsync();
    }

    /// <summary>
    /// Shows a dialog to specify options and then downloads the image
    /// </summary>
    public async Task SaveImageAsync()
    {
        DialogOptionsEx dlgOptions = SaveDialogOptions ?? DialogOptionsEx.DefaultDialogOptions;
        var parameters = new DialogParameters
        {
            {nameof(MudExObjectEditDialog<MudExImageViewerSaveOptions>.DefaultPropertyResetSettings), new PropertyResetSettings() {AllowReset = false}},
            {nameof(MudExObjectEditDialog<MudExImageViewerSaveOptions>.GlobalResetSettings), new GlobalResetSettings() {AllowReset = false}},
            {nameof(MudExObjectEditDialog<MudExImageViewerSaveOptions>.AllowSearch), false},
            {nameof(MudExObjectEditDialog<MudExImageViewerSaveOptions>.DialogIcon), SaveButtonIcon},
        };
        var r = await DialogService.EditObject(new MudExImageViewerSaveOptions {FileName = FileDisplayInfos?.FileName ?? Path.GetFileName(Src)}, TryLocalize("Save image"), dlgOptions,
            meta =>
            {
                meta.Property(m => m.FileName).WithLabel(TryLocalize("Filename"));
                meta.Property(m => m.VisibleViewPortOnly).WithLabel(TryLocalize("Save visible area only")).WithDescription(TryLocalize("Otherwise the full image will be downloaded"));
                meta.Property(m => m.Format).WithLabel(TryLocalize("Image format"));
            }, parameters);
        if(r.Cancelled)
            return;
        await SaveImageAsync(r.Result);

    }

    /// <summary>
    /// Downloads the image with the given options
    /// </summary>
    public async Task SaveImageAsync(MudExImageViewerSaveOptions options)
    {
        var url = options.VisibleViewPortOnly ? await JsReference.InvokeAsync<string>("getCurrentViewImageDataUrl") : Src;
        var format = options.GetImageFormat();
        var extension = format?.FileExtensions.FirstOrDefault() ?? MimeType.GetExtension(FileDisplayInfos.ContentType);
        string name = !string.IsNullOrEmpty(options.FileName) ? Path.ChangeExtension(options.FileName, extension) : $"{Guid.NewGuid().ToFormattedId()}{extension.EnsureStartsWith(".")}";
        
        var result = await ConvertImageToAsync(url, format);
        await JsRuntime.InvokeVoidAsync("MudBlazorExtensions.downloadFile", new
        {
            Url = result,
            FileName = name,
            MimeType = format?.DefaultMimeType
        });
    }
    
    private Task<string> ConvertImageToAsync(Stream stream, ImageViewerExportFormat format = ImageViewerExportFormat.Png) => ConvertImageToAsync(stream, MudExImageViewerSaveOptions.GetImageFormat(format));

    private async Task<string> ConvertImageToAsync(Stream stream, IImageFormat format)
    {
        await SetStatusTextAsync("Please wait while the image is being converted");

        using var image = await Image.LoadAsync(stream);
        using var resultStream = new MemoryStream();

        await image.SaveAsync(resultStream, format);

        resultStream.Position = 0;
        var resultUrl = await FileService.CreateDataUrlAsync(resultStream.ToArray(), "image/png", true);
        await RemoveStatusTextAsync();
        return resultUrl;
    }

    private Task<string> ConvertImageToAsync(string url, ImageViewerExportFormat format = ImageViewerExportFormat.Png) => ConvertImageToAsync(url, MudExImageViewerSaveOptions.GetImageFormat(format));

    private async Task<string> ConvertImageToAsync(string url, IImageFormat format)
    {
        var cacheKey = (format.Name, url);

        if (_convertedUrlMapping.TryGetValue(cacheKey, out var convertedUrl))
        {
            return convertedUrl;
        }

        await using var stream = await FileService.ReadStreamAsync(url);
        var result = await ConvertImageToAsync(stream, format);
        _convertedUrlMapping.TryAdd(cacheKey, result);
        return result;
    }


    private async Task<string> ConvertUrlIfNeededAsync(string url)
    {
        if (IsTiff(FileDisplayInfos) || IsTiff(await MimeType.ReadMimeTypeFromUrlAsync(url)))
            url = await ConvertImageToAsync(url);
        return url;
    }

    private async Task<object> Options()
    {
        var url = await ConvertUrlIfNeededAsync(Src);
        return new
        {
            id = _id,
            Src = url,
            NavigatorClass,
            ShowNavigator,
            AnimationTime,
            MaxZoomPixelRatio,
            MinZoomLevel,
            NavigatorSizeRatio,
            NavigatorRectangleColor = NavigatorRectangleColor.ToCssStringValue(),
            NavigatorPosition = NavigatorPosition.GetDescription().Replace("-", "_").ToUpper()
        };
    }

    private string StyleStr() => MudExStyleBuilder.Default
        .WithSize(Size)
        .WithPosition(Core.Css.Position.Relative)
        .WithDisplay(Display.Flex, !string.IsNullOrEmpty(StatusText))
        .WithJustifyContent("center", !string.IsNullOrEmpty(StatusText))
        .WithAlignItems(Core.Css.AlignItems.Center, !string.IsNullOrEmpty(StatusText))
        .WithBorder(BorderSize, BorderStyle, BorderColor, !BorderColor.Is(Color.Transparent) && !BorderColor.Is(Color.Inherit))
        .AddRaw(Style)
        .Build();

    private string ToolbarStyleStr()
    {
        var toolbarAlign = ToolbarAlign();
        var vertical = ToolbarButtonPosition is Origin.CenterLeft or Origin.CenterRight;
        return MudExStyleBuilder.Default
            .WithPosition(Core.Css.Position.Absolute, toolbarAlign.ToolbarPosition != NavigatorPosition.GetDescription().Split("-").First().ToLower())
            .WithWidth("100%", !vertical)
            .WithWidth("50px", vertical)
            .WithPadding(0)
            .WithBottom("50%", toolbarAlign.ToolbarPosition == "center")
            .WithZIndex(2)
            .WithRight(0, ToolbarButtonPosition == Origin.CenterRight)
            .WithBackgroundColor(ToolbarBackgroundColor, !ToolbarBackgroundColor.Is(Color.Transparent) && !ToolbarBackgroundColor.Is(Color.Inherit))
            .AddRaw(ToolbarStyle)
            .Build();
    }

    private string InnerViewerStyle() => MudExStyleBuilder.Default
        .WithSize("100%")
        .WithDisplay(Display.None, !string.IsNullOrEmpty(StatusText))
        .Build();

    private Task ZoomInClick() => ZoomBy(1.3);
    private Task ZoomOutClick() => ZoomBy(0.8);
    private Task ResetClick() => ResetZoom();
    private Task FullscreenClick() => ToggleFullScreen();
    private Task SaveAsClick() => SaveImageAsync();
    private Task PrintClick() => JsReference.InvokeVoidAsync("print").AsTask();

    private bool NeedSpacer(bool toolsRendered)
    {
        string align = ToolbarButtonPosition.GetDescription().Split("-").Last().ToLower();
        return ToolbarButtonPosition is Origin.CenterLeft or Origin.CenterRight || (align == "left" && toolsRendered) || (align == "right" && !toolsRendered) || align == "center";
    }
    private (string ToolbarPosition, string ItemAlign) ToolbarAlign() => (
        ToolbarButtonPosition.GetDescription().Split("-").First().ToLower(),
        ToolbarButtonPosition.GetDescription().Split("-").Last().ToLower());

}
