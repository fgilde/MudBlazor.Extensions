```razor
@inherits ExampleBase

<div style="width: 100%">
    <MudExChipSelect @ref="ComponentRef"
                     AvailableItems="@AvailableItems" 
                     @bind-SelectedValues="SelectedItems"
                     ChipSize="SelectedSize"
                     ChipVariant="SelectedVariant"
                     ChipColor="SelectedColor">
    </MudExChipSelect>

    <MudDivider Class="my-4" />

    <MudGrid>
        <MudItem xs="12" md="4">
            <MudExEnumSelect @bind-Value="SelectedSize" Label="@L["Size"]" />
        </MudItem>
        <MudItem xs="12" md="4">
            <MudExEnumSelect @bind-Value="SelectedVariant" Label="@L["Variant"]" />
        </MudItem>
        <MudItem xs="12" md="4">
            <MudExEnumSelect @bind-Value="SelectedColor" Label="@L["Color"]" />
        </MudItem>
    </MudGrid>
</div>

@code {
    public IEnumerable<ColorItem> SelectedItems { get; set; } = new List<ColorItem>();
    public Size SelectedSize { get; set; } = Size.Medium;
    public Variant SelectedVariant { get; set; } = Variant.Filled;
    public Color SelectedColor { get; set; } = Color.Primary;
    
    public List<ColorItem> AvailableItems { get; set; } = new()
    {
        new ColorItem { Id = 1, Name = "Red", Hex = "#F44336" },
        new ColorItem { Id = 2, Name = "Green", Hex = "#4CAF50" },
        new ColorItem { Id = 3, Name = "Blue", Hex = "#2196F3" },
        new ColorItem { Id = 4, Name = "Yellow", Hex = "#FFEB3B" },
        new ColorItem { Id = 5, Name = "Purple", Hex = "#9C27B0" }
    };

    public class ColorItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Hex { get; set; }
        public override string ToString() => Name;
    }
}

```
