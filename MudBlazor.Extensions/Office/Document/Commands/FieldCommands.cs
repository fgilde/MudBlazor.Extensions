using System.Xml.Linq;

namespace MudBlazor.Extensions.Office.Document;

/// <summary>Replaces a range with a field such as <c>PAGE</c> or <c>TOC</c>. The field is kept as one token and written to .docx as a simple field; Word updates it when the document is opened or the field is updated.</summary>
public sealed class InsertFieldCommand : TextDocumentCommand
{
    private readonly DocumentRange _at;
    private readonly string _instruction;
    private readonly string _display;

    /// <summary>Creates the command.</summary>
    /// <param name="at">The range to replace, in either direction; an empty range inserts at a position.</param>
    /// <param name="instruction">The field instruction, for example <c>PAGE</c>.</param>
    /// <param name="display">The text shown until the field is updated.</param>
    /// <exception cref="ArgumentException">The instruction is empty.</exception>
    public InsertFieldCommand(DocumentRange at, string instruction, string display)
    {
        if (string.IsNullOrWhiteSpace(instruction)) throw new ArgumentException("The field instruction is empty.", nameof(instruction));
        (_at, _instruction, _display) = (at, instruction.Trim(), display ?? "");
    }

    /// <inheritdoc />
    public override string Description => "Insert field";

    private protected override (IReadOnlyList<BlockEdit>, Func<TextDocument, DocumentRange>) Compute(TextDocument document)
    {
        var (paragraph, offset) = document.Resolve(document.Snap(_at).Start);
        var format = InlineEdit.InsertionStyle(paragraph, offset).Format;
        var node = new XElement(FormatXml.W + "fldSimple", new XAttribute(FormatXml.W + "instr", _instruction), new XElement(FormatXml.W + "r", new XElement(FormatXml.W + "t", _display)));
        return document.ReplaceRange(_at, [[new OpaqueInline("field", _display) { Format = format, Instruction = _instruction, Nodes = [node] }]]);
    }
}
