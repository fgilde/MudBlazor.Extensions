namespace MudBlazor.Extensions.Core;

public interface IDialogEventService
{
    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IDialogEvent;
    public void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IDialogEvent;
    public Task Publish<TEvent>(TEvent eventToPublish) where TEvent : IDialogEvent;
}