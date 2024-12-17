using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using System.Reflection;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Simple MessageDialog
/// </summary>
public partial class MudExMessageDialog
{
    private ComponentBase _component;
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; }

    /// <summary>
    /// The color of the dialog icon for when the dialog is used in relation to a particular color theme.
    /// </summary>
    [Parameter]
    public MudExColor IconColor { get; set; } = Color.Error;

    /// <summary>
    /// Gets or sets the class for the content of the dialog
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public string ContentClass { get; set; } = string.Empty;

    /// <summary>
    /// The CSS class to apply to the action buttons of the dialog.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public string ClassActions { get; set; }

    /// <summary>
    /// The CSS styles to apply to the content area of the dialog.
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public string ContentStyle { get; set; }

    /// <summary>
    /// Gets or sets the message of the dialog
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public string Message { get; set; }

    /// <summary>
    /// Gets or sets whether empty actions are allowed in the dialog
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool AllowEmptyActions { get; set; }

    /// <summary>
    /// Gets or sets the icon of the dialog
    /// </summary>
    [Parameter, SafeCategory("Appearance")]
    public string Icon { get; set; }

    /// <summary>
    /// Gets or sets the details of the dialog
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public IEnumerable<string> Details { get; set; }

    /// <summary>
    /// Gets or sets the buttons of the dialog
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public MudExDialogResultAction[] Buttons { get; set; }

    /// <summary>
    /// Gets or sets the content of the dialog
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public RenderFragment Content { get; set; }

    /// <summary>
    /// Gets or sets whether progress is shown in the dialog
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool ShowProgress { get; set; }

    /// <summary>
    /// Gets or sets the value of the progress shown in the dialog
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public int ProgressValue { get; set; }

    /// <summary>
    /// Gets or sets the minimum value of the progress shown in the dialog
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public double ProgressMin { get; set; }

    /// <summary>
    /// Gets or sets the maximum value of the progress shown in the dialog
    /// </summary>
    [Parameter, SafeCategory("Data")]
    public double ProgressMax { get; set; } = 100.0;

    /// <summary>
    /// If true StateHasChanged is not happen
    /// </summary>
    [Parameter, SafeCategory("Behavior")]
    public bool PreventStateHasChanged { get; set; }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        if(firstRender && Buttons?.Any(b => b.HasCondition) == true)
            UpdateConditions();
        base.OnAfterRender(firstRender);
    }

    private void UpdateConditions()
    {
        foreach (var btn in Buttons.Where(b => b.RenderedReference != null && b.HasCondition))
            btn.RenderedReference.SetDisabled(!btn.CanExecuted(Component));
    }

    /// <inheritdoc />
    protected override bool ShouldRender() => !PreventStateHasChanged && base.ShouldRender();

    /// <summary>
    /// Gets or sets the component associated with the dialog
    /// </summary>
    internal ComponentBase Component
    {
        get => _component;
        set
        {
            if(_component == value) return;
            _component = value;
            if (_component != null && Buttons?.Any(b => b.HasCondition) == true)
                AdjustEventCallbacks(_component); // Attach Event callbacks to update conditions for buttons
        }
    }

    private void AdjustEventCallbacks(ComponentBase component)
    {
        if(component == null) return;
        var eventCallbackProperties = component.GetType().GetProperties()
            .Where(p => p.PropertyType.IsGenericType &&
                        p.PropertyType.GetGenericTypeDefinition() == typeof(EventCallback<>));

        foreach (var prop in eventCallbackProperties)
        {
            var existingCallback = prop.GetValue(component);
            if (existingCallback == null) continue;

            var callbackType = prop.PropertyType.GetGenericArguments().FirstOrDefault();
            if (callbackType == null) continue;

            var method = GetType().GetMethod(nameof(WrapCallback), BindingFlags.NonPublic | BindingFlags.Instance)?.MakeGenericMethod(callbackType);
            if(method == null) continue;
            var newCallback = method.Invoke(this, new[] { existingCallback, prop.Name });
            prop.SetValue(component, newCallback);
        }
    }

    private EventCallback<T> WrapCallback<T>(EventCallback<T> existing, string propName)
    {
        var wrappedCallback = EventCallback.Factory.Create<T>(
            Component,
            (value) =>
            {
                if (existing.HasDelegate)
                {
                    // If we have an existing delegate, we invoke it to keep bindings working
                    existing.InvokeAsync(value);
                }
                else
                {
                    // Otherwise, we set the value of the property directly to ensure correct value in condition checks
                    var valueProperty = propName[..^"Changed".Length];
                    Component.GetType().GetProperty(valueProperty)?.SetValue(Component, value);
                }

                UpdateConditions();
            });

        return wrappedCallback;
    }

    void Submit(DialogResult result)
    {
        MudDialog.Close(result);     
    }

    /// <summary>
    /// Cancels the dialog
    /// </summary>
    void Cancel() => MudDialog.CloseAnimatedIf(JsRuntime);
}
