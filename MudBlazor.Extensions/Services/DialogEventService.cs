using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Extensions.Core;
using Nextended.Core.Attributes;

namespace MudBlazor.Extensions.Services;

[RegisterAs(typeof(IDialogEventService), RegisterAsImplementation = true, ServiceLifetime = ServiceLifetime.Singleton)]
public class DialogEventService : IDialogEventService
{
    private ConcurrentDictionary<Type, List<Delegate>> eventHandlers = new ConcurrentDictionary<Type, List<Delegate>>();

    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IDialogEvent
    {
        var handlers = eventHandlers.GetOrAdd(typeof(TEvent), _ => new List<Delegate>());
        lock (handlers)
        {
            handlers.Add(handler);
        }
    }

    public void Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IDialogEvent
    {
        if (eventHandlers.TryGetValue(typeof(TEvent), out var handlers))
        {
            lock (handlers)
            {
                handlers.RemoveAll(h => h.Equals(handler));
            }
        }
    }

    public Task Publish<TEvent>(TEvent eventToPublish) where TEvent : IDialogEvent
    {
        if (eventHandlers.TryGetValue(typeof(TEvent), out var handlers))
        {
            var tasks = handlers.Cast<Func<TEvent, Task>>().Select(handler => handler(eventToPublish));
            return Task.WhenAll(tasks);
        }
        return Task.CompletedTask;
    }
}
