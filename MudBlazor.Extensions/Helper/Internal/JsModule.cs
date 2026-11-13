using Microsoft.JSInterop;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Helper.Internal;

internal sealed class JsModule<T> : IAsyncDisposable where T : class
{
    private IJSObjectReference _module;
    private DotNetObjectReference<T> _dotnet;
    private bool _disposed;

    internal IJSObjectReference Js { get; private set; }

    internal async Task<IJSObjectReference> StartAsync(IJSRuntime runtime, T owner, string name, Func<DotNetObjectReference<T>, object[]> arguments)
    {
        if (_disposed || Js != null)
            return Js;
        var dotnet = DotNetObjectReference.Create(owner);
        try
        {
            await runtime.InitializeMudBlazorExtensionsCoreAsync();
            (_module, Js) = await runtime.ImportModuleAndCreateJsAsync<T>(name, arguments(dotnet));
            _dotnet = dotnet;
        }
        catch (Exception e) when (e is JSException or JSDisconnectedException or TaskCanceledException or ObjectDisposedException or InvalidOperationException)
        {
            dotnet.Dispose();
            return null;
        }

        if (!_disposed)
            return Js;
        await ReleaseAsync();
        return null;
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        await ReleaseAsync();
    }

    private async Task ReleaseAsync()
    {
        var (js, module, dotnet) = (Js, _module, _dotnet);
        (Js, _module, _dotnet) = (null, null, null);
        if (js != null)
        {
            try { await js.InvokeVoidAsync("dispose"); } catch { }
            try { await js.DisposeAsync(); } catch { }
        }

        if (module != null)
            try { await module.DisposeAsync(); } catch { }
        dotnet?.Dispose();
    }
}
