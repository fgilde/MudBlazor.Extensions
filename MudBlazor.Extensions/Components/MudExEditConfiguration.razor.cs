using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Options;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

public enum AutoConnectMode
{
    ToChild,
    ToParent
}

public partial class MudExEditConfiguration
{
    private bool _showConfig;

    [Inject] protected IDialogService DialogService { get; set; }

    [Parameter] public string Tooltip { get; set; } = "Edit Configuration";
    [Parameter] public string Icon { get; set; } = Icons.Material.Filled.Settings;
    [Parameter] public AutoConnectMode ConnectTo { get; set; } = AutoConnectMode.ToChild;
    [Parameter] public MudExColor ActiveColor { get; set; } = MudExColor.Primary;
    [Parameter] public bool AllowConfiguration { get; set; } = true;
    [Parameter] public RenderFragment? ChildContent { get; set; } = default!;
    [Parameter] public EventCallback<(Type controlType, object instance)> OnEditConfig { get; set; }
    [Parameter] public DialogOptionsEx? DialogOptions { get; set; }

    [Parameter] public object? ReferenceToEdit { get; set; }
    [CascadingParameter] public object? ParentInstance { get; set; }
    public object? ChildInstance { get; private set; }

    private void ChildCaptured(object obj)
    {
        ChildInstance = obj;
    }

    private object GetControlInstanceOrThrow()
    {
        var result = ReferenceToEdit ?? (ConnectTo == AutoConnectMode.ToChild ? ChildInstance : ParentInstance);
        if (result == null)
            throw new InvalidOperationException($"{nameof(MudExEditConfiguration)} requires a cascading parameter of type 'object' representing the control instance.");
        return result;
    }
    
    private async Task OnConfigClicked()
    {
        var controlInstance = GetControlInstanceOrThrow();

        if (OnEditConfig.HasDelegate)
        {
            await OnEditConfig.InvokeAsync((controlInstance.GetType(), controlInstance));
        }
        else
        {
            await DialogService.EditComponentAsync(controlInstance, options: GetOptions());
        }

    }

    private DialogOptionsEx GetOptions()
    {
        return DialogOptions ?? DialogOptionsEx.SlideInFromRight.SetProperties(o =>
        {
            o.Resizeable = true;
            o.DialogBackgroundAppearance = MudExAppearance.FromStyle(b =>
            {
                b.WithBackgroundColor(Color.Transparent)
                    .WithOpacity(0.0);
            });
        });
    }
}