using System.Text;
using System.Xml;

namespace MudBlazor.Extensions.Office.Document;

internal sealed class RtfPolicy
{
    internal const int TabMark = 0xFFFC;
    private const int CodePageFallback = 1252;

    private static readonly HashSet<string> Destinations =
        ["fonttbl", "colortbl", "stylesheet", "info", "pict", "object", "objdata", "fldinst", "listtext", "pntext", "listtable", "listoverridetable"];

    private readonly RtfLimits _limits;
    private readonly Action<RtfToken> _output;
    private readonly Group[] _groups;
    private readonly StringBuilder _text = new();
    private int _depth, _written, _skipFrom, _paragraphs;
    private long _segments, _characters, _runWork, _tableWork, _elements, _hexRun;
    private bool _inText, _started, _ended, _ucWritten, _pendingOpen, _pendingStar;
    private int? _high;

    private RtfPolicy(RtfLimits limits, Action<RtfToken> output)
    {
        _limits = limits;
        _output = output;
        _groups = new Group[limits.Depth + 2];
    }

    internal static void Apply(IEnumerable<RtfToken> tokens, RtfLimits limits, Action<RtfToken> output)
    {
        var policy = new RtfPolicy(limits, output);
        foreach (var token in tokens) policy.Add(token);
        policy.Finish();
    }

    private void Add(RtfToken token)
    {
        if (token.Kind != RtfTokenKind.Unicode) _high = null;
        if (token.Kind == RtfTokenKind.GroupOpen)
        {
            if (_ended) throw RtfTokenizer.Damaged();
            _started = true;
            if (++_depth > _limits.Depth) throw LoadLimit.Exceeded("The RTF is nested too deeply.");
            _groups[_depth] = _groups[_depth - 1] with { Fresh = true };
            if (_skipFrom > 0) return;
            FlushOpen();
            _pendingOpen = true;
            return;
        }

        if (token.Kind == RtfTokenKind.GroupClose)
        {
            if (_depth == 0) return;
            if (_skipFrom > 0)
            {
                if (_depth-- == _skipFrom) _skipFrom = 0;
                return;
            }

            FlushOpen();
            _depth--;
            Emit(RtfToken.Close);
            _ended = _depth == 0;
            return;
        }

        if (_depth == 0)
        {
            if (_started && token.Kind == RtfTokenKind.Text && token.Name.All(c => c == '\0' || char.IsWhiteSpace(c))) return;
            throw RtfTokenizer.Damaged();
        }

        if (_skipFrom > 0) return;
        if (_pendingOpen && !_pendingStar && token is { Kind: RtfTokenKind.Symbol, Value: '*' })
        {
            _pendingStar = true;
            _groups[_depth].Skip = true;
            return;
        }

        if (_pendingOpen && token.Is("ud"))
        {
            (_pendingOpen, _pendingStar, _skipFrom) = (false, false, _depth);
            return;
        }

        if (token is { Kind: RtfTokenKind.Word, Name: "upr" or "ud" or "uc" or "bin" }) return; // dropped words leave the group-start state alone
        FlushOpen();
        var fresh =_groups[_depth].Fresh;
        _groups[_depth].Fresh = false;
        switch (token.Kind)
        {
            case RtfTokenKind.Unicode:
                AddUnicode(token.Value);
                break;
            case RtfTokenKind.Symbol when token.Value is '~' or '-' or '_' or '*' or ':' or '|':
                if (token.Value == '*' && fresh) _groups[_depth].Skip = true;
                Emit(token);
                break;
            case RtfTokenKind.Word:
                AddWord(token, fresh);
                break;
            case RtfTokenKind.Text or RtfTokenKind.Hex:
                Emit(token);
                break;
        }
    }

    private void AddWord(RtfToken token, bool fresh)
    {
        var name = token.Name;
        if (fresh && Destinations.Contains(name)) _groups[_depth].Skip = true;
        if (name == "itap" && token.Value > RtfLimits.TableNesting) throw LoadLimit.Exceeded("The RTF nests tables too deeply.");
        if (name == "ilvl") token = RtfToken.Word(name, Math.Clamp(token.Value, 0, RtfLimits.ListLevel));
        else if (name is "ansicpg" or "cpg" && token.HasValue && !Available(token.Value)) token = RtfToken.Word(name, CodePageFallback);
        else if (name == "tab" && !_groups[_depth].Skip) Write(RtfToken.Unicode(TabMark));
        Emit(token);
        if (_ucWritten) return;
        _ucWritten = true;
        Write(RtfToken.Word("uc", 1));
    }

