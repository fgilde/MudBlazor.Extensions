namespace MudBlazor.Extensions.Components;

internal enum MudExRibbonGroupState
{
    Full,
    Reduced,
    Compact,
    Collapsed
}

internal static class MudExRibbonReduction
{
    private const int Levels = (int)MudExRibbonGroupState.Collapsed;

    internal static int MaxStep(IReadOnlyCollection<MudExRibbonGroup> groups) => groups.Count * Levels;

    internal static IReadOnlyDictionary<MudExRibbonGroup, MudExRibbonGroupState> Apply(IReadOnlyList<MudExRibbonGroup> groups, int step)
    {
        var count = groups.Count;
        step = Math.Clamp(step, 0, MaxStep(groups));
        return groups
            .Select((group, index) => (group, index))
            .OrderBy(x => x.group.CollapsePriority)
            .ThenByDescending(x => x.index)
            .Select((x, rank) => (x.group, state: (MudExRibbonGroupState)(step / count + (rank < step % count ? 1 : 0))))
            .ToDictionary(x => x.group, x => x.state);
    }

    internal static IReadOnlySet<MudExRibbonItemBase> Overflow(IEnumerable<MudExRibbonGroup> groups, int step)
        => groups.SelectMany(g => g.Items).Where(i => i.Visible).TakeLast(Math.Max(0, step)).ToHashSet();
}
