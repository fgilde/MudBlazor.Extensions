using System.Linq;
using NUnit.Framework;
using Playzor.Core;

namespace Try.Tests;

[TestFixture]
public class InlineCodeTests
{
    [Test]
    public void Roundtrip_PreservesFilesAndContent()
    {
        var files = new[]
        {
            new CodeFile { Path = "__Main.razor", Content = "<h1>hello</h1>\n@code { int x = 1; }" },
            new CodeFile { Path = "Components/Card.razor", Content = "<div>card</div>" },
        };

        var decoded = InlineCode.Decode(InlineCode.Encode(files)).ToArray();

        Assert.That(decoded.Select(f => f.Path), Is.EqualTo(new[] { "__Main.razor", "Components/Card.razor" }));
        Assert.That(decoded.Select(f => f.Content), Is.EqualTo(files.Select(f => f.Content)));
    }

    [Test]
    public void Encode_ProducesUrlSafeOutput()
    {
        var encoded = InlineCode.Encode(new[] { new CodeFile { Path = "__Main.razor", Content = "<h1>ünïcödé & <tags>?</tags></h1>" } });

        Assert.That(encoded, Does.Not.Contain("+").And.Not.Contain("/").And.Not.Contain("="));
        Assert.That(InlineCode.Decode(encoded).Single().Content, Is.EqualTo("<h1>ünïcödé & <tags>?</tags></h1>"));
    }

    [Test]
    public void Roundtrip_EmptyContent()
    {
        var decoded = InlineCode.Decode(InlineCode.Encode(new[] { new CodeFile { Path = "Empty.razor", Content = "" } })).Single();

        Assert.That(decoded.Path, Is.EqualTo("Empty.razor"));
        Assert.That(decoded.Content, Is.Empty);
    }
}
