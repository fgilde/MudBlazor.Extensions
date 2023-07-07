using Microsoft.AspNetCore.Components;
using System.Reflection;

namespace MudBlazor.Extensions.Core;


/// <summary>
/// Interface for a MudBlazor dialog reference.
/// </summary>
public interface IMudExDialogReference<out TComponent> : IDialogReference where TComponent : ComponentBase
{
    /// <summary>
    /// Gets the dialog component.
    /// </summary>
    TComponent DialogComponent { get; }

    /// <summary>
    /// Executes an action on the dialog component.
    /// </summary>
    void ExecuteOnDialogComponent(Action<TComponent> action);

    /// <summary>
    /// Updates the state of the dialog component.
    /// </summary>
    void CallStateHasChanged();

    /// <summary>
    /// Gets the dialog reference.
    /// </summary>
    IDialogReference DialogReference { get; }
}

/// <summary>
/// Extension methods for IMudExDialogReference.
/// </summary>
public static class MudExDialogReferenceExtensions
{
    /// <summary>
    /// Converts a regular dialog reference to a MudBlazor dialog reference.
    /// </summary>
    public static IMudExDialogReference<TComponent> AsMudExDialogReference<TComponent>(this IDialogReference dialogReference)
        where TComponent : ComponentBase => new MudExDialogRef<TComponent>(dialogReference);

    /// <summary>
    /// Converts a regular dialog reference to a MudBlazor dialog reference asynchronously.
    /// </summary>
    public static async Task<IMudExDialogReference<TComponent>> AsMudExDialogReferenceAsync<TComponent>(this Task<IDialogReference> dialogReference)
        where TComponent : ComponentBase
    {
        return (await dialogReference).AsMudExDialogReference<TComponent>();
    }

    /// <summary>
    /// Gets the dialog component from a dialog reference.
    /// </summary>
    public static TComponent GetDialogComponent<TComponent>(this IDialogReference dialogReference) where TComponent : ComponentBase => dialogReference.AsMudExDialogReference<TComponent>().DialogComponent;
}


/// <summary>
/// Implementation of a MudBlazor dialog reference.
/// </summary>
internal class MudExDialogRef<T> : IMudExDialogReference<T> where T : ComponentBase
{
    /// <summary>
    /// The DialogReference
    /// </summary>
    public IDialogReference DialogReference { get; }

    /// <summary>
    /// The synchronization context to ensure thread safety.
    /// </summary>
    private readonly SynchronizationContext _synchronizationContext;

    /// <summary>
    /// Constructor for MudExDialogRef.
    /// </summary>
    public MudExDialogRef(IDialogReference dialogReference)
    {
        _synchronizationContext = SynchronizationContext.Current;
        DialogReference = dialogReference;
    }
    /// <summary>
    /// Closes the dialog.
    /// </summary>
    public void Close() => DialogReference.Close();

    /// <summary>
    /// Closes the dialog with a specific result.
    /// </summary>
    public void Close(DialogResult result) => DialogReference.Close(result);

    /// <summary>
    /// Dismisses the dialog with a specific result.
    /// </summary>
    public bool Dismiss(DialogResult result) => DialogReference.Dismiss(result);

    /// <summary>
    /// Injects a RenderFragment into the dialog.
    /// </summary>
    public void InjectRenderFragment(RenderFragment rf) => DialogReference.InjectRenderFragment(rf);

    /// <summary>
    /// Injects a dialog instance into the dialog.
    /// </summary>
    public void InjectDialog(object inst) => DialogReference.InjectDialog(inst);

    /// <summary>
    /// Gets the return value of the dialog asynchronously.
    /// </summary>
    public Task<T1> GetReturnValueAsync<T1>() => DialogReference.GetReturnValueAsync<T1>();

    /// <summary>
    /// Gets the ID of the dialog.
    /// </summary>
    public Guid Id => DialogReference.Id;
    public RenderFragment RenderFragment
    {
        get => DialogReference.RenderFragment;
        set => DialogReference.RenderFragment = value;
    }
    public Task<DialogResult> Result => DialogReference.Result;

    public TaskCompletionSource<bool> RenderCompleteTaskCompletionSource => DialogReference.RenderCompleteTaskCompletionSource;

    public object Dialog => DialogReference.Dialog;
    public T DialogComponent => Dialog as T;

    /// <summary>
    /// Invokes an action on the synchronization context.
    /// </summary>
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

    /// <summary>
    /// Executes an action on the dialog component and updates its state.
    /// </summary>
    public void ExecuteOnDialogComponent(Action<T> action)
    {
        Invoke(() =>
        {
            action(DialogComponent);
            CallStateHasChanged();
        });
    }

    /// <summary>
    /// Invokes the StateHasChanged method of the dialog.
    /// </summary>
    public void CallStateHasChanged()
    {
        Invoke(() =>
        {
            var methodInfo = Dialog?.GetType().GetMethod("StateHasChanged", BindingFlags.Instance | BindingFlags.NonPublic);
            methodInfo?.Invoke(Dialog, Array.Empty<object>());
        });
    }

}