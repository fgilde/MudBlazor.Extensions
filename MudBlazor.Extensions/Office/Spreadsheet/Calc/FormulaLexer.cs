using System.Text.RegularExpressions;

namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>Kinds of formula tokens.</summary>
public enum FormulaTokenKind
{
    /// <summary>A number literal.</summary>
    Number,

    /// <summary>A quoted text literal including its quotes.</summary>
    Text,

    /// <summary><c>TRUE</c> or <c>FALSE</c>.</summary>
    Boolean,

    /// <summary>An error literal, optionally sheet-qualified.</summary>
    Error,

    /// <summary>A cell or range reference, optionally sheet-qualified, or a spill range reference such as <c>A1#</c>.</summary>
    Reference,

    /// <summary>A function name (the next character is the opening parenthesis).</summary>
    Function,

    /// <summary>A defined name.</summary>
    Name,

    /// <summary>An operator such as <c>+</c>, <c>&lt;=</c>, <c>%</c> or <c>:</c>.</summary>
    Operator,

    /// <summary><c>(</c></summary>
    OpenParen,

    /// <summary><c>)</c></summary>
    CloseParen,

    /// <summary><c>{</c></summary>
    OpenBrace,

    /// <summary><c>}</c></summary>
    CloseBrace,

    /// <summary><c>,</c></summary>
    Comma,

    /// <summary><c>;</c></summary>
    Semicolon
}

/// <summary>A lexical token of a formula.</summary>
/// <param name="Kind">The token kind.</param>
/// <param name="Text">The token text exactly as written.</param>
/// <param name="Start">Offset of the token in the formula text.</param>
/// <param name="SpaceBefore">True if whitespace precedes the token.</param>
public readonly record struct FormulaToken(FormulaTokenKind Kind, string Text, int Start, bool SpaceBefore);

/// <summary>Splits formula text into tokens, for example to colour references while typing.</summary>
public static class FormulaLexer
{
    private static readonly Regex RefPattern = new(
        @"\G(?:\$?[A-Za-z]{1,3}\$?[0-9]+(?::\$?[A-Za-z]{1,3}\$?[0-9]+)?|\$?[A-Za-z]{1,3}:\$?[A-Za-z]{1,3}|\$?[0-9]+:\$?[0-9]+)",
        RegexOptions.CultureInvariant);

    private static readonly Regex NumberPattern = new(@"\G(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)(?:[eE][+-]?[0-9]+)?",
        RegexOptions.CultureInvariant);

    internal static readonly (string Code, CellError Error)[] ErrorCodes = Enum.GetValues<CellError>()
        .Select(e => (CellValue.Error(e).ToString(), e)).OrderByDescending(e => e.Item1.Length).ToArray();

    /// <summary>Tokenizes a formula; a leading equals sign is skipped.</summary>
    /// <param name="formula">The formula text.</param>
    /// <returns>The tokens in order.</returns>
    /// <exception cref="FormatException">The text contains a character or literal that is not valid in a formula.</exception>
    public static IReadOnlyList<FormulaToken> Tokenize(string formula)
    {
        var s = formula ?? "";
        var tokens = new List<FormulaToken>();
        var i = s.StartsWith('=') ? 1 : 0;
        var space = false;
        while (i < s.Length)
        {
            if (char.IsWhiteSpace(s[i]))
            {
                space = true;
                i++;
                continue;
            }

            var start = i;
            var kind = Next(s, ref i);
            tokens.Add(new FormulaToken(kind, s[start..i], start, space));
            space = false;
        }

        return tokens;
    }

