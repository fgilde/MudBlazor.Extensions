using System.Globalization;
using System.Text;

namespace MudBlazor.Extensions.Office.Document;

internal enum RtfTokenKind
{
    GroupOpen,
    GroupClose,
    Word,
    Symbol,
    Hex,
    Unicode,
    Text
}

internal readonly record struct RtfToken(RtfTokenKind Kind, string Name = null, int Value = 0, bool HasValue = false)
{
    internal static RtfToken Open { get; } = new(RtfTokenKind.GroupOpen);

    internal static RtfToken Close { get; } = new(RtfTokenKind.GroupClose);

    internal static RtfToken Word(string name, int? value = null) => new(RtfTokenKind.Word, name, value ?? 0, value.HasValue);

    internal static RtfToken Unicode(int value) => new(RtfTokenKind.Unicode, Value: value & 0xFFFF);

    internal static RtfToken Text(string text) => new(RtfTokenKind.Text, text);

    internal bool Is(string word) => Kind == RtfTokenKind.Word && Name == word;

    internal void Write(StringBuilder output)
    {
        switch (Kind)
        {
            case RtfTokenKind.GroupOpen:
                output.Append('{');
                break;
            case RtfTokenKind.GroupClose:
                output.Append('}');
                break;
            case RtfTokenKind.Word:
                output.Append('\\').Append(Name);
                if (HasValue) output.Append(Value.ToString(CultureInfo.InvariantCulture));
                output.Append(' ');
                break;
            case RtfTokenKind.Symbol:
                output.Append('\\').Append((char)Value);
                break;
            case RtfTokenKind.Hex:
                output.Append("\\'").Append(Value.ToString("x2", CultureInfo.InvariantCulture));
                break;
            case RtfTokenKind.Unicode:
                WriteUnicode(output, Value);
                break;
            default:
                foreach (var c in Name)
                    if (c is '\\' or '{' or '}') output.Append('\\').Append(c);
                    else if (c > 0x7F) WriteUnicode(output, c);
                    else if (c is not ('\r' or '\n')) output.Append(c);
                break;
        }
    }

    private static void WriteUnicode(StringBuilder output, int value) =>
        output.Append("\\u").Append(((short)value).ToString(CultureInfo.InvariantCulture)).Append('?');
}

internal static class RtfTokenizer
{
    internal static IEnumerable<RtfToken> Tokenize(string rtf, int maxTokens)
    {
        var uc = new List<int> { 1 };
        var text = new StringBuilder();
        int i = 0, skip = 0, tokens = 0;
        while (i < rtf.Length)
        {
            var c = rtf[i++];
            if (c is '\r' or '\n') continue;
            if (c is '{' or '}')
            {
                Count();
                skip = 0;
                if (text.Length > 0) yield return Flush();
                if (c == '{') uc.Add(uc[^1]);
                else if (uc.Count > 1) uc.RemoveAt(uc.Count - 1);
                yield return c == '{' ? RtfToken.Open : RtfToken.Close;
                continue;
            }

            RtfToken unit;
            if (c == '\t')
            {
                Count();
                unit = RtfToken.Word("tab");
            }
            else if (c != '\\' || i < rtf.Length && rtf[i] is '\\' or '{' or '}')
            {
                if (c == '\\') c = rtf[i++];
                if (skip > 0) skip--;
                else if (c > 0x7F)
                {
                    if (text.Length > 0) yield return Flush();
                    yield return RtfToken.Unicode(c);
                }
                else text.Append(c);
                continue;
            }
            else if (i == rtf.Length) break;
            else if (rtf[i] == '\'')
            {
                if (i + 2 >= rtf.Length || !char.IsAsciiHexDigit(rtf[i + 1]) || !char.IsAsciiHexDigit(rtf[i + 2])) throw Damaged();
                unit = new RtfToken(RtfTokenKind.Hex, Value: Convert.ToByte(rtf.Substring(i + 1, 2), 16));
                i += 3;
            }
            else if (rtf[i] is '\r' or '\n')
            {
                Count();
                i++;
                unit = RtfToken.Word("par");
            }
            else if (!char.IsAsciiLetter(rtf[i]))
            {
                Count();
                unit = new RtfToken(RtfTokenKind.Symbol, Value: rtf[i++]);
            }
            else
            {
                Count();
                unit = ReadWord(rtf, ref i);
            }

            if (skip > 0)
            {
                skip--;
                continue;
            }

            if (text.Length > 0) yield return Flush();
            if (unit.Is("u"))
            {
                yield return RtfToken.Unicode(unit.Value);
                skip = uc[^1];
                continue;
            }

            if (unit.Is("uc")) uc[^1] = Math.Max(unit.Value, 0);
            yield return unit;
        }

        if (text.Length > 0) yield return Flush();

        void Count()
        {
            if (++tokens > maxTokens) throw LoadLimit.Exceeded("The RTF has too many control words.");
        }

        RtfToken Flush()
        {
            var token = RtfToken.Text(text.ToString());
            text.Clear();
            return token;
        }
    }

    internal static InvalidDataException Damaged() => new("The RTF document is damaged.");

    private static RtfToken ReadWord(string rtf, ref int i)
    {
        var start = i;
        while (i < rtf.Length && char.IsAsciiLetter(rtf[i])) i++;
        var name = rtf[start..i];
        var digits = i;
        if (i < rtf.Length && rtf[i] == '-')
        {
            if (i + 1 >= rtf.Length || !char.IsAsciiDigit(rtf[i + 1])) throw Damaged();
            i++;
        }

        while (i < rtf.Length && char.IsAsciiDigit(rtf[i])) i++;
        int? value = null;
        if (i > digits)
        {
            if (!int.TryParse(rtf.AsSpan(digits, i - digits), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var number)) throw Damaged();
            value = number;
        }

        if (i < rtf.Length && rtf[i] == ' ') i++;
        if (name == "bin" && value > 0) throw new InvalidDataException("The RTF contains binary data, which is not supported.");
        return RtfToken.Word(name, value);
    }
}
