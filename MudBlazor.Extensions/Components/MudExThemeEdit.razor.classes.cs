using Nextended.Core.Extensions;
using System.Linq.Expressions;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Theme preset is just a wrapper class for a name and a theme
/// </summary>
/// <typeparam name="TTheme"></typeparam>
public class ThemePreset<TTheme> where TTheme : MudTheme
{
    /// <summary>
    /// Id of the preset can used if item is stored or loaded from database
    /// </summary>
    public virtual object Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Name of the theme
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Optional description for the theme
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// The Theme itself. 
    /// </summary>
    public TTheme Theme { get; set; }

    /// <summary>
    /// Creates an instance
    /// </summary>
    public ThemePreset()
    {}

    /// <summary>
    /// Creates an instance with name and theme
    /// </summary>
    public ThemePreset(string name, TTheme theme)
    {
        Name = name;
        Theme = theme;
    }

    /// <summary>
    /// Creates an instance with name, theme and description
    /// </summary>
    public ThemePreset(string name, string description, TTheme theme)
    {
        Name = name;
        Description = description;
        Theme = theme;
    }

    /// <summary>
    /// Implicit assignable as TTheme
    /// </summary>
    public static implicit operator TTheme(ThemePreset<TTheme> set) => set.Theme;
}

/// <summary>
/// Static helper class for creating instances of ThemePresets.
/// </summary>
public static class ThemePreset
{
    /// <summary>
    /// Creates a new theme preset with a given name and theme.
    /// </summary>
    public static ThemePreset<TTheme> Create<TTheme>(string name, TTheme theme) where TTheme : MudTheme => new(name, theme);

    /// <summary>
    /// Creates a new theme preset with a given name, description, and theme.
    /// </summary>
    public static ThemePreset<TTheme> Create<TTheme>(string name, string description, TTheme theme) where TTheme : MudTheme => new(name, description, theme);

    /// <summary>
    /// Creates a collection of theme presets from an array of key-value pairs.
    /// </summary>
    public static ICollection<ThemePreset<TTheme>> Create<TTheme>(params KeyValuePair<string, TTheme>[] sets) where TTheme : MudTheme
        => sets.Select(p => Create(p.Key, p.Value)).ToList();

    /// <summary>
    /// Creates a collection of theme presets from an array of tuples.
    /// </summary>
    public static ICollection<ThemePreset<TTheme>> Create<TTheme>(params Tuple<string, TTheme>[] sets) where TTheme : MudTheme
        => sets.Select(p => Create(p.Item1, p.Item2)).ToList();

    /// <summary>
    /// Creates a collection of theme presets from an array of tuples with descriptions.
    /// </summary>
    public static ICollection<ThemePreset<TTheme>> Create<TTheme>(params Tuple<string, string, TTheme>[] sets) where TTheme : MudTheme
        => sets.Select(p => Create(p.Item1, p.Item2, p.Item3)).ToList();

    /// <summary>
    /// Creates a collection of theme presets from a dictionary.
    /// </summary>
    public static ICollection<ThemePreset<TTheme>> Create<TTheme>(IDictionary<string, TTheme> dict) where TTheme : MudTheme
        => dict.Select(p => Create(p.Key, p.Value)).ToList();

    /// <summary>
    /// Creates a collection of theme presets from an array of tuples.
    /// </summary>
    public static ICollection<ThemePreset<TTheme>> Create<TTheme>(params (string Name, TTheme Theme)[] sets) where TTheme : MudTheme
        => sets.Select(p => Create(p.Name, p.Theme)).ToList();

    /// <summary>
    /// Creates a collection of theme presets from an array of expressions.
    /// </summary>
    public static ICollection<ThemePreset<TTheme>> Create<TTheme>(params Expression<Func<TTheme>>[] expressions) where TTheme : MudTheme
        => Create(expressions.Select(e => (e.GetMemberName(), e.Compile()())).ToArray());

    /// <summary>
    /// Creates a collection of theme presets from an array of tuples with descriptions.
    /// </summary>
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