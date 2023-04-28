MudExAppLoader is a custom Web Component that provides a stylish loading screen for your Blazor WebAssembly applications. The loader automatically hides after the app has loaded, making the transition smooth and visually appealing.

## Installation

1. Include the following script in the <head> section of your _Host.cshtml (for server-side) or index.html (for client-side) file:

```
<script src="_content/MudBlazor.Extensions/js/components/MudExAppLoader.min.js"></script>
```

## Usage

To use the MudExAppLoader component in your application, add the following tag in the <body> section of your _Host.cshtml (for server-side) or index.html (for client-side) file:

```
<mud-ex-app-loader AccentColor="#ff4081" MainAppId="app" Logo="https://your-logo-url.com/logo.png" />
```

Make sure that the MainAppId attribute value matches the ID of the <div> element that will render your Blazor application:

```
<div id="app"></div>
```

## Attributes

- `AccentColor`: (Optional) A string representing the accent color used for the progress circle. Default is `#ff0000`.
- `MainAppId`: (Optional) A string representing the ID of the div element where your Blazor application will be rendered. Default is `app`.
- `Logo`: (Optional) A string representing the URL of the logo to be displayed in the loader.


## Customization

You can customize the appearance of the MudExAppLoader by modifying the CSS classes in the _content/MudBlazor.Extensions/css/MudExAppLoader.css file.
