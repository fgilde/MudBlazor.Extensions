using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Services;
using BlazorJS;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Helper.Internal;
using Nextended.Core;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Simple component to display markdown files with MudExFileDisplay
/// </summary>
public partial class MudExImageViewer: IMudExFileDisplay
{
    private string _id = Guid.NewGuid().ToFormattedId();
    private string _src;
    private string _navigatorClass = MudExCss.Classes.Backgrounds.TransparentIndicator;
    private bool _showNavigator = true;
    private double _maxZoomPixelRatio = 50.0;
    private double _animationTime = 0.3;
    private double _navigatorSizeRatio = 0.15;
    private Origin _navigatorPosition = Origin.BottomRight;

    [Inject] private MudExFileService fileService { get; set; }

    /// <summary>
    /// Size of the viewer
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Behavior)] 
    public MudExDimension Size { get; set; } = "100%";

    /// <summary>
    /// If true tool bar is shown
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Behavior)] 
    public bool ShowTools { get; set; } = true;

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
    /// Image source
    /// </summary>
    [Parameter, SafeCategory(CategoryTypes.FormComponent.Behavior)]
    public Origin NavigatorPosition
    {
        get => _navigatorPosition;
        set => Set(ref _navigatorPosition, value, _ => Update().AndForget());
    }

    /// <summary>
    /// Css class for the navigator
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

    private async Task Update()
    {
        await WaitReferenceCreatedAsync();
        await JsReference.InvokeVoidAsync("createViewer", Options());
    }

    #region Interface Implementation

    /// <summary>
    /// The name of the component
    /// </summary>
    public string Name => nameof(MudExImageViewer);

    /// <summary>
    /// The file display infos
    /// </summary>
    [Parameter]
    public IMudExFileDisplayInfos FileDisplayInfos { get; set; }

    [CascadingParameter] public MudExFileDisplay MudExFileDisplay { get; set; }

    #endregion

    [JSInvokable]
    public void OnViewerCreated()
    {
          
    }
    
    
    public override object[] GetJsArguments() => new[] { ElementReference, CreateDotNetObjectReference(), Options() };

    public override async Task ImportModuleAndCreateJsAsync()
    {
        await JsRuntime.LoadFilesAsync(JsImportHelper.JsPath("/js/libs/MudExImageView.min.js"));
        await JsRuntime.WaitForNamespaceAsync("MudExImageView", TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(200));
        await base.ImportModuleAndCreateJsAsync();
    }

    /// <summary>
    /// Returns true if its a markdown file and we can handle it
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

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var updateRequired = (parameters.TryGetValue<IMudExFileDisplayInfos>(nameof(FileDisplayInfos), out var fileDisplayInfos) && FileDisplayInfos != fileDisplayInfos);
        await base.SetParametersAsync(parameters);
        if (updateRequired || Src == null)
        {
            try
            {
                Src = fileDisplayInfos?.Url ?? await fileService.CreateDataUrlAsync(fileDisplayInfos?.ContentStream?.ToByteArray() ?? throw new ArgumentNullException("No stream and no url available"), fileDisplayInfos.ContentType, MudExFileDisplay.StreamUrlHandling == StreamUrlHandling.BlobUrl);
            }
            catch (Exception e)
            {
                MudExFileDisplay?.ShowError(e.Message);
            }
            StateHasChanged();
        }
    }

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

    public override async ValueTask DisposeAsync()
    {
        await fileService.DisposeAsync();
        await base.DisposeAsync();
    }

    private object Options()
    {
        return new
        {
            id = _id,
            Src,
            NavigatorClass,
            ShowNavigator,
            AnimationTime,
            MaxZoomPixelRatio,
            NavigatorSizeRatio,
            NavigatorPosition = NavigatorPosition.GetDescription().Replace("-", "_").ToUpper()
        };
    }

    private string StyleStr() => MudExStyleBuilder.Default
        .WithSize(Size)
        .AddRaw(Style)
        .Build();

    private string ToolbarStyleStr() => MudExStyleBuilder.Default
        .WithPosition(Core.Css.Position.Absolute)
        //.WithWidth("100%")
        .WithZIndex(2)
        .WithBackgroundColor(ToolbarBackgroundColor, !ToolbarBackgroundColor.Is(Color.Transparent) && !ToolbarBackgroundColor.Is(Color.Inherit))
        .AddRaw(ToolbarStyle)
        .Build();

    private Task ZoomInClick() => ZoomBy(1.3);
    private Task ZoomOutClick() => ZoomBy(0.8);
    private Task ResetClick() => ResetZoom();
    private Task FullscreenClick() => ToggleFullScreen();
    
}
