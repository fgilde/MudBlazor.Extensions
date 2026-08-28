using System;
using System.Linq;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Helper;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Helper;

/// <summary>
/// Guards the tag list that Samples/MudEx.WebComponents registers as browser custom elements.
/// </summary>
public class MudExWebComponentsTests
{
    private static readonly System.Reflection.Assembly LibraryAssembly = typeof(MudExFileDisplay).Assembly;

    [Fact]
    public void RegistrableComponentsContainTheFileDisplay()
    {
        var tags = MudExWebComponents.GetRegistrableComponents(LibraryAssembly);

        Assert.Contains(tags, t => t.Type == typeof(MudExFileDisplay) && t.Tag == "mudex-file-display");
    }

    [Fact]
    public void TagsAreValidCustomElementNamesAndUnique()
    {
        var tags = MudExWebComponents.GetRegistrableComponents(LibraryAssembly);

        Assert.NotEmpty(tags);
        Assert.Equal(tags.Select(t => t.Tag).Distinct().Count(), tags.Count);
        foreach (var (type, tag) in tags)
        {
            // a custom element name must be lowercase and contain a dash
            Assert.True(tag.Contains('-'), $"{type.Name} produced the invalid tag '{tag}'");
            Assert.Equal(tag.ToLowerInvariant(), tag);
            Assert.StartsWith(MudExWebComponents.TagPrefix, tag);
        }
    }

    [Fact]
    public void GenericComponentsAreSkippedWithAReason()
    {
        var skipped = MudExWebComponents.GetSkippedComponents(LibraryAssembly);
        var tags = MudExWebComponents.GetRegistrableComponents(LibraryAssembly);

        Assert.Contains(skipped, s => s.Type.IsGenericTypeDefinition && s.Reason == "generic component");
        Assert.All(skipped, s => Assert.DoesNotContain(tags, t => t.Type == s.Type));
    }

    [Theory]
    [InlineData(typeof(MudExFileDisplay), "mudex-file-display")]
    [InlineData(typeof(MudExFileDisplayExcelUniver), "mudex-file-display-excel-univer")]
    [InlineData(typeof(MoveContent), "mudex-move-content")]
    public void TagNameFollowsTheDocumentedScheme(Type type, string expected)
        => Assert.Equal(expected, MudExWebComponents.TagName(type));

    [Fact]
    public void SetAssetBaseCanBeReset()
    {
        try
        {
            MudExWebComponents.SetAssetBase("https://cdn.example.org/wc/");
            Assert.Equal("https://cdn.example.org/wc/_content/MudBlazor.Extensions/js/x.js",
                JsImportHelperAccess.JsPath("/js/x.js"));
        }
        finally
        {
            MudExWebComponents.SetAssetBase(null);
        }

        Assert.Equal("./_content/MudBlazor.Extensions/js/x.js", JsImportHelperAccess.JsPath("/js/x.js"));
    }

    /// <summary>
    /// JsImportHelper.JsPath is internal, this keeps the reflection in one place.
    /// </summary>
    private static class JsImportHelperAccess
    {
        public static string JsPath(string path)
        {
            var method = typeof(JsImportHelper).GetMethod("JsPath",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(method);
            return (string)method!.Invoke(null, new object[] { path, "MudBlazor.Extensions", true })!;
        }
    }
}
