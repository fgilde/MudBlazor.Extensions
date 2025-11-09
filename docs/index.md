# MudBlazor.Extensions

<div align="center" markdown>

[![GitHub Repo stars](https://img.shields.io/github/stars/fgilde/mudblazor.extensions?color=594ae2&style=flat-square&logo=github)](https://github.com/fgilde/mudblazor.extensions/stargazers)
[![GitHub](https://img.shields.io/github/license/garderoben/mudblazor?color=594ae2&style=flat-square&logo=github)](https://github.com/fgilde/MudBlazor.Extensions/blob/master/LICENSE)
[![Nuget version](https://img.shields.io/nuget/v/MudBlazor.Extensions?color=ff4081&label=nuget%20version&logo=nuget&style=flat-square)](https://www.nuget.org/packages/MudBlazor.Extensions/)
[![Nuget downloads](https://img.shields.io/nuget/dt/MudBlazor.Extensions?color=ff4081&label=nuget%20downloads&logo=nuget&style=flat-square)](https://www.nuget.org/packages/MudBlazor.Extensions)

</div>

Welcome to the comprehensive documentation for **MudBlazor.Extensions** - a powerful package that extends the capabilities of the MudBlazor component library with additional components, utilities, and features to accelerate your Blazor application development.

## Overview

MudBlazor.Extensions is a convenient package that extends the capabilities of the MudBlazor component library. This documentation provides detailed information about the setup process, components, extensions, and functionalities provided by the package.

!!! info "Prerequisites"
    This package requires a MudBlazor project and the referenced MudBlazor package. For more information about MudBlazor, visit the official documentation at [mudblazor.com](https://mudblazor.com/).

## Key Features

<div class="grid cards" markdown>

-   :material-file-edit:{ .lg .middle } __Object Editing__

    ---

    Automatically generate UI for object editing with validation support

    [:octicons-arrow-right-24: Learn more](components/object-edit.md)

-   :material-upload:{ .lg .middle } __File Upload & Display__

    ---

    Advanced file upload with preview, drag & drop, and cloud integration

    [:octicons-arrow-right-24: Learn more](components/upload-edit.md)

-   :material-resize:{ .lg .middle } __Dialog Extensions__

    ---

    Resizable, draggable dialogs with animations and custom buttons

    [:octicons-arrow-right-24: Learn more](extensions/dialog-extensions.md)

-   :material-tree:{ .lg .middle } __Tree View Components__

    ---

    Enhanced tree view components with breadcrumb navigation

    [:octicons-arrow-right-24: Learn more](components/tree-view.md)

-   :material-palette:{ .lg .middle } __Styling Utilities__

    ---

    CSS and style builders for dynamic styling

    [:octicons-arrow-right-24: Learn more](utilities/css-builder.md)

-   :material-cog:{ .lg .middle } __Rich Utilities__

    ---

    Color utilities, size helpers, and SVG components

    [:octicons-arrow-right-24: Learn more](utilities/index.md)

</div>

## Quick Start

Get started with MudBlazor.Extensions in just a few steps:

### 1. Installation

Add the NuGet package to your Blazor project:

```bash
dotnet add package MudBlazor.Extensions
```

Or add it to your `.csproj` file:

```xml
<PackageReference Include="MudBlazor.Extensions" Version="*" />
```

### 2. Configuration

Update your `_Imports.razor`:

```csharp
@using MudBlazor.Extensions
@using MudBlazor.Extensions.Components
@using MudBlazor.Extensions.Components.ObjectEdit
```

Register services in `Program.cs` or `Startup.cs`:

```csharp
// Add MudServices and MudBlazor.Extensions together
builder.Services.AddMudServicesWithExtensions();

// Or add MudBlazor.Extensions separately (after MudServices)
builder.Services.AddMudExtensions();
```

### 3. Start Building

You're ready to use MudBlazor.Extensions components and utilities! Check out the [Getting Started](getting-started/setup.md) guide for detailed setup instructions.

## Demo & Try Online

Experience MudBlazor.Extensions in action:

- **[Live Demo on Azure](https://mudex.azurewebsites.net/)** - Full-featured demo application
- **[Live Demo on Cloudflare](https://mudblazor-extensions.pages.dev)** - Alternative demo hosting
- **[Try Online](https://try.mudex.org/)** - Interactive playground

## API Reference

<div align="center" markdown>

### 📚 [Complete API Reference](https://www.mudex.org/api?layout=empty)

Browse the full API documentation with detailed information about all components, methods, and properties.

</div>

## Popular Components

### MudExObjectEdit

Robust component for automatic UI generation from your models with built-in validation support:

```razor
<MudExObjectEditForm OnValidSubmit="@OnSubmit" Value="@MyModel" />
```

[Learn more about Object Edit →](components/object-edit.md)

### MudExUploadEdit

Feature-rich file upload component with preview, drag & drop, cloud integration, and more:

```razor
<MudExUploadEdit @bind-Files="Files" />
```

[Learn more about Upload Edit →](components/upload-edit.md)

### MudExFileDisplay

Display file contents with automatic handling for various file types:

```razor
<MudExFileDisplay FileName="document.pdf" 
                  ContentType="application/pdf" 
                  Url="@fileUrl" />
```

[Learn more about File Display →](components/file-display.md)

## Extensions Highlights

### Resizable & Draggable Dialogs

Create interactive dialogs with custom behaviors:

```csharp
var options = new DialogOptionsEx { 
    Resizeable = true, 
    DragMode = MudDialogDragMode.Simple,
    MaximizeButton = true,
    Animation = AnimationType.SlideIn
};
await dialogService.ShowEx<MyDialog>("Title", parameters, options);
```

[Explore Dialog Extensions →](extensions/dialog-extensions.md)

## Community & Support

- **GitHub:** [fgilde/MudBlazor.Extensions](https://github.com/fgilde/MudBlazor.Extensions)
- **NuGet:** [MudBlazor.Extensions](https://www.nuget.org/packages/MudBlazor.Extensions)
- **Issues:** [Report bugs or request features](https://github.com/fgilde/MudBlazor.Extensions/issues)

## What's Next?

<div class="grid cards" markdown>

-   [**Installation Guide**](getting-started/installation.md)
    
    Detailed installation and configuration instructions

-   [**Components**](components/index.md)
    
    Explore all available components with examples

-   [**Extensions**](extensions/index.md)
    
    Discover extension methods and utilities

-   [**API Reference**](api/index.md)
    
    Complete API documentation

</div>

---

<div align="center">
    <p>If you find this package helpful, please ⭐ star it on <a href="https://github.com/fgilde/MudBlazor.Extensions">GitHub</a> and share it with your friends!</p>
</div>
