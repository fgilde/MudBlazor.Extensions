using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Extensions.Core;
using Nextended.Core.Attributes;

namespace MudBlazor.Extensions.Services;

/// <summary>
/// Dialog event service
/// </summary>
[RegisterAs(typeof(IDialogEventService), RegisterAsImplementation = true, ServiceLifetime = ServiceLifetime.Singleton)]
public class DialogEventService : IDialogEventService
{
    private ConcurrentDictionary<Type, List<Delegate>> eventHandlers = new ConcurrentDictionary<Type, List<Delegate>>();

    /// <summary>
    /// Subscribe to a dialog event
    /// </summary>
    public IDialogEventService Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IDialogEvent
    {
        var handlers = eventHandlers.GetOrAdd(typeof(TEvent), _ => new List<Delegate>());
        lock (handlers)
        {
            handlers.Add(handler);
        }
        return this;
    }

    /// <summary>
    /// Unsubscribe from a dialog event
    /// </summary>
    public IDialogEventService Unsubscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IDialogEvent
    {
        if (eventHandlers.TryGetValue(typeof(TEvent), out var handlers))
        {
            lock (handlers)
            {
                handlers.RemoveAll(h => h.Equals(handler));
            }
        }

        return this;
    }

    /// <summary>
    /// Publish a dialog event
    /// </summary>
    public async Task<TEvent> Publish<TEvent>(TEvent eventToPublish) where TEvent : IDialogEvent
    {
        if (eventHandlers.TryGetValue(typeof(TEvent), out var handlers))
        {
            var tasks = handlers.Cast<Func<TEvent, Task>>().Select(handler => handler(eventToPublish));
            await Task.WhenAll(tasks);
        }
        return eventToPublish;
    }
}
