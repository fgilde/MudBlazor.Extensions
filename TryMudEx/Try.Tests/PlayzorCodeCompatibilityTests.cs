using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Playzor.Core;

namespace Try.Tests;

/// <summary>
/// Playzor.Blazor re-implements the url encoder so the package stays dependency free.
/// These tests are the contract between the two implementations — if they drift, shared
/// embed links break.
/// </summary>
[TestFixture]
public class PlayzorCodeCompatibilityTests
{
    [Test]
    public void PackageEncoded_IsReadableByPlayground()
    {
        var files = new Dictionary<string, string>
        {
            ["__Main.razor"] = "<MudText>hello from the package</MudText>",
            ["Components/Card.razor"] = "<div>card</div>",
        };

        var encoded = Playzor.Blazor.PlayzorCode.Encode(files);
        var decoded = InlineCode.Decode(encoded).ToArray();

        Assert.That(decoded.Select(f => f.Path), Is.EqualTo(files.Keys));
        Assert.That(decoded.Select(f => f.Content), Is.EqualTo(files.Values));
    }

    [Test]
    public void PackageEncoded_SingleFileOverload()
    {
        var encoded = Playzor.Blazor.PlayzorCode.Encode("<h1>single</h1>");
        var decoded = InlineCode.Decode(encoded).Single();

        Assert.That(decoded.Path, Is.EqualTo("__Main.razor"));
        Assert.That(decoded.Content, Is.EqualTo("<h1>single</h1>"));
    }

    [Test]
    public void BothEncoders_ProduceIdenticalOutput()
    {
        var codeFiles = new[] { new CodeFile { Path = "__Main.razor", Content = "<p>same bytes please</p>" } };
        var pairs = codeFiles.Select(f => new KeyValuePair<string, string>(f.Path, f.Content));

        Assert.That(Playzor.Blazor.PlayzorCode.Encode(pairs), Is.EqualTo(InlineCode.Encode(codeFiles)));
    }

    /// <summary>
    /// The web component (playzor-embed.js) is a third implementation of the same format, built on
    /// CompressionStream('deflate-raw'). This string was produced by raw deflate in javascript —
    /// if the playground can no longer read it, embeds written in plain html break.
    /// </summary>
    [Test]
    public void WebComponentEncoded_IsReadableByPlayground()
    {
        const string fromJavaScript = "i4_3TczM0ytKrMovkrfJMLbzSM3JyVcIyEmsrMovstHPMLaTd87PLcjPS80rKdZ3TixKgSlOsktOLEqx0U-yAwA";

        var decoded = InlineCode.Decode(fromJavaScript).ToArray();

        Assert.That(decoded.Select(f => f.Path),
            Is.EqualTo(new[] { "__Main.razor", "Components/Card.razor" }));
        Assert.That(decoded.Select(f => f.Content),
            Is.EqualTo(new[] { "<h3>Hello Playzor</h3>", "<b>card</b>" }));
    }
}
