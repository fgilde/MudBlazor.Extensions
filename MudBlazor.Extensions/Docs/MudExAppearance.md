# MudExAppearance

## Table of Contents
- [Introduction](#introduction)
- [Properties](#properties)
- [Methods](#methods)
- [Usage](#usage)

## Introduction
The `MudExAppearance` class is a powerful tool that helps to manage CSS and styles of MudBlazor components dynamically. 

## Properties
- `Class`: Get or set the CSS class.
- `Style`: Get or set the CSS style.
- `KeepExisting`: Indicates whether to keep the existing style or class.

## Methods
- `WithStyle`: Add a style to the existing style.
- `WithCss`: Add a CSS class to the existing class.
- `ApplyToAsync`: Apply the current CSS class and style to a specific MudBlazor component.

## Static Methods
- `FromCss`: Create a `MudExAppearance` instance from a CSS class.
- `FromStyle`: Create a `MudExAppearance` instance from a CSS style.

## Usage
To use `MudExAppearance`, you need to create an instance and set the CSS class or style, then apply it to a MudBlazor component. Here is an example:

```c#
var appearance = MudExAppearance.FromCss("my-css-class");
appearance.WithStyle("color: red;");
await appearance.ApplyToAsync(myMudBlazorComponent);
```

You can also chain methods:

```c#
await MudExAppearance.FromCss("my-css-class")
    .WithStyle("color: red;")
    .ApplyToAsync(myMudBlazorComponent);
```

This class is used as the properties to change dialog and dialog background as well. 

```c#
    DialogOptionsEx Options = new();
    
    Options.DialogAppearance = MudExAppearance.FromCss(MudExCss.Classes.Dialog.ColorfullGlass)
        .WithStyle(b => b
            .WithHeight(50, CssUnit.Percentage)
            .WithWidth(50, CssUnit.Percentage)
            .WithBoxShadow($" 0 8px 32px 0 {_primaryColor.SetAlpha(0.4).ToString(MudColorOutputFormats.RGBA)}")
            .WithBackgroundColor(_primaryColor.SetAlpha(0.2))
        );
    Options.DialogBackgroundAppearance = MudExAppearance.FromCss(MudExCss.Classes.Backgrounds.MovingDots)
        .WithStyle(new
        {
            Border = "4px solid",
            BorderColor = Color.Secondary,
            BorderRadius = 8
        });
```