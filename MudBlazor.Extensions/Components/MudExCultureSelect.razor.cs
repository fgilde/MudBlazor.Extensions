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

    /// <summary>
    /// Set how to handle neutral cultures
    /// </summary>
    [Parameter] public NeutralCultureHandling CultureHandling { get; set; } = NeutralCultureHandling.AllowAllCultures;

    /// <summary>
    /// Set to true to ignore invariant culture
    /// </summary>
    [Parameter] public bool IgnoreInvariant { get; set; }

    /// <inheritdoc />
    protected override Task<IList<CultureInfo>> GetAvailableItemsAsync(CancellationToken cancellation = default)
    {
        return Task.Run(() =>
        {
            return (IList<CultureInfo>)(CultureInfo.GetCultures(CultureTypes.AllCultures))
                .Where(i => !string.IsNullOrWhiteSpace(i.Name) 
                    && (!IgnoreInvariant || !Equals(i, CultureInfo.InvariantCulture))
                    && CultureHandling == NeutralCultureHandling.AllowAllCultures 
                    || (i.IsNeutralCulture && CultureHandling == NeutralCultureHandling.IgnoreNonNeutralCultures) 
                    || (!i.IsNeutralCulture && CultureHandling == NeutralCultureHandling.IgnoreNeutralCultures) 
                    )
                .OrderBy(c => c.DisplayName)
                .ToList();
        }, cancellation);
    }

    public override Func<CultureInfo, string> ToStringFunc { get; set; } = i => i?.DisplayName;


    protected override Task OnInitializedAsync()
    {
        Virtualize = true;
        return base.OnInitializedAsync();
    }

}

public enum NeutralCultureHandling
{
    AllowAllCultures,
    IgnoreNonNeutralCultures,
    IgnoreNeutralCultures,
}
