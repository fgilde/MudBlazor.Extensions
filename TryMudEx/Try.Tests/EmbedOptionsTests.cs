using NUnit.Framework;
using TryMudEx.Client.Models;

namespace Try.Tests;

[TestFixture]
public class EmbedOptionsTests
{
    private const string Base = "https://try.mudex.org/embed/abc";

    [Test]
    public void Defaults_WhenNoQuery()
    {
        var options = EmbedOptions.Parse(Base);

        Assert.That(options.View, Is.EqualTo(EmbedView.Split));
        Assert.That(options.AutoRun, Is.True);
        Assert.That(options.ReadOnly, Is.False);
        Assert.That(options.HideHeader, Is.False);
        Assert.That(options.Theme, Is.EqualTo("auto"));
        Assert.That(options.File, Is.Null);
    }

    [TestCase("?view=code", EmbedView.Code)]
    [TestCase("?view=preview", EmbedView.Preview)]
    [TestCase("?view=split", EmbedView.Split)]
    [TestCase("?view=nonsense", EmbedView.Split)]
    public void View_IsParsed(string query, EmbedView expected)
    {
        Assert.That(EmbedOptions.Parse(Base + query).View, Is.EqualTo(expected));
    }

    [Test]
    public void Flags_PresenceWithoutValueMeansTrue()
    {
        var options = EmbedOptions.Parse(Base + "?readonly&hideheader");

        Assert.That(options.ReadOnly, Is.True);
        Assert.That(options.HideHeader, Is.True);
    }

    [Test]
    public void AutoRun_CanBeDisabled()
    {
        Assert.That(EmbedOptions.Parse(Base + "?autorun=false").AutoRun, Is.False);
        Assert.That(EmbedOptions.Parse(Base + "?autorun=true").AutoRun, Is.True);
    }

    [Test]
    public void Theme_OnlyKnownValues()
    {
        Assert.That(EmbedOptions.Parse(Base + "?theme=dark").Theme, Is.EqualTo("dark"));
        Assert.That(EmbedOptions.Parse(Base + "?theme=light").Theme, Is.EqualTo("light"));
        Assert.That(EmbedOptions.Parse(Base + "?theme=pink").Theme, Is.EqualTo("auto"));
    }

    [Test]
    public void File_IsParsedAndUnescaped()
    {
        Assert.That(EmbedOptions.Parse(Base + "?file=Components%2FCard.razor").File, Is.EqualTo("Components/Card.razor"));
    }

    [Test]
    public void ToQueryString_RoundtripsThroughParse()
    {
        var original = new EmbedOptions
        {
            View = EmbedView.Code,
            File = "Components/Card.razor",
            ReadOnly = true,
            AutoRun = false,
            Theme = "dark",
            HideHeader = true,
        };

        var parsed = EmbedOptions.Parse(Base + "?" + original.ToQueryString());

        Assert.That(parsed, Is.EqualTo(original));
    }

    [Test]
    public void ToQueryString_OmitsDefaults()
    {
        Assert.That(new EmbedOptions().ToQueryString(), Is.Empty);
    }
}
