using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using Nextended.Core.Extensions;
using System.Timers;
using MudBlazor.Extensions.Core;


namespace MudBlazor.Extensions.Components.Base;

/// <summary>
/// Base component for the most of all MudExComponents
/// </summary>
public abstract class MudExBaseComponent<T> : MudComponentBase, IMudExComponent, IAsyncDisposable
    where T : MudExBaseComponent<T>
{
    private object _previousKey;
    private System.Timers.Timer _renderFinishTimer;
    private readonly TaskCompletionSource _renderedCompletionSource = new();

    private IStringLocalizer<T> _fallbackLocalizer => Get<IStringLocalizer<T>>();

    /// <summary>
    /// Is true if init was called
    /// </summary>
    protected bool IsInitialized;

    /// <summary>
    /// Contains all parameters before init
    /// </summary>
    protected string[] PreInitParameters;

    /// <summary>
    /// Checks if a parameter is overwritten by user
    /// </summary>
    protected bool IsOverwritten(string paramName) => PreInitParameters?.Contains(paramName) == true;

    
    /// <summary>
    /// Is true if dispose was called
    /// </summary>
    protected bool IsDisposed { get; private set; }

    /// <summary>
    /// Set value and call callback if value is changed
    /// </summary>
    protected TValue Set<TValue>(ref TValue field, TValue value, Action<TValue> callback = null)
    {
        if (Equals(field, value))
            return field;

        field = value;
        callback?.Invoke(value);
        return field;
    }

    /// <summary>
    /// Element id
    /// </summary>
    [Parameter, IgnoreOnObjectEdit, SafeCategory("Common")]
    public string ElementId { get; set; } = Guid.NewGuid().ToFormattedId();

    /// <summary>
    /// Localizer for localize texts
    /// </summary>
    [Parameter] public IStringLocalizer Localizer { get; set; }

    /// <summary>
    /// Render key for refresh component
    /// </summary>    
    [Parameter, IgnoreOnObjectEdit] public object RenderKey { get; set; }

    /// <summary>
    /// Injected service provider
    /// </summary>
    [Inject] protected IServiceProvider ServiceProvider { get; set; }

    /// <summary>
    /// JsRuntime
    /// </summary>
    [Inject] public IJSRuntime JsRuntime { get; set; }

    /// <summary>
    /// MudExConfiguration
    /// </summary>    
    protected MudExConfiguration MudExConfiguration => Get<MudExConfiguration>();

    /// <summary>
    /// This is true if render has called already
    /// </summary>
    public bool IsRendered { get; protected set; }

    /// <summary>
    /// This is true if render has called already and finished all after render calls
    /// </summary>
    public bool IsFullyRendered { get; protected set; }

    /// <summary>
    /// DialogService
    /// </summary>
    protected IDialogService DialogService => Get<IDialogService>();

    /// <summary>
    /// The localizer to use
    /// </summary>
    protected IStringLocalizer LocalizerToUse => Localizer ?? _fallbackLocalizer;

    /// <summary>
    /// Returns the injected service for TService
    /// </summary>
    protected TService Get<TService>() => ServiceProvider != null ? ServiceProvider.GetService<TService>() : default;

    /// <summary>
    /// Returns the injected services for TService
    /// </summary>
    protected IEnumerable<TService> GetServices<TService>() => ServiceProvider?.GetServices<TService>();

    /// <summary>
    /// Tries to localize given text if localizer and translation is available
    /// </summary>
    public string TryLocalize(string text, params object[] args) => LocalizerToUse.TryLocalize(text, args);

    /// <summary>
    /// returns true if translation is available
    /// </summary>
    public bool IsLocalized(string text, params object[] args) => LocalizerToUse.IsLocalized(text, args);

    /// <summary>
    /// Returns a task that is completed after element is fully rendered
    /// </summary>
    public Task EnsureFullyRenderedAsync() => _renderedCompletionSource.Task;

    /// <inheritdoc />
    public override Task SetParametersAsync(ParameterView parameters)
    {
        if (!IsInitialized)
            PreInitParameters = parameters.ToDictionary().Select(x => x.Key).ToArray();
        return base.SetParametersAsync(parameters);
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        IsInitialized = true;
    }

    /// <inheritdoc />
    protected override bool ShouldRender()
    {
        if (base.ShouldRender() || !Equals(_previousKey, RenderKey))
        {
            _previousKey = RenderKey;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Calls StateHasChanged
    /// </summary>
    protected virtual void CallStateHasChanged(StateChangeMode mode = StateChangeMode.Auto)
    {
        mode = mode == StateChangeMode.Auto ? (MudExResource.IsClientSide ? StateChangeMode.Synchronous : StateChangeMode.Asynchronous) : mode;
        if (mode == StateChangeMode.Synchronous)
            StateHasChanged();
        else
            StateHasChangedAsync();
    }

    /// <summary>
    /// Async state change invocation
    /// </summary>    
    protected Task StateHasChangedAsync() => InvokeAsync(StateHasChanged);


    /// <summary>
    /// Called when rendering is finished
    /// </summary>
    protected virtual Task OnFinishedRenderAsync()
    {
        _renderedCompletionSource.SetResult();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender && MudExConfiguration is not { DisableAutomaticCssLoading: true })
            await JsRuntime.LoadCssAsync();
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
        IsRendered = true;

        if (_renderFinishTimer != null)
        {
            _renderFinishTimer.Stop();
            _renderFinishTimer.Elapsed -= OnRenderFinishTimerElapsed;  // Unhook the event
            _renderFinishTimer.Dispose();
        }

        _renderFinishTimer = new System.Timers.Timer(300); // 300 milliseconds
        _renderFinishTimer.AutoReset = false;  // Make sure it ticks only once
        _renderFinishTimer.Elapsed += OnRenderFinishTimerElapsed;
        _renderFinishTimer.Start();
    }

    private void OnRenderFinishTimerElapsed(object sender, ElapsedEventArgs e)
    {
        InvokeAsync(async () =>
        {
            _renderFinishTimer?.Dispose();
            _renderFinishTimer = null;
            IsFullyRendered = true;
            await OnFinishedRenderAsync();
        });
    }

    /// <summary>
    /// Refreshes this component and forces render
    /// </summary>
    /// <returns></returns>
    public virtual T Refresh()
    {
        RenderKey = Guid.NewGuid();
        CallStateHasChanged();
        return (T)this;
    }


    /// <inheritdoc />
    public virtual async ValueTask DisposeAsync()
    {
        IsDisposed = true;
        _renderFinishTimer?.Stop();
        if (_renderFinishTimer is IAsyncDisposable renderFinishTimerAsyncDisposable)
            await renderFinishTimerAsyncDisposable.DisposeAsync();
        else
            _renderFinishTimer?.Dispose();
    }
}