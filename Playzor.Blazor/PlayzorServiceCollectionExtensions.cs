using Blazored.LocalStorage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using MudBlazor.Extensions;
using MudBlazor.Extensions.Options;
using Playzor.Blazor.Services;
using Try.Core;

namespace Playzor.Blazor;

/// <summary>
/// Registration of everything <see cref="Components.PlayzorEditor"/> resolves from the container.
/// </summary>
public static class PlayzorServiceCollectionExtensions
{
    /// <summary>
    /// Adds the playground services. Everything is registered with TryAdd, so a host that already
    /// brought its own local storage, localizer or compilation service keeps it.
    /// <para>
    /// The editor also needs MudBlazor and MudBlazor.Extensions. Pass <paramref name="configureMudEx"/>
    /// to let this method add them, or leave it null when the host calls
    /// <c>AddMudServicesWithExtensions</c> itself (it usually wants its own configuration).
    /// </para>
    /// <para>
    /// An <see cref="System.Net.Http.HttpClient"/> with the app base address has to be registered by
    /// the host: the package cannot know where the nuget proxy of that app lives.
    /// </para>
    /// </summary>
    public static IServiceCollection AddPlayzor(this IServiceCollection services, Action<MudExConfiguration> configureMudEx = null)
    {
        if (configureMudEx != null)
            services.AddMudServicesWithExtensions(configureMudEx);

        if (services.All(s => s.ServiceType != typeof(ILocalStorageService)))
            services.AddBlazoredLocalStorage();

        // the monaco interop is synchronous, which webassembly allows
        services.TryAddSingleton(sp => (IJSInProcessRuntime)sp.GetRequiredService<IJSRuntime>());

        services.TryAddScoped<PlayzorLocalizer>();
        services.TryAddScoped<IStringLocalizer>(sp => sp.GetRequiredService<PlayzorLocalizer>());

        services.TryAddScoped<CompilationService>();
        services.TryAddScoped<NugetReferenceService>();
        services.TryAddScoped<NuGetPackageSearcher>();

        return services;
    }
}
