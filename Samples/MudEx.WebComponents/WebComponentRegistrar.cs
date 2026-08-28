using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Helper;

namespace MudEx.WebComponents;

/// <summary>
/// Registers every MudEx component that can be exposed as a browser custom element.
/// </summary>
public static class WebComponentRegistrar
{
    /// <summary>
    /// Tags that have been registered successfully.
    /// </summary>
    public static IReadOnlyList<string> RegisteredTags { get; private set; } = Array.Empty<string>();

    public static void RegisterAll(RootComponentMappingCollection rootComponents)
    {
        var registered = new List<string>();
        var register = typeof(WebComponentRegistrar).GetMethod(nameof(Register),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        foreach (var (type, tag) in MudExWebComponents.GetRegistrableComponents(typeof(MudExFileDisplay).Assembly))
        {
            try
            {
                register!.MakeGenericMethod(type).Invoke(null, new object[] { rootComponents, tag });
                registered.Add(tag);
            }
            catch (Exception e)
            {
                // A single component that cannot be registered must not take the whole bundle down.
                Console.WriteLine($"[MudEx] skipped <{tag}> ({type.Name}): {e.InnerException?.Message ?? e.Message}");
            }
        }

        RegisteredTags = registered;
        Console.WriteLine($"[MudEx] registered {registered.Count} web components");
    }

    private static void Register<TComponent>(RootComponentMappingCollection rootComponents, string tag)
        where TComponent : IComponent
        => rootComponents.RegisterCustomElement<TComponent>(tag);
}
