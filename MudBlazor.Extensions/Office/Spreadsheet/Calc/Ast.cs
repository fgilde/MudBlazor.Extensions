namespace MudBlazor.Extensions.Office.Spreadsheet;

/// <summary>Base type of a parsed formula expression.</summary>
public abstract record FormulaNode;

/// <summary>A number literal.</summary>
/// <param name="Value">The number.</param>
public sealed record NumberNode(double Value) : FormulaNode;

/// <summary>A text literal.</summary>
/// <param name="Value">The unescaped text.</param>
public sealed record TextNode(string Value) : FormulaNode;

/// <summary>A boolean literal.</summary>
/// <param name="Value">The boolean.</param>
public sealed record BoolNode(bool Value) : FormulaNode;

/// <summary>An error literal such as <c>#REF!</c>.</summary>
/// <param name="Error">The error.</param>
public sealed record ErrorNode(CellError Error) : FormulaNode;

/// <summary>A single cell reference, or with <paramref name="Spill"/> the spill range of a formula cell (<c>A1#</c>).</summary>
/// <param name="Cell">The cell, including absolute markers.</param>
/// <param name="Sheet">The sheet name, or null for the sheet the formula lives on.</param>
/// <param name="Spill">True for <c>A1#</c>: the whole range the array result of the formula in the cell spills into.</param>
public sealed record RefNode(CellRef Cell, string Sheet, bool Spill = false) : FormulaNode;

/// <summary>A rectangular range reference, including whole columns (<c>A:A</c>) and rows (<c>1:1</c>).</summary>
/// <param name="Range">The range, including absolute markers.</param>
/// <param name="Sheet">The sheet name, or null for the sheet the formula lives on.</param>
public sealed record RangeNode(CellRange Range, string Sheet) : FormulaNode;

/// <summary>The rows of a table a structured reference selects; <see cref="Headers"/>, <see cref="Data"/> and <see cref="Totals"/> combine.</summary>
[Flags]
public enum StructuredItem
{
    /// <summary><c>#Data</c>: the data rows (the default when only columns are given).</summary>
    Data = 1,

    /// <summary><c>#Headers</c>: the header row.</summary>
    Headers = 2,

    /// <summary><c>#Totals</c>: the totals row.</summary>
    Totals = 4,

    /// <summary><c>#All</c>: header, data and totals rows.</summary>
    All = Data | Headers | Totals,

    /// <summary><c>@</c> or <c>#This Row</c>: the data row of the formula cell.</summary>
    ThisRow = 8
}

/// <summary>A structured reference to a table, such as <c>Sales[Amount]</c>, <c>[@Qty]</c> or <c>Sales[[#Headers],[A]:[C]]</c>.</summary>
/// <param name="Table">The table name, or null for the table containing the formula cell.</param>
/// <param name="Column1">The first (or only) column, or null for all columns.</param>
/// <param name="Column2">The last column of a column range, or null.</param>
/// <param name="Items">The selected rows.</param>
public sealed record StructuredRefNode(string Table, string Column1, string Column2, StructuredItem Items) : FormulaNode;

/// <summary>A defined name.</summary>
/// <param name="Name">The name as written.</param>
public sealed record NameNode(string Name) : FormulaNode;

/// <summary>A prefix sign.</summary>
/// <param name="Operator"><c>-</c> or <c>+</c>.</param>
/// <param name="Operand">The operand.</param>
public sealed record UnaryNode(char Operator, FormulaNode Operand) : FormulaNode;

/// <summary>A postfix percent sign (divides by 100).</summary>
/// <param name="Operand">The operand.</param>
public sealed record PercentNode(FormulaNode Operand) : FormulaNode;

/// <summary>Binary operators, from reference operators to comparisons.</summary>
public enum BinaryOp
{
    /// <summary><c>:</c> bounding range of two references.</summary>
    Range,

    /// <summary>Space: intersection of two references.</summary>
    Intersect,

