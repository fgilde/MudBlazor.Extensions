using NUnit.Framework;
using TryMudEx.Client.Services;

namespace Try.Tests;

[TestFixture]
public class CodeFilesHelperTests
{
    [TestCase("__Main.razor", "__Main.razor")]
    [TestCase("Components/Header.razor", "Components/Header.razor")]
    [TestCase(@"Components\Header.razor", "Components/Header.razor")]
    [TestCase("Services/My/Deep/Service.cs", "Services/My/Deep/Service.cs")]
    [TestCase("Header", "Header.razor")]
    [TestCase(" Components/Card.razor ", "Components/Card.razor")]
    public void NormalizeCodeFilePath_ValidPaths(string input, string expected)
    {
        var result = CodeFilesHelper.NormalizeCodeFilePath(input, out var error);
        Assert.That(error, Is.Null);
        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase("../evil.razor")]
    [TestCase("/rooted.razor")]
    [TestCase("a//b.razor")]
    [TestCase("bad-folder/File.razor")]
    [TestCase("Folder/lowercase.razor")]
    [TestCase("Folder/File.txt")]
    [TestCase("")]
    [TestCase(null)]
    public void NormalizeCodeFilePath_InvalidPaths(string input)
    {
        var result = CodeFilesHelper.NormalizeCodeFilePath(input, out var error);
        Assert.That(error, Is.Not.Null.And.Not.Empty);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ValidateCodeFilesForSnippetCreation_AcceptsFolderPaths()
    {
        var files = new[]
        {
            new Try.Core.CodeFile { Path = "__Main.razor", Content = "<h1>hello world</h1>" },
            new Try.Core.CodeFile { Path = "Components/Card.razor", Content = "<div/>" },
        };
        Assert.That(CodeFilesHelper.ValidateCodeFilesForSnippetCreation(files), Is.Null);
    }
}
