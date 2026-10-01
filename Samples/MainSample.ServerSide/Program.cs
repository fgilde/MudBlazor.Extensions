using MainSample.WebAssembly;
using MainSample.WebAssembly.ObjectEditMetaConfig;
using MainSample.WebAssembly.Services;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.StaticFiles;
using MudBlazor;
using MudBlazor.Extensions;
using MudBlazor.Extensions.Helper;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
// Register the YAML localizer (ILanguageContainerService) just like the WebAssembly host does.
// The shared pages (BasePage / YamlLocalizer<T>) and their validators depend on it, so without
// this the server host fails to build any localized page (and DI ValidateOnBuild rejects it).
builder.Services.AddYamlLocalizer();
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddMudServicesWithExtensions(AppConstants.MudExConfiguration ,typeof(LocalStorageService).Assembly);

builder.Services.AddMudMarkdownServices();
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<SampleDataService>();
builder.Services.AddScoped<DeploymentsService>();
//builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped(sp => new HttpClient { });

MySimpleTypeRegistrations.RegisterRenderDefaults();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
var sampleContentTypes = new FileExtensionContentTypeProvider();
foreach (var (extension, contentType) in new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    [".ai"] = "application/vnd.adobe.illustrator", [".eps"] = "application/postscript",
    [".psd"] = "image/vnd.adobe.photoshop", [".psb"] = "image/vnd.adobe.photoshop",
    [".svgz"] = "image/svg+xml", [".tga"] = "image/x-tga", [".qoi"] = "image/qoi",
    [".pbm"] = "image/x-portable-bitmap", [".pgm"] = "image/x-portable-graymap", [".ppm"] = "image/x-portable-pixmap",
    [".avif"] = "image/avif", [".heic"] = "image/heic", [".heif"] = "image/heif",
    [".mht"] = "application/x-mimearchive", [".mhtml"] = "application/x-mimearchive",
    [".cbz"] = "application/vnd.comicbook+zip", [".cbr"] = "application/vnd.comicbook-rar",
    [".odt"] = "application/vnd.oasis.opendocument.text", [".ods"] = "application/vnd.oasis.opendocument.spreadsheet",
    [".odp"] = "application/vnd.oasis.opendocument.presentation", [".odg"] = "application/vnd.oasis.opendocument.graphics",
    [".xps"] = "application/vnd.ms-xpsdocument", [".oxps"] = "application/oxps",
    [".parquet"] = "application/vnd.apache.parquet", [".arrow"] = "application/vnd.apache.arrow.file",
    [".feather"] = "application/x-feather", [".dcm"] = "application/dicom"
})
    sampleContentTypes.Mappings[extension] = contentType;
app.UseStaticFiles(new StaticFileOptions { ContentTypeProvider = sampleContentTypes });

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.Use(MudExWebApp.MudExMiddleware);


app.Run();
