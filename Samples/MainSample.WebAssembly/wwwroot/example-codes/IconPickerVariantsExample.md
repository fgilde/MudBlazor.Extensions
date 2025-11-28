```razor
@inherits ExampleBase

<MudExIconPicker @ref="ComponentRef" 
                 @bind-Value="_icon" 
                 Variant="Variant.Outlined"
                 Label="@L["Outlined variant"]" />

<MudExIconPicker @bind-Value="_icon2" 
                 Variant="Variant.Filled"
                 Label="@L["Filled variant"]"
                 Class="mt-4" />

@code {
    private string _icon = Icons.Material.Filled.Settings;
    private string _icon2 = Icons.Material.Filled.Person;
}

```
