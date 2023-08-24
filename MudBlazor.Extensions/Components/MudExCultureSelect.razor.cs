using Microsoft.AspNetCore.Components;
using System.Globalization;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Drop down component to select a culture
/// </summary>
public partial class MudExCultureSelect
{
    /// <summary>
    /// The inherited render
    /// </summary>
    protected RenderFragment Inherited() => builder => base.BuildRenderTree(builder);

    /// <inheritdoc />
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

}