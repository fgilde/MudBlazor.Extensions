using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using MudBlazor.Utilities;
using System.Collections.Concurrent;

namespace MudBlazor.Extensions.Helper;

/// <summary>
/// Simple Utility class to help with building css class strings
/// </summary>
[HasDocumentation("MudExCssBuilder.md")]
public sealed class MudExCssBuilder: IAsyncDisposable, IMudExClassAppearance
{
    private readonly ConcurrentDictionary<string, byte> _cssClasses = new();
    private readonly ConcurrentDictionary<string, byte> _temporaryCssClasses = new();
    private readonly List<IAsyncDisposable> _disposables = new();

    public MudExCssBuilder()
    {}

    public MudExCssBuilder(string cls, params string[] other)
    {
        AddClass(cls, other);
    }    

    /// <summary>
    /// Static Property to access an instance <see cref="MudExStyleBuilder"/>
    /// </summary>
    public static MudExCssBuilder Default => new();

    /// <summary>
    /// Static method to create new MudExCssBuilder with existing classes
    /// </summary>
    public static MudExCssBuilder From(string cls, params string[] other) => Default.AddClass(cls, other);

    /// <summary>
    /// Static method to create new MudExCssBuilder with existing classes
    /// </summary>
    public static MudExCssBuilder From(MudExCss.Classes cls, params MudExCss.Classes[] other) => Default.AddClass(cls, other);

    /// <summary>
    /// Static method to create new MudExCssBuilder with existing classes
    /// </summary>
    public static MudExCssBuilder From(MudExCssBuilder builder) => Default.AddClass(builder);

    /// <summary>
    /// Static method to create new MudExCssBuilder with existing styles. All styles will stored in temporary css classes and added to the builder
    /// </summary>
    public static Task<MudExCssBuilder> FromStyleAsync(object styleObj) => Default.AddClassFromStyleAsync(styleObj);

    /// <summary>
    /// Static method to create new MudExCssBuilder with existing styles. All styles will stored in temporary css classes and added to the builder
    /// </summary>
    public static Task<MudExCssBuilder> FromStyleAsync(string style) => Default.AddClassFromStyleAsync(style);

    /// <summary>
    /// Static method to create new MudExCssBuilder with existing styles. All styles will stored in temporary css classes and added to the builder
    /// </summary>
    public static Task<MudExCssBuilder> FromStyleAsync(MudExStyleBuilder styleBuilder) => Default.AddClassFromStyleAsync(styleBuilder);

    /// <summary>
    /// Remove class from this builder
    /// </summary>
    public async Task<MudExCssBuilder> RemoveClassesAsync(params string[] values)
    {
        foreach (var value in values)
        {
            if (_temporaryCssClasses.ContainsKey(value))
            {
                await new MudExStyleBuilder().RemoveClassRuleAsync(value);
                _temporaryCssClasses.TryRemove(value, out _);
            }

            _cssClasses.TryRemove(value, out _);
        }
        return this;
    }

    /// <summary>
    /// Adds a css class to this builder
    /// </summary>
    public MudExCssBuilder AddClass(string value)
    {
        if (!string.IsNullOrEmpty(value))
            _cssClasses.TryAdd(value, 0);
        return this;
    }

    /// <summary>
    /// Adds one or more css classes to this builder if given condition is true
    /// </summary>
    public MudExCssBuilder AddClass(MudExCss.Classes cssClass, bool when) => AddClass(cssClass.ToString(), when);

    /// <summary>
    /// Adds one or more css classes to this builder
    /// </summary>
    public MudExCssBuilder AddClass(MudExCss.Classes cssClass, params MudExCss.Classes[] other)
        => new[] { cssClass }.Concat(other).Aggregate(this, (acc, f) => acc.AddClass(f, true));
  
    /// <summary>
    /// Adds one or more css classes to this builder 
    /// </summary>
    public MudExCssBuilder AddClass(string value, params string[] other)
        => new[] { value }.Concat(other).Aggregate(this, (acc, f) => acc.AddClass(f, true));
    
    /// <summary>
    /// Adds one or more css classes to this builder if given condition is true
    /// </summary>
    public MudExCssBuilder AddClass(string value, bool when) => !when ? this : AddClass(value);

    /// <summary>
    /// Adds one or more css classes to this builder if given condition is true
    /// </summary>
    public MudExCssBuilder AddClass(string value, Func<bool> when) => AddClass(value, when != null && when());
    
