namespace MudBlazor.Extensions.Office.Document;

internal static class TextDiff
{
    internal static (int Start, int OldEnd, int NewEnd) Compute(string before, string after, int? caret)
    {
        var max = Math.Min(before.Length, after.Length);
        var prefix = Prefix(max);
        var suffix = Suffix(max - prefix);
        if (caret is { } c)
        {
            var inserted = after.Length - suffix - prefix;
            var shifted = Math.Clamp(c - inserted, Math.Min(prefix, Prefix(max - Suffix(max))), prefix);
            suffix = Math.Min(suffix + prefix - shifted, max - shifted);
            prefix = shifted;
        }

        prefix = Math.Min(TextDocument.SnapOffset(before, prefix, true), TextDocument.SnapOffset(after, prefix, true));
        var oldEnd = TextDocument.SnapOffset(before, before.Length - suffix, false);
        var newEnd = TextDocument.SnapOffset(after, after.Length - suffix, false);
        suffix = Math.Min(before.Length - oldEnd, after.Length - newEnd);
        return (prefix, before.Length - suffix, after.Length - suffix);

        int Prefix(int limit)
        {
            var i = 0;
            while (i < limit && before[i] == after[i]) i++;
            return i;
        }

        int Suffix(int limit)
        {
            var i = 0;
            while (i < limit && before[^(i + 1)] == after[^(i + 1)]) i++;
            return i;
        }
    }
}
