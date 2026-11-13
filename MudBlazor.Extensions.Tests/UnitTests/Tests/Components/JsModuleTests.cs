using Bunit;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Helper.Internal;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

public class JsModuleTests
{
    private sealed class Runtime(IJSRuntime inner, IJSObjectReference module) : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args) =>
            identifier == "import" && args?.OfType<string>().Any(a => a.Contains("MudExSlideCanvas", StringComparison.Ordinal)) == true
                ? ValueTask.FromResult((TValue)module)
                : inner.InvokeAsync<TValue>(identifier, cancellationToken, args);
    }

    private sealed class Reference(Task<IJSObjectReference> created = null) : IJSObjectReference
    {
        public List<(string Identifier, object[] Args)> Calls { get; } = [];

        public bool Disposed { get; private set; }

        public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args)
        {
            Calls.Add((identifier, args));
            return created != null && typeof(TValue) == typeof(IJSObjectReference) ? (TValue)await created : default!;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args) => InvokeAsync<TValue>(identifier, args);

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task JsModule_DisposedWhileStarting_ReleasesTheModuleAndTheDotNetReference()
    {
        await using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        var created = new TaskCompletionSource<IJSObjectReference>();
        var module = new Reference(created.Task);
        var runtime = new Runtime(context.JSInterop.JSRuntime, module);
        var lifetime = new JsModule<MudExSlideCanvas>();
        var owner = new MudExSlideCanvas();

        var start = lifetime.StartAsync(runtime, owner, null, dotnet => [dotnet]);
        await lifetime.DisposeAsync();
        var js = new Reference();
        created.SetResult(js);

        Assert.Null(await start);
        Assert.Null(lifetime.Js);
        Assert.Equal(["dispose"], js.Calls.Select(c => c.Identifier));
        Assert.True(js.Disposed);
        Assert.True(module.Disposed);
        var initialize = Assert.Single(module.Calls);
        Assert.Equal("initializeMudExSlideCanvas", initialize.Identifier);
        var dotnet = Assert.IsType<DotNetObjectReference<MudExSlideCanvas>>(initialize.Args[0]);
        Assert.Throws<ObjectDisposedException>(() => dotnet.Value);
        Assert.Null(await lifetime.StartAsync(runtime, owner, null, d => [d]));
    }
}
