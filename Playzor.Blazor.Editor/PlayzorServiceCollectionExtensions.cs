using Blazored.LocalStorage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using MudBlazor.Extensions;
using MudBlazor.Extensions.Options;
using Playzor.Blazor.Editor.Services;
using Playzor.Core;
using Playzor.Core.Api;

namespace Playzor.Blazor.Editor;

/// <summary>
/// Registration of everything <see cref="Components.PlayzorEditor"/> resolves from the container.
/// </summary>
public static class PlayzorServiceCollectionExtensions
{
    /// <summary>
    /// Adds the playground services. Everything is registered with TryAdd, so a host that already
    /// brought its own local storage, localizer, api or compilation service keeps it.
    /// <para>
    /// The editor also needs MudBlazor and MudBlazor.Extensions. Pass <paramref name="configureMudEx"/>
    /// to let this method add them, or leave it null when the host calls
    /// <c>AddMudServicesWithExtensions</c> itself (it usually wants its own configuration).
    /// </para>
    /// <para>
    /// An <see cref="System.Net.Http.HttpClient"/> with the app base address has to be registered
    /// by the host.
    /// </para>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureApi">
    /// Where the package proxy lives. Without it the editor calls its own origin, which is what a
    /// host running <c>MapPlayzorApi()</c> wants. Set
    /// <c>o.BaseAddress = PlayzorApiOptions.PlayzorNet</c> to borrow the public playground instead.
    /// </param>
    /// <param name="configureMudEx">Optional MudBlazor.Extensions configuration.</param>
    public static IServiceCollection AddPlayzor(this IServiceCollection services,
        Action<PlayzorApiOptions> configureApi = null,
        Action<MudExConfiguration> configureMudEx = null)
    {
        if (configureMudEx != null)
            services.AddMudServicesWithExtensions(configureMudEx);

        if (services.All(s => s.ServiceType != typeof(ILocalStorageService)))
            services.AddBlazoredLocalStorage();

        // the monaco interop is synchronous, which webassembly allows
        services.TryAddSingleton(sp => (IJSInProcessRuntime)sp.GetRequiredService<IJSRuntime>());

        var apiOptions = new PlayzorApiOptions();
        configureApi?.Invoke(apiOptions);
        services.TryAddSingleton(apiOptions);
        services.TryAddScoped<IPlayzorApi>(sp =>
            new PlayzorApi(sp.GetRequiredService<HttpClient>(), sp.GetRequiredService<PlayzorApiOptions>()));

        services.TryAddScoped<PlayzorLocalizer>();
        services.TryAddScoped<IStringLocalizer>(sp => sp.GetRequiredService<PlayzorLocalizer>());

        services.TryAddScoped<CompilationService>();
        services.TryAddScoped<NugetReferenceService>();
        services.TryAddScoped<NuGetPackageSearcher>();

        return services;
    }

    /// <summary>
    /// Adds the http snippet store, so the editor's save and samples buttons work against the
    /// endpoints of the Playzor.Server package without the host wiring any events.
    /// </summary>
    public static IServiceCollection AddPlayzorHttpSnippetStore(this IServiceCollection services)
    {
        services.TryAddScoped<IPlayzorSnippetStore>(sp =>
            new PlayzorHttpSnippetStore(sp.GetRequiredService<HttpClient>(), sp.GetRequiredService<PlayzorApiOptions>()));
        return services;
    }
}
