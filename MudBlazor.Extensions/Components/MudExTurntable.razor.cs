using System.Globalization;
using System.Text.Json;
using AuralizeBlazor;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// A turntable with scratching, tone arm, a second deck and a DJ mixer (EQ, filter, crossfader, hot cues).
/// Wraps the free <c>&lt;audiola-turntable&gt;</c> web component, see https://audiola.de/turntable.html.
/// </summary>
public partial class MudExTurntable
{
    /// <summary>The default url of the web component script.</summary>
    public const string DefaultScriptUrl = "https://audiola.de/widgets/turntable/v1.js";

    private object _pushedRecords;
    private object _pushedTrackList;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web); // JsonSerializerOptions.Web needs net9
    private IReadOnlyList<TurntableRecord> _sentRecords = [];
    // captured once, renders started from JS run with the default culture instead of the one the app set
    private readonly string _cultureLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName is var l && l is "de" or "en" ? l : null;

    /// <summary>The records (albums) to play. Takes precedence over <see cref="TrackList"/> and <see cref="RecordsUrl"/>.</summary>
    [Parameter, SafeCategory("Data")] public IEnumerable<TurntableRecord> Records { get; set; }

    /// <summary>
    /// Plain tracks, e.g. the same <see cref="IAuralizerTrack"/> list as for <see cref="MudExAudioPlayer.TrackList"/>.
    /// Tracks with the same <see cref="IAuralizerTrack.Image"/> become one record with that image as cover.
    /// </summary>
    [Parameter, SafeCategory("Data")] public IEnumerable<IAuralizerTrack> TrackList { get; set; }

    /// <summary>The title of the records built from <see cref="TrackList"/> that contain more than one track.</summary>
    [Parameter, SafeCategory("Data")] public string TrackListTitle { get; set; }

    /// <summary>Url of a records JSON file in the format of https://audiola.de/turntable.html. Relative paths in it are relative to the file.</summary>
    [Parameter, SafeCategory("Data")] public string RecordsUrl { get; set; }

    /// <summary>The visitor can add own songs per button and drag and drop; cover, title and track list come from the ID3 tags.</summary>
    [Parameter, SafeCategory("Behavior")] public bool AllowUploads { get; set; }

    /// <summary>Shows the button that opens the mixer with the second deck.</summary>
    [Parameter, SafeCategory("Behavior")] public bool EnableMixer { get; set; } = true;

    /// <summary>Single turntable or DJ mode with opened mixer. Needs <see cref="EnableMixer"/> for <see cref="TurntableMode.Dj"/>.</summary>
    [Parameter, SafeCategory("Behavior")] public TurntableMode Mode { get; set; }

    /// <summary>Raised when the mixer is opened or closed, also from the turntable itself.</summary>
    [Parameter] public EventCallback<TurntableMode> ModeChanged { get; set; }

    /// <summary>Raised when a deck starts, stops, loads, fails or changes the record or track.</summary>
    [Parameter] public EventCallback<TurntableDeckState> DeckChanged { get; set; }

    /// <summary>The current state of deck A.</summary>
    public TurntableDeckState DeckA { get; private set; } = new() { Deck = TurntableDeck.A };

    /// <summary>The current state of deck B.</summary>
    public TurntableDeckState DeckB { get; private set; } = new() { Deck = TurntableDeck.B };

    /// <summary>The language of the labels. <see cref="TurntableLanguage.Auto"/> follows the current UI culture and falls back to the browser language.</summary>
    [Parameter, SafeCategory("Appearance")] public TurntableLanguage Language { get; set; }

    /// <summary>Image on the empty platter before a record is picked.</summary>
    [Parameter, SafeCategory("Appearance")] public string Logo { get; set; }

    /// <summary>Accent color for buttons and faders.</summary>
    [Parameter, SafeCategory("Appearance")] public MudExColor AccentColor { get; set; } = MudExColor.Primary;

    /// <summary>The text color.</summary>
    [Parameter, SafeCategory("Appearance")] public MudExColor TextColor { get; set; } = "--mud-palette-text-primary";

    /// <summary>The color of secondary texts.</summary>
    [Parameter, SafeCategory("Appearance")] public MudExColor MutedColor { get; set; } = "--mud-palette-text-secondary";

    /// <summary>The minimum height, e.g. 100vh to use it as page header. Null keeps the default of the web component (780px).</summary>
    [Parameter, SafeCategory("Appearance")] public MudExSize<double>? MinHeight { get; set; }

    /// <summary>The inner padding as CSS value. Null keeps the default of the web component (48px 5%).</summary>
    [Parameter, SafeCategory("Appearance")] public string Padding { get; set; }

    /// <summary>Content left of the turntable that slides out when the mixer opens, e.g. a header.</summary>
    [Parameter] public RenderFragment ChildContent { get; set; }

    /// <summary>The url of the web component script, change it to host the script yourself.</summary>
    [Parameter, SafeCategory("Behavior")] public string ScriptUrl { get; set; } = DefaultScriptUrl;

    private bool HasTypedRecords => Records != null || TrackList != null;

    private string Lang => Language switch
    {
        TurntableLanguage.German => "de",
        TurntableLanguage.English => "en",
        _ => _cultureLang
    };

    private string CssStyle => string.Join(";", new[]
    {
        $"--at-accent:{AccentColor.ToCssStringValue()}",
        $"--at-text:{TextColor.ToCssStringValue()}",
        $"--at-muted:{MutedColor.ToCssStringValue()}",
        "--at-font:var(--mud-typography-default-family)",
        MinHeight is { } h ? $"--at-min-height:{h}" : null,
        Padding is null ? null : $"--at-padding:{Padding}",
        Style
    }.Where(s => !string.IsNullOrWhiteSpace(s)));

    /// <inheritdoc />
    public override object[] GetJsArguments() => [ElementReference, CreateDotNetObjectReference(), ScriptUrl];

    /// <inheritdoc />
    public override async Task ImportModuleAndCreateJsAsync()
    {
        await base.ImportModuleAndCreateJsAsync();
        await RefreshRecordsAsync();
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        if (JsReference != null && (!ReferenceEquals(_pushedRecords, Records) || !ReferenceEquals(_pushedTrackList, TrackList)))
            await RefreshRecordsAsync();
    }

    /// <summary>
    /// Sends <see cref="Records"/> or <see cref="TrackList"/> to the turntable again.
    /// Only needed after changing these collections in place, a new collection is sent automatically.
    /// </summary>
    public async Task RefreshRecordsAsync()
    {
        _pushedRecords = Records;
        _pushedTrackList = TrackList;
        if (JsReference == null || (!HasTypedRecords && RecordsUrl != null))
        {
            _sentRecords = [];
            return;
        }
        var records = (Records ?? TurntableRecord.FromTracks(TrackList, TrackListTitle)).Select(r => (Record: r, Js: r?.ToJs())).Where(r => r.Js != null).ToList();
        _sentRecords = records.Select(r => r.Record).ToList();
        // cast to object, otherwise the array is spread into the params of InvokeVoidAsync
        await JsReference.InvokeVoidAsync("setRecords", (object)records.Select(r => r.Js).ToArray());
    }

    /// <summary>Plays on, or starts the first record if none is on the deck.</summary>
    public Task PlayAsync(TurntableDeck deck = TurntableDeck.A) => InvokeDeckAsync("play", null, 0, Js(deck));

    /// <summary>Puts the record with the given index on the deck, starting at the given track. Own songs follow behind the records.</summary>
    public Task PlayAsync(int record, int track = 0, TurntableDeck deck = TurntableDeck.A) => InvokeDeckAsync("play", record, track, Js(deck));

    /// <summary>Puts the record on the deck, starting at the given track. The record has to be one of <see cref="Records"/>.</summary>
    public Task PlayAsync(TurntableRecord record, int track = 0, TurntableDeck deck = TurntableDeck.A)
        => IndexOf(record) is var i and >= 0 ? PlayAsync(i, track, deck) : Task.CompletedTask;

    /// <summary>Plays the track, e.g. one of <see cref="TrackList"/> or of the tracks of <see cref="Records"/>.</summary>
    public Task PlayAsync(IAuralizerTrack track, TurntableDeck deck = TurntableDeck.A)
    {
        for (var i = 0; i < _sentRecords.Count; i++)
        {
            if (_sentRecords[i].PlayableTracks().ToList().IndexOf(track) is var t and >= 0)
                return PlayAsync(i, t, deck);
        }
        return Task.CompletedTask;
    }

    /// <summary>Lifts the needle, the record runs out audibly.</summary>
    public Task PauseAsync(TurntableDeck deck = TurntableDeck.A) => InvokeDeckAsync("pause", Js(deck));

    /// <summary>Plays the next track of the record on the deck.</summary>
    public Task NextAsync(TurntableDeck deck = TurntableDeck.A) => InvokeDeckAsync("next", Js(deck));

    /// <summary>Plays the previous track of the record on the deck.</summary>
    public Task PreviousAsync(TurntableDeck deck = TurntableDeck.A) => InvokeDeckAsync("previous", Js(deck));

    private int IndexOf(TurntableRecord record) => _sentRecords.ToList().IndexOf(record);

    private static string Js(TurntableDeck deck) => deck == TurntableDeck.B ? "b" : "a";

    private async Task InvokeDeckAsync(string method, params object[] args)
    {
        var js = await WaitReferenceCreatedAsync();
        await js.InvokeVoidAsync(method, args);
    }

    /// <summary>Called by the web component when a deck changes.</summary>
    [JSInvokable]
    public Task OnDeckChanged(JsonElement detail)
    {
        var state = TurntableDeckState.From(detail.Deserialize<TurntableDeckState.Detail>(JsonOptions), _sentRecords);
        if (state.Deck == TurntableDeck.B)
            DeckB = state;
        else
            DeckA = state;
        return DeckChanged.InvokeAsync(state);
    }

    /// <summary>Called by the web component when the mixer is opened or closed.</summary>
    [JSInvokable]
    public Task OnModeChanged(string mode)
    {
        var value = mode == "dj" ? TurntableMode.Dj : TurntableMode.Single;
        if (value == Mode)
            return Task.CompletedTask;
        Mode = value;
        return ModeChanged.InvokeAsync(value);
    }
}
