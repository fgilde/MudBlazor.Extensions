using Nextended.Core.Extensions;
using System.Linq.Expressions;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Theme preset
/// </summary>
/// <typeparam name="TTheme"></typeparam>
public class ThemePreset<TTheme> where TTheme : MudTheme
{
    public virtual object Id { get; set; } = Guid.NewGuid();
    
    public string Name { get; set; }
    public string? Description { get; set; }
    public TTheme Theme { get; set; }

    public ThemePreset()
    {}

    public ThemePreset(string name, TTheme theme)
    {
        Name = name;
        Theme = theme;
    }

    public ThemePreset(string name, string description, TTheme theme)
    {
        Name = name;
        Description = description;
        Theme = theme;
    }

    public static implicit operator TTheme(ThemePreset<TTheme> set) => set.Theme;
}

public static class ThemePreset
{
    public static ThemePreset<TTheme> Create<TTheme>(string name, TTheme theme) where TTheme : MudTheme => new(name, theme);
    public static ThemePreset<TTheme> Create<TTheme>(string name, string description, TTheme theme) where TTheme : MudTheme => new(name, description, theme);
    
    public static ICollection<ThemePreset<TTheme>> Create<TTheme>(params KeyValuePair<string, TTheme>[] sets) where TTheme : MudTheme
        => sets.Select(p => Create(p.Key, p.Value)).ToList();

    public static ICollection<ThemePreset<TTheme>> Create<TTheme>(params Tuple<string, TTheme>[] sets) where TTheme : MudTheme
        => sets.Select(p => Create(p.Item1, p.Item2)).ToList();
    public static ICollection<ThemePreset<TTheme>> Create<TTheme>(params Tuple<string, string, TTheme>[] sets) where TTheme : MudTheme
        => sets.Select(p => Create(p.Item1, p.Item2, p.Item3)).ToList();

    public static ICollection<ThemePreset<TTheme>> Create<TTheme>(IDictionary<string, TTheme> dict) where TTheme : MudTheme
        => dict.Select(p => Create(p.Key, p.Value)).ToList();

    public static ICollection<ThemePreset<TTheme>> Create<TTheme>(params (string Name, TTheme Theme)[] sets) where TTheme : MudTheme
        => sets.Select(p => Create(p.Name, p.Theme)).ToList();

    public static ICollection<ThemePreset<TTheme>> Create<TTheme>(params Expression<Func<TTheme>>[] expressions) where TTheme : MudTheme 
        => Create(expressions.Select(e => (e.GetMemberName(), e.Compile()())).ToArray());

    public static ICollection<ThemePreset<TTheme>> Create<TTheme>(params (string Name, string Description, TTheme Theme)[] sets) where TTheme : MudTheme
        => sets.Select(p => Create(p.Name, p.Description, p.Theme)).ToList();
}


/// <summary>
/// Theme edit mode for MudExThemeEdit
/// </summary>
public enum ThemeEditMode
{
    Simple,
    Full,
}