    /// <summary>
    /// Adds one or more css classes to this builder if given condition is true
    /// </summary>
    public MudExCssBuilder AddClass(Func<string> value, bool when = true) => !when ? this : AddClass(value());

    /// <summary>
    /// Adds one or more css classes to this builder if given condition is true
    /// </summary>
    public MudExCssBuilder AddClass(Func<string> value, Func<bool> when) => AddClass(value, when != null && when());

    /// <summary>
    /// Adds all classes from existing CssBuilder to this builder if given condition is true
    /// </summary>
    public MudExCssBuilder AddClass(CssBuilder builder, bool when = true) => !when ? this : AddClass(builder.Build());

    /// <summary>
    /// Adds all classes from existing CssBuilder to this builder if given condition is true
    /// </summary>
    public MudExCssBuilder AddClass(CssBuilder builder, Func<bool> when) => AddClass(builder, when != null && when());

    /// <summary>
    /// Adds all classes from existing CssBuilder to this builder if given condition is true
    /// </summary>
    public MudExCssBuilder AddClass(MudExCssBuilder builder, Func<bool> when) => AddClass(builder, when != null && when());

    /// <summary>
    /// Adds all classes from existing CssBuilder to this builder if given condition is true
    /// </summary>
    public MudExCssBuilder AddClass(MudExCssBuilder builder, bool when = true)
    {
        if (!when)
            return this;
        _disposables.Add(builder);
        return AddClass(builder.Build());
    }

    /// <summary>
    /// Adds a temporary class with all styles from given styleBuilder to this cssBuilder
    /// </summary>
    public async Task<MudExCssBuilder> AddClassFromStyleAsync(MudExStyleBuilder builder, bool when = true)
    {
        if (!when)
            return this;
        _disposables.Add(builder);
        var tmpClass = await builder.BuildAsClassRuleAsync();
        _temporaryCssClasses.TryAdd(tmpClass, 0);
        return AddClass(tmpClass);
    }

    /// <summary>
    /// Adds a temporary class with all styles from given styleBuilder to this cssBuilder if given condition func returns true
    /// </summary>
    public Task<MudExCssBuilder> AddClassFromStyleAsync(MudExStyleBuilder builder, Func<bool> when) => AddClassFromStyleAsync(builder, when != null && when());

    /// <summary>
    /// Adds a temporary class with all styles from given object to this cssBuilder if given condition is true
    /// </summary>
    public Task<MudExCssBuilder> AddClassFromStyleAsync(object styleObject, bool when = true) => AddClassFromStyleAsync(MudExStyleBuilder.FromObject(styleObject), when);

    /// <summary>
    /// Adds a temporary class with all styles from given string to this cssBuilder if given condition is true
    /// </summary>
    public Task<MudExCssBuilder> AddClassFromStyleAsync(string styleString, bool when = true) => AddClassFromStyleAsync(MudExStyleBuilder.FromStyle(styleString), when);

    /// <summary>
    /// Adds a temporary class with all styles from given object to this cssBuilder if given condition func returns true
    /// </summary>
    public Task<MudExCssBuilder> AddClassFromStyleAsync(object styleObject, Func<bool> when) => AddClassFromStyleAsync(MudExStyleBuilder.FromObject(styleObject), when);

    /// <summary>
    /// Adds a temporary class with all styles from given object to this cssBuilder if given condition func returns true
    /// </summary>
    public Task<MudExCssBuilder> AddClassFromStyleAsync(string styleString, Func<bool> when) => AddClassFromStyleAsync(MudExStyleBuilder.FromStyle(styleString), when);

    /// <summary>
    /// Adds classes from Attributes containing classes
    /// </summary>
    public MudExCssBuilder AddClassFromAttributes(IReadOnlyDictionary<string, object> additionalAttributes) =>
        additionalAttributes != null && additionalAttributes.TryGetValue("class", out var obj) ? AddClass(obj.ToString()) : this;

    /// <summary>
    /// Builds and returns an applicable css string
    /// </summary>
    public string Build() => string.Join(" ", _cssClasses.Select(c => $"{c.Key}"));

    /// <inheritdoc />
    public override string ToString() => Build();

    /// <summary>
    /// Disposes this instance
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        foreach (var disposable in _disposables)
            await disposable.DisposeAsync();
    }

    /// <summary>
    /// Builds and returns an applicable css string
    /// </summary>
    public string Class => Build();
}