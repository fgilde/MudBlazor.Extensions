using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using MudBlazor.Interop;
using Nextended.Core.Extensions;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Core.Css;

namespace MudBlazor.Extensions.Components.Base;

/// <summary>
/// Base component for a picker
/// </summary>
public partial class MudExPickerBase<T>
{
    private IStringLocalizer<MudExPickerBase<T>> _fallbackLocalizer => ServiceProvider.GetService<IStringLocalizer<MudExPickerBase<T>>>();
    private ElementReference _pickerInlineRef;
    private Task CloseOverlayAsync() => CloseAsync(PickerActions == null);
    private double? _currentWidth;

    /// <summary>
    /// Is true if init was called
    /// </summary>
    protected bool IsInitialized;

    /// <summary>
    /// Is true if the component is rendered
    /// </summary>
    protected bool Rendered { get; set; }

    /// <summary>
    /// This animation will be used for the picker if the <see cref="PickerVariant"/> is <see cref="PickerVariant.Inline"/>.
    /// </summary>
    [Parameter]
    public AnimationType PopverAnimation { get; set; } = AnimationType.Pulse;

    /// <summary>
    /// Contains all parameters before init
    /// </summary>
    protected string[] PreInitParameters;

    /// <summary>
    /// Checks if a parameter is overwritten by user
    /// </summary>
    protected bool IsOverwritten(string paramName) => PreInitParameters?.Contains(paramName) == true;

    /// <summary>
    /// Returns true if any of the parameters is overwritten
    /// </summary>
    protected bool IsOverwritten(string[] paramName) => PreInitParameters?.Any(paramName.Contains) == true;

    /// <summary>
    /// The dialog options to be used for the picker if the <see cref="PickerVariant"/> is <see cref="PickerVariant.Dialog"/>.
    /// </summary>
    [Parameter]
    public DialogOptionsEx DialogOptions { get; set; }

    /// <summary>
    /// Simple possibility to set the border color of the picker.
    /// </summary>
    [Parameter]
    public MudExColor BorderColor { get; set; }

    /// <summary>
    /// Style of the picker.
    /// </summary>
    [Parameter]
    public string PickerStyle { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="IServiceProvider"/> to be used for dependency injection.
    /// </summary>
    [Inject]
    public IServiceProvider ServiceProvider { get; protected set; }

    /// <summary>
    /// Gets or sets the <see cref="IJSRuntime"/> to be used.
    /// </summary>
    [Inject]
    protected IJSRuntime JsRuntime { get; set; }

    /// <summary>
    /// Gets the <see cref="IStringLocalizer"/> to be used for localizing strings.
    /// </summary>
    private IStringLocalizer LocalizerToUse => Localizer ?? _fallbackLocalizer;

    /// <summary>
    /// Gets or sets the <see cref="IStringLocalizer"/> to be used for localizing strings.
    /// </summary>
    [Parameter, SafeCategory("Common")]
    public IStringLocalizer Localizer { get; set; }
    
    /// <summary>
    /// Gets or sets the value of the color picker.
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public T Value
    {
        get => _value;
        set
        {
            var org = _value;
            if(BeforeValueChanged(org, value))
            {
                _value = value;
                AfterValueChanged(org, value);
            }
        }
    }

    private int GetActiveElevation()
    {
        return Elevation;
    }

    /// <summary>
    /// Called before the value is changed. If the return value is false, the value will not be changed.
    /// </summary>
    /// <returns></returns>
    protected virtual bool BeforeValueChanged(T from, T to)
    {
        return !Equals(from, to);
    }

    /// <summary>
    /// Called after the value is changed.
    /// </summary>
    protected virtual void AfterValueChanged(T from, T to)
    {
        RaiseChangedIf();
    }

    /// <summary>
    /// Gets or sets the callback method when the value of the color picker is changed.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public EventCallback<T> ValueChanged { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to delay the value change of the color picker until the picker is closed.
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool DelayValueChangeToPickerClose { get; set; }

    /// <summary>
    /// If this is set to true, the picker will fit the width of the parent container.
    /// This is only has effect if <see cref="PickerVariant"/> is set to <see cref="PickerVariant.Inline"/>.
    /// </summary>
    [Parameter]
    public bool BindWidthToPicker { get; set; } = true;

    /// <summary>
    /// tries to localize given string
    /// </summary>
    public string TryLocalize(string text, params object[] args) => LocalizerToUse.TryLocalize(text, args);

    /// <summary>
    /// If this is set to true, the picker will open also if the input is read only.
    /// </summary>
    [Parameter]
    public bool AllowOpenOnReadOnly { get; set; }

    /// <summary>
    /// Id for picker element
    /// </summary>
    protected string Id = $"mud-ex-picker-{Guid.NewGuid().ToFormattedId()}";

    /// <inheritdoc />
    public override Task SetParametersAsync(ParameterView parameters)
    {
        if (!IsInitialized)
            PreInitParameters = parameters.ToDictionary().Select(x => x.Key).ToArray();
        return base.SetParametersAsync(parameters);
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        IsInitialized = true;
        UserAttributes ??= new();
        UserAttributes.AddOrUpdate("data-picker-id", Id);
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender || !Rendered)
        {
            Rendered = true;
        }

        base.OnAfterRender(firstRender);
    }

