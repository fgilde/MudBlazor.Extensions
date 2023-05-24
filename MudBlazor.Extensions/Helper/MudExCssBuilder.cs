using MudBlazor.Extensions.Core;
using MudBlazor.Utilities;
using System.Collections.Concurrent;

namespace MudBlazor.Extensions.Helper;

public sealed class MudExCssBuilder: IAsyncDisposable, IMudExClassAppearance
{
    private readonly ConcurrentDictionary<string, byte> _cssClasses = new();
    private readonly ConcurrentDictionary<string, byte> _temporaryCssClasses = new();
    private readonly List<IAsyncDisposable> _disposables = new();
    
    public static MudExCssBuilder Default => new();
    public static MudExCssBuilder From(string cls, params string[] other) => Default.AddClass(cls, other);
    public static MudExCssBuilder From(MudExCss.Classes cls, params MudExCss.Classes[] other) => Default.AddClass(cls, other);
    public static MudExCssBuilder From(MudExCssBuilder builder) => Default.AddClass(builder);
    public static Task<MudExCssBuilder> FromStyleAsync(object styleObj) => Default.AddClassFromStyleAsync(styleObj);
    public static Task<MudExCssBuilder> FromStyleAsync(string style) => Default.AddClassFromStyleAsync(style);
    public static Task<MudExCssBuilder> FromStyleAsync(MudExStyleBuilder styleBuilder) => Default.AddClassFromStyleAsync(styleBuilder);
    

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

    public MudExCssBuilder AddClass(string value)
    {
        _cssClasses.TryAdd(value, 0);
        return this;
    }
    
    public MudExCssBuilder AddClass(MudExCss.Classes cssClass, bool when) => AddClass(cssClass.ToString(), when);
    public MudExCssBuilder AddClass(MudExCss.Classes cssClass, params MudExCss.Classes[] other)
        => new[] { cssClass }.Concat(other).Aggregate(this, (acc, f) => acc.AddClass(f, true));
    public MudExCssBuilder AddClass(string value, params string[] other)
        => new[] { value }.Concat(other).Aggregate(this, (acc, f) => acc.AddClass(f, true));
    public MudExCssBuilder AddClass(string value, bool when) => !when ? this : AddClass(value);
    public MudExCssBuilder AddClass(string value, Func<bool> when) => AddClass(value, when != null && when());
    public MudExCssBuilder AddClass(Func<string> value, bool when = true) => !when ? this : AddClass(value());
    public MudExCssBuilder AddClass(Func<string> value, Func<bool> when) => AddClass(value, when != null && when());
    public MudExCssBuilder AddClass(CssBuilder builder, bool when = true) => !when ? this : AddClass(builder.Build());
    public MudExCssBuilder AddClass(CssBuilder builder, Func<bool> when) => AddClass(builder, when != null && when());
    public MudExCssBuilder AddClass(MudExCssBuilder builder, Func<bool> when) => AddClass(builder, when != null && when());

    public MudExCssBuilder AddClass(MudExCssBuilder builder, bool when = true)
    {
        if (!when)
            return this;
        _disposables.Add(builder);
        return AddClass(builder.Build());
    }
    
    public async Task<MudExCssBuilder> AddClassFromStyleAsync(MudExStyleBuilder builder, bool when = true)
    {
        if (!when)
            return this;
        _disposables.Add(builder);
        var tmpClass = await builder.BuildAsClassRuleAsync();
        _temporaryCssClasses.TryAdd(tmpClass, 0);
        return AddClass(tmpClass);
    }

    public Task<MudExCssBuilder> AddClassFromStyleAsync(MudExStyleBuilder builder, Func<bool> when) => AddClassFromStyleAsync(builder, when != null && when());
    public Task<MudExCssBuilder> AddClassFromStyleAsync(object styleObject, bool when = true) => AddClassFromStyleAsync(MudExStyleBuilder.FromObject(styleObject), when);
    public Task<MudExCssBuilder> AddClassFromStyleAsync(string styleString, bool when = true) => AddClassFromStyleAsync(MudExStyleBuilder.FromStyle(styleString), when);
    public Task<MudExCssBuilder> AddClassFromStyleAsync(object styleObject, Func<bool> when) => AddClassFromStyleAsync(MudExStyleBuilder.FromObject(styleObject), when);
    public Task<MudExCssBuilder> AddClassFromStyleAsync(string styleString, Func<bool> when) => AddClassFromStyleAsync(MudExStyleBuilder.FromStyle(styleString), when);

    public MudExCssBuilder AddClassFromAttributes(IReadOnlyDictionary<string, object> additionalAttributes) =>
        additionalAttributes != null && additionalAttributes.TryGetValue("class", out var obj) ? AddClass(obj.ToString()) : this;

    public string Build() => string.Join(" ", _cssClasses.Select(c => $"{c.Key}"));

    public async ValueTask DisposeAsync()
    {
        foreach (var disposable in _disposables)
            await disposable.DisposeAsync();
    }

    public string Class => Build();
}