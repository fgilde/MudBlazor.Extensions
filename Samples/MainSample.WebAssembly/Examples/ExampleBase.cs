using System.Text.RegularExpressions;
using MainSample.WebAssembly.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace MainSample.WebAssembly.Examples;

public class ExampleBase : ComponentBase, IExample
{
    private string code;
    private static readonly Regex[] PatternsToRemove = new[]
    {
        new Regex(@"@inherits\s+ExampleBase\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"@ref\s*=\s*""ComponentRef""", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    };

    public Action<IComponent>? ComponentRefSet { get; set; }

    public IComponent? ComponentRef
    {
        get => field;
        set
        {
            field = value;
            if(value != null)
                ComponentRefSet?.Invoke(value);
        }
    }

    [Inject] protected NavigationManager NavigationManager { get; set; }
    [Inject] protected IStringLocalizer<ExampleBase> L { get; set; }

    [CascadingParameter] protected IExampleHolder? Registrar { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        if (Registrar != null)
        {
            await Registrar.RegisterAsync(this);
        }
    }
    
    public async Task<string> GetSourceCodeAsync()
    {
        if (!string.IsNullOrEmpty(code))
            return code;
        string exampleName = GetType().Name;
        var client = new HttpClient();
        var url = NavigationManager.ToAbsoluteUri($"example-codes/{exampleName}.md");
        code = await client.GetStringAsync(url);
        
        return CleanCode(code);
    }

    private string CleanCode(string code)
    {
        code = PatternsToRemove.Aggregate(code, (current, pattern) => pattern.Replace(current, string.Empty));

        var lines = code.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        var cleanedLines = lines.Where(line => !string.IsNullOrWhiteSpace(line));

        return string.Join(Environment.NewLine, cleanedLines);
    }

    public IComponent? GetComponent()
    {
        return ComponentRef;
    }
}

public interface IExample {
    public Action<IComponent>? ComponentRefSet { get; set; }
    public Task<string> GetSourceCodeAsync();
    public IComponent? ComponentRef { get; }
}