using System.Globalization;
using System.Text;

namespace MudBlazor.Extensions.Office.Document;

internal static class NumberingLabels
{
    private const int Levels = 9;

    private static readonly (int Value, string Symbol)[] Romans =
        [(1000, "M"), (900, "CM"), (500, "D"), (400, "CD"), (100, "C"), (90, "XC"), (50, "L"), (40, "XL"), (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I")];

    internal static IReadOnlyDictionary<string, string> Compute(Numbering numbering, IEnumerable<Paragraph> inDocumentOrder)
    {
        var labels = new Dictionary<string, string>();
        var counters = new Dictionary<(int AbstractId, int NumId), int?[]>();
        foreach (var paragraph in inDocumentOrder)
        {
            if (paragraph.Id == null || paragraph.Format.Numbering is not { NumId: not 0, Level: >= 0 and < Levels } reference || numbering.Level(reference) is not { } level) continue;
            var instance = numbering.Instances.First(i => i.Id == reference.NumId);
            var key = (instance.AbstractId, instance.Overrides.Count == 0 ? 0 : instance.Id);
            if (!counters.TryGetValue(key, out var values)) counters[key] = values = new int?[Levels];
            for (var lower = reference.Level + 1; lower < Levels; lower++)
            {
                var restart = numbering.Level(reference with { Level = lower })?.Restart ?? lower;
                if (restart != 0 && reference.Level < restart) values[lower] = null;
            }

            values[reference.Level] = values[reference.Level] + 1 ?? level.Start;
            labels[paragraph.Id] = Label(numbering, reference, level, values);
        }

        return labels;
    }

    private static string Label(Numbering numbering, NumberingRef reference, NumberingLevel level, int?[] values)
    {
        var text = level.Text ?? "";
        var label = new StringBuilder();
        for (var i = 0; i < text.Length; i++)
            if (text[i] == '%' && i + 1 < text.Length && text[i + 1] is >= '1' and <= '9')
            {
                var index = text[++i] - '1';
                var referenced = index == reference.Level ? level : numbering.Level(reference with { Level = index });
                label.Append(Format(values[index] ?? referenced?.Start ?? 0, referenced?.Format ?? NumberFormat.Decimal));
            }
            else
                label.Append(Symbol(text[i]));

        return label.ToString();
    }

    private static string Format(int value, NumberFormat format) => format switch
    {
        NumberFormat.DecimalZero => value.ToString("00", CultureInfo.InvariantCulture),
        NumberFormat.LowerLetter => Letters(value).ToLowerInvariant(),
        NumberFormat.UpperLetter => Letters(value),
        NumberFormat.LowerRoman => Roman(value).ToLowerInvariant(),
        NumberFormat.UpperRoman => Roman(value),
        NumberFormat.Bullet or NumberFormat.None => "",
        _ => value.ToString(CultureInfo.InvariantCulture)
    };

    private static string Letters(int value) =>
        value < 1 ? value.ToString(CultureInfo.InvariantCulture) : new string((char)('A' + (value - 1) % 26), (value - 1) / 26 + 1);

    internal static string Roman(int value)
    {
        if (value is < 1 or > 3999) return value.ToString(CultureInfo.InvariantCulture);
        var roman = new StringBuilder();
        foreach (var (step, symbol) in Romans)
            for (; value >= step; value -= step)
                roman.Append(symbol);
        return roman.ToString();
    }

    private static string Symbol(char c) => c switch
    {
        '\uF0B7' => "\u2022",
        '\uF0A7' => "\u25AA",
        '\uF0D8' => "\u27A2",
        '\uF0FC' => "\u2713",
        '\uF076' => "\u2756",
        >= '\uF000' and <= '\uF0FF' => "\u2022",
        _ => c.ToString()
    };
}
