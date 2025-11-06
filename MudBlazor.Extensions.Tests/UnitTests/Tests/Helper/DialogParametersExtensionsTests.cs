using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Helper;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Helper;

public class DialogParametersExtensionsTests
{
    [Fact]
    public void MergeWithCombinesTwoDialogParameters()
    {
        var params1 = new DialogParameters { { "key1", "value1" } };
        var params2 = new DialogParameters { { "key2", "value2" } };
        
        var result = params1.MergeWith(params2);
        
        Assert.Equal(2, result.Count);
        Assert.Equal("value1", result.Get<string>("key1"));
        Assert.Equal("value2", result.Get<string>("key2"));
    }

    [Fact]
    public void MergeWithHandlesNullFirstParameter()
    {
        DialogParameters params1 = null;
        var params2 = new DialogParameters { { "key2", "value2" } };
        
        var result = params1.MergeWith(params2);
        
        Assert.Single(result);
        Assert.Equal("value2", result.Get<string>("key2"));
    }

    [Fact]
    public void MergeWithOverwritesExistingKeys()
    {
        var params1 = new DialogParameters { { "key1", "value1" } };
        var params2 = new DialogParameters { { "key1", "newValue1" } };
        
        var result = params1.MergeWith(params2);
        
        Assert.Equal("value1", result.Get<string>("key1"));
    }

    [Fact]
    public void ToDialogParametersConvertsKeyValuePairs()
    {
        var kvps = new List<KeyValuePair<string, object>>
        {
            new KeyValuePair<string, object>("key1", "value1"),
            new KeyValuePair<string, object>("key2", 42)
        };
        
        var result = kvps.ToDialogParameters();
        
        Assert.Equal(2, result.Count);
        Assert.Equal("value1", result.Get<string>("key1"));
        Assert.Equal(42, result.Get<int>("key2"));
    }

    [Fact]
    public void ToDialogParametersHandlesEmptyCollection()
    {
        var kvps = new List<KeyValuePair<string, object>>();
        
        var result = kvps.ToDialogParameters();
        
        Assert.Empty(result);
    }

    [Fact]
    public void ConvertToDialogParametersFromObjectCreatesDialogParameters()
    {
        var testObj = new TestDialogModel { Title = "Test", Message = "Hello" };
        
        var result = testObj.ConvertToDialogParameters();
        
        Assert.NotEmpty(result);
        Assert.Equal("Test", result.Get<string>("Title"));
        Assert.Equal("Hello", result.Get<string>("Message"));
    }

    [Fact]
    public void ConvertToDialogParametersFromActionCreatesDialogParameters()
    {
        var result = DialogParametersExtensions.ConvertToDialogParameters<TestDialogModel>(
            x =>
            {
                x.Title = "Test Action";
                x.Message = "Action Message";
            });
        
        Assert.NotEmpty(result);
        Assert.Equal("Test Action", result.Get<string>("Title"));
        Assert.Equal("Action Message", result.Get<string>("Message"));
    }

    [Fact]
    public void ConvertToDialogParametersIgnoresReadOnlyProperties()
    {
        var testObj = new TestDialogModelWithReadOnly { Title = "Test" };
        
        var result = testObj.ConvertToDialogParameters();
        
        // ReadOnlyProp should be excluded as it's read-only
        Assert.DoesNotContain("ReadOnlyProp", result.Select(p => p.Key));
    }

    private class TestDialogModel: IComponent
    {
        [Parameter]
        public string Title { get; set; }
        [Parameter]
        public string Message { get; set; }
        public void Attach(RenderHandle renderHandle)
        {
            
        }

        public Task SetParametersAsync(ParameterView parameters)
        {
            return Task.CompletedTask;
        }
    }

    private class TestDialogModelWithReadOnly
    {
        public string Title { get; set; }
        public string ReadOnlyProp => "ReadOnly";
    }
}
