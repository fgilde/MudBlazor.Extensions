```razor
@inherits ExampleBase

<MudButton @ref="ComponentRef">
    Hello
</MudButton>

@code {
    protected override async Task OnInitializedAsync()
    {
        var c = await GetSourceCodeAsync();
        Console.WriteLine(c);
        await base.OnInitializedAsync();
    }

}

```
