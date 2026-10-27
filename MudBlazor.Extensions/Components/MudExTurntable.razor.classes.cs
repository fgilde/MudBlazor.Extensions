using AuralizeBlazor;
using MudBlazor.Utilities;

namespace MudBlazor.Extensions.Components;

/// <summary>Single turntable or DJ mode with opened mixer.</summary>
public enum TurntableMode
{
    /// <summary>One turntable, the mixer can be opened with a button.</summary>
    Single,
    /// <summary>The mixer with the second deck is open.</summary>
    Dj
}

/// <summary>A deck of the turntable. Deck B is only playable while the mixer is open.</summary>
public enum TurntableDeck
{
    /// <summary>The left deck, the only one without the mixer.</summary>
    A,
    /// <summary>The right deck of the mixer.</summary>
    B
}

/// <summary>What a deck is doing right now, see <see cref="MudExTurntable.DeckChanged"/>.</summary>
public class TurntableDeckState
{
    /// <summary>The deck.</summary>
    public TurntableDeck Deck { get; init; }

    /// <summary>The needle is on the record and the record turns.</summary>
    public bool IsPlaying { get; init; }

    /// <summary>The track is still loading.</summary>
    public bool IsLoading { get; init; }

    /// <summary>The track could not be loaded.</summary>
    public bool HasFailed { get; init; }

    /// <summary>The index of the record in the record list, own songs follow behind; -1 without record.</summary>
    public int RecordIndex { get; init; } = -1;

    /// <summary>The record comes from the own songs of the visitor (<see cref="MudExTurntable.AllowUploads"/>).</summary>
    public bool IsUploaded { get; init; }

    /// <summary>
    /// The record on the deck. The instance of <see cref="MudExTurntable.Records"/> (or the one built from <see cref="MudExTurntable.TrackList"/>),
    /// for records from <see cref="MudExTurntable.RecordsUrl"/> and own songs a new instance without tracks.
    /// </summary>
    public TurntableRecord Record { get; init; }

    /// <summary>The index of the track on the record.</summary>
    public int TrackIndex { get; init; }

    /// <summary>The track: the instance of the record when it comes from C#, otherwise a new <see cref="TurntableTrack"/>.</summary>
    public IAuralizerTrack Track { get; init; }

    internal record Detail(string Deck, bool Playing, bool Loading, bool Failed, RecordDetail Record, TrackDetail Track);
    internal record RecordDetail(int Index, bool Uploaded, string Title, string Artist, string Cover, string Link, string Accent);
    internal record TrackDetail(int Index, string Title, string Src, double? Duration);

    // records are the ones sent to the web component, so the index of the event points into them
    internal static TurntableDeckState From(Detail detail, IReadOnlyList<TurntableRecord> records)
    {
        // index -1: the record was just replaced by a new list and is about to leave the deck
        var known = detail.Record is { Uploaded: false, Index: >= 0 } r && r.Index < records.Count ? records[r.Index] : null;
        var record = known ?? (detail.Record is { } d ? new TurntableRecord
        {
            Title = d.Title, Artist = d.Artist, Cover = d.Cover, Link = d.Link,
            Accent = string.IsNullOrEmpty(d.Accent) ? null : new MudColor(d.Accent)
        } : null);
        var track = detail.Track is { } t
            ? known?.PlayableTracks().ElementAtOrDefault(t.Index) ?? new TurntableTrack(t.Src, t.Title, t.Duration is { } s ? TimeSpan.FromSeconds(s) : null)
            : null;
        return new TurntableDeckState
        {
            Deck = detail.Deck == "b" ? TurntableDeck.B : TurntableDeck.A,
            IsPlaying = detail.Playing,
            IsLoading = detail.Loading,
            HasFailed = detail.Failed,
            RecordIndex = detail.Record?.Index ?? -1,
            IsUploaded = detail.Record?.Uploaded ?? false,
            Record = record,
            TrackIndex = detail.Track?.Index ?? 0,
            Track = track
        };
    }
}

/// <summary>The language of the turntable labels.</summary>
public enum TurntableLanguage
{
    /// <summary>The current UI culture if it is German or English, otherwise the browser language.</summary>
    Auto,
    /// <summary>German.</summary>
    German,
    /// <summary>English.</summary>
    English
}

/// <summary>
/// A track of a <see cref="TurntableRecord"/>. Any <see cref="IAuralizerTrack"/> works as track,
/// this one adds the optional <see cref="Duration"/> which is otherwise read when the track loads.
/// </summary>
public class TurntableTrack : AuralizerTrack
{
    /// <summary>Creates an empty track.</summary>
    public TurntableTrack() { }

    /// <summary>Creates a track with url, label and optional duration.</summary>
    public TurntableTrack(string url, string label, TimeSpan? duration = null) : base(url, label)
        => Duration = duration;

    /// <summary>The length of the track.</summary>
    public TimeSpan? Duration { get; set; }
}

/// <summary>A record (album) on the turntable.</summary>
public class TurntableRecord
{
    /// <summary>The title of the record.</summary>
    public string Title { get; set; }

    /// <summary>The artist.</summary>
    public string Artist { get; set; }

    /// <summary>Url of the cover image. Missing colors are computed from it.</summary>
    public string Cover { get; set; }

    /// <summary>Optional link of the record, e.g. the homepage of the artist.</summary>
    public string Link { get; set; }

    /// <summary>Color of the label and the visuals. Computed from the cover if not set.</summary>
    public MudColor Accent { get; set; }

    /// <summary>Exactly three colors for the label gradient. Computed from the cover or the accent if not set.</summary>
    public MudColor[] Gradient { get; set; }

    /// <summary>The tracks, e.g. <see cref="AuralizerTrack"/> or <see cref="TurntableTrack"/>. A record without tracks is skipped.</summary>
    public IList<IAuralizerTrack> Tracks { get; set; } = [];

    /// <summary>
    /// Builds records from plain tracks: tracks with the same <see cref="IAuralizerTrack.Image"/> become one record with that image as cover.
    /// A record of a single track is named after the track, others after <paramref name="title"/>.
    /// </summary>
    public static IEnumerable<TurntableRecord> FromTracks(IEnumerable<IAuralizerTrack> tracks, string title = null)
        => (tracks ?? []).Where(t => t != null).GroupBy(t => t.Image).Select(g => new TurntableRecord
        {
            Title = g.Count() == 1 ? g.First().Label : title ?? "Playlist",
            Cover = g.Key,
            Tracks = g.ToList()
        });

    // the web component skips tracks without url, so its track indexes count only these
    internal IEnumerable<IAuralizerTrack> PlayableTracks() => Tracks?.Where(t => !string.IsNullOrEmpty(t?.Url)) ?? [];

    // The format of the records JSON of the web component, colors as #rrggbb
    internal object ToJs()
    {
        var tracks = PlayableTracks().Select(t => new
        {
            title = t.Label,
            src = t.Url,
            duration = (t as TurntableTrack)?.Duration?.TotalSeconds
        }).ToArray();
        return tracks is not { Length: > 0 } ? null : new
        {
            title = Title,
            artist = Artist,
            cover = Cover ?? Tracks.FirstOrDefault(t => !string.IsNullOrEmpty(t?.Image))?.Image,
            link = Link,
            accent = Accent?.ToString(MudColorOutputFormats.Hex),
            gradient = Gradient is { Length: 3 } ? Gradient.Select(c => c.ToString(MudColorOutputFormats.Hex)).ToArray() : null,
            tracks
        };
    }
}
