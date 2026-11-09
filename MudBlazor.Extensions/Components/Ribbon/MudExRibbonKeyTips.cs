namespace MudBlazor.Extensions.Components;

internal static class MudExRibbonKeyTips
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const string Suffixes = "1234567890ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const int Explicit = -1;

    internal static string[] Assign(IReadOnlyList<(string Text, string KeyTip)> entries)
    {
        var tips = entries.Select(e => string.IsNullOrWhiteSpace(e.KeyTip) ? null : e.KeyTip.Trim().ToUpperInvariant()).ToArray();
        var multis = tips.Where(t => t?.Length > 1).ToHashSet();
        var prefixes = multis.Select(t => t[0]).ToHashSet();
        var singles = new Dictionary<char, int>();
        foreach (var tip in tips.Where(t => t?.Length == 1))
            singles.TryAdd(tip[0], Explicit);

        var pending = new List<int>();
        for (var i = 0; i < tips.Length; i++)
        {
            if (tips[i] != null)
                continue;
            var free = Candidates(entries[i].Text).Where(c => !singles.ContainsKey(c) && !prefixes.Contains(c)).Select(c => (char?)c).FirstOrDefault();
            if (free is { } c)
            {
                singles[c] = i;
                tips[i] = c.ToString();
            }
            else
            {
                pending.Add(i);
            }
        }

        foreach (var i in pending)
        {
            var candidates = Candidates(entries[i].Text);
            var prefix = candidates.Where(c => !(singles.TryGetValue(c, out var holder) && holder == Explicit)).DefaultIfEmpty(candidates[0]).First();
            if (singles.TryGetValue(prefix, out var owner) && owner != Explicit)
            {
                singles.Remove(prefix);
                tips[owner] = TwoCharacters(prefix);
            }
            tips[i] = TwoCharacters(prefix);
        }
        return tips;

        string TwoCharacters(char prefix)
        {
            prefixes.Add(prefix);
            var tip = Suffixes.Select(s => $"{prefix}{s}").FirstOrDefault(t => !multis.Contains(t));
            if (tip != null)
                multis.Add(tip);
            return tip;
        }
    }

    private static List<char> Candidates(string text)
    {
        var letters = (text ?? string.Empty).Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).Distinct().ToList();
        return letters.Count > 0 ? letters : Alphabet.ToList();
    }
}
