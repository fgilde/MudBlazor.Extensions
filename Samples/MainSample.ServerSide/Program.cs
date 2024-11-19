using MainSample.WebAssembly;
using MainSample.WebAssembly.ObjectEditMetaConfig;
using MudBlazor;
using MudBlazor.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddMudServicesWithExtensions(AppConstants.MudExConfiguration ,typeof(LocalStorageService).Assembly);

builder.Services.AddMudMarkdownServices();
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<SampleDataService>();
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
app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
//app.UseMudExtensions();

app.Run();
