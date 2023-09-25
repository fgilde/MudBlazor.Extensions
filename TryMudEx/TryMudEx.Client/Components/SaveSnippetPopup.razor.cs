namespace TryMudEx.Client.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using TryMudEx.Client.Services;
    using TryMudEx.Client.Models;
    using Try.Core;
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;
    using MudBlazor;

    public partial class SaveSnippetPopup
    {
        [Inject]
        public ISnackbar Snackbar { get; set; }

        [Inject]
        protected IJsApiService JsApiService { get; set; }

        [Inject]
        public IJSInProcessRuntime JsRuntime { get; set; }

        [Inject]
        public SnippetsService SnippetsService { get; set; }

        [Inject]
        public NavigationManager NavigationManager { get; set; }

        [Parameter]
        public bool Visible { get; set; }

        [Parameter]
        public EventCallback<bool> VisibleChanged { get; set; }

        [Parameter]
        public IEnumerable<CodeFile> CodeFiles { get; set; } = Enumerable.Empty<CodeFile>();

        [Parameter]
        public Action UpdateActiveCodeFileContentAction { get; set; }

        private bool Loading { get; set; }
        private string SnippetLink { get; set; }

        private async Task CopyLinkToClipboard()
        {
            await JsApiService.CopyToClipboardAsync(SnippetLink);
        }

        private async Task SaveAsync()
        {
            Loading = true;

            try
            {
                this.UpdateActiveCodeFileContentAction?.Invoke();

                var snippetId = await this.SnippetsService.SaveSnippetAsync(this.CodeFiles);
                var urlBuilder = new UriBuilder(this.NavigationManager.BaseUri) { Path = $"snippet/{snippetId}" };
                this.SnippetLink = urlBuilder.Uri.ToString();
                this.JsRuntime.InvokeVoid(Try.ChangeDisplayUrl, SnippetLink);
            }
            catch (InvalidOperationException ex)
            {
                Snackbar.Add(ex.Message, Severity.Error);
            }
            catch (Exception)
            {
                Snackbar.Add("Error while saving snippet. Please try again later.", Severity.Error);
            }
            finally
            {
                Loading = false;
            }
        }

        private async void OnClose()
        {
            Loading = false;
            SnippetLink = string.Empty;

            await VisibleChanged.InvokeAsync(false);
        }
    }
}
