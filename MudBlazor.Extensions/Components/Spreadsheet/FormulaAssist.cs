using MudBlazor.Extensions.Office.Spreadsheet;

namespace MudBlazor.Extensions.Components;

/// <summary>A completion offered while typing a formula.</summary>
/// <param name="Name">The function or defined name.</param>
/// <param name="Description">Short description, or the reference a name refers to.</param>
/// <param name="IsName">True for a defined name, false for a function.</param>
internal sealed record FormulaSuggestion(string Name, string Description, bool IsName);

/// <summary>What formula assist offers at the caret: completions for the word being typed and the signature of the enclosing function.</summary>
/// <param name="Prefix">The word being typed, or empty.</param>
/// <param name="PrefixStart">Offset of the word in the text.</param>
/// <param name="Suggestions">Matching functions and names.</param>
/// <param name="Function">The innermost function whose argument list contains the caret, or null.</param>
/// <param name="ArgumentIndex">Zero-based index of the argument at the caret.</param>
internal sealed record FormulaAssistResult(string Prefix, int PrefixStart, IReadOnlyList<FormulaSuggestion> Suggestions, FunctionInfo Function, int ArgumentIndex)
{
    public static FormulaAssistResult None { get; } = new("", 0, [], null, 0);

    public string Accept(string text, int index)
    {
        var s = Suggestions[index];
        var end = PrefixStart + Prefix.Length;
        var insert = s.IsName ? s.Name : s.Name + "(";
        if (!s.IsName && end < text.Length && text[end] == '(') insert = s.Name;
        return text[..PrefixStart] + insert + text[end..];
    }

    public (string Before, string Current, string After) SignatureParts()
    {
        var signature = Function?.Signature;
        if (string.IsNullOrEmpty(signature)) return ("", "", "");
        var open = signature.IndexOf('(');
        var close = signature.LastIndexOf(')');
        if (open < 0 || close < open) return (signature, "", "");
        var args = signature[(open + 1)..close].Split(", ");
        if (ArgumentIndex >= args.Length && args[^1] != "...") return (signature, "", "");
        var i = Math.Min(ArgumentIndex, args.Length - 1);
        if (args[i] == "..." && i > 0) i--;
        var before = signature[..(open + 1)] + string.Join(", ", args[..i]) + (i > 0 ? ", " : "");
        var after = (i < args.Length - 1 ? ", " + string.Join(", ", args[(i + 1)..]) : "") + signature[close..];
        return (before, args[i], after);
    }
}

/// <summary>Formula autocomplete from the function catalog and defined names, and the signature tip of the enclosing function.</summary>
internal static class FormulaAssist
{
    private const int MaxSuggestions = 50;

    public static FormulaAssistResult Analyze(string text, int caret, FunctionRegistry functions, IEnumerable<string> names)
    {
        if (string.IsNullOrEmpty(text) || text[0] != '=') return FormulaAssistResult.None;
        caret = Math.Clamp(caret, 0, text.Length);
        var stack = new Stack<(string Name, int Arg)>();
        var inString = false;
        for (var i = 1; i < caret; i++)
        {
            var ch = text[i];
            if (ch == '"') inString = !inString;
            if (inString) continue;
            switch (ch)
            {
                case '(':
                    var start = i;
                    while (start > 1 && IsWordChar(text[start - 1])) start--;
                    stack.Push((text[start..i], 0));
                    break;
                case ')' when stack.Count > 0:
                    stack.Pop();
                    break;
                case ',' or ';' when stack.Count > 0:
                    var top = stack.Pop();
                    stack.Push((top.Name, top.Arg + 1));
                    break;
            }
        }

        FunctionInfo function = null;
        var argument = 0;
        foreach (var (name, arg) in stack)
            if (name.Length > 0 && functions.TryGet(name, out var f))
            {
                function = f.Info;
                argument = arg;
                break;
            }

        if (inString) return new FormulaAssistResult("", caret, [], function, argument);
        var prefixStart = caret;
        while (prefixStart > 1 && IsWordChar(text[prefixStart - 1])) prefixStart--;
        var prefix = text[prefixStart..caret];
        if (prefix.Length == 0 || !char.IsLetter(prefix[0]) || prefixStart > 1 && text[prefixStart - 1] is '$' or '!' or ':')
            return new FormulaAssistResult("", caret, [], function, argument);

        var suggestions = functions.Functions
            .Where(f => f.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f.Name, StringComparer.Ordinal)
            .Select(f => new FormulaSuggestion(f.Name, f.Info.Description, false))
            .Concat(names.Where(n => n.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .Select(n => new FormulaSuggestion(n, null, true)))
            .Take(MaxSuggestions)
            .ToList();
        return new FormulaAssistResult(prefix, prefixStart, suggestions, function, argument);
    }

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c is '_' or '.';
}
