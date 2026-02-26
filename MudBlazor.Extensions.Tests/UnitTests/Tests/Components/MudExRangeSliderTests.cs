using Bunit;
using MudBlazor.Extensions.Components;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Types;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

public class MudExRangeSliderTests
{
    /// <summary>
    /// Test for the issue where MinLength constraint is not properly enforced when dragging thumbs.
    /// When dragging the start thumb towards the end with MinLength constraint,
    /// it should not allow the start to violate the minimum span.
    /// 
    /// Configuration:
    /// - Min = 0, Max = 10
    /// - Initial start = 4, Initial end = 10
    /// - Min span = 4
    /// - Step size = 2
    /// 
    /// Expected: Start thumb should not be able to go beyond 6 (since end is 10 and min span is 4)
    /// </summary>
    [Fact]
    public void StartThumb_ShouldRespectMinLength_WhenDraggingTowardsEnd()
    {
        // Arrange
        using var context = new TestContext();
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        
        var minValue = 0;
        var maxValue = 10;
        var minSpan = 4;
        var stepSize = 2;
        var initialStart = 4;
        var initialEnd = 10;

        var sizeRange = new MudExRange<int>(minValue, maxValue);
        var stepLength = new RangeLength<int>(stepSize);
        var minLength = new RangeLength<int>(minSpan);
        var initialValue = new MudExRange<int>(initialStart, initialEnd);

        var cut = context.Render<MudExRangeSlider<int>>(parameters => parameters
            .Add(p => p.SizeRange, sizeRange)
            .Add(p => p.StepLength, stepLength)
            .Add(p => p.MinLength, minLength)
            .Add(p => p.Value, initialValue)
        );

        // Act - Simulate setting the start value to 8 (which would violate min span)
        // This simulates dragging the start thumb to position 8
        var instance = cut.Instance;
        
        // Use reflection to call the private SetStartAsync method
        var setStartMethod = typeof(MudExRangeSlider<int>).GetMethod("SetStartAsync", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // Try to set start to 8 (should be clamped to 6 due to min span)
        setStartMethod?.Invoke(instance, new object[] { 8, true });
        
        // Assert
        // With end at 10 and min span of 4, start should be at most 6
        var maxAllowedStart = initialEnd - minSpan; // 10 - 4 = 6
        
        Assert.True(instance.Value.Start <= maxAllowedStart, 
            $"Start value {instance.Value.Start} should not exceed {maxAllowedStart} when end is {instance.Value.End} and min span is {minSpan}");
        
        // The start should be 6 (the maximum allowed value)
        Assert.Equal(6, instance.Value.Start);
        Assert.Equal(10, instance.Value.End);
    }

    /// <summary>
    /// Test that the end thumb respects MinLength constraint when dragging towards start.
    /// </summary>
    [Fact]
    public void EndThumb_ShouldRespectMinLength_WhenDraggingTowardsStart()
    {
        // Arrange
        using var context = new TestContext();
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        
        var minValue = 0;
        var maxValue = 10;
        var minSpan = 4;
        var stepSize = 2;
        var initialStart = 0;
        var initialEnd = 6;

        var sizeRange = new MudExRange<int>(minValue, maxValue);
        var stepLength = new RangeLength<int>(stepSize);
        var minLength = new RangeLength<int>(minSpan);
        var initialValue = new MudExRange<int>(initialStart, initialEnd);

        var cut = context.Render<MudExRangeSlider<int>>(parameters => parameters
            .Add(p => p.SizeRange, sizeRange)
            .Add(p => p.StepLength, stepLength)
            .Add(p => p.MinLength, minLength)
            .Add(p => p.Value, initialValue)
        );

        // Act - Simulate setting the end value to 2 (which would violate min span)
        var instance = cut.Instance;
        
        var setEndMethod = typeof(MudExRangeSlider<int>).GetMethod("SetEndAsync", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // Try to set end to 2 (should be clamped to 4 due to min span)
        setEndMethod?.Invoke(instance, new object[] { 2, true });
        
        // Assert
        // With start at 0 and min span of 4, end should be at least 4
        var minAllowedEnd = initialStart + minSpan; // 0 + 4 = 4
        
        Assert.True(instance.Value.End >= minAllowedEnd, 
            $"End value {instance.Value.End} should not be less than {minAllowedEnd} when start is {instance.Value.Start} and min span is {minSpan}");
        
        // The end should be 4 (the minimum allowed value)
        Assert.Equal(0, instance.Value.Start);
        Assert.Equal(4, instance.Value.End);
    }

    /// <summary>
    /// Test that with no MinLength constraint, thumbs can move freely.
    /// </summary>
    [Fact]
    public void WithoutMinLength_ThumbsShouldMoveFreelyWithinSizeRange()
    {
        // Arrange
        using var context = new TestContext();
        context.Services.AddMudServicesWithExtensions();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        
        var minValue = 0;
        var maxValue = 10;
        var stepSize = 1;
        var initialStart = 5;
        var initialEnd = 8;

        var sizeRange = new MudExRange<int>(minValue, maxValue);
        var stepLength = new RangeLength<int>(stepSize);
        var initialValue = new MudExRange<int>(initialStart, initialEnd);

        var cut = context.Render<MudExRangeSlider<int>>(parameters => parameters
            .Add(p => p.SizeRange, sizeRange)
            .Add(p => p.StepLength, stepLength)
            .Add(p => p.Value, initialValue)
        );

        // Act
        var instance = cut.Instance;
        
        var setStartMethod = typeof(MudExRangeSlider<int>).GetMethod("SetStartAsync", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // Should be able to set start all the way to end
        setStartMethod?.Invoke(instance, new object[] { 8, true });
        
        // Assert
        Assert.Equal(8, instance.Value.Start);
        Assert.Equal(8, instance.Value.End);
    }
}
