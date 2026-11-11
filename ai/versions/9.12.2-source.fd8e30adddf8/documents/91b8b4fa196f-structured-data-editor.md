# MudExStructuredDataEditor

Edit structured data like JSON, XML, or YAML with automatic UI generation. This component supports all features of MudExObjectEditForm with structured data formats.

## Features

- ✅ **Multiple Formats** - JSON, XML, YAML support
- ✅ **Auto UI Generation** - Automatically creates editing interface
- ✅ **Validation** - Built-in validation support
- ✅ **Type Detection** - Automatic format detection

## Basic Usage

```razor
<MudExStructuredDataEditor @bind-Data="_dataString" />

@code {
    private string _dataString = "{ \"name\": \"John\", \"age\": 30 }";
}
```

## Dialog Usage

```csharp
await dialogService.EditStructuredDataString(
    _dataType, 
    _dataString, 
    $"Auto Generated Editor for {_dataType}", 
    ((_,_) => Task.FromResult(""))
);
```

## Live Demo

[Structured Data Editor Demo](https://www.mudex.org/structured-data-edit)
