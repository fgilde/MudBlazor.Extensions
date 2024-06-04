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
    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : IDialogEvent
    {
        var handlers = eventHandlers.GetOrAdd(typeof(TEvent), _ => new List<Delegate>());
        lock (handlers)
        {
            handlers.Add(handler);
        }
    }

    /// <summary>
    /// Unsubscribe from a dialog event
    /// </summary>
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

    /// <summary>
    /// Publish a dialog event
    /// </summary>
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
