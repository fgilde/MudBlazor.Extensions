using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Components.Base;

/// <summary>
/// Base component for a picker
/// </summary>
public partial class MudExPickerBase<T>
{
    private IStringLocalizer<MudExPickerBase<T>> _fallbackLocalizer => ServiceProvider.GetService<IStringLocalizer<MudExPickerBase<T>>>();
    
    /// <summary>
    /// Is true if the component is rendered
    /// </summary>
    protected bool Rendered { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="IServiceProvider"/> to be used for dependency injection.
    /// </summary>
    [Inject]
    protected IServiceProvider ServiceProvider { get; set; }

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
    /// tries to localize given string
    /// </summary>
    public string TryLocalize(string text, params object[] args) => LocalizerToUse.TryLocalize(text, args);


    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender || !Rendered)
            Rendered = true;
        base.OnAfterRender(firstRender);
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
    
}