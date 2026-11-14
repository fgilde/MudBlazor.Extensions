using System.Text.Json;
using System.Text.Json.Nodes;

namespace MudBlazor.Extensions.Office.Notebook;

/// <summary>
/// An element this version cannot read, for example of a newer element type or with an unknown value. It can be moved, reordered, duplicated and
/// deleted; its JSON is saved unchanged apart from id and position.
/// </summary>
public sealed class OpaqueElement : NoteElement
{
    internal OpaqueElement()
    {
    }

    /// <summary>The element type named in the file.</summary>
    public string Type { get; internal set; } = "";

    internal JsonElement Raw { get; set; }

    internal static OpaqueElement From(JsonElement json)
    {
        if (!json.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.String || id.GetString() is not { } value || !NoteValues.IsId(value))
            throw new JsonException("An element has no valid id.");
        return new OpaqueElement
        {
            Id = value, Type = json.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.String ? type.GetString() : "",
            X = Number(json, "x"), Y = Number(json, "y"), Width = Math.Max(0, Number(json, "width")), Height = Math.Max(0, Number(json, "height")), Raw = json.Clone()
        };
    }

    internal JsonElement ToJson()
    {
        if (Raw.GetProperty("id").GetString() == Id && Number(Raw, "x") == X && Number(Raw, "y") == Y) return Raw;
        var node = JsonNode.Parse(Raw.GetRawText())!.AsObject();
        node["id"] = Id;
        node["x"] = X;
        node["y"] = Y;
        using var document = JsonDocument.Parse(node.ToJsonString());
        return document.RootElement.Clone();
    }

    private static double Number(JsonElement json, string name) =>
        json.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number) && double.IsFinite(number) ? number : 0;
}
