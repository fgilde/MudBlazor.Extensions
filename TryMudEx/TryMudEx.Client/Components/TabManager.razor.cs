﻿namespace TryMudEx.Client.Components
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using TryMudEx.Client.Services;
    using Microsoft.AspNetCore.Components;
    using MudBlazor;

    public partial class TabManager
    {
        private const int DefaultActiveIndex = 0;

        private bool _tabCreating;
        private bool _shouldFocusNewTabInput;
        private string _newTab;
        private ElementReference _newTabInput;
        private string _previousInvalidTab;

        [Parameter]
        public IList<string> Tabs { get; set; }

        [Parameter]
        public EventCallback<string> OnTabActivate { get; set; }

        [Parameter]
        public EventCallback<string> OnTabClose { get; set; }

        [Parameter]
        public EventCallback<string> OnTabCreate { get; set; }

        [Inject]
        public ISnackbar Snackbar { get; set; }

        private int ActiveIndex { get; set; } = DefaultActiveIndex;

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

        private Task ActivateTabAsync(int activeIndex)
        {
            if (activeIndex < 0 || activeIndex >= Tabs.Count)
            {
                return Task.CompletedTask;
            }

            ActiveIndex = activeIndex;

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
    }
}
