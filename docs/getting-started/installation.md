# Installation

This guide will walk you through the process of installing MudBlazor.Extensions in your Blazor project.

## Prerequisites

Before installing MudBlazor.Extensions, ensure you have:

- A Blazor project (WebAssembly, Server, or Hybrid)
- MudBlazor installed and configured
- .NET 6.0 or later

!!! info "MudBlazor Required"
    MudBlazor.Extensions requires MudBlazor to be installed. If you haven't installed MudBlazor yet, visit the [official MudBlazor documentation](https://mudblazor.com/getting-started/installation) for installation instructions.

## Installation Methods

### Using .NET CLI

The easiest way to install MudBlazor.Extensions is via the .NET CLI:

```bash
dotnet add package MudBlazor.Extensions
```

### Using Package Manager Console

In Visual Studio, you can use the Package Manager Console:

```powershell
Install-Package MudBlazor.Extensions
```

### Using Visual Studio NuGet Manager

1. Right-click on your project in Solution Explorer
2. Select "Manage NuGet Packages"
3. Search for "MudBlazor.Extensions"
4. Click "Install"

### Manual Package Reference

Add the package reference directly to your `.csproj` file:

```xml
<ItemGroup>
  <PackageReference Include="MudBlazor.Extensions" Version="*" />
</ItemGroup>
```

!!! tip "Version Selection"
    Using `Version="*"` will always get the latest version. For production, it's recommended to specify a specific version number.

## Verify Installation

After installation, verify that the package is correctly installed:

1. Build your project to ensure there are no errors
2. Check your project file to confirm the package reference exists
3. Restore packages if needed:

```bash
dotnet restore
```

## Platform-Specific Considerations

### Blazor WebAssembly

No additional steps required. The package works out of the box.

### Blazor Server

For Blazor Server applications, you may need to add middleware (see [Setup](setup.md) page).

### MAUI Blazor Hybrid

The package is compatible with MAUI Blazor Hybrid applications. Follow the standard installation steps.

## Troubleshooting

### Package Not Found

If you encounter issues finding the package:

1. Ensure your NuGet sources include `nuget.org`
2. Clear your NuGet cache:

```bash
dotnet nuget locals all --clear
```

3. Try restoring packages again:

```bash
dotnet restore
```

### Version Conflicts

If you encounter version conflicts with MudBlazor:

1. Ensure you're using compatible versions
2. Check the [changelog](../changelog.md) for compatibility information
3. Update MudBlazor to the latest version if needed

### Build Errors After Installation

If you experience build errors:

1. Clean your solution:

```bash
dotnet clean
```

2. Rebuild:

```bash
dotnet build
```

3. If issues persist, try deleting the `bin` and `obj` folders and rebuilding

## Next Steps

Now that you have MudBlazor.Extensions installed, proceed to the [Setup](setup.md) guide to configure it in your application.

## Getting Help

If you encounter issues during installation:

- Check the [GitHub Issues](https://github.com/fgilde/MudBlazor.Extensions/issues)
- Create a new issue with details about your problem
- Visit the [MudBlazor Discord](https://discord.gg/mudblazor) community
