using Bunit;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

/// <summary>
/// A <c>MetaConfiguration</c> passed as parameter must reach the rendered editors, not only a prebuilt <c>MetaInformation</c>.
/// </summary>
public class ObjectEditMetaConfigurationTests
{
    public class Person
    {
        public string FirstName { get; set; }
    }

    private static TestContext CreateContext()
    {
        var context = new TestContext();
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        return context;
    }

    [Fact]
    public async Task FormShowsLabelFromMetaConfiguration()
    {
        await using var context = CreateContext();
        var cut = context.Render<MudExObjectEditForm<Person>>(p => p
            .Add(c => c.Value, new Person { FirstName = "Ada" })
            .Add(c => c.MetaConfiguration, meta => meta.Property(m => m.FirstName).WithLabel("Given name")));

        Assert.Contains("Given name", cut.Markup);
    }

    [Fact]
    public async Task EditShowsLabelFromMetaConfiguration()
    {
        await using var context = CreateContext();
        var cut = context.Render<MudExObjectEdit<Person>>(p => p
            .Add(c => c.Value, new Person { FirstName = "Ada" })
            .Add(c => c.MetaConfiguration, meta => meta.Property(m => m.FirstName).WithLabel("Given name")));

        Assert.Contains("Given name", cut.Markup);
    }

    [Fact]
    public async Task NullValueStillGetsAConfiguredMeta()
    {
        await using var context = CreateContext();
        var cut = context.Render<MudExObjectEdit<Person>>(p => p
            .Add(c => c.Value, null)
            .Add(c => c.MetaConfiguration, meta => meta.Property(m => m.FirstName).WithLabel("Given name")));

        Assert.Contains("Given name", cut.Markup);
    }

    [Fact]
    public async Task PassedMetaInformationHasItsConditionsApplied()
    {
        await using var context = CreateContext();
        var person = new Person { FirstName = "Ada" };
        var meta = person.ObjectEditMeta(m => m.Property(p => p.FirstName).WithLabel("Given name").IgnoreIf<Person>(p => p.FirstName == "Ada"));
        var cut = context.Render<MudExObjectEdit<Person>>(p => p
            .Add(c => c.Value, person)
            .Add(c => c.MetaInformation, meta));

        Assert.DoesNotContain("Given name", cut.Markup);
    }

    [Fact]
    public async Task FormAppliesTheConditionsOfAPassedMetaInformation()
    {
        await using var context = CreateContext();
        var person = new Person { FirstName = "Ada" };
        var meta = person.ObjectEditMeta(m => m.Property(p => p.FirstName).WithLabel("Given name").IgnoreIf<Person>(p => p.FirstName == "Ada"));
        var cut = context.Render<MudExObjectEditForm<Person>>(p => p
            .Add(c => c.Value, person)
            .Add(c => c.MetaInformation, meta));

        Assert.DoesNotContain("Given name", cut.Markup);
    }

    [Fact]
    public async Task DialogAppliesTheConditionsOfAPassedMetaInformation()
    {
        await using var context = CreateContext();
        var person = new Person { FirstName = "Ada" };
        var meta = person.ObjectEditMeta(m => m.Property(p => p.FirstName).WithLabel("Given name").IgnoreIf<Person>(p => p.FirstName == "Ada"));
        var provider = context.Render<MudDialogProvider>();
        var dialogs = context.Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters { { nameof(MudExObjectEditDialog<Person>.MetaInformation), meta } };

        _ = provider.InvokeAsync(() => dialogs.EditObjectAsync(person, "Edit", DialogOptionsEx.DefaultDialogOptions, null, parameters));

        provider.WaitForAssertion(() => Assert.Contains("mud-dialog", provider.Markup));
        Assert.DoesNotContain("Given name", provider.Markup);
    }

    [Fact]
    public async Task ReconfiguredPassedMetaKeepsItsConditions()
    {
        await using var context = CreateContext();
        var person = new Person { FirstName = "Ada" };
        var meta = person.ObjectEditMeta(m => m.Property(p => p.FirstName).WithLabel("Given name").IgnoreIf<Person>(p => p.FirstName == "Ada"));
        var cut = context.Render<MudExObjectEditForm<Person>>(p => p
            .Add(c => c.Value, person)
            .Add(c => c.ConfigureMetaInformationAlways, true)
            .Add(c => c.MetaInformation, meta));

        cut.WaitForAssertion(() => Assert.DoesNotContain("Given name", cut.Markup));
    }
}
