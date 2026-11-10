using Bunit;
using MainSample.WebAssembly;
using MainSample.WebAssembly.Examples;
using MainSample.WebAssembly.Services;
using MainSample.WebAssembly.Types;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Core;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Samples;

public class OfficeDemoConventionsTests
{
    private static readonly string[] PageGroups = ["Spreadsheet", "Ribbon", "Slide Editor"];
    private static readonly string[] PagePrefixes = ["Page_Spreadsheet", "Page_Ribbon", "Page_Slides", "Page_FileDisplayEdit"];
    private static readonly string[] ExampleNamespaces =
    [
        "MainSample.WebAssembly.Examples.Spreadsheet", "MainSample.WebAssembly.Examples.Ribbon",
        "MainSample.WebAssembly.Examples.Slides", "MainSample.WebAssembly.Examples.FileDisplay"
    ];

    private static readonly string[] NeedsInteraction = ["SpreadsheetLargeExample"];
    private static readonly string[] Helpers = ["SpreadsheetLazyDemo"];

    private static IEnumerable<Type> OfficeComponents() => typeof(ExampleBase).Assembly.GetTypes()
        .Where(t => typeof(IComponent).IsAssignableFrom(t) && !t.IsAbstract && ExampleNamespaces.Contains(t.Namespace) && !Helpers.Contains(t.Name));

    public static TheoryData<string> ExampleNames => new(OfficeComponents().Select(t => t.Name).Order());

    private static Type ExampleType(string name) => OfficeComponents().Single(t => t.Name == name);

    [Fact]
    public void OfficePages_UseDemoNew()
    {
        var pages = typeof(ExampleBase).Assembly.GetTypes()
            .Select(t => (Type: t, Demo: t.GetCustomAttributes(typeof(DemoAttribute), false).Cast<DemoAttribute>().FirstOrDefault()))
            .Where(p => p.Demo != null && (PageGroups.Contains(p.Demo.Group) || PagePrefixes.Any(p.Type.Name.StartsWith)))
            .ToList();

        Assert.True(pages.Count >= 14);
        Assert.All(pages, p => Assert.IsType<DemoNewAttribute>(p.Demo));
        Assert.Contains(pages, p => p.Demo.Group == "Slide Editor");
    }

    [Theory]
    [MemberData(nameof(ExampleNames))]
    public void OfficeExamples_InheritExampleBase(string name) => Assert.True(typeof(ExampleBase).IsAssignableFrom(ExampleType(name)));

    [Theory]
    [MemberData(nameof(ExampleNames))]
    public async Task OfficeExamples_SetComponentRef(string name)
    {
        if (NeedsInteraction.Contains(name))
            return;

        await using var context = new BunitContext();
        context.Services.AddMudServicesWithExtensions();
        context.Services.AddLocalization();
        context.Services.AddScoped<LocalStorageService>();
        context.Services.AddSingleton<DeploymentsService>();
        context.Services.AddScoped(_ => new HttpClient(new SampleDataHandler()) { BaseAddress = new Uri("http://localhost/") });
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.JSInterop.Setup<CssVariable[]>("MudExCssHelper.getCssVariables", _ => true).SetResult([]);
        context.Render<MudPopoverProvider>();
        context.Render<MudDialogProvider>();

        var type = ExampleType(name);
        var cut = context.Render<DynamicComponent>(p => p.Add(c => c.Type, type));
        var example = cut.FindComponents<ExampleBase>().First().Instance;
        cut.WaitForAssertion(() => Assert.NotNull(example.ComponentRef), TimeSpan.FromSeconds(10));

        var expected = type.Namespace switch
        {
            "MainSample.WebAssembly.Examples.Slides" => typeof(MudExSlideEditor),
            "MainSample.WebAssembly.Examples.Ribbon" => typeof(MudExRibbon),
            "MainSample.WebAssembly.Examples.FileDisplay" => typeof(MudExFileDisplay),
            _ => name.Contains("FileDisplay") || name.Contains("ServerSave") ? typeof(MudExFileDisplay) : typeof(MudExSpreadsheetEditor)
        };
        Assert.IsType(expected, example.ComponentRef);
    }

    private sealed class SampleDataHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var file = Path.GetFileName(request.RequestUri!.AbsolutePath);
            var path = Directory.EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "TestData"), file, SearchOption.AllDirectories).FirstOrDefault();
            return Task.FromResult(path == null
                ? new HttpResponseMessage(System.Net.HttpStatusCode.NotFound)
                : new HttpResponseMessage { Content = new ByteArrayContent(File.ReadAllBytes(path)) });
        }
    }
}
