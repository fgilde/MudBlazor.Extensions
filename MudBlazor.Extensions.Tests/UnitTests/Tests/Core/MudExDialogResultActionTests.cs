using MudBlazor.Extensions;
using Xunit;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Core;

public class MudExDialogResultActionTests
{
    [Fact]
    public void OkCreatesOkAction()
    {
        var actions = MudExDialogResultAction.Ok();
        
        Assert.Single(actions);
        Assert.Equal("Ok", actions[0].Label);
        Assert.True(actions[0].Result.Data is bool && (bool)actions[0].Result.Data);
    }

    [Fact]
    public void OkWithCustomTextCreatesOkActionWithCustomLabel()
    {
        var actions = MudExDialogResultAction.Ok("Confirm");
        
        Assert.Single(actions);
        Assert.Equal("Confirm", actions[0].Label);
    }

    [Fact]
    public void CancelCreatesCancelAction()
    {
        var actions = MudExDialogResultAction.Cancel();
        
        Assert.Single(actions);
        Assert.True(actions[0].Result.Canceled);
    }

    [Fact]
    public void CancelWithCustomTextCreatesCancelActionWithCustomLabel()
    {
        var actions = MudExDialogResultAction.Cancel("Abort");
        
        Assert.Single(actions);
        Assert.Equal("Abort", actions[0].Label);
    }

    [Fact]
    public void OkCancelCreatesTwoActions()
    {
        var actions = MudExDialogResultAction.OkCancel();
        
        Assert.Equal(2, actions.Length);
        Assert.True(actions[0].Result.Canceled);
        Assert.True(actions[1].Result.Data is bool && (bool)actions[1].Result.Data);
    }

    [Fact]
    public void OkCancelWithCustomTextCreatesActionsWithCustomLabels()
    {
        var actions = MudExDialogResultAction.OkCancel("Save", "Discard");
        
        Assert.Equal(2, actions.Length);
        Assert.Equal("Discard", actions[0].Label);
        Assert.Equal("Save", actions[1].Label);
    }

    [Fact]
    public void OkActionHasCorrectDefaultProperties()
    {
        var actions = MudExDialogResultAction.Ok();
        var okAction = actions[0];
        
        Assert.Equal(Color.Error, okAction.Color);
        Assert.Equal(Variant.Filled, okAction.Variant);
    }

    [Fact]
    public void CancelActionHasCorrectDefaultProperties()
    {
        var actions = MudExDialogResultAction.OkCancel();
        var cancelAction = actions[0];
        
        Assert.Equal(Variant.Text, cancelAction.Variant);
    }

    [Fact]
    public void WithConditionSetsConditionFunction()
    {
        var action = new MudExDialogResultAction();
        action.WithCondition<TestComponent>(c => c.IsValid);
        
        Assert.True(action.HasCondition);
    }

    [Fact]
    public void HasConditionReturnsFalseWhenNoConditionSet()
    {
        var action = new MudExDialogResultAction();
        
        Assert.False(action.HasCondition);
    }

    [Fact]
    public void HasConditionReturnsTrueWhenConditionSet()
    {
        var action = new MudExDialogResultAction();
        action.WithCondition<TestComponent>(c => true);
        
        Assert.True(action.HasCondition);
    }

    [Fact]
    public void OnClickCanBeSet()
    {
        var clicked = false;
        var action = new MudExDialogResultAction
        {
            OnClick = () => clicked = true
        };
        
        action.OnClick();
        
        Assert.True(clicked);
    }

    [Fact]
    public void ResultCanBeSet()
    {
        var action = new MudExDialogResultAction
        {
            Result = DialogResult.Ok("test")
        };
        
        Assert.Equal("test", action.Result.Data);
    }

    [Fact]
    public void LabelCanBeSet()
    {
        var action = new MudExDialogResultAction
        {
            Label = "Custom Label"
        };
        
        Assert.Equal("Custom Label", action.Label);
    }

    [Fact]
    public void VariantCanBeSet()
    {
        var action = new MudExDialogResultAction
        {
            Variant = Variant.Outlined
        };
        
        Assert.Equal(Variant.Outlined, action.Variant);
    }

    [Fact]
    public void ColorCanBeSet()
    {
        var action = new MudExDialogResultAction
        {
            Color = Color.Primary
        };
        
        Assert.Equal(Color.Primary, action.Color);
    }

    private class TestComponent : Microsoft.AspNetCore.Components.ComponentBase
    {
        public bool IsValid { get; set; }
    }
}
