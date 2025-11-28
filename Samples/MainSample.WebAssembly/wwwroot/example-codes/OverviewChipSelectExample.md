```razor
@inherits ExampleBase

<MudExChipSelect @ref="ComponentRef"
                 AvailableItems="@_available" 
                 @bind-SelectedValues="_selected">
</MudExChipSelect>

<MudText Typo="Typo.body2" Class="mt-3">
    @L["Selected"]: @string.Join(", ", _selected?.Select(x => x) ?? Array.Empty<string>())
</MudText>

@code {
    private IEnumerable<string> _selected = new List<string>();
    private List<string> _available = new() { "C#", "JavaScript", "Python", "TypeScript", "Go" };
}

```
