using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Components;

internal sealed class WithoutRender : IHandleEvent
{
    public static readonly WithoutRender Instance = new();

    public Task HandleEventAsync(EventCallbackWorkItem item, object arg) => item.InvokeAsync(arg);
}