    protected internal async Task ToggleStateExtAsync()
    {
        if (GetDisabledState() || (GetReadOnlyState() && !AllowOpenOnReadOnly))
            return;
        if (Open)
        {
            Open = false;
            await OnClosedAsync();
        }
        else
        {
            Open = true;
            await OnOpenedAsync();
            await FocusAsync();
        }
    }

    private async Task OnClickAsync(MouseEventArgs args)
    {
        if (!Editable)
        {
            await ToggleStateExtAsync();
        }

        if (OnClick.HasDelegate)
        {
            await OnClick.InvokeAsync(args);
        }
    }


    /// <inheritdoc />
    protected override async Task OnOpenedAsync()
    {
        await base.OnOpenedAsync();
        await BindPickerWidthAsync();
    }
    
    private async Task BindPickerWidthAsync()
    {
        if (BindWidthToPicker && PickerVariant == PickerVariant.Inline)
        {
            var el = _inputReference?.InputReference?.ElementReference;
            var selector = $"[data-picker-id=\"{Id}\"]";
            if (el != null)
                await JsRuntime.InvokeVoidAsync("MudExDomHelper.syncSize", el, selector, null, DotNetObjectReference.Create(this));
        }
    }

    /// <summary>
    /// Called when the size of the picker is changed by binding.
    /// </summary>
    [JSInvokable]
    public Task OnSyncResized(BoundingClientRect size, ElementReference? owneReference, ElementReference? targetReference)
    {
        _currentWidth = size.Width;
        return OnSyncResized(size);
    }

    /// <summary>
    /// Called when the size of the picker is changed by binding.
    /// </summary>
    protected virtual Task OnSyncResized(BoundingClientRect size)
    {
        return Task.CompletedTask;
    }


    /// <inheritdoc />
    protected override Task OnPickerClosedAsync()
    {
        if (DelayValueChangeToPickerClose)
            RaiseChanged();
        return base.OnPickerClosedAsync();
    }
    
    /// <summary>
    /// Raises the <see cref="ValueChanged"/> event.
    /// </summary>
    protected virtual void RaiseChanged()
    {
        ValueChanged.InvokeAsync(Value);
    }


    /// <summary>
    /// Calls the <see cref="RaiseChanged"/> method if picker is closed or <see cref="DelayValueChangeToPickerClose"/> is false.
    /// </summary>
    protected virtual void RaiseChangedIf()
    {
        if (DelayValueChangeToPickerClose && Open)
            return;
        RaiseChanged();
    }

    private string GetPopOverStyle()
    {
        return MudExStyleBuilder.Default
            .WithWidth(_currentWidth ?? 0, BindWidthToPicker && _currentWidth.HasValue)
            .Style;
    }

    private string GetPickerPaperStyle()
    {
        return MudExStyleBuilder.FromStyle(PickerPaperStylename)
            .WithBorder(1, BorderStyle.Solid, BorderColor, BorderColor.IsSet())
            .AddRaw(PickerStyle)
            .Style;
    }

    private DialogOptionsEx GetDialogOptions()
    {
        var options = (DialogOptions ?? DialogOptionsEx.DefaultDialogOptions).CloneOptions();
        options.DialogAppearance = MudExAppearance.FromStyle(b => 
            b.WithBorder(1, BorderStyle.Solid, BorderColor, BorderColor.IsSet())
                .AddRaw(PickerStyle)
            );
        return options;
    }
}