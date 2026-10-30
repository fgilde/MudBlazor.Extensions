using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Extensions.Components.ObjectEdit;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

/// <summary>
/// RenderData replaced via <c>RenderWith</c> must keep a wrapper set before, and conditional attributes must be reset when the condition turns false.
/// </summary>
public class ObjectEditRenderDataTests
{
    public class Person
    {
        public string FirstName { get; set; }
        public bool Locked { get; set; }
    }

    private static TestContext CreateContext()
    {
        var context = new TestContext();
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        return context;
    }

    private static AngleSharp.Dom.IElement FirstNameInput(IRenderedComponent<MudExObjectEdit<Person>> cut)
        => cut.Find(".mud-ex-property-container-for-FirstName input");

    [Fact]
    public void RenderWithKeepsTheWrapperSetByWrapEachInMudItem()
    {
        var person = new Person { FirstName = "Ada" };
        var meta = person.ObjectEditMeta(m =>
        {
            m.WrapEachInMudItem();
            m.Property(p => p.FirstName).RenderWith<MudTextField<string>, string>(f => f.Value);
        });

        var renderData = meta.Property(p => p.FirstName).RenderData;

        Assert.Equal(typeof(MudTextField<string>), renderData.ComponentType);
        Assert.NotNull(renderData.Wrapper);
        Assert.Equal(typeof(MudItem), renderData.Wrapper.ComponentType);
    }

    [Fact]
    public void RenderWithKeepsTheWrapperSetByWrapIn()
    {
        var person = new Person { FirstName = "Ada" };
        var meta = person.ObjectEditMeta(m =>
        {
            m.Property(p => p.FirstName).WrapInMudItem();
            m.Property(p => p.FirstName).RenderWith(typeof(MudTextField<string>));
        });

        Assert.NotNull(meta.Property(p => p.FirstName).RenderData.Wrapper);
    }

    [Fact]
    public async Task AsDisabledIfEnablesTheFieldAgainWhenTheConditionTurnsFalse()
    {
        await using var context = CreateContext();
        var person = new Person { FirstName = "Ada", Locked = true };
        var cut = context.Render<MudExObjectEdit<Person>>(p => p
            .Add(c => c.Value, person)
            .Add(c => c.MetaConfiguration, meta => meta.Property(m => m.FirstName).AsDisabledIf<Person>(x => x.Locked)));
        cut.WaitForAssertion(() => Assert.True(FirstNameInput(cut).HasAttribute("disabled")));

        person.Locked = false;
        await cut.InvokeAsync(() => cut.Instance.UpdateAllConditions());
        cut.Render();

        cut.WaitForAssertion(() => Assert.False(FirstNameInput(cut).HasAttribute("disabled")));
    }

    // WithAttributesIf<TModel, TComponent> takes every changed property of a component instance, also non-parameters like
    // ValidationErrors; resetting those passed them to MudTextField, which then threw because UserAttributes captures unmatched values
    [Fact]
    public void WithAttributesIfResetsOnlyParametersWhenTheConditionIsFalse()
    {
        var person = new Person { FirstName = "Ada" };
        var meta = person.ObjectEditMeta(m => m.Property(p => p.FirstName)
            .WithAttributesIf<Person, MudTextField<string>>(x => x.Locked, f => f.Mask = new PatternMask("###")));

        var renderData = meta.Property(p => p.FirstName).RenderData;
        renderData.UpdateConditionalSettings(person);

        Assert.True(renderData.Attributes.ContainsKey(nameof(MudTextField<string>.Mask)));
        Assert.All(renderData.Attributes.Keys, key => Assert.True(Nextended.Blazor.Helper.ComponentRenderHelper.IsValidParameter(typeof(MudTextField<string>), key, null), key));
    }

    // UpdateAllConditions re-evaluated IgnoreIf but rendered the cached grouping, so the property grid's
    // "show inherited" toggle only showed up after some other change
    [Fact]
    public async Task UpdateAllConditionsShowsAPropertyWhoseIgnoreConditionTurnedFalse()
    {
        await using var context = CreateContext();
        var person = new Person { FirstName = "Ada", Locked = true };
        var cut = context.Render<MudExObjectEdit<Person>>(p => p
            .Add(c => c.Value, person)
            .Add(c => c.MetaConfiguration, meta => meta.Property(m => m.FirstName).IgnoreIf<Person>(x => x.Locked)));
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".mud-ex-property-container-for-FirstName")));

        person.Locked = false;
        await cut.InvokeAsync(() => cut.Instance.UpdateAllConditions());
        cut.Render();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".mud-ex-property-container-for-FirstName")));
    }

    // The property grid of the demos gets the same component instance again when the component changed itself;
    // the editors have to show the changed value after that render
    [Fact]
    public async Task ParentRenderWithTheSameValueShowsChangedPropertyValues()
    {
        await using var context = CreateContext();
        var person = new Person { FirstName = "Ada" };
        var cut = context.Render<MudExObjectEdit<Person>>(p => p.Add(c => c.Value, person));
        cut.WaitForAssertion(() => Assert.Equal("Ada", FirstNameInput(cut).GetAttribute("value")));

        person.FirstName = "Grace";
        cut.Render(p => p.Add(c => c.Value, person));

        cut.WaitForAssertion(() => Assert.Equal("Grace", FirstNameInput(cut).GetAttribute("value")));
    }
}
