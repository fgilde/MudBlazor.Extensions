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

        /// <summary>
        /// Document a popped out panel is hosted in. Must be served from the same origin, dockview
        /// copies the stylesheets into it. Used by <see cref="MudExDockItem.CanPopout"/>.
        /// </summary>
        [ForJs, Parameter] public string PopoutUrl { get; set; } = "/popout.html";

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

        internal async void NotifyItemDisposed(string id)
        {
            try
            {
                if (JsReference != null && !string.IsNullOrEmpty(id))
                    await JsReference.InvokeVoidAsync("removePanelById", id);
            }
            catch { /* layout itself may be tearing down */ }
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
                initialLayoutJson = InitialLayoutJson,
                popoutTitle = TryLocalize("Open in new window"),
                popoutBackTitle = TryLocalize("Move back into the layout")
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

        /// <summary>Adds a panel at runtime. Options JSON uses the same shape as MudExDockItem's data-options (id, title, direction, stackWith, ...).</summary>
        public Task AddPanelAsync(string optionsJson)
            => JsReference!.InvokeVoidAsync("addPanelByOptions", optionsJson).AsTask();

        public Task RemovePanelAsync(string id)
            => JsReference!.InvokeVoidAsync("removePanelById", id).AsTask();

        /// <summary>
        /// Activates a panel. <paramref name="highlight"/> glows its border for a moment, which is
        /// worth it whenever something other than the user's click caused the activation.
        /// </summary>
        public Task ActivatePanelAsync(string id, bool highlight = false)
            => JsReference!.InvokeVoidAsync("activatePanel", id, highlight).AsTask();

        /// <summary>Glows the panel border for a moment without changing the active panel.</summary>
        public Task HighlightPanelAsync(string id)
            => JsReference!.InvokeVoidAsync("highlightPanel", id).AsTask();

        public Task FloatPanelAsync(string id)
            => JsReference!.InvokeVoidAsync("floatPanel", id).AsTask();

        /// <summary>Ids of all panels dockview currently holds (open panels).</summary>
        public Task<string[]> GetPanelIdsAsync()
            => JsReference!.InvokeAsync<string[]>("getPanelIds").AsTask();

        /// <summary>
        /// Moves the panel into a real browser window. Needs a host page (default /popout.html)
        /// that is served from the same origin; dockview copies the stylesheets into it.
        /// </summary>
        public Task<bool> PopoutPanelAsync(string id, string popoutUrl = null)
            => JsReference!.InvokeAsync<bool>("popoutPanel", id, popoutUrl).AsTask();

        public Task<bool> IsPopoutAsync(string id)
            => JsReference!.InvokeAsync<bool>("isPopout", id).AsTask();

        /// <summary>Brings a popped out panel back into the layout.</summary>
        public Task<bool> ReturnPanelAsync(string id)
            => JsReference!.InvokeAsync<bool>("returnPanel", id).AsTask();

        public Task MaximizePanelAsync(string id)
            => JsReference!.InvokeVoidAsync("maximizePanel", id).AsTask();

        public Task ExitMaximizedAsync()
            => JsReference!.InvokeVoidAsync("exitMaximized").AsTask();

        public async Task ReinitializeAsync()
        {
            await JsReference!.InvokeVoidAsync("reinitialize");
        }
    }

    public record DockviewMovePanelEvent(string PanelId, string FromGroupId, string ToGroupId, int ToIndex);
}
