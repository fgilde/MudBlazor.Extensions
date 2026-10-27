using System.Text.Json;
using AuralizeBlazor;
using MudBlazor.Extensions.Components;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

public sealed class MudExTurntableTests
{
    [Fact]
    public void TracksWithTheSameImageBecomeOneRecord()
    {
        IAuralizerTrack[] tracks =
        [
            new AuralizerTrack("a.mp3", "A", "cover.webp"),
            new AuralizerTrack("b.mp3", "B", "cover.webp"),
            new AuralizerTrack("c.mp3", "C"),
            null
        ];

        var records = TurntableRecord.FromTracks(tracks, "Album").ToList();

        Assert.Equal(2, records.Count);
        Assert.Equal(("Album", "cover.webp", 2), (records[0].Title, records[0].Cover, records[0].Tracks.Count));
        Assert.Equal(("C", null, 1), (records[1].Title, records[1].Cover, records[1].Tracks.Count));
    }

    [Fact]
    public void ToJsWritesTheWebComponentFormat()
    {
        var record = new TurntableRecord
        {
            Title = "R", Accent = "#3de0ff", Gradient = ["#3de0ff", "#6a7bff", "#c93dff"],
            Tracks = [new TurntableTrack("01.mp3", "One", TimeSpan.FromSeconds(202.5)), new AuralizerTrack("02.mp3", "Two", "cover.webp"), new AuralizerTrack()]
        };

        var json = JsonSerializer.Serialize(record.ToJs());

        Assert.Equal("""{"title":"R","artist":null,"cover":"cover.webp","link":null,"accent":"#3de0ff","gradient":["#3de0ff","#6a7bff","#c93dff"],"tracks":[{"title":"One","src":"01.mp3","duration":202.5},{"title":"Two","src":"02.mp3","duration":null}]}""", json);
        Assert.Null(new TurntableRecord { Tracks = [new AuralizerTrack()] }.ToJs());
    }

    [Fact]
    public void DeckEventMapsToTheSentInstances()
    {
        var playable = new AuralizerTrack("b.mp3", "B");
        var sent = new TurntableRecord { Title = "R", Tracks = [new AuralizerTrack(), playable] };
        var detail = JsonSerializer.Deserialize<TurntableDeckState.Detail>(
            """{"deck":"b","playing":true,"loading":false,"failed":false,"record":{"index":0,"uploaded":false,"title":"R"},"track":{"index":0,"title":"B","src":"b.mp3"}}""", JsonSerializerOptions.Web);

        var state = TurntableDeckState.From(detail, [sent]);
        Assert.Equal((TurntableDeck.B, true, 0), (state.Deck, state.IsPlaying, state.RecordIndex));
        Assert.Same(sent, state.Record);
        Assert.Same(playable, state.Track); // the web component counts only tracks with url

        var uploaded = TurntableDeckState.From(detail with { Record = detail.Record with { Uploaded = true, Accent = "#3de0ff" } }, [sent]);
        Assert.NotSame(sent, uploaded.Record);
        Assert.Equal(("R", "#3de0ff", "b.mp3"), (uploaded.Record.Title, uploaded.Record.Accent.ToString(MudBlazor.Utilities.MudColorOutputFormats.Hex), uploaded.Track.Url));

        var replaced = TurntableDeckState.From(detail with { Record = detail.Record with { Index = -1 } }, [sent]);
        Assert.Equal("R", replaced.Record.Title);
    }
}
