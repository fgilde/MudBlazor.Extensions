using Microsoft.AspNetCore.Components;
using System.Reflection;

namespace MudBlazor.Extensions.Core;

public interface IMudExDialogReference<out TComponent> : IDialogReference where TComponent : ComponentBase
{
    TComponent DialogComponent { get; }
    void ExecuteOnDialogComponent(Action<TComponent> action);
    void CallStateHasChanged();
    IDialogReference DialogReference { get; }
}

public static class MudExDialogReferenceExtensions
{
    public static IMudExDialogReference<TComponent> AsMudExDialogReference<TComponent>(this IDialogReference dialogReference)
        where TComponent : ComponentBase => new MudExDialogRef<TComponent>(dialogReference);
    public static async Task<IMudExDialogReference<TComponent>> AsMudExDialogReferenceAsync<TComponent>(this Task<IDialogReference> dialogReference)
        where TComponent : ComponentBase
    {
        return (await dialogReference).AsMudExDialogReference<TComponent>();
    }

    public static TComponent GetDialogComponent<TComponent>(this IDialogReference dialogReference) where TComponent : ComponentBase => dialogReference.AsMudExDialogReference<TComponent>().DialogComponent;
}

internal class MudExDialogRef<T> : IMudExDialogReference<T> where T : ComponentBase
{
    public IDialogReference DialogReference { get; private set; }
    private readonly SynchronizationContext _synchronizationContext;

    public MudExDialogRef(IDialogReference dialogReference)
    {
        _synchronizationContext = SynchronizationContext.Current;
        DialogReference = dialogReference;
    }
    
    public void Close() => DialogReference.Close();

    public void Close(DialogResult result) => DialogReference.Close(result);

    public bool Dismiss(DialogResult result) => DialogReference.Dismiss(result);

    public void InjectRenderFragment(RenderFragment rf) => DialogReference.InjectRenderFragment(rf);

    public void InjectDialog(object inst) => DialogReference.InjectDialog(inst);

    public Task<T1> GetReturnValueAsync<T1>() => DialogReference.GetReturnValueAsync<T1>();
    
    public Guid Id => DialogReference.Id;
    public RenderFragment RenderFragment { 
        get => DialogReference.RenderFragment;
        set => DialogReference.RenderFragment = value;
    }
    public Task<DialogResult> Result => DialogReference.Result;

    public TaskCompletionSource<bool> RenderCompleteTaskCompletionSource => DialogReference.RenderCompleteTaskCompletionSource;

    public object Dialog => DialogReference.Dialog;
    public T DialogComponent => Dialog as T;

    private void Invoke(Action action)
    {
        SynchronizationContext context = _synchronizationContext ?? SynchronizationContext.Current;
        if (context == null)
        {
            action();
        }
        else
        {
            context.Post(_ => action(), null);
        }
    }
    
    public void ExecuteOnDialogComponent(Action<T> action)
    {
        Invoke(() =>
        {
            action(DialogComponent);
            CallStateHasChanged();
        });
    }

    public void CallStateHasChanged()
    {
        Invoke(() =>
        {
            var methodInfo = Dialog?.GetType().GetMethod("StateHasChanged", BindingFlags.Instance | BindingFlags.NonPublic);
            methodInfo?.Invoke(Dialog, Array.Empty<object>());
        });
    }
}