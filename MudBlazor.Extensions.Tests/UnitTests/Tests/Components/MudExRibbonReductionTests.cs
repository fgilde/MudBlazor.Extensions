using MudBlazor.Extensions.Components;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

public class MudExRibbonReductionTests
{
    private static MudExRibbonGroup[] Groups(params int[] priorities)
        => priorities.Select((priority, index) => new MudExRibbonGroup { Text = $"G{index}", CollapsePriority = priority }).ToArray();

    private static MudExRibbonGroupState[] States(IReadOnlyList<MudExRibbonGroup> groups, int step)
    {
        var states = MudExRibbonReduction.Apply(groups, step);
        return groups.Select(g => states[g]).ToArray();
    }

    [Fact]
    public void Step0_AllFull()
    {
        var groups = Groups(0, 1, 2);

        Assert.All(States(groups, 0), s => Assert.Equal(MudExRibbonGroupState.Full, s));
        Assert.Empty(MudExRibbonReduction.Apply([], 3));
    }

    [Fact]
    public void Steps_ReduceLowestPriorityFirst_RightToLeftOnTie()
    {
        var groups = Groups(1, 0, 0, 2);
        const MudExRibbonGroupState F = MudExRibbonGroupState.Full, R = MudExRibbonGroupState.Reduced;

        Assert.Equal([F, F, R, F], States(groups, 1));
        Assert.Equal([F, R, R, F], States(groups, 2));
        Assert.Equal([R, R, R, F], States(groups, 3));
        Assert.Equal([R, R, R, R], States(groups, 4));
    }

    [Fact]
    public void NoGroupCompact_BeforeAllReduced()
    {
        var groups = Groups(0, 5, 1);

        for (var step = 0; step <= 9; step++)
        {
            var states = States(groups, step);
            Assert.True(states.Max() - states.Min() <= 1, $"step {step}: {string.Join(",", states)}");
            Assert.Equal(step, states.Sum(s => (int)s));
        }
        Assert.Equal([MudExRibbonGroupState.Compact, MudExRibbonGroupState.Reduced, MudExRibbonGroupState.Reduced], States(groups, 4));
    }

    [Fact]
    public void MaxStep_AllCollapsed_FurtherStepsClamp()
    {
        var groups = Groups(0, 0);

        Assert.Equal(6, MudExRibbonReduction.MaxStep(groups));
        Assert.All(States(groups, 6), s => Assert.Equal(MudExRibbonGroupState.Collapsed, s));
        Assert.Equal(States(groups, 6), States(groups, 100));
        Assert.Equal(States(groups, 0), States(groups, -3));
    }
}
