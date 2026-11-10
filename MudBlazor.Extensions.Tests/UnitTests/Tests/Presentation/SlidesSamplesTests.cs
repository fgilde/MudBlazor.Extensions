using MainSample.WebAssembly.Examples.Slides;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Presentation;

using MudBlazor.Extensions.Office.Presentation;

public class SlidesSamplesTests
{
    [Fact]
    public void SlidesSamples_DeckRoundTrips()
    {
        var deck = SlidesSamples.CreateDeck();
        using var stream = new MemoryStream();
        deck.Save(stream);
        stream.Position = 0;
        var loaded = Presentation.Load(stream);

        Assert.Equal(deck.Slides.Count, loaded.Slides.Count);
        Assert.True(loaded.Slides.Count >= 4);
        Assert.All(loaded.Slides, s => Assert.False(string.IsNullOrWhiteSpace(s.Notes)));
        Assert.Contains(loaded.Slides.SelectMany(s => s.Shapes), s => s is PictureShape);
        Assert.Contains(loaded.Slides.SelectMany(s => s.Shapes.OfType<AutoShape>()), s => s.Geometry != "rect");
        Assert.Contains(loaded.Slides[0].Shapes.OfType<AutoShape>(), s => s.Text.Paragraphs.Any(p => p.Runs.Any(r => r.Text.Length > 0)));
    }
}
