using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

public sealed class MudExTagFieldTests : IAsyncDisposable
{
    private static readonly string[] Standard = ["openid", "profile", "email", "offline_access"];
    private static readonly char[] Separators = [' ', ',', ';'];

    private readonly TestContext _context = new();
    private List<string> _values = [];
    private int _changes;
    private IRenderedComponent<MudPopoverProvider> _popovers;

    public MudExTagFieldTests()
    {
        _context.Services.AddMudServicesWithExtensions();
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public ValueTask DisposeAsync() => _context.DisposeAsync();

    private static Task<IEnumerable<string>> Offer(string text, CancellationToken _) =>
        Task.FromResult(Standard.Where(s => s.Contains(text)).AsEnumerable());

    private IRenderedComponent<MudExTagField<string>> Open(
        List<string> values = null,
        Func<string, string> validate = null,
        Func<string, CancellationToken, Task<IEnumerable<string>>> search = null,
        Func<string, bool> closable = null,
        bool commitOnBlur = true,
        bool allowDuplicates = false,
        int maxSuggestions = 0,
        bool autoClear = true,
        bool readOnly = false,
        bool closeable = true,
        bool disabled = false)
    {
        _values = values ?? [];
        _popovers ??= _context.Render<MudPopoverProvider>();
        return _context.Render<MudExTagField<string>>(p => p
            .Add(c => c.Values, _values)
            .Add(c => c.ValuesChanged, v => { _values = v; _changes++; })
            .Add(c => c.Delimiters, Separators)
            .Add(c => c.ValidateChip, validate)
            .Add(c => c.SearchFunc, search)
            .Add(c => c.IsChipClosable, closable)
            .Add(c => c.CommitOnBlur, commitOnBlur)
            .Add(c => c.AllowDuplicates, allowDuplicates)
            .Add(c => c.MaxSuggestions, maxSuggestions)
            .Add(c => c.AutoClear, autoClear)
            .Add(c => c.ReadOnly, readOnly)
            .Add(c => c.Closeable, closeable)
            .Add(c => c.Disabled, disabled));
    }

    private static IElement Input<T>(IRenderedComponent<MudExTagField<T>> field) => field.Find("input");

    private static string Text(IRenderedComponent<MudExTagField<string>> field) => Input(field).GetAttribute("value");

    private static Task TypeAsync(IRenderedComponent<MudExTagField<string>> field, string text) => Input(field).InputAsync(new ChangeEventArgs { Value = text });

    private static Task KeyAsync(IRenderedComponent<MudExTagField<string>> field, string key) => Input(field).KeyDownAsync(new KeyboardEventArgs { Key = key });

    private static Task BlurAsync(IRenderedComponent<MudExTagField<string>> field) => Input(field).BlurAsync(new FocusEventArgs());

    private IReadOnlyList<string> Suggestions() => _popovers.FindAll("[data-suggestion]").Select(e => e.GetAttribute("data-suggestion")).ToList();

    [Fact]
    public async Task EnterAddsAChipAndClearsTheField()
    {
        var field = Open();

        await TypeAsync(field, "profile");
        await KeyAsync(field, "Enter");

        Assert.Equal(["profile"], _values);
        field.Find("[data-chip='profile']");
        Assert.True(string.IsNullOrEmpty(Text(field)));
    }

    [Fact]
    public async Task ADelimiterAddsTheTextBeforeIt()
    {
        var field = Open();

        await TypeAsync(field, "openid,");

        Assert.Equal(["openid"], _values);
        Assert.True(string.IsNullOrEmpty(Text(field)));
    }

    [Fact]
    public async Task APastedListBecomesSeveralChips()
    {
        var field = Open();

        await TypeAsync(field, "openid profile\nemail;phone");

        Assert.Equal(["openid", "profile", "email", "phone"], _values);
        Assert.Equal(4, field.FindAll("[data-chip]").Count);
    }

    [Fact]
    public async Task DuplicatesAreIgnored()
    {
        var field = Open(["a"]);

        await TypeAsync(field, "a b a B ");

        Assert.Equal(["a", "b", "B"], _values);
    }

    [Fact]
    public async Task ARepeatedSingleEntryStaysInTheFieldWithTheDuplicateError()
    {
        var field = Open(["a"]);

        await TypeAsync(field, "a");
        await KeyAsync(field, "Enter");

        Assert.Equal(0, _changes);
        Assert.Equal("a", Text(field));
        Assert.Contains("Duplicate values", field.Markup);
    }

    [Fact]
    public async Task DuplicatesAreAddedWhenAllowed()
    {
        var field = Open(["a"], allowDuplicates: true);

        await TypeAsync(field, "a");
        await KeyAsync(field, "Enter");

        Assert.Equal(["a", "a"], _values);
    }

    [Fact]
    public async Task InvalidTextShowsTheErrorAndAddsNothing()
    {
        var field = Open(validate: v => v.StartsWith("https:") ? null : "Not an absolute address");

        await TypeAsync(field, "/callback");
        await KeyAsync(field, "Enter");

        Assert.Equal(0, _changes);
        Assert.Contains("Not an absolute address", field.Markup);
        Assert.Empty(field.FindAll("[data-chip]"));
        Assert.Equal("/callback", Text(field));
    }

    [Fact]
    public async Task ValidTokensOfAMixedPasteAreAddedAndTheInvalidOnesStayInTheField()
    {
        var field = Open(validate: v => v.Contains('!') ? "No special characters" : null);

        await TypeAsync(field, "ok bad! fine");

        Assert.Equal(["ok", "fine"], _values);
        Assert.Equal("bad!", Text(field));
        Assert.Contains("No special characters", field.Markup);
    }

    [Fact]
    public async Task RejectedTokensAreNotRepeatedInTheField()
    {
        var field = Open(validate: v => v.Contains('!') ? "No special characters" : null);

        await TypeAsync(field, "bad! bad! ok");

        Assert.Equal(["ok"], _values);
        Assert.Equal("bad!", Text(field));
    }

    [Fact]
    public async Task TypingOnAfterARejectionClearsTheError()
    {
        var field = Open(validate: v => v.Contains('!') ? "No special characters" : null);
        await TypeAsync(field, "bad!");
        await KeyAsync(field, "Enter");
        Assert.Contains("No special characters", field.Markup);

        await TypeAsync(field, "bad");

        Assert.DoesNotContain("No special characters", field.Markup);
    }

    [Fact]
    public void StoredInvalidValuesAreShownAsErrorChipsNotDropped()
    {
        var field = Open(["relative/path", "https://a.example/cb"], v => v.StartsWith("https:") ? null : "Not an absolute address");

        Assert.Contains("mud-chip-color-error", field.Find("[data-chip='relative/path']").ClassList);
        Assert.DoesNotContain("mud-chip-color-error", field.Find("[data-chip='https://a.example/cb']").ClassList);
        Assert.Equal(0, _changes);
        Assert.Equal(2, _values.Count);
    }

    [Fact]
    public async Task ClosingAChipRemovesIt()
    {
        var field = Open(["a", "b"]);

        await field.Find("[data-chip='a'] .mud-chip-close-button").ClickAsync(new());

        Assert.Equal(["b"], _values);
        Assert.Empty(field.FindAll("[data-chip='a']"));
    }

    [Fact]
    public async Task FixedChipsHaveNoCloseIconAndBackspaceKeepsThem()
    {
        var field = Open(["profile", "openid"], closable: v => v != "openid");

        Assert.Empty(field.FindAll("[data-chip='openid'] .mud-chip-close-button"));
        Assert.Single(field.FindAll("[data-chip='profile'] .mud-chip-close-button"));

        await KeyAsync(field, "Backspace");
        Assert.Equal(["profile", "openid"], _values);
    }

    [Fact]
    public async Task BackspaceOnAnEmptyFieldRemovesTheLastChip()
    {
        var field = Open(["a", "b"]);

        await KeyAsync(field, "Backspace");

        Assert.Equal(["a"], _values);
    }

    [Fact]
    public void ChipsAreNotClosableWhenCloseableIsOff()
    {
        var field = _context.Render<MudExTagField<string>>(p => p.Add(c => c.Values, ["a"]).Add(c => c.Closeable, false));

        Assert.Empty(field.FindAll(".mud-chip-close-button"));
    }

    [Fact]
    public async Task SuggestionsAreShownForTheTypedTextWithoutTheChosenValues()
    {
        var field = Open(["profile"], search: Offer);

        await TypeAsync(field, "o");

        Assert.Equal(["openid", "offline_access"], Suggestions());
    }

    [Fact]
    public async Task MaxSuggestionsCapsTheList()
    {
        var field = Open(search: Offer, maxSuggestions: 2);

        await TypeAsync(field, "i");

        Assert.Equal(2, Suggestions().Count);
    }

    [Fact]
    public void WithoutASearchFuncNoSuggestionsAreOffered()
    {
        var field = Open();

        Assert.Empty(field.FindComponents<MudPopover>());
    }

    [Fact]
    public async Task ClickingASuggestionAddsItAndClearsTheField()
    {
        var field = Open(search: Offer);
        await TypeAsync(field, "pro");

        await _popovers.Find("[data-suggestion='profile']").ClickAsync(new());

        Assert.Equal(["profile"], _values);
        Assert.Equal(1, _changes);
        Assert.True(string.IsNullOrEmpty(Text(field)));
        Assert.Empty(Suggestions());
    }

    [Fact]
    public async Task EnterPicksTheHighlightedSuggestionOnly()
    {
        var field = Open(search: Offer);
        await TypeAsync(field, "pro");

        await KeyAsync(field, "Enter");

        Assert.Equal(["profile"], _values);
        Assert.Equal(1, _changes);
        Assert.True(string.IsNullOrEmpty(Text(field)));
    }

    [Fact]
    public async Task ArrowKeysMoveTheHighlight()
    {
        var field = Open(search: Offer);
        await TypeAsync(field, "o");

        await KeyAsync(field, "ArrowDown");
        await KeyAsync(field, "ArrowDown");
        await KeyAsync(field, "Enter");

        Assert.Equal(["offline_access"], _values);
    }

    [Fact]
    public async Task ArrowUpWrapsToTheLastSuggestion()
    {
        var field = Open(search: Offer);
        await TypeAsync(field, "o");

        await KeyAsync(field, "ArrowUp");
        await KeyAsync(field, "Enter");

        Assert.Equal(["offline_access"], _values);
    }

    [Fact]
    public async Task ArrowDownOpensTheListForAnEmptyField()
    {
        var field = Open(search: Offer);

        await KeyAsync(field, "ArrowDown");

        Assert.Equal(Standard, Suggestions());
    }

    [Fact]
    public async Task EscapeDiscardsTheTypedTextAndClosesTheList()
    {
        var field = Open(search: Offer);
        await TypeAsync(field, "pro");

        await KeyAsync(field, "Escape");

        Assert.Equal(0, _changes);
        Assert.Empty(field.FindAll("[data-chip]"));
        Assert.True(string.IsNullOrEmpty(Text(field)));
        Assert.Empty(Suggestions());
    }

    [Fact]
    public async Task LeavingTheFieldAddsTheTypedText()
    {
        var field = Open();

        await TypeAsync(field, "custom.scope");
        await BlurAsync(field);

        Assert.Equal(["custom.scope"], _values);
        Assert.True(string.IsNullOrEmpty(Text(field)));
    }

    [Fact]
    public async Task LeavingTheFieldAddsNothingWithoutCommitOnBlur()
    {
        var field = Open(commitOnBlur: false);

        await TypeAsync(field, "custom.scope");
        await BlurAsync(field);

        Assert.Equal(0, _changes);
    }

    [Fact]
    public async Task LeavingTheFieldWithInvalidTextAddsNothing()
    {
        var field = Open(validate: v => v.Contains('/') ? "No slashes" : null);

        await TypeAsync(field, "a/b");
        await BlurAsync(field);

        Assert.Equal(0, _changes);
        Assert.Contains("No slashes", field.Markup);
    }

    [Fact]
    public async Task LeavingTheFieldWithTheListOpenAddsTheTextNotTheSuggestion()
    {
        var field = Open(search: Offer);
        await TypeAsync(field, "pro");

        await KeyAsync(field, "Tab");
        await BlurAsync(field);

        Assert.Equal(["pro"], _values);
        Assert.Empty(Suggestions());
    }

    [Fact]
    public async Task AListThatClosesForLackOfMatchesDoesNotAddTheText()
    {
        var field = Open(search: Offer);
        await TypeAsync(field, "pr");
        Assert.Equal(["profile"], Suggestions());

        await TypeAsync(field, "prx");

        Assert.Empty(Suggestions());
        Assert.Equal(0, _changes);

        await BlurAsync(field);
        Assert.Equal(["prx"], _values);
    }

    [Fact]
    public async Task TextWhoseOnlyMatchIsAlreadyChosenIsNotAddedWhileTyping()
    {
        var field = Open(["profile"], search: Offer);

        await TypeAsync(field, "pro");

        Assert.Empty(Suggestions());
        Assert.Equal(0, _changes);
    }

    [Fact]
    public async Task EnterWithNoSuggestionAddsTheTypedText()
    {
        var field = Open(search: Offer);

        await TypeAsync(field, "prxy");
        await KeyAsync(field, "Enter");

        Assert.Equal(["prxy"], _values);
    }

    [Fact]
    public async Task EnterAndBlurOnAnEmptyFieldKeepTheErrorSetFromOutside()
    {
        _popovers ??= _context.Render<MudPopoverProvider>();
        var field = _context.Render<MudExTagField<string>>(p => p
            .Add(c => c.Values, [])
            .Add(c => c.Error, true)
            .Add(c => c.ErrorText, "Form error")
            .Add(c => c.CommitOnBlur, true));
        Assert.True(field.Instance.HasErrors);

        await KeyAsync(field, "Enter");
        await BlurAsync(field);

        Assert.True(field.Instance.HasErrors);
        Assert.Equal("Form error", field.Instance.ErrorText);
        Assert.Empty(field.FindAll("[data-chip]"));
    }
    [Fact]
    public async Task DelimitersTypedAloneAreDropped()
    {
        var field = Open();

        await TypeAsync(field, ",");

        Assert.Equal(0, _changes);
        Assert.True(string.IsNullOrEmpty(Text(field)));
    }

    [Fact]
    public async Task ADelimiterResetsTheTextEvenWithoutAutoClear()
    {
        var field = Open(autoClear: false);

        await TypeAsync(field, "a,");
        Assert.Equal(["a"], _values);
        Assert.True(string.IsNullOrEmpty(Text(field)));

        await TypeAsync(field, "b,");
        Assert.Equal(["a", "b"], _values);
        Assert.True(string.IsNullOrEmpty(Text(field)));
    }

    [Fact]
    public async Task WithoutAutoClearEnterKeepsTheText()
    {
        var field = Open(autoClear: false);

        await TypeAsync(field, "abc");
        await KeyAsync(field, "Enter");

        Assert.Equal(["abc"], _values);
        await KeyAsync(field, "Enter");
        Assert.Equal(1, _changes);
        Assert.Contains("Duplicate values", field.Markup);
    }

    [Fact]
    public async Task BackspaceKeepsChipsWhenReadOnlyNotCloseableOrDisabled()
    {
        foreach (var (readOnly, closeable, disabled) in new[] { (true, true, false), (false, false, false), (false, true, true) })
        {
            var field = Open(["a", "b"], readOnly: readOnly, closeable: closeable, disabled: disabled);

            await KeyAsync(field, "Backspace");

            Assert.Equal(["a", "b"], _values);
        }
    }

    [Fact]
    public void ReadOnlyAndDisabledChipsHaveNoCloseIcon()
    {
        Assert.Empty(Open(["a"], readOnly: true).FindAll(".mud-chip-close-button"));
        Assert.Empty(Open(["a"], disabled: true).FindAll(".mud-chip-close-button"));
    }

    [Fact]
    public async Task EscapeWithoutASearchFuncKeepsTheOldBehaviourAndTheText()
    {
        var field = Open(commitOnBlur: false);
        await TypeAsync(field, "pro");

        await KeyAsync(field, "Escape");
        await KeyAsync(field, "Enter");

        Assert.Equal(["pro"], _values);
    }

    [Fact]
    public async Task AFailingSearchShowsNoSuggestionsAndKeepsTheText()
    {
        var field = Open(search: (_, _) => throw new InvalidOperationException("backend down"));

        await TypeAsync(field, "pro");
        Assert.Empty(Suggestions());
        await KeyAsync(field, "Enter");

        Assert.Equal(["pro"], _values);
    }

    [Fact]
    public async Task AnAsyncFailingSearchShowsNoSuggestions()
    {
        var field = Open(search: async (_, _) => { await Task.Yield(); throw new InvalidOperationException("backend down"); });

        await TypeAsync(field, "pro");

        Assert.Empty(Suggestions());
    }

    [Fact]
    public async Task ANonStringValueTypeIsConvertedAndAnUnparsableTokenStaysInTheField()
    {
        var values = new List<int>();
        _popovers ??= _context.Render<MudPopoverProvider>();
        var field = _context.Render<MudExTagField<int>>(p => p
            .Add(c => c.Values, values)
            .Add(c => c.Delimiters, Separators)
            .Add(c => c.ValidateChip, v => v > 100 ? "Too big" : null));

        await Input(field).InputAsync(new ChangeEventArgs { Value = "7 abc 8 500" });

        Assert.Equal([7, 8], values);
        Assert.Equal("abc 500", Input(field).GetAttribute("value"));
        Assert.Equal(2, field.FindAll("[data-chip]").Count);
    }

    [Fact]
    public async Task ANonStringValueTypeOffersAndPicksSuggestions()
    {
        var values = new List<int>();
        _popovers ??= _context.Render<MudPopoverProvider>();
        var field = _context.Render<MudExTagField<int>>(p => p
            .Add(c => c.Values, values)
            .Add(c => c.SearchFunc, (text, _) => Task.FromResult(new[] { 1, 12, 20 }.Where(i => i.ToString().Contains(text)))));

        await Input(field).InputAsync(new ChangeEventArgs { Value = "1" });
        await _popovers.Find("[data-suggestion='12']").ClickAsync(new());

        Assert.Equal([12], values);
    }
}