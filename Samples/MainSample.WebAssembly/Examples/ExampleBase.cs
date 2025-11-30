using MainSample.WebAssembly.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using MudBlazor.Extensions.Components;
using System.Text.RegularExpressions;
using YamlDotNet.Core.Tokens;
using static System.Net.WebRequestMethods;

namespace MainSample.WebAssembly.Examples;

public class ExampleBase : ComponentBase, IExample
{
    private string code;
    private static readonly Regex[] PatternsToRemove = new[]
    {
        new Regex(@"@inherits\s+ExampleBase\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"@ref\s*=\s*""ComponentRef""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new Regex(@"</?MudExEditConfiguration\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled)
//        new Regex(@"</?"+nameof(MudExEditConfiguration)+"\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    };

    public Action<IComponent>? ComponentRefSet { get; set; }

    private List<IComponent> _componentRefs = new ();
    public IComponent[] ComponentRefs => _componentRefs.ToArray();
    public bool HasAdditionalCodeFiles => AdditionalCodeFiles?.Any() ?? false;


    [Parameter]
    public string[] AdditionalCodeFiles { get; set; }

    public IComponent? ComponentRef
    {
        get => field;
        set
        {
            field = value;
            if (value != null)
            {
                _componentRefs.Add(value);
                ComponentRefSet?.Invoke(value);
            }
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
        try
        {
            code = await client.GetStringAsync(url);
        }
        catch (Exception e)
        {
            code = await client.GetStringAsync($"https://www.mudex.org/example-codes/{exampleName}.md");

        }

        return code = CleanCode(code);
    }

    public async Task<IDictionary<string, string>> GetAdditionalCodeFilesAsync(bool asMarkDown = true)
    {
        var client = new HttpClient();
        var result = new Dictionary<string, string>();
        foreach (var file in AdditionalCodeFiles)
        {
            var fileUrl = file.StartsWith("http") ? file : GH.Path(file);
            var code = await client.GetStringAsync(fileUrl);
            result[file] = asMarkDown ? MudExCodeView.CodeAsMarkup(code) : code;
        }
        return result;
    }


    private string CleanCode(string str)
    {
        str = PatternsToRemove.Aggregate(str, (current, pattern) => pattern.Replace(current, string.Empty));

        var lines = str.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        var cleanedLines = lines.Where(line => !string.IsNullOrWhiteSpace(line));

        return WithoutLocalizer(string.Join(Environment.NewLine, cleanedLines));
    }

    private string WithoutLocalizer(string str)
    {
        var pattern =
            @"@L\[\s*(?:(?:""((?:[^""\\]|\\.)*)"")|(?:'((?:[^'\\]|\\.)*)'))\s*(?:,(.*?))?\]";

        return Regex.Replace(str, pattern, match =>
        {
            var content = match.Groups[1].Success
                ? match.Groups[1].Value
                : match.Groups[2].Value;

            var args = match.Groups[3]?.Value?.Trim();

            content = Regex.Unescape(content);

            if (string.IsNullOrEmpty(args))
            {
                return @$"@(""{content}"")";
            }

            return @$"@(string.Format(""{content}"", {args}))";
        });
    }

    public IComponent? GetComponent()
    {
        return ComponentRef;
    }
}

public interface IExample {
    public Action<IComponent>? ComponentRefSet { get; set; }
    public Task<string> GetSourceCodeAsync();
    public IComponent[]? ComponentRefs { get; }
    public bool HasAdditionalCodeFiles { get; }
    public Task<IDictionary<string, string>> GetAdditionalCodeFilesAsync(bool asMarkDown = true);
}