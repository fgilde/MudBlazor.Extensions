using BlazorJS;
using BlazorJS.Attributes;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Helper.Internal;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components
{
    /// <summary>
    /// A Component to quickly select a color
    /// </summary>
    public partial class MudExDockLayout
    {
        private string _dockViewPath = "/js/libs/dockview/dist";
        private string DockViewFile(string name, bool absolute= true) => JsImportHelper.JsPath($"{_dockViewPath}{name.EnsureStartsWith("/")}", absolute: absolute);
        private ElementReference _containerRef;


        [Parameter] public string Id { get; set; } = nameof(MudExDockLayout);
        [Parameter] public RenderFragment ChildContent { get; set; }
        [Parameter] public DockTheme Theme { get; set; } = DockTheme.MudBlazor;
        [Parameter] public string ContainerStyle { get; set; } = "height:60vh;width:100%;min-height:320px;";
        [ForJs, Parameter] public string? InitialLayoutJson { get; set; }
        [Parameter] public bool HideTabHeaders { get; set; }
        // Events (optional)
        [Parameter] public EventCallback<string> OnPanelAdded { get; set; }
        [Parameter] public EventCallback<string?> OnActiveChanged { get; set; }
        [Parameter] public EventCallback<string> OnPanelRemoved { get; set; }

        internal List<MudExDockItem> RootItems { get; } = new();

        internal int RegisterRoot(MudExDockItem item)
        {
            if (!RootItems.Contains(item)) RootItems.Add(item);
            return RootItems.IndexOf(item);
        }

        internal void UnregisterRoot(MudExDockItem item)
        {
            RootItems.Remove(item);
        }

        private string ClassName => MudExCssBuilder.Default.AddClass(Theme.GetDescription()).AddClass(Class).ToString();

        protected override Task OnJsOptionsChanged()
        {
            return UpdateJsOptions();
        }
        
        public override async Task ImportModuleAndCreateJsAsync()
        {
            await JsRuntime.LoadFilesAsync(
                DockViewFile("/styles/dockview.css")
            );

            await base.ImportModuleAndCreateJsAsync();
        }

        /// <summary>
        /// Gets the JavaScript arguments to pass to the component.
        /// </summary>
        public override object[] GetJsArguments()
        {
            
            return new[] { ElementReference, _containerRef, CreateDotNetObjectReference(), JsOptions() };
        }

        /// <summary>
        /// Update JS options
        /// </summary>
        protected async Task UpdateJsOptions()
        {
            if (JsReference != null)
                await JsReference.InvokeVoidAsync("setOptions", JsOptions());
        }

        private object JsOptions()
        {
            return this.AsJsObject(new
            {
                module = DockViewFile("/dockview-core.esm.js", false),
                className = ClassName
            });
        }



        [JSInvokable]
        public async Task OnJsReady()
        {
        }

        [JSInvokable] public Task OnJsPanelAdded(string id) => OnPanelAdded.InvokeAsync(id);
        [JSInvokable] public Task OnJsActiveChanged(string? id) => OnActiveChanged.InvokeAsync(id);
        [JSInvokable] public Task OnJsPanelRemoved(string id) => OnPanelRemoved.InvokeAsync(id);

        public Task<string> SaveLayoutAsync()
            => JsReference!.InvokeAsync<string>("toJSON").AsTask();

        public Task RestoreLayoutAsync(string json)
            => JsReference!.InvokeVoidAsync("fromJSON", json).AsTask();

    }


}