using System.Reflection;
using BlazorPrettyCode;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Extensions;
using SampleApplication.Client;
using SampleApplication.Client.ObjectEditMetaConfig;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddBlazorPrettyCode();
builder.Services.AddMudServicesWithExtensions();
builder.Services.AddMudMarkdownServices();

MySimpleTypeRegistrations.RegisterRenderDefaults();

await builder.Build().RunAsync();
