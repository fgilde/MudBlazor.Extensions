using MudBlazor.Utilities;

namespace MudBlazor.Extensions.Helper;

public sealed class StringMudColorConverter : IReversibleConverter<string, MudColor?>, IReversibleConverter<MudColor?, string>
{
    public MudColor Convert(string input)
        => string.IsNullOrWhiteSpace(input) ? new MudColor("#000000") : new MudColor(input);

    public string ConvertBack(MudColor output)
        => output.ToString(MudColorOutputFormats.Hex);

    public static StringMudColorConverter Instance { get; } = new();
    public string Convert(MudColor input)
    {
        return ConvertBack(input);
    }

    public MudColor ConvertBack(string input)
    {
        return Convert(input);
    }
}

public class MudExDefaultConverter<T>(Func<T, string> convertFn, Func<string, T> convertBackFn) : MudExDefaultConverter<T?, string?>(convertFn, convertBackFn)
{

}

public class MudExDefaultConverter<T, T2>(Func<T, T2> convertFn, Func<T2, T> convertBackFn)
    : IReversibleConverter<T?, T2?>
{
    public T2 Convert(T input)
    {
        if (input is null) return default;
        return convertFn(input);
    }

    public T ConvertBack(T2 input)
    {
        if (input is null || convertBackFn is null) return default;
        return convertBackFn(input);
    }
}