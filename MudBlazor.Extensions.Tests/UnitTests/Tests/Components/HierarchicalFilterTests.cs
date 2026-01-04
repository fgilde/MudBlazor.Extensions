using MudBlazor.Extensions.Components;
using Nextended.Core.Types;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

public class HierarchicalFilterTests
{
    private class TestItem : Hierarchical<TestItem>
    {
        public string Name { get; set; }
        
        public TestItem() { }
        
        public TestItem(string name)
        {
            Name = name;
        }
        
        public override string ToString() => Name;
    }

    [Fact]
    public void GetMatchedSearch_CachesResults()
    {
        // Arrange
        var filter = new HierarchicalFilter<TestItem>
        {
            Filter = "test",
            FilterBehaviour = HierarchicalFilterBehaviour.Default
        };
        
        var item = new TestItem("test item");
        
        // Act - Call twice to test caching
        var result1 = filter.GetMatchedSearch(item);
        var result2 = filter.GetMatchedSearch(item);
        
        // Assert
        Assert.True(result1.Found);
        Assert.True(result2.Found);
        Assert.Equal(result1.Term, result2.Term);
    }

    [Fact]
    public void GetMatchedSearch_ReturnsCorrectResultForMatchingItem()
    {
        // Arrange
        var filter = new HierarchicalFilter<TestItem>
        {
            Filter = "match",
            FilterBehaviour = HierarchicalFilterBehaviour.Default
        };
        
        var matchingItem = new TestItem("matching item");
        var nonMatchingItem = new TestItem("other");
        
        // Act
        var matchResult = filter.GetMatchedSearch(matchingItem);
        var nonMatchResult = filter.GetMatchedSearch(nonMatchingItem);
        
        // Assert
        Assert.True(matchResult.Found);
        Assert.Equal("match", matchResult.Term);
        Assert.False(nonMatchResult.Found);
    }

    [Fact]
    public void GetMatchedSearch_HandlesChildrenRecursively()
    {
        // Arrange
        var filter = new HierarchicalFilter<TestItem>
        {
            Filter = "child",
            FilterBehaviour = HierarchicalFilterBehaviour.Default
        };
        
        var parent = new TestItem("parent")
        {
            Children = new HashSet<TestItem>
            {
                new TestItem("child item")
            }
        };
        
        // Act
        var result = filter.GetMatchedSearch(parent);
        
        // Assert
        Assert.True(result.Found);
    }

    [Fact]
    public void FilteredItems_CachesResults()
    {
        // Arrange
        var items = new HashSet<TestItem>
        {
            new TestItem("item1"),
            new TestItem("item2"),
            new TestItem("test item")
        };
        
        var filter = new HierarchicalFilter<TestItem>
        {
            Items = items,
            Filter = "test",
            FilterBehaviour = HierarchicalFilterBehaviour.Flat
        };
        
        // Act - Call twice to test caching
        var result1 = filter.FilteredItems();
        var result2 = filter.FilteredItems();
        
        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Same(result1, result2); // Should return same cached instance
    }

    [Fact]
    public void CacheInvalidatesOnFilterChange()
    {
        // Arrange
        var items = new HashSet<TestItem>
        {
            new TestItem("test1"),
            new TestItem("test2"),
            new TestItem("other")
        };
        
        var filter = new HierarchicalFilter<TestItem>
        {
            Items = items,
            FilterBehaviour = HierarchicalFilterBehaviour.Flat
        };
        
        // Act
        filter.Filter = "test";
        var result1 = filter.FilteredItems();
        
        filter.Filter = "other";
        var result2 = filter.FilteredItems();
        
        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotSame(result1, result2); // Should be different instances due to cache invalidation
        Assert.Equal(2, result1.Count);
        Assert.Single(result2);
    }

    [Fact]
    public void GetMatchedSearch_ReturnsFoundForFlatBehaviour()
    {
        // Arrange
        var filter = new HierarchicalFilter<TestItem>
        {
            FilterBehaviour = HierarchicalFilterBehaviour.Flat
        };
        
        var item = new TestItem("any item");
        
        // Act
        var result = filter.GetMatchedSearch(item);
        
        // Assert
        Assert.True(result.Found);
        Assert.Empty(result.Term);
    }

    [Fact]
    public void GetMatchedSearch_HandlesMultipleFilters()
    {
        // Arrange
        var filter = new HierarchicalFilter<TestItem>
        {
            Filters = new List<string> { "one", "two", "three" },
            FilterBehaviour = HierarchicalFilterBehaviour.Default
        };
        
        var item1 = new TestItem("one item");
        var item2 = new TestItem("two item");
        var item3 = new TestItem("nomatch");
        
        // Act
        var result1 = filter.GetMatchedSearch(item1);
        var result2 = filter.GetMatchedSearch(item2);
        var result3 = filter.GetMatchedSearch(item3);
        
        // Assert
        Assert.True(result1.Found);
        Assert.True(result2.Found);
        Assert.False(result3.Found);
    }
}
