namespace MudBlazor.Extensions.Components;

internal static class DocumentShortcuts
{
    public static readonly IReadOnlyDictionary<string, string> Map = new Dictionary<string, string>
    {
        ["Ctrl+B"] = "bold",
        ["Ctrl+I"] = "italic",
        ["Ctrl+U"] = "underline",
        ["Ctrl+Shift+D"] = "doubleUnderline",
        ["Ctrl+="] = "subscript",
        ["Ctrl+Shift+="] = "superscript",
        ["Ctrl+Space"] = "clearCharacter",
        ["Ctrl+]"] = "sizeUp",
        ["Ctrl+["] = "sizeDown",
        ["Ctrl+Shift+."] = "grow",
        ["Ctrl+Shift+,"] = "shrink",
        ["Ctrl+L"] = "alignLeft",
        ["Ctrl+E"] = "alignCenter",
        ["Ctrl+R"] = "alignRight",
        ["Ctrl+J"] = "alignJustify",
        ["Ctrl+M"] = "indent",
        ["Ctrl+Shift+M"] = "outdent",
        ["Ctrl+Shift+L"] = "bullets",
        ["Ctrl+1"] = "lineSpacing1",
        ["Ctrl+5"] = "lineSpacing15",
        ["Ctrl+2"] = "lineSpacing2",
        ["Ctrl+0"] = "spaceBefore",
        ["Ctrl+Alt+1"] = "heading1",
        ["Ctrl+Alt+2"] = "heading2",
        ["Ctrl+Alt+3"] = "heading3",
        ["Ctrl+Shift+N"] = "normal",
        ["Ctrl+D"] = "fontDialog",
        ["Ctrl+S"] = "save",
        ["Ctrl+F"] = "find",
        ["Ctrl+H"] = "replace",
        ["Ctrl+K"] = "link"
    };

    public static string ActionOf(string key) => key == null ? null : Map.GetValueOrDefault(key);

    public static string[] Keys(params string[] without) => [.. Map.Keys.Where(k => !without.Contains(ActionOf(k)))];
}
