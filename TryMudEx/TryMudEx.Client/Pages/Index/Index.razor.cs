using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using TryMudEx.Client.Components;
using TryMudEx.Client.Services;

namespace TryMudEx.Client.Pages.Index;

public partial class Index
{
    [Inject] private SnippetsService SnippetsService { get; set; }
    [Inject] private LayoutService LayoutService { get; set; }
    [Inject] private NavigationManager NavigationManager { get; set; }

    private string _code;
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        
        var data = await SnippetsService.LoadSampleAsync("CardList");
        _code = data.First().Content;
    }

}