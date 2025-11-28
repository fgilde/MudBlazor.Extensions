```razor
@inherits ExampleBase

<div style="width: 100%">
    <MudExChipSelect @ref="ComponentRef"
                     AvailableItems="@AvailableItems" 
                     @bind-SelectedValues="SelectedItems">
    </MudExChipSelect>

    <MudText Typo="Typo.body2" Class="mt-4">
        @L["Selected"]: @string.Join(", ", SelectedItems?.Select(x => x.Name) ?? Array.Empty<string>())
    </MudText>
</div>

@code {
    public IEnumerable<ChipItem> SelectedItems { get; set; } = new List<ChipItem>();
    
    public List<ChipItem> AvailableItems { get; set; } = new()
    {
        new ChipItem { Id = 1, Name = "Apple" },
        new ChipItem { Id = 2, Name = "Banana" },
        new ChipItem { Id = 3, Name = "Cherry" },
        new ChipItem { Id = 4, Name = "Date" },
        new ChipItem { Id = 5, Name = "Elderberry" }
    };

    public class ChipItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public override string ToString() => Name;
    }
}

```
