using MainSample.WebAssembly;
using MainSample.WebAssembly.ObjectEditMetaConfig;
using MainSample.WebAssembly.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Extensions;
using MudBlazor.Extensions.CodeGator.Adapter;
using System.Globalization;
using AKSoftware.Localization.MultiLanguages;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<SampleDataService>();
builder.Services.AddSingleton<DeploymentsService>();
builder.Services.AddMudServicesWithExtensions(AppConstants.MudExConfiguration);


// CodeGator Adapter for MudExObjectEdit
builder.Services.AddMudExObjectEditCGBlazorFormsAdapter();
builder.Services.AddFormGeneration(); // CodeGator. Just to show a compare for models with CG Attributes
builder.Services.AddYamlLocalizer();

MySimpleTypeRegistrations.RegisterRenderDefaults();


var host = builder.Build();

var cultureInfo = new CultureInfo("en-US");
CultureInfo.CurrentUICulture = cultureInfo;
CultureInfo.CurrentCulture = cultureInfo;
host.Services.GetRequiredService<ILanguageContainerService>().SetLanguage(cultureInfo);

await builder.Build().RunAsync();
