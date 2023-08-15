using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Options;
using MudBlazor.Extensions.Services;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// MudExDialog is the component to use when you want to show a dialog inlined in your page with all DialogExtensions.
/// </summary>
public partial class MudExDialog : IMudExComponent
{
    private DialogOptionsEx _options;
    //private IDialogReference _reference;

    [Inject] private IJSRuntime Js { get; set; }
    [Inject] private MudExAppearanceService AppearanceService { get; set; }

    /// <summary>
    /// Render base component
    /// </summary>
    protected RenderFragment Inherited() => builder => base.BuildRenderTree(builder);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        EnsureInitialClass();
        base.OnParametersSet();
    }

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        if (parameters.TryGetValue<DialogOptions>(nameof(Options), out var options) && options is DialogOptionsEx)
            _options = (DialogOptionsEx) options;
        
        bool oldVisible = IsVisible;
        await base.SetParametersAsync(parameters);
        if (oldVisible != IsVisible)
        {
            if (IsVisible)
                await Show();
            else
                Close();
        }
    }


    /// <summary>Show this inlined dialog</summary>
    /// <param name="title"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public new async Task<IDialogReference> Show(string title = null, DialogOptions options = null)
    {
        OptionsEx.JsRuntime = Js;
        OptionsEx.AppearanceService = AppearanceService;
        await DialogServiceExt.PrepareOptionsBeforeShow(OptionsEx);
        return await base.Show(title, options).InjectOptionsAsync(OptionsEx);
    }

    /// <summary>
    /// DialogOptionsEx for this dialog
    /// </summary>
    [Parameter]
    public DialogOptionsEx OptionsEx
    {
        get => _options;
        set
        {
            _options = value;
            Options = value;
        }
    }

    private void EnsureInitialClass()
    {
        if (string.IsNullOrEmpty(Class))
            Class = "mud-ex-dialog-initial";
        if (!Class.Contains("mud-ex-dialog-initial"))
            Class += " mud-ex-dialog-initial";
    }
}