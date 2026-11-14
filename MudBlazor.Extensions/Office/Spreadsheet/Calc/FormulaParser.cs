using System.Globalization;
using System.Text;

namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>
/// Parses formulas with Excel precedence (<c>:</c>, space, union, unary sign, <c>%</c>, <c>^</c>, <c>* /</c>, <c>+ -</c>,
/// <c>&amp;</c>, comparisons) and formats them back in Excel form with <c>,</c> as argument separator.
/// </summary>
public static class FormulaParser
{
    /// <summary>Maximum formula length in characters (as in Excel).</summary>
    public const int MaxLength = 8192;

    private const int MaxDepth = 64;

    private static readonly string[][] Levels = [["=", "<>", "<", "<=", ">", ">="], ["&"], ["+", "-"], ["*", "/"], ["^"]];

    private static readonly Dictionary<string, BinaryOp> Operators = new()
    {
        ["="] = BinaryOp.Equal, ["<>"] = BinaryOp.NotEqual, ["<"] = BinaryOp.Less, ["<="] = BinaryOp.LessOrEqual,
        [">"] = BinaryOp.Greater, [">="] = BinaryOp.GreaterOrEqual, ["&"] = BinaryOp.Concat, ["+"] = BinaryOp.Add,
        ["-"] = BinaryOp.Subtract, ["*"] = BinaryOp.Multiply, ["/"] = BinaryOp.Divide, ["^"] = BinaryOp.Power,
        [":"] = BinaryOp.Range, [" "] = BinaryOp.Intersect, [","] = BinaryOp.Union
    };

    private static readonly Dictionary<BinaryOp, string> Symbols = Operators.ToDictionary(p => p.Value, p => p.Key);

    /// <summary>Parses a formula, with or without leading equals sign.</summary>
    /// <param name="formula">The formula text.</param>
    /// <returns>The expression tree.</returns>
    /// <exception cref="FormatException">The formula is not valid.</exception>
    public static FormulaNode Parse(string formula) =>
        TryParse(formula, out var node, out var error) ? node : throw new FormatException(error);

