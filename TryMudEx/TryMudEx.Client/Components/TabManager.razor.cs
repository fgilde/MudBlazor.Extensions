using Try.Core;

namespace TryMudEx.Client.Components
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using TryMudEx.Client.Services;
    using Microsoft.AspNetCore.Components;
    using MudBlazor;
    using TryMudEx.Client.Enums;
    using Microsoft.AspNetCore.Components.Web;

    public partial class TabManager
    {
        private const int DefaultActiveIndex = 0;

        private bool _tabCreating;
        private bool _shouldFocusNewTabInput;
        private string _newTab;
        private ElementReference _newTabInput;
        private string _previousInvalidTab;
        private CodeViewMode _viewMode;

        [Parameter]
        public IList<string> Tabs { get; set; }

        [Parameter]
        public EventCallback<string> OnTabActivate { get; set; }

        [Parameter]
        public EventCallback<string> OnTabClose { get; set; }

        [Parameter]
        public EventCallback<string> OnTabCreate { get; set; }

        [Parameter]
        public EventCallback<CodeFile> OnTemplateCreate { get; set; }

        [Parameter]
        public RenderFragment Tools { get; set; }

        [Parameter]
        public CodeViewMode ViewMode
        {
            get => _viewMode;
            set
            {
                if (_viewMode != value)
                {
                    _viewMode = value;
                    ViewModeChanged.InvokeAsync(value);
                }
            }
        }

        [Parameter]
        public EventCallback<CodeViewMode> ViewModeChanged { get; set; }

        [Inject]
        public ISnackbar Snackbar { get; set; }

        [Parameter]
        public int ActiveIndex { get; set; } = DefaultActiveIndex;

        [Parameter]
        public EventCallback<int> ActiveIndexChanged { get; set; }

        private string TabCreatingDisplayStyle => _tabCreating ? string.Empty : "display: none;";

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (_shouldFocusNewTabInput)
            {
                _shouldFocusNewTabInput = false;

                await _newTabInput.FocusAsync();
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        private Color ViewModeColor(CodeViewMode mode)
        {
            return mode == ViewMode ? Color.Warning : Color.Default;
        }

        private Task ActivateTabAsync(int activeIndex)
        {
            if (activeIndex < 0 || activeIndex >= Tabs.Count)
            {
                return Task.CompletedTask;
            }

            ActiveIndex = activeIndex;
            ActiveIndexChanged.InvokeAsync(activeIndex);
            return OnTabActivate.InvokeAsync(Tabs[activeIndex]);
        }

        private async Task CloseTabAsync(int index)
        {
            if (index < 0 || index >= Tabs.Count)
            {
                return;
            }

            if (index == DefaultActiveIndex)
            {
                return;
            }

            var tab = Tabs[index];
            Tabs.RemoveAt(index);

            await OnTabClose.InvokeAsync(tab);

            if (index == ActiveIndex)
            {
                await ActivateTabAsync(DefaultActiveIndex);
            }
        }

        private void InitTabCreating()
        {
            _tabCreating = true;
            _shouldFocusNewTabInput = true;
        }

        private void TerminateTabCreating()
        {
            _tabCreating = false;
            _newTab = null;
        }
        private void OnTabCreateInputKeyDown(KeyboardEventArgs args)
        {
            if (args.Key == "Escape")
            {
                _newTab = string.Empty;
                TerminateTabCreating();
            }
        }

        private async Task CreateTabAsync()
        {
            if (string.IsNullOrWhiteSpace(_newTab))
            {
                TerminateTabCreating();
                return;
            }

            var normalizedTab = CodeFilesHelper.NormalizeCodeFilePath(_newTab, out var error);
            if (Tabs.Contains(normalizedTab))
            {
                Snackbar.Add("This file already exists!", Severity.Warning);
                await _newTabInput.FocusAsync();
                return;
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                if (_previousInvalidTab != _newTab)
                {
                    Snackbar.Add(error, Severity.Warning);
                    _previousInvalidTab = _newTab;
                }

                await _newTabInput.FocusAsync();
                return;
            }

            _previousInvalidTab = null;

            Tabs.Add(normalizedTab);

            TerminateTabCreating();
            var newTabIndex = Tabs.Count - 1;

            await OnTabCreate.InvokeAsync(normalizedTab);

            await ActivateTabAsync(newTabIndex);
        }

        CodeViewMode? _lastModeBeforeAutoChanged = null;
        private void CheckHidden(bool hidden, CodeViewMode mode)
        {
            if (hidden && _viewMode == mode)
            {
                _lastModeBeforeAutoChanged = mode;
                ViewMode = CodeViewMode.DockedBottom;
            }

            if (!hidden && _lastModeBeforeAutoChanged != null)
            {
                ViewMode = _lastModeBeforeAutoChanged.Value;
                _lastModeBeforeAutoChanged = null;
            }
        }

        private async Task CreateFromTemplate(CodeFile file)
        {
            Tabs.Add(file.Path);
            await OnTemplateCreate.InvokeAsync(file);
        }
    }
}
