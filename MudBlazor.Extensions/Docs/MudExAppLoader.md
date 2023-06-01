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
- `Size`: (Optional) A string representing the size and max size for the logo `app-name`. Default is 200.
- `LoadingTextColor`: (Optional) A string representing the loading text color. Default is the same as AccentColor.
- `AppNameColor`: (Optional) A string representing the application name text color. Default is the same as AccentColor.
- `Timeout`:  (Optional) An integer representing the timeout for the loader animation in milliseconds. Default is 2000.
- `PreLoadText`: (Optional) A string representing the text to be displayed before the loader percentage is shown. Default is 'Loading...'.
- `AppName`: (Optional) A string representing the application name to be displayed. Default is an empty string.
- `ContainerClass`: (Optional) A string representing the additional CSS class for the `mud-ex-app-loader-loading-container`.
- `ProgressClass`: (Optional) A string representing the additional CSS class for the `mud-ex-app-loader-loading-progress`.
- `CircleClass`: (Optional) A string representing the additional CSS class for the `mud-ex-app-loader-circle`.
- `LogoClass`: (Optional) A string representing the additional CSS class for the `logo`.
- `PercentageClass`: (Optional) A string representing the additional CSS class for the `mud-ex-app-loader-loading-percentage`.
- `AppNameClass`: (Optional) A string representing the additional CSS class for the `app-name`.
- `OnAppLoaded`: (Optional) A string representing the callback function that will be invoked when the application is fully loaded.
- `OnAnimationFinished`: (Optional) A string representing the callback function that will be invoked when the application is fully loaded and the animation also finished.

## Rendering Child Content

If no `Logo` attribute is provided, the loader will render the child content of the `mud-ex-app-loader` element. For example:

```html
<mud-ex-app-loader AppName="MudBlazor.Extensions" AccentColor="#ff4081" MainAppId="app"><p>MyContent</p></mud-ex-app-loader>
```

## Customization

You can customize the appearance of the MudExAppLoader by modifying the CSS classes in the _content/MudBlazor.Extensions/css/MudExAppLoader.css file.

### Overwriting CSS Example

To overwrite some CSS, you can for example create a new CSS file and add the following content:

```css
:root {
    --accent-color1: #a94553;
    --accent-color2: #423859;
    --accent-color3: #16868d;
    --accent-color4: #d0a733;
}

.mud-ex-app-loader-loading-container .app-name {
    background: linear-gradient( 90deg,var(--accent-color1),var(--accent-color2),var(--accent-color3),var(--accent-color4));
    background-size: 200% auto;
    -webkit-background-clip: text;
    -webkit-text-fill-color: transparent;
}

@keyframes rotate-background {
    0% {
        transform: scale(1.5) rotate(0deg);
    }

    100% {
        transform: scale(1.5) rotate(360deg);
    }
}

.mud-ex-app-loader-loading-container {
    position: relative;
    overflow: hidden;
}

.mud-ex-app-loader-loading-container::before {
    content: "";
    opacity:.15;
    position: absolute;
    top: -50%;
    left: -50%;
    width: 200%;
    height: 200%;
    background-image: linear-gradient(45deg, transparent 50%, var(--accent-color1) 50%), linear-gradient(135deg, transparent 50%, var(--accent-color2) 50%), linear-gradient(-135deg, transparent 50%, var(--accent-color3) 50%), linear-gradient(-45deg, transparent 50%, var(--accent-color4) 50%);
    background-blend-mode: screen;

    
    animation: rotate-background 30s linear infinite;
    z-index: -1;
}
```

Include the new CSS file in your project's <head> section after the MudExAppLoader.css file.