    /// <summary>Tries to parse a formula, with or without leading equals sign.</summary>
    /// <param name="formula">The formula text.</param>
    /// <param name="node">The expression tree when successful.</param>
    /// <param name="error">A description of the problem when unsuccessful.</param>
    /// <returns>True if the formula is valid.</returns>
    public static bool TryParse(string formula, out FormulaNode node, out string error)
    {
        node = null;
        error = null;
        try
        {
            if (formula?.Length > MaxLength) throw new FormatException($"Formulas are limited to {MaxLength} characters.");
            var parser = new Parser(FormulaLexer.Tokenize(formula));
            node = parser.ParseFormula();
            return true;
        }
        catch (FormatException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>Formats an expression tree in Excel form without leading equals sign.</summary>
    /// <param name="node">The expression tree.</param>
    /// <returns>The formula text, for example <c>SUM(A1:B2,1)</c>.</returns>
    public static string Format(FormulaNode node)
    {
        var sb = new StringBuilder();
        Write(sb, node);
        return sb.ToString();
    }

    private static void Write(StringBuilder sb, FormulaNode node)
    {
        switch (node)
        {
            case NumberNode n:
                sb.Append(n.Value.ToString("R", CultureInfo.InvariantCulture));
                break;
            case TextNode t:
                sb.Append('"').Append(t.Value.Replace("\"", "\"\"")).Append('"');
                break;
            case BoolNode b:
                sb.Append(b.Value ? "TRUE" : "FALSE");
                break;
            case ErrorNode e:
                sb.Append(CellValue.Error(e.Error));
                break;
            case RefNode r:
                WriteSheet(sb, r.Sheet).Append(r.Cell.ToA1());
                if (r.Spill) sb.Append('#');
                break;
            case RangeNode r:
                WriteSheet(sb, r.Sheet).Append(r.Range.ToA1());
                break;
            case NameNode n:
                sb.Append(n.Name);
                break;
            case StructuredRefNode s:
                sb.Append(StructuredReferences.Format(s, false));
                break;
            case ParenNode p:
                sb.Append('(');
                Write(sb, p.Inner);
                sb.Append(')');
                break;
            case UnaryNode u:
                sb.Append(u.Operator);
                Write(sb, u.Operand);
                break;
            case PercentNode p:
                Write(sb, p.Operand);
                sb.Append('%');
                break;
            case BinaryNode b:
                var spine = new Stack<BinaryNode>();
                FormulaNode bottom = b;
                while (bottom is BinaryNode inner)
                {
                    spine.Push(inner);
                    bottom = inner.Left;
                }

                Write(sb, bottom);
                while (spine.Count > 0)
                {
                    var x = spine.Pop();
                    sb.Append(Symbols[x.Op]);
                    Write(sb, x.Right);
                }

                break;
            case CallNode c:
                sb.Append(c.Name).Append('(');
                for (var i = 0; i < c.Args.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    Write(sb, c.Args[i]);
                }

                sb.Append(')');
                break;
            case ArrayConstNode a:
                sb.Append('{');
                for (var r = 0; r < a.Rows.Count; r++)
                {
                    if (r > 0) sb.Append(';');
                    for (var c = 0; c < a.Rows[r].Count; c++)
                    {
                        if (c > 0) sb.Append(',');
                        Write(sb, a.Rows[r][c]);
                    }
                }

                sb.Append('}');
                break;
        }
    }

    internal static FormulaNode ParseReference(string text)
    {
        var bang = text.LastIndexOf('!');
        var sheet = bang < 0 ? null : UnquoteSheet(text[..bang]);
        var spill = text.EndsWith('#');
        var range = CellRange.Parse(text[(bang + 1)..(spill ? ^1 : ^0)]);
        return text.IndexOf(':', bang + 1) < 0 ? new RefNode(range.Start, sheet, spill) : new RangeNode(range, sheet);
    }

    internal static string UnquoteSheet(string sheet) => sheet is ['\'', .., '\''] ? sheet[1..^1].Replace("''", "'") : sheet;

    private static StringBuilder WriteSheet(StringBuilder sb, string sheet) =>
        sheet == null ? sb : sb.Append(Workbook.QuoteSheetName(sheet)).Append('!');

    private sealed class Parser(IReadOnlyList<FormulaToken> tokens)
    {
        private int _pos;
        private int _depth;

        public FormulaNode ParseFormula()
        {
            if (tokens.Count == 0) throw new FormatException("The formula is empty.");
            var node = ParseExpression();
            if (_pos < tokens.Count) throw Unexpected();
            return node;
        }

        private bool Is(FormulaTokenKind kind, string text = null) =>
            _pos < tokens.Count && tokens[_pos].Kind == kind && (text == null || tokens[_pos].Text == text);

        private FormatException Unexpected() => _pos < tokens.Count
            ? new FormatException($"Unexpected '{tokens[_pos].Text}' at position {tokens[_pos].Start + 1}.")
            : new FormatException("Unexpected end of formula.");

        private void Expect(FormulaTokenKind kind)
        {
            if (!Is(kind)) throw Unexpected();
            _pos++;
        }

        private FormulaNode ParseExpression()
        {
            if (++_depth > MaxDepth) throw new FormatException($"Formulas may nest at most {MaxDepth} levels.");
            var node = ParseLevel(0);
            _depth--;
            return node;
        }

        private FormulaNode ParseLevel(int level)
        {
            if (level == Levels.Length) return ParsePercent();
            var left = ParseLevel(level + 1);
            while (_pos < tokens.Count && tokens[_pos].Kind == FormulaTokenKind.Operator && Levels[level].Contains(tokens[_pos].Text))
            {
                var op = Operators[tokens[_pos++].Text];
                left = new BinaryNode(op, left, ParseLevel(level + 1));
            }

            return left;
        }

        private FormulaNode ParsePercent()
        {
            var node = ParseUnary();
            for (var count = 0; Is(FormulaTokenKind.Operator, "%"); count++)
            {
                if (count == MaxDepth) throw new FormatException("Too many percent signs.");
                _pos++;
                node = new PercentNode(node);
            }

            return node;
        }

        private FormulaNode ParseUnary()
        {
            var signs = new List<char>();
            while (Is(FormulaTokenKind.Operator, "-") || Is(FormulaTokenKind.Operator, "+"))
            {
                if (signs.Count == MaxDepth) throw new FormatException("Too many signs.");
                signs.Add(tokens[_pos++].Text[0]);
            }

            var node = ParseIntersect();
            for (var i = signs.Count - 1; i >= 0; i--) node = new UnaryNode(signs[i], node);
            return node;
        }

        private FormulaNode ParseIntersect()
        {
            var left = ParseRange();
            while (_pos < tokens.Count && tokens[_pos].SpaceBefore && tokens[_pos].Kind is FormulaTokenKind.Reference
                       or FormulaTokenKind.StructuredReference or FormulaTokenKind.Name or FormulaTokenKind.Function or FormulaTokenKind.OpenParen)
                left = new BinaryNode(BinaryOp.Intersect, left, ParseRange());
            return left;
        }

        private FormulaNode ParseRange()
        {
            var left = ParsePrimary();
            while (Is(FormulaTokenKind.Operator, ":"))
            {
                _pos++;
                left = new BinaryNode(BinaryOp.Range, left, ParsePrimary());
            }

            return left;
        }

        private FormulaNode ParsePrimary()
        {
            if (_pos >= tokens.Count) throw Unexpected();
            var t = tokens[_pos];
            switch (t.Kind)
            {
                case FormulaTokenKind.Function:
                    return ParseCall();
                case FormulaTokenKind.OpenParen:
                    _pos++;
                    var inner = ParseExpression();
                    while (Is(FormulaTokenKind.Comma))
                    {
                        _pos++;
                        inner = new BinaryNode(BinaryOp.Union, inner, ParseExpression());
                    }

                    Expect(FormulaTokenKind.CloseParen);
                    return new ParenNode(inner);
                case FormulaTokenKind.OpenBrace:
                    return ParseArray();
                case FormulaTokenKind.Reference:
                    _pos++;
                    return ParseReference(t.Text);
                case FormulaTokenKind.Name:
                    _pos++;
                    return new NameNode(t.Text);
                case FormulaTokenKind.StructuredReference:
                    _pos++;
                    return StructuredReferences.Parse(t.Text);
                default:
                    return ParseLiteral();
            }
        }

        private FormulaNode ParseLiteral()
        {
            if (_pos >= tokens.Count) throw Unexpected();
            var t = tokens[_pos];
            FormulaNode node = t.Kind switch
            {
                FormulaTokenKind.Number => new NumberNode(double.Parse(t.Text, NumberStyles.Float, CultureInfo.InvariantCulture)),
                FormulaTokenKind.Text => new TextNode(t.Text[1..^1].Replace("\"\"", "\"")),
                FormulaTokenKind.Boolean => new BoolNode(t.Text.Equals("TRUE", StringComparison.OrdinalIgnoreCase)),
                FormulaTokenKind.Error => new ErrorNode(FormulaLexer.ErrorCodes
                    .First(e => t.Text.EndsWith(e.Code, StringComparison.OrdinalIgnoreCase)).Error),
                _ => throw Unexpected()
            };
            if (node is NumberNode { Value: var v } && !double.IsFinite(v)) throw new FormatException($"Number '{t.Text}' is too large.");
            _pos++;
            return node;
        }

        private FormulaNode ParseCall()
        {
            var name = tokens[_pos].Text;
            _pos += 2;
            var args = new List<FormulaNode>();
            if (Is(FormulaTokenKind.CloseParen))
            {
                _pos++;
            }
            else
            {
                while (true)
                {
                    args.Add(Is(FormulaTokenKind.Comma) || Is(FormulaTokenKind.Semicolon) || Is(FormulaTokenKind.CloseParen)
                        ? new MissingArgNode()
                        : ParseExpression());
                    if (Is(FormulaTokenKind.Comma) || Is(FormulaTokenKind.Semicolon))
                    {
                        _pos++;
                        continue;
                    }

                    Expect(FormulaTokenKind.CloseParen);
                    break;
                }
            }

            return new CallNode(name.StartsWith('_') ? name : name.ToUpperInvariant(), args);
        }

        private FormulaNode ParseArray()
        {
            _pos++;
            var rows = new List<IReadOnlyList<FormulaNode>>();
            var row = new List<FormulaNode>();
            while (true)
            {
                var negate = Is(FormulaTokenKind.Operator, "-");
                if (negate) _pos++;
                var item = ParseLiteral();
                if (negate)
                    item = item is NumberNode n ? new NumberNode(-n.Value) : throw new FormatException("Only numbers can be negated in an array constant.");
                row.Add(item);
                if (Is(FormulaTokenKind.Comma))
                {
                    _pos++;
                    continue;
                }

                rows.Add(row);
                if (Is(FormulaTokenKind.Semicolon))
                {
                    _pos++;
                    row = new List<FormulaNode>();
                    continue;
                }

                Expect(FormulaTokenKind.CloseBrace);
                break;
            }

            if (rows.Any(r => r.Count != rows[0].Count)) throw new FormatException("All rows of an array constant need the same length.");
            return new ArrayConstNode(rows);
        }
    }
}