    private static FormulaTokenKind Next(string s, ref int i)
    {
        var c = s[i];
        switch (c)
        {
            case '"':
                i = EndOfQuoted(s, i);
                return FormulaTokenKind.Text;
            case '#':
                i = EndOfError(s, i);
                return FormulaTokenKind.Error;
            case '(': i++; return FormulaTokenKind.OpenParen;
            case ')': i++; return FormulaTokenKind.CloseParen;
            case '{': i++; return FormulaTokenKind.OpenBrace;
            case '}': i++; return FormulaTokenKind.CloseBrace;
            case ',': i++; return FormulaTokenKind.Comma;
            case ';': i++; return FormulaTokenKind.Semicolon;
            case '<':
                i += i + 1 < s.Length && s[i + 1] is '>' or '=' ? 2 : 1;
                return FormulaTokenKind.Operator;
            case '>':
                i += i + 1 < s.Length && s[i + 1] == '=' ? 2 : 1;
                return FormulaTokenKind.Operator;
            case '+' or '-' or '*' or '/' or '^' or '&' or '=' or '%' or ':':
                i++;
                return FormulaTokenKind.Operator;
            case '\'':
                var end = EndOfQuoted(s, i);
                if (end >= s.Length || s[end] != '!') throw Unexpected(s, end);
                i = end + 1;
                return AfterSheet(s, ref i);
        }

        var j = i + 1;
        while (j < s.Length && IsNameChar(s[j])) j++;
        if (IsNameChar(c) && j < s.Length && s[j] == '!')
        {
            i = j + 1;
            return AfterSheet(s, ref i);
        }

        if (TryReference(s, ref i)) return FormulaTokenKind.Reference;
        if (char.IsAsciiDigit(c) || c == '.')
        {
            var m = NumberPattern.Match(s, i);
            if (!m.Success) throw Unexpected(s, i);
            i += m.Length;
            return FormulaTokenKind.Number;
        }

        if (!char.IsLetter(c) && c is not ('_' or '\\')) throw Unexpected(s, i);
        var word = s[i..j];
        i = j;
        if (i < s.Length && s[i] == '(') return FormulaTokenKind.Function;
        return word.Equals("TRUE", StringComparison.OrdinalIgnoreCase) || word.Equals("FALSE", StringComparison.OrdinalIgnoreCase)
            ? FormulaTokenKind.Boolean
            : FormulaTokenKind.Name;
    }

    private static FormulaTokenKind AfterSheet(string s, ref int i)
    {
        if (TryReference(s, ref i)) return FormulaTokenKind.Reference;
        if (i < s.Length && s[i] == '#')
        {
            i = EndOfError(s, i);
            return FormulaTokenKind.Error;
        }

        throw Unexpected(s, i);
    }

    private static bool TryReference(string s, ref int i)
    {
        var m = RefPattern.Match(s, i);
        if (!m.Success) return false;
        var end = i + m.Length;
        if (end < s.Length && (IsNameChar(s[end]) || s[end] is '(' or '$')) return false;
        if (!CellRange.TryParse(m.Value, out _)) return false;
        if (end < s.Length && s[end] == '#' && !m.Value.Contains(':')) end++;
        i = end;
        return true;
    }

    private static bool IsNameChar(char c) => char.IsLetterOrDigit(c) || c is '_' or '.' or '\\';

    private static int EndOfQuoted(string s, int i)
    {
        var quote = s[i];
        for (var j = i + 1; j < s.Length; j++)
        {
            if (s[j] != quote) continue;
            if (j + 1 < s.Length && s[j + 1] == quote) j++;
            else return j + 1;
        }

        throw new FormatException($"Unterminated {(quote == '"' ? "text" : "sheet name")} starting at position {i + 1}.");
    }

    private static int EndOfError(string s, int i)
    {
        foreach (var (code, _) in ErrorCodes)
            if (string.Compare(s, i, code, 0, code.Length, StringComparison.OrdinalIgnoreCase) == 0)
                return i + code.Length;
        throw Unexpected(s, i);
    }

    private static FormatException Unexpected(string s, int i) => i >= s.Length
        ? new FormatException("Unexpected end of formula.")
        : new FormatException($"Unexpected '{s[i]}' at position {i + 1}.");
}
