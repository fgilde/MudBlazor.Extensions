using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Component to add additional adornment to a MudInput
/// </summary>
public partial class MudExAdditionalAdornment
{
    private int _parentOwnerCountForUsingAsChildContent = 2;
    private string _cls = $"{MudExCss.For<MudExAdditionalAdornment>()}-{Guid.NewGuid().ToFormattedId()}";
    private string[] _selectors = { ".mud-input-adornment", ".mud-input" };

    /// <summary>
    /// Position of the additional adornment
    /// </summary>
    [Parameter] public MoveContentPosition Position { get; set; } = MoveContentPosition.BeforeBegin;
    
    /// <summary>
    /// Child content of the additional adornment
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; }
    
    /// <summary>
    /// MudInput to add the additional adornment to
    /// </summary>
    [Parameter] public RenderFragment For { get; set; }
    
    private string[] Selectors => _selectors.Select(Selector).ToArray();
    private string Selector(string s) => For == null ? s : $".{_cls} >* {s}";
}