    /// <summary><c>,</c> inside parentheses: union of two references.</summary>
    Union,

    /// <summary><c>^</c></summary>
    Power,

    /// <summary><c>*</c></summary>
    Multiply,

    /// <summary><c>/</c></summary>
    Divide,

    /// <summary><c>+</c></summary>
    Add,

    /// <summary><c>-</c></summary>
    Subtract,

    /// <summary><c>&amp;</c></summary>
    Concat,

    /// <summary><c>=</c></summary>
    Equal,

    /// <summary><c>&lt;&gt;</c></summary>
    NotEqual,

    /// <summary><c>&lt;</c></summary>
    Less,

    /// <summary><c>&lt;=</c></summary>
    LessOrEqual,

    /// <summary><c>&gt;</c></summary>
    Greater,

    /// <summary><c>&gt;=</c></summary>
    GreaterOrEqual
}

/// <summary>A binary operation.</summary>
/// <param name="Op">The operator.</param>
/// <param name="Left">Left operand.</param>
/// <param name="Right">Right operand.</param>
public sealed record BinaryNode(BinaryOp Op, FormulaNode Left, FormulaNode Right) : FormulaNode;

/// <summary>A function call.</summary>
/// <param name="Name">Function name (upper case unless it carries a prefix such as <c>_xlfn.</c>).</param>
/// <param name="Args">The arguments; empty positions are <see cref="MissingArgNode"/>.</param>
public sealed record CallNode(string Name, IReadOnlyList<FormulaNode> Args) : FormulaNode;

/// <summary>An array constant such as <c>{1,2;3,4}</c>.</summary>
/// <param name="Rows">Rows of literal nodes, all of equal length.</param>
public sealed record ArrayConstNode(IReadOnlyList<IReadOnlyList<FormulaNode>> Rows) : FormulaNode;

/// <summary>Parentheses kept from the source so formatting round-trips.</summary>
/// <param name="Inner">The enclosed expression.</param>
public sealed record ParenNode(FormulaNode Inner) : FormulaNode;

/// <summary>An empty argument position, as in <c>IF(A1,,1)</c>.</summary>
public sealed record MissingArgNode : FormulaNode;

internal static class FormulaNodeExtensions
{
    internal static FormulaNode Rewrite(this FormulaNode node, Func<FormulaNode, FormulaNode> leaf)
    {
        switch (node)
        {
            case ParenNode p:
                var inner = p.Inner.Rewrite(leaf);
                return ReferenceEquals(inner, p.Inner) ? p : new ParenNode(inner);
            case UnaryNode u:
                var operand = u.Operand.Rewrite(leaf);
                return ReferenceEquals(operand, u.Operand) ? u : u with { Operand = operand };
            case PercentNode pc:
                var pOperand = pc.Operand.Rewrite(leaf);
                return ReferenceEquals(pOperand, pc.Operand) ? pc : new PercentNode(pOperand);
            case BinaryNode b:
                var spine = new Stack<BinaryNode>();
                FormulaNode bottom = b;
                while (bottom is BinaryNode next)
                {
                    spine.Push(next);
                    bottom = next.Left;
                }

                var result = bottom.Rewrite(leaf);
                while (spine.Count > 0)
                {
                    var x = spine.Pop();
                    var right = x.Right.Rewrite(leaf);
                    result = ReferenceEquals(result, x.Left) && ReferenceEquals(right, x.Right) ? x : x with { Left = result, Right = right };
                }

                return result;
            case CallNode c:
                FormulaNode[] args = null;
                for (var i = 0; i < c.Args.Count; i++)
                {
                    var arg = c.Args[i].Rewrite(leaf);
                    if (ReferenceEquals(arg, c.Args[i])) continue;
                    args ??= c.Args.ToArray();
                    args[i] = arg;
                }

                return args == null ? c : c with { Args = args };
            case ArrayConstNode:
                return node;
            default:
                return leaf(node);
        }
    }
}
