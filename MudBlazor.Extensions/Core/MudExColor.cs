using MudBlazor.Extensions.Helper;
using MudBlazor.Utilities;
using OneOf;

namespace MudBlazor.Extensions.Core;

public readonly struct MudExColor
{
    private readonly OneOf<Color, MudColor, string, int> _value;

    public MudExColor(OneOf<Color, MudColor, string, int> value)
    {
        _value = value.IsT2 switch
        {
            true when Enum.TryParse(value.AsT2, out Color color) => color,
            true when ColorExtensions.TryParseFromHtmlColorName(value.AsT2, out System.Drawing.Color dc) => dc.ToMudColor(),
            _ => value
        };
    }

    public object Value => _value.Value;
    public bool IsColor => _value.IsT0;
    public bool IsMudColor => _value.IsT1;
    public bool IsString => _value.IsT2;
    public bool IsInt => _value.IsT3;

    public Color AsColor => _value.AsT0;
    public MudColor AsMudColor => _value.AsT1;
    public string AsString => _value.AsT2;
    public int AsInt => _value.AsT3;
    public TResult Match<TResult>(Func<Color, TResult> f0, Func<MudColor, TResult> f1, Func<string, TResult> f2, Func<int, TResult> f3) => _value.Match(f0, f1, f2, f3);
    public void Switch(Action<Color> f0, Action<MudColor> f1, Action<string> f2, Action<int> f3) => _value.Switch(f0, f1, f2, f3);


    // Implicit conversions
    public static implicit operator MudExColor(Color c) => new MudExColor(c);
    public static implicit operator MudExColor(MudColor c) => new MudExColor(c);
    public static implicit operator MudExColor(string s) => new MudExColor(s);
    public static implicit operator MudExColor(int i) => new MudExColor(i);

    public bool Is(Color c) => _value.Value.Equals(c);
    public bool Is(string c) => _value.Value.Equals(c);
    public bool Is(MudColor c) => _value.Value.Equals(c);

    public override string ToString()
        => Match(
            color => color.ToString(),
            mudColor => mudColor.ToString(),
            s => s,
            i => i.ToString() // adjust this based on how you want to handle int
        );

    public string ToCssStringValue(MudColorOutputFormats format = MudColorOutputFormats.RGBA)
        => Match(
            color => color.CssVarDeclaration(),
            mudColor => mudColor.ToString(format),
            s => s.ToLower().StartsWith("var") ? s : new MudColor(s).ToString(format),
            //s => new MudColor(s).ToString(format),
            i => FromInt(i).ToString(format)
        );

    public Task<MudColor> ToMudColorAsync()
        => Match(
            color => color.ToMudColorAsync(),
            Task.FromResult,
            s => Task.FromResult(new MudColor(s)),
            i => Task.FromResult(FromInt(i))
        );

    public static async Task<IEnumerable<(string Name, MudColor Color)>> GetColorsFromThemeAsync(int count = 10)
    {
        var themeColors = await MudExCss.GetCssColorVariablesAsync();
        return themeColors
            //.Where(c => !c.Key.Contains("background", StringComparison.InvariantCultureIgnoreCase) && !c.Key.Contains("surface", StringComparison.InvariantCultureIgnoreCase) && !c.Value.IsBlack() && !c.Value.IsWhite() && c.Value.APercentage >= 1.0)
            .Select(x => (x.Key, x.Value))
            .Distinct()
            .Take(count);
    }


    internal static MudColor FromInt(int value)
    {
        return new MudColor((byte)((value >> 16) & 0xFF),
            (byte)((value >> 8) & 0xFF), (byte)(value & 0xFF), (byte)((value >> 24) & 0xFF));
    }
}
