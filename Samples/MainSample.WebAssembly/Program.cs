using System.Reflection;

using MainSample.WebAssembly;
using MainSample.WebAssembly.ObjectEditMetaConfig;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Extensions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddMudServicesWithExtensions();
builder.Services.AddMudMarkdownServices();

MySimpleTypeRegistrations.RegisterRenderDefaults();

await builder.Build().RunAsync();
