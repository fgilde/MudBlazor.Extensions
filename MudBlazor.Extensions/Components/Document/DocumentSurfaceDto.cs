using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MudBlazor.Extensions.Components;

/// <summary>A block of the document editor surface as sent to the browser. Infrastructure of the document editor, not meant to be used directly.</summary>
/// <param name="Id">The block id.</param>
/// <param name="K">The kind: <c>p</c> paragraph, <c>t</c> table, <c>o</c> visible opaque block (read-only preview in <paramref name="Blocks"/>), <c>h</c> hidden block.</param>
/// <param name="Css">The inline style of the element.</param>
/// <param name="Text">The model text of a paragraph (<see cref="Office.Document.Paragraph.Text"/>); null for other kinds.</param>
/// <param name="Label">The list label of a paragraph, shown as text; null if none.</param>
/// <param name="LabelCss">The inline style of the list label; null if none.</param>
/// <param name="Runs">The runs of a paragraph; null for other kinds.</param>
/// <param name="Grid">The widths of the grid columns of a table in pixels; null for other kinds.</param>
/// <param name="Rows">The rows of a table; null for other kinds.</param>
/// <param name="Blocks">The preview of an opaque block; null for other kinds.</param>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record SurfaceBlock(string Id, string K, string Css, string Text, string Label, string LabelCss, IReadOnlyList<SurfaceRun> Runs, IReadOnlyList<int> Grid,
    IReadOnlyList<SurfaceRow> Rows, IReadOnlyList<SurfaceBlock> Blocks);

/// <summary>A run of a surface paragraph. Infrastructure of the document editor, not meant to be used directly.</summary>
/// <param name="K">The kind: <c>t</c> text (tabs inside), <c>v</c> line break, <c>f</c> page or column break, <c>o</c> token, <c>i</c> image.</param>
/// <param name="T">The text of a text run or the display text of a token; null for other kinds.</param>
/// <param name="Css">The inline style of the run.</param>
/// <param name="Media">The media part of an image; null for other kinds.</param>
/// <param name="W">The width of an image in pixels; 0 for other kinds.</param>
/// <param name="H">The height of an image in pixels; 0 for other kinds.</param>
/// <param name="Link">The validated target (<c>http</c>, <c>https</c> or <c>mailto</c>) of a link run; null if the run is not a link or the target is not safe to open.</param>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record SurfaceRun(string K, string T, string Css, string Media, int W, int H, string Link = null);

/// <summary>A row of a surface table. Infrastructure of the document editor, not meant to be used directly.</summary>
/// <param name="Css">The inline style of the row.</param>
/// <param name="Cells">The visible cells; cells continuing a vertical merge are left out.</param>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record SurfaceRow(string Css, IReadOnlyList<SurfaceCell> Cells);

/// <summary>A cell of a surface table row. Infrastructure of the document editor, not meant to be used directly.</summary>
/// <param name="ColSpan">The number of grid columns the cell spans.</param>
/// <param name="RowSpan">The number of rows the cell spans.</param>
/// <param name="Css">The inline style of the cell.</param>
/// <param name="Blocks">The content.</param>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record SurfaceCell(int ColSpan, int RowSpan, string Css, IReadOnlyList<SurfaceBlock> Blocks);

/// <summary>Inserts or replaces a block of the surface. Infrastructure of the document editor, not meant to be used directly.</summary>
/// <param name="Block">The rendered block.</param>
/// <param name="After">The id of the top-level block the block goes after, or null for the first position.</param>
/// <param name="InPlace">True to replace the element with the same id wherever it is; false to place the block after <paramref name="After"/>.</param>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record SurfaceUpsert(SurfaceBlock Block, string After, bool InPlace);

/// <summary>A change of the surface. Infrastructure of the document editor, not meant to be used directly.</summary>
/// <param name="Seq">The sequence number; patches are applied in this order.</param>
/// <param name="Reset">True to clear the surface before applying the patch.</param>
/// <param name="SheetCss">The new inline style of the sheet, or null to keep it.</param>
/// <param name="Remove">The ids of the elements to remove.</param>
/// <param name="Upsert">The blocks to insert or replace, in order.</param>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record SurfacePatch(int Seq, bool Reset, string SheetCss, IReadOnlyList<string> Remove, IReadOnlyList<SurfaceUpsert> Upsert);

/// <summary>A caret position on the surface. Infrastructure of the document editor, not meant to be used directly.</summary>
/// <param name="P">The paragraph id.</param>
/// <param name="O">The offset in the model text of the paragraph.</param>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record SurfacePoint(string P, int O);

/// <summary>A selection on the surface. Infrastructure of the document editor, not meant to be used directly.</summary>
/// <param name="Anchor">Where the selection starts.</param>
/// <param name="Focus">Where the caret is.</param>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record SurfaceSelection(SurfacePoint Anchor, SurfacePoint Focus);

/// <summary>A paragraph typed in the browser. Infrastructure of the document editor, not meant to be used directly.</summary>
/// <param name="P">The paragraph id.</param>
/// <param name="T">The full new model text of the paragraph.</param>
/// <param name="C">The caret offset in <paramref name="T"/>, or null if the caret is not in this paragraph.</param>
/// <param name="F">The caret format token of the typed text, or null.</param>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record SurfaceTyped(string P, string T, int? C, string F);

/// <summary>The state the browser reports with every message. Infrastructure of the document editor, not meant to be used directly.</summary>
/// <param name="Seen">The sequence number of the last patch the browser applied.</param>
/// <param name="Typed">The paragraphs typed since the last message.</param>
/// <param name="Selection">The selection, or null if it is not in the document.</param>
/// <param name="EndTyping">True to start a new undo step with the next typing.</param>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record SurfaceSync(
    int Seen, [property: JsonConverter(typeof(SurfaceTypedListConverter))] IReadOnlyList<SurfaceTyped> Typed, SurfaceSelection Selection, bool EndTyping);

/// <summary>A pending format for the next typed text. Infrastructure of the document editor, not meant to be used directly.</summary>
/// <param name="Token">The token to send with the typed text.</param>
/// <param name="Css">The inline style of the typed text.</param>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record SurfaceCaretFormat(string Token, string Css);

/// <summary>The answer to a message of the browser. Infrastructure of the document editor, not meant to be used directly.</summary>
/// <param name="Patches">The patches created while the message was handled.</param>
/// <param name="Selection">The selection to set, or null to keep the selection.</param>
/// <param name="CaretFormat">The pending format for the next typed text, or null if there is none.</param>
/// <param name="Retry">True if the message could not be read; the browser keeps its typing and sends it again with the next message.</param>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed record SurfaceResult(IReadOnlyList<SurfacePatch> Patches, SurfaceSelection Selection, SurfaceCaretFormat CaretFormat, bool Retry = false);

internal sealed class SurfaceTypedListConverter : JsonConverter<IReadOnlyList<SurfaceTyped>>
{
    internal const int MaxTyped = 10_000;

    public override IReadOnlyList<SurfaceTyped> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException();
        var list = new List<SurfaceTyped>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (list.Count > MaxTyped)
            {
                reader.Skip();
                continue;
            }

            list.Add(JsonSerializer.Deserialize<SurfaceTyped>(ref reader, options));
        }

        return list;
    }

    public override void Write(Utf8JsonWriter writer, IReadOnlyList<SurfaceTyped> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var item in value) JsonSerializer.Serialize(writer, item, options);
        writer.WriteEndArray();
    }
}
