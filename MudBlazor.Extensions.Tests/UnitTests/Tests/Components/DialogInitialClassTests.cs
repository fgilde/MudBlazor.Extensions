using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

/// <summary>
/// A dialog opened with ShowEx starts hidden with MudExCss.Classes.Dialog.Initial: ShowEx removes the class right before the open
/// animation, so the animation starts cleanly instead of the dialog flashing in place. A dialog opened with plain MudBlazor ShowAsync
/// must not carry it (nobody would remove it and the dialog stayed hidden).
/// </summary>
public class DialogInitialClassTests
{
    private static string Root([System.Runtime.CompilerServices.CallerFilePath] string file = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, "../../../.."));

    [Fact]
    public void Every_dialog_opened_with_ShowEx_starts_hidden_with_the_initial_class()
    {
        var root = Root();
        var sources = Directory.EnumerateFiles(Path.Combine(root, "MudBlazor.Extensions"), "*.*", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Path.Combine(root, "Samples"), "*.*", SearchOption.AllDirectories))
            .Where(f => (f.EndsWith(".cs") || f.EndsWith(".razor")) && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Select(f => File.ReadAllText(f))
            .ToArray();
        var openedWithShowEx = sources
            .SelectMany(s => Regex.Matches(s, @"(?:ShowEx\w*|ShowCommandDialogAsync)<(\w+)>|ShowEx\w*\(typeof\((\w+)\)").Select(m => m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value))
            .ToHashSet();

        var dialogs = Directory.EnumerateFiles(root, "*.razor", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(f => Regex.IsMatch(File.ReadAllText(f), @"<MudDialog[\s>]"))
            .ToArray();
        dialogs.Should().NotBeEmpty();

        var missing = dialogs
            .Where(f => openedWithShowEx.Contains(Path.GetFileNameWithoutExtension(f)))
            .Where(f => !HasInitialClass(f))
            .Select(Path.GetFileNameWithoutExtension)
            .ToArray();

        missing.Should().BeEmpty("dialogs opened with ShowEx start hidden with MudExCss.Classes.Dialog.Initial");
        openedWithShowEx.Should().Contain(["MudExSpreadsheetGoToDialog", "MudExSpreadsheetTableDialog", "MudExSlideFindReplaceDialog", "MudExNotebookLinkDialog"]);
    }

    [Fact]
    public void Library_components_open_their_dialogs_with_ShowEx()
    {
        var library = Path.Combine(Root(), "MudBlazor.Extensions");
        var plain = Directory.EnumerateFiles(library, "*.*", SearchOption.AllDirectories)
            .Where(f => (f.EndsWith(".cs") || f.EndsWith(".razor")) && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(f => Regex.IsMatch(File.ReadAllText(f), @"DialogService\.ShowAsync<"))
            .Select(Path.GetFileName)
            .ToArray();

        plain.Should().BeEmpty("a MudEx component opens its dialogs with ShowExAsync, so they get the MudEx options and the initial class is removed");
    }

    // in the markup, or set as default in the code behind (MudExPromptDialog: Class ??= ...Dialog.Initial)
    private static bool HasInitialClass(string razor)
    {
        var markup = File.ReadAllText(razor);
        var codeBehind = File.Exists(razor + ".cs") ? File.ReadAllText(razor + ".cs") : string.Empty;
        return Regex.IsMatch(markup + codeBehind, @"Dialog\._?Initial\b|mud-ex-dialog-initial");
    }
}
