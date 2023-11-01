using MainSample.WebAssembly;
using MainSample.WebAssembly.ObjectEditMetaConfig;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Extensions;
using MudBlazor.Extensions.CodeGator.Adapter;
using MudBlazor.Extensions.Options;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<SampleDataService>();
builder.Services.AddMudServicesWithExtensions(AppConstants.MudExConfiguration);


// CodeGator Adapter for MudExObjectEdit
builder.Services.AddMudExObjectEditCGBlazorFormsAdapter();
builder.Services.AddFormGeneration(); // CodeGator. Just to show a compare for models with CG Attributes

MySimpleTypeRegistrations.RegisterRenderDefaults();

await builder.Build().RunAsync();
