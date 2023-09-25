namespace TryMudEx.Client.Shared
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Try.Core;
    using Microsoft.AspNetCore.Components;
    using Services;
    using MudBlazor;
    using TryMudEx.Client;

    public partial class MainLayout : LayoutComponentBase, IDisposable
    {
        [Inject] public HttpClient HttpClient { get; set; }
        [Inject] private LayoutService LayoutService { get; set; }

        private MudThemeProvider _mudThemeProvider;

        protected override void OnInitialized()
        {
            LayoutService.MajorUpdateOccured += LayoutServiceOnMajorUpdateOccured;
            base.OnInitialized();
        }
        
        protected override async Task OnInitializedAsync()
        {
            await CompilationService.InitAsync(this.HttpClient);

            await base.OnInitializedAsync();
        }
        
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            
            if (firstRender)
            {
                await ApplyUserPreferences();
                StateHasChanged();
            }
        }
        
        private async Task ApplyUserPreferences()
        {
            var defaultDarkMode = await _mudThemeProvider.GetSystemPreference();
            await LayoutService.ApplyUserPreferences(defaultDarkMode);
        }

        public void Dispose()
        {
            LayoutService.MajorUpdateOccured -= LayoutServiceOnMajorUpdateOccured;
        }

        private void LayoutServiceOnMajorUpdateOccured(object sender, EventArgs e) => StateHasChanged();
    }
}