    private void AddUnicode(int value)
    {
        var c = (char)value;
        if (char.IsHighSurrogate(c)) _high = value;
        else if (char.IsLowSurrogate(c))
        {
            if (_high is not { } high) return;
            _high = null;
            Emit(RtfToken.Unicode(high));
            Emit(RtfToken.Unicode(value));
        }
        else if (value != TabMark && XmlConvert.IsXmlChar(c)) Emit(RtfToken.Unicode(value));
    }

    private void FlushOpen()
    {
        if (!_pendingOpen) return;
        _pendingOpen = false;
        Emit(RtfToken.Open);
        if (!_pendingStar) return;
        _pendingStar = false;
        Emit(new RtfToken(RtfTokenKind.Symbol, Value: '*'));
    }

    private void Finish()
    {
        FlushOpen();
        while (_written > 0) Write(RtfToken.Close);
        End("sect");
    }

    private void Emit(RtfToken token)
    {
        Account(token);
        Write(token);
    }

    private void Write(RtfToken token)
    {
        if (token.Kind == RtfTokenKind.Text)
        {
            _text.Append(token.Name);
            return;
        }

        if (_text.Length > 0)
        {
            _output(RtfToken.Text(_text.ToString()));
            _text.Clear();
        }

        if (token.Kind == RtfTokenKind.GroupOpen) _written++;
        else if (token.Kind == RtfTokenKind.GroupClose) _written--;
        _output(token);
    }

    private void Account(RtfToken token)
    {
        if (token.Kind == RtfTokenKind.Hex) _runWork += ++_hexRun >> 7;
        else _hexRun = 0;
        switch (token.Kind)
        {
            case RtfTokenKind.Text:
                AddText(token.Name.Length);
                return;
            case RtfTokenKind.Hex or RtfTokenKind.Unicode:
            case RtfTokenKind.Symbol when token.Value is '~' or '-' or '_':
                AddText(1);
                return;
            case not RtfTokenKind.Word:
                _inText = false;
                return;
        }

        switch (token.Name)
        {
            case "emdash" or "endash" or "bullet" or "lquote" or "rquote" or "ldblquote" or "rdblquote" or "emspace" or "enspace" or "qmspace" or
                "zwj" or "zwnj" or "zwbo" or "zwnbo" or "ltrmark" or "rtlmark":
                AddText(1);
                return;
            case "chdate" or "chdpl" or "chdpa" or "chtime":
                AddText(RtfLimits.DateLength);
                return;
        }

        _inText = false;
        switch (token.Name)
        {
            case "tab" or "line":
                _segments++;
                break;
            case "page":
                End("par");
                break;
            case "sect":
                _paragraphs++;
                End("sect");
                break;
            case "par" or "cell" or "nestcell" or "row" or "nestrow":
                End(token.Name);
                break;
            case "intbl":
                _groups[_depth].InTable = true;
                break;
            case "pard":
                _groups[_depth].InTable = false;
                break;
        }
    }

    private void AddText(int length)
    {
        if (!_inText) _segments++;
        _characters += length;
        _inText = true;
    }

    private void End(string word)
    {
        _runWork += _segments * Math.Max(_segments, _characters);
        (_segments, _characters, _inText) = (0, 0, false);
        var inTable = _groups[_depth].InTable;
        if (word is not ("par" or "sect") || word is "par" && inTable) _tableWork += _elements + RtfLimits.CellWork;
        if (word is "row" or "nestrow" || word is "par" && !inTable) _elements++;
        if (word is not "sect") _paragraphs++;
        if (_runWork > _limits.RunWork || _tableWork > _limits.TableWork || _paragraphs > _limits.Paragraphs)
            throw LoadLimit.Exceeded("The RTF is too complex to read.");
        if (word is "sect") _elements = 0;
    }

    private static bool Available(int codePage) =>
        codePage is 0 or 42 or 1252 or 20127 or 28591 or 65001 || codePage is > 0 and <= ushort.MaxValue && CodePagesEncodingProvider.Instance.GetEncoding(codePage) != null;

    private record struct Group(bool Skip, bool Fresh, bool InTable);
}
