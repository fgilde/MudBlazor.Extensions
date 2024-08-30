using Nextended.Core.Types;

namespace MudBlazor.Extensions.Helper;

public static class ConvertExtensions
{
    public static TreeItemData<T> ToTreeItemData<T>(this T hierarchical, Func<T, string> toText = null) where T : IHierarchical<T>
    {
        return new TreeItemData<T>
        {
            Text = toText != null ? toText(hierarchical) : hierarchical.ToString(),
            Value = hierarchical,
            //Icon = hierarchical.Icon,
            //Expanded = hierarchical.Expanded,
            //Selected = hierarchical.Selected,
            Children = hierarchical.Children?.Select(s => ToTreeItemData(hierarchical, toText)).ToList()
        };
    }

    public static IReadOnlyCollection<TreeItemData<T>> ToTreeItemData<T>(this IReadOnlyCollection<T> items, Func<T, string> toText = null) where T : IHierarchical<T>
    {
        return items.Select(s => ToTreeItemData(s, toText)).ToHashSet();
    }
}