using System.Text;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Document;

using MudBlazor.Extensions.Office.Document;

internal static class DocumentDescriber
{
    public static string Describe(TextDocument document)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"doc template={document.IsTemplate} tracked={document.HasTrackedChanges} evenOdd={document.EvenAndOddHeaders} {document.Properties}");
        Blocks(sb, document.Blocks, "");
        foreach (var section in document.Sections)
            sb.AppendLine($"section {section.Page} last={section.LastBlockId != null} {section.Start} title={section.TitlePage} cols={section.Columns} " +
                          $"headers={Refs(section.Headers)} footers={Refs(section.Footers)}");
        foreach (var part in document.HeadersFooters)
        {
            sb.AppendLine($"{part.Kind} {part.Id}");
            Blocks(sb, part.Blocks, "  ");
        }

        foreach (var comment in document.Comments) sb.AppendLine($"comment {comment with { Id = null, ParagraphId = null }}");
        return sb.ToString();
    }

    private static void Blocks(StringBuilder sb, IEnumerable<Block> blocks, string indent)
    {
        foreach (var block in blocks)
            switch (block)
            {
                case Paragraph p:
                    sb.AppendLine($"{indent}p {p.Format with { Tabs = null }} tabs={string.Join(",", p.Format.Tabs ?? [])} mark={p.MarkFormat}");
                    foreach (var inline in p.Inlines) sb.AppendLine($"{indent}  {Inline(inline)} {inline.Format} link={inline.Link}");
                    break;
                case Table t:
                    sb.AppendLine($"{indent}table {t.Format} grid={string.Join(",", t.Grid)}");
                    foreach (var row in t.Rows)
                    {
                        sb.AppendLine($"{indent}  tr {row.Height} header={row.IsHeader}");
                        foreach (var cell in row.Cells)
                        {
                            sb.AppendLine($"{indent}    tc {cell.GridSpan} {cell.VMerge} {cell.Width} {cell.Shading} {cell.Borders} {cell.VerticalAlign}");
                            Blocks(sb, cell.Blocks, indent + "      ");
                        }
                    }

                    break;
                case OpaqueBlock o:
                    sb.AppendLine($"{indent}opaque {o.Kind} visible={o.Visible}");
                    if (o.Preview != null) Blocks(sb, o.Preview, indent + "  ");
                    break;
            }
    }

    private static string Inline(Inline inline) => inline switch
    {
        TextRun r => $"r '{r.Text}'",
        InlineImage i => $"img {i.MediaPart} {i.WidthEmu}x{i.HeightEmu} {i.Placement} alt={i.AltText}",
        OpaqueInline o => $"opaque {o.Kind} '{o.DisplayText}' {o.Instruction}",
        Marker m => $"marker {m.Kind} {m.Name}",
        Break b => $"break {b.Kind}",
        _ => inline.GetType().Name
    };

    private static string Refs(IReadOnlyDictionary<HeaderFooterType, string> refs) => string.Join(",", refs.OrderBy(r => r.Key).Select(r => $"{r.Key}:{r.Value}"));
}
