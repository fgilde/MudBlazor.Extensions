using BlazorJS;
using BlazorJS.Attributes;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Helper.Internal;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components
{

    public partial class MudExDockLayout: IReinitializable
    {
        private string _dockViewPath = "/js/libs/dockview/dist";
        private string DockViewFile(string name, bool absolute = true) => JsImportHelper.JsPath($"{_dockViewPath}{name.EnsureStartsWith("/")}", absolute: absolute);
        private ElementReference _containerRef;

        [Parameter] public string Id { get; set; } = nameof(MudExDockLayout);
        [Parameter] public RenderFragment ChildContent { get; set; }
        [Parameter] public DockTheme Theme { get; set; } = DockTheme.MudBlazor;
        //[Parameter] public DockMode Mode { get; set; } = DockMode.Dock;
        [Parameter] public string ContainerStyle { get; set; } = "height:60vh;width:100%;min-height:320px;";
        [ForJs, Parameter] public string InitialLayoutJson { get; set; }
        [Parameter] public bool HideTabHeaders { get; set; }

        [Parameter] public EventCallback<string> OnPanelAdded { get; set; }
        [Parameter] public EventCallback<string?> OnActiveChanged { get; set; }
        [Parameter] public EventCallback<string> OnPanelRemoved { get; set; }
        [Parameter] public EventCallback<DockviewMovePanelEvent> OnPanelMoved { get; set; }

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

        private string ClassName => MudExCssBuilder.Default.AddClass(Theme.GetDescription).AddClass(Class).ToString();

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

        public override object[] GetJsArguments()
        {
            return new[] { ElementReference, _containerRef, CreateDotNetObjectReference(), JsOptions() };
        }

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
                className = ClassName,
                //mode = Mode.ToString().ToLowerInvariant(),
                initialLayoutJson = InitialLayoutJson
            });
        }

        [JSInvokable] public Task OnJsReady() => Task.CompletedTask;
        [JSInvokable] public Task OnJsPanelAdded(string id) => OnPanelAdded.InvokeAsync(id);
        [JSInvokable] public Task OnJsActiveChanged(string? id) => OnActiveChanged.InvokeAsync(id);
        [JSInvokable] public Task OnJsPanelRemoved(string id) => OnPanelRemoved.InvokeAsync(id);
        [JSInvokable] public Task OnJsPanelMoved(DockviewMovePanelEvent e) => OnPanelMoved.InvokeAsync(e);

        public Task<string> SaveLayoutAsync()
            => JsReference!.InvokeAsync<string>("toJSON").AsTask();

        public Task RestoreLayoutAsync(string json)
            => JsReference!.InvokeVoidAsync("fromJSON", json).AsTask();

        public async Task ReinitializeAsync()
        {
            await JsReference!.InvokeVoidAsync("reinitialize");
        }
    }

    public record DockviewMovePanelEvent(string PanelId, string FromGroupId, string ToGroupId, int ToIndex);
}
