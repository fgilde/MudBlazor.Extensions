```razor
@inherits ExampleBase

<MudExIconPicker @ref="ComponentRef" 
                 @bind-Value="_icon" 
                 PickerVariant="PickerVariant.Inline" 
                 Label="@L["Inline icon picker"]" />

@code {
    private string _icon = Icons.Material.Filled.Home;
}

```
