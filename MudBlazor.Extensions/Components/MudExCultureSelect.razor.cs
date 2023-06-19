using Microsoft.AspNetCore.Components;
using System.Globalization;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Drop down component to select a culture
/// </summary>
public partial class MudExCultureSelect
{
    protected RenderFragment Inherited() => builder => base.BuildRenderTree(builder);

    protected override Task<IList<CultureInfo>> GetAvailableItemsAsync(CancellationToken cancellation = default)
    {
        return Task.Run(() =>
        {
            return (IList<CultureInfo>)(CultureInfo.GetCultures(CultureTypes.AllCultures))
                .Where(i => !string.IsNullOrWhiteSpace(i.Name))
                .OrderBy(c => c.DisplayName)
                .ToList();
        }, cancellation);
    }

    protected override void OnInitialized()
    {
        ViewMode = ViewMode.NoChips;
        MultiSelect = false;
        base.OnInitialized();
    }
}