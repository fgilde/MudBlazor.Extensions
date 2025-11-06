# Setup

This guide covers the complete setup process for MudBlazor.Extensions in your Blazor application.

## Step 1: Update Imports

Add the following namespaces to your `_Imports.razor` file:

```csharp
@using MudBlazor.Extensions
@using MudBlazor.Extensions.Components
@using MudBlazor.Extensions.Components.ObjectEdit
```

This will make the MudBlazor.Extensions components and utilities available throughout your application without needing to add using statements to each file.

## Step 2: Register Services

Register MudBlazor.Extensions services in your `Program.cs` (or `Startup.cs` for older projects).

### Option 1: Register with MudServices (Recommended)

This method registers both MudBlazor services and MudBlazor.Extensions in one call:

```csharp
builder.Services.AddMudServicesWithExtensions();
```

### Option 2: Register Separately

If you've already registered MudBlazor services, add MudBlazor.Extensions after:

```csharp
// MudBlazor services (already registered)
builder.Services.AddMudServices();

// Add MudBlazor.Extensions
builder.Services.AddMudExtensions();
```

!!! warning "Order Matters"
    Make sure to add MudBlazor.Extensions **after** MudBlazor services are registered.

## Step 3: Configure Options (Optional)

You can customize MudBlazor.Extensions behavior during registration:

### Default Dialog Options

```csharp
builder.Services.AddMudServicesWithExtensions(c =>
{
    c.WithDefaultDialogOptions(ex =>
    {
        ex.Position = DialogPosition.BottomRight;
        ex.CloseButton = true;
        ex.Resizeable = true;
    });
});
```

### Cloud Integration

Configure external storage providers for the file upload component:

```csharp
builder.Services.AddMudServicesWithExtensions(c =>
{
    c.EnableDropBoxIntegration("<DROP_BOX_API_KEY>")
     .EnableGoogleDriveIntegration("<GOOGLE_DRIVE_CLIENT_ID>")
     .EnableOneDriveIntegration("<ONE_DRIVE_CLIENT_ID>");
});
```

### Disable Automatic CSS Loading

If you prefer to load styles manually, disable automatic loading:

```csharp
builder.Services.AddMudServicesWithExtensions(c => 
    c.WithoutAutomaticCssLoading()
);
```

Then add the stylesheet manually to your `index.html` (WebAssembly) or `_Host.cshtml` (Server):

```html
<link id="mudex-styles" 
      href="_content/MudBlazor.Extensions/mudBlazorExtensions.min.css" 
      rel="stylesheet">
```

## Step 4: Blazor Server Middleware (Server Only)

If you're using Blazor Server, add the MudBlazor.Extensions middleware to your application pipeline:

```csharp
var app = builder.Build();

// Add MudEx middleware
app.Use(MudExWebApp.MudExMiddleware);

// ... other middleware
app.Run();
```

!!! info "WebAssembly Applications"
    This middleware is **not required** for Blazor WebAssembly applications.

## Step 5: OneDrive Integration (Optional)

If you're using OneDrive integration with the file upload component, add the OneDrive JavaScript SDK to your `index.html` or `_Host.cshtml`:

```html
<script src="https://js.live.net/v7.2/OneDrive.js" 
        type="text/javascript" 
        charset="utf-8"></script>
```

This ensures proper redirect handling after page reload.

## Complete Setup Examples

### Blazor WebAssembly

```csharp title="Program.cs"
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Extensions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => 
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Register MudBlazor and MudBlazor.Extensions
builder.Services.AddMudServicesWithExtensions(c =>
{
    c.WithDefaultDialogOptions(ex =>
    {
        ex.CloseButton = true;
        ex.MaxWidth = MaxWidth.Medium;
    });
});

await builder.Build().RunAsync();
```

### Blazor Server

```csharp title="Program.cs"
using MudBlazor.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Register MudBlazor and MudBlazor.Extensions
builder.Services.AddMudServicesWithExtensions();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Add MudEx middleware
app.Use(MudExWebApp.MudExMiddleware);

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
```

## Verify Setup

To verify your setup is correct:

1. Build your project without errors
2. Run your application
3. Try using a simple MudBlazor.Extensions component:

```razor
@page "/test"
@using MudBlazor.Extensions.Components

<MudExAppLoader />

<h3>Setup Test</h3>
<p>If you can see this page without errors, the setup is correct!</p>
```

## Troubleshooting

### Styles Not Loading

**Symptoms:** Components render but don't have proper styling

**Solutions:**
- Ensure automatic CSS loading is enabled (default)
- If loading manually, check the stylesheet link in your HTML file
- Clear browser cache and rebuild

### Services Not Registered

**Symptoms:** Dependency injection errors when using components

**Solutions:**
- Verify `AddMudServicesWithExtensions()` or `AddMudExtensions()` is called
- Check that it's called after MudBlazor services are registered
- Rebuild your project

### Middleware Issues (Blazor Server)

**Symptoms:** SignalR connection issues or component state problems

**Solutions:**
- Ensure `MudExWebApp.MudExMiddleware` is added to the pipeline
- Check middleware order - it should be added early
- Verify you're using Blazor Server (not needed for WebAssembly)

## Next Steps

Now that setup is complete, explore:

- [Components](../components/index.md) - Browse available components
- [Object Edit](../components/object-edit.md) - Auto-generate forms from models
- [Dialog Extensions](../extensions/dialog-extensions.md) - Enhanced dialog functionality

## Additional Resources

- [GitHub Repository](https://github.com/fgilde/MudBlazor.Extensions)
- [NuGet Package](https://www.nuget.org/packages/MudBlazor.Extensions)
- [Live Demo](https://mudex.azurewebsites.net/)
