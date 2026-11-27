using MudBlazor.Extensions.Components.ObjectEdit.Options;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

/// <summary>
/// An indexer is a property for reflection, but it has no value without an index. Reading it threw and took
/// down the property grid of the spreadsheet playground (StyleTable has one).
/// </summary>
public class ObjectEditIndexerTests
{
    private class Table
    {
        public int Count { get; set; } = 2;
        public string this[int index] => index.ToString();
    }

    [Fact]
    public void IndexersAreLeftOutAndEveryValueCanBeRead()
    {
        var meta = new ObjectEditMeta<Table>(new Table());

        Assert.DoesNotContain(meta.AllProperties, p => p.PropertyInfo.GetIndexParameters().Length > 0);
        Assert.Contains(meta.AllProperties, p => p.PropertyName == nameof(Table.Count));
        foreach (var property in meta.AllProperties)
            _ = property.Value;
    }
}
