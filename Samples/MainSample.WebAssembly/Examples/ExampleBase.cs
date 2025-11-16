using MainSample.WebAssembly.Shared;
using Microsoft.AspNetCore.Components;

namespace MainSample.WebAssembly.Examples;

public class ExampleBase : ComponentBase, IExample
{
    public IComponent? ComponentRef { get; set; }

    [Inject]
    protected NavigationManager NavigationManager { get; set; }

    [CascadingParameter] protected IExampleHolder? Registrar { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        if (Registrar != null)
        {
            await Registrar.RegisterAsync(this);
        }
    }
    


    private string code;
    public async Task<string> GetSourceCodeAsync()
    {
        if (!string.IsNullOrEmpty(code))
            return code;
        string exampleName = GetType().Name;
        var client = new HttpClient();
        var url = NavigationManager.ToAbsoluteUri($"example-codes/{exampleName}.md");
        code = await client.GetStringAsync(url);
        code = code.Replace("@inherits ExampleBase", "", StringComparison.InvariantCultureIgnoreCase);
        return code;
    }

    public IComponent? GetComponent()
    {
        return ComponentRef;
    }
}

public interface IExample {
    public Task<string> GetSourceCodeAsync();
    public IComponent? ComponentRef { get; }
}