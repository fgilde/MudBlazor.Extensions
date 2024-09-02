namespace MudBlazor.Extensions.Core;

/// <summary>
/// Dialog event service
/// </summary>
public interface IDialogEventService
{

    /// <summary>
    /// Subscribe to a dialog event
    /// </summary>
    public IDialogEventService Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IDialogEvent;


    /// <summary>
    /// Unsubscribe from a dialog event
    /// </summary>
    public IDialogEventService Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IDialogEvent;

    /// <summary>
    /// Publish a dialog event
    /// </summary>
    public Task<TEvent> Publish<TEvent>(TEvent eventToPublish) where TEvent : IDialogEvent;
}