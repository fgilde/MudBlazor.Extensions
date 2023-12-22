using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Helper;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

public partial class MudExAdditionalAdornment
{
    private int _parentOwnerCountForUsingAsChildContent = 2;
    private string _cls = $"{MudExCss.For<MudExAdditionalAdornment>()}-{Guid.NewGuid().ToFormattedId()}";
    private string[] _selectors = { ".mud-input-adornment", ".mud-input" };

    [Parameter] public MoveContentPosition Position { get; set; } = MoveContentPosition.BeforeBegin;
    [Parameter] public RenderFragment ChildContent { get; set; }
    [Parameter] public RenderFragment For { get; set; }
    
    private string[] Selectors => _selectors.Select(Selector).ToArray();
    private string Selector(string s) => For == null ? s : $".{_cls} >* {s}";
}