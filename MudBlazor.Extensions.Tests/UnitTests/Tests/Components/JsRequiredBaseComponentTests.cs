using Bunit;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Components.Base;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

public class JsRequiredBaseComponentTests
{
    public sealed class JsLifetimeProbe : MudExJsRequiredBaseComponent<JsLifetimeProbe>;

    public sealed class JsLifetimeFormProbe : MudExJsRequiredBaseFormComponent<JsLifetimeFormProbe, string, string>;

    private sealed class Runtime(IJSRuntime inner, IJSObjectReference module) : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args) =>
            identifier == "import" && args?.OfType<string>().Any(a => a.Contains("JsLifetime", StringComparison.Ordinal)) == true
                ? ValueTask.FromResult((TValue)module)
                : inner.InvokeAsync<TValue>(identifier, cancellationToken, args);
    }

    private sealed class Reference(Task<IJSObjectReference> created = null) : IJSObjectReference
    {
        public List<string> Calls { get; } = [];

        public bool Disposed { get; private set; }

        public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args)
        {
            Calls.Add(identifier);
            return created != null && typeof(TValue) == typeof(IJSObjectReference) ? (TValue)await created : default!;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args) => InvokeAsync<TValue>(identifier, args);

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private static async Task<(Reference Js, Reference Module)> DisposeWhileImporting(Func<IJSRuntime, Task> import, Func<ValueTask> dispose)
    {
        await using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        var created = new TaskCompletionSource<IJSObjectReference>();
        var module = new Reference(created.Task);

        var importing = import(new Runtime(context.JSInterop.JSRuntime, module));
        await dispose();
        var js = new Reference();
        created.SetResult(js);
        await importing;
        return (js, module);
    }

    [Fact]
    public async Task Component_DisposedWhileImporting_ReleasesTheJsObjectAndTheModule()
    {
        var component = new JsLifetimeProbe();

        var (js, module) = await DisposeWhileImporting(runtime =>
        {
            component.JsRuntime = runtime;
            return component.ImportModuleAndCreateJsAsync();
        }, component.DisposeAsync);

        Assert.Equal(["dispose"], js.Calls);
        Assert.True(js.Disposed);
        Assert.True(module.Disposed);
        Assert.Null(component.JsReference);
        Assert.Null(component.ModuleReference);
    }

    [Fact]
    public async Task FormComponent_DisposedWhileImporting_ReleasesTheJsObjectAndTheModule()
    {
        var component = new JsLifetimeFormProbe();

        var (js, module) = await DisposeWhileImporting(runtime =>
        {
            component.JsRuntime = runtime;
            return component.ImportModuleAndCreateJsAsync();
        }, component.DisposeAsync);

        Assert.Equal(["dispose"], js.Calls);
        Assert.True(js.Disposed);
        Assert.True(module.Disposed);
        Assert.Null(component.JsReference);
        Assert.Null(component.ModuleReference);
    }
}
