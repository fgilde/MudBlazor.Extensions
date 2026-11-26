namespace MudBlazor.Extensions.Office.Document;

public sealed partial class TextDocument
{
    internal TableCellRef? TableCellOf(Paragraph paragraph) => CellOf(paragraph);
}
