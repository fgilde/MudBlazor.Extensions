using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Services;
using Nextended.Blazor.Models;
using Nextended.Core;

namespace MudBlazor.Extensions.Components
{
    /// <summary>
    /// Simple Excel file viewer
    /// </summary>
    public partial class MudExFileDisplayOfficeLive : IMudExFileDisplay
    {

        private string _iframeUrl;

        [Inject] private MudExFileService FileService { get; set; }
        
        /// <summary>
        /// The name of the component
        /// </summary>
        public string Name => TryLocalize("Office live viewer");


        /// <summary>
        /// The Current code string provided from file
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// The file display infos
        /// </summary>
        [Parameter]
        public IMudExFileDisplayInfos FileDisplayInfos { get; set; }

        /// <summary>
        /// Src url of xlsx file
        /// </summary>
        [Parameter]
        public string Src { get; set; }

        /// <summary>
        /// Reference to the parent MudExFileDisplay if the component is used inside a MudExFileDisplay
        /// </summary>
        [CascadingParameter] public MudExFileDisplay MudExFileDisplay { get; set; }


        /// <inheritdoc />
        public async Task<bool> CanHandleFileAsync(IMudExFileDisplayInfos fileDisplayInfos, IMudExFileService fileService)
        {
            if(fileDisplayInfos == null || string.IsNullOrEmpty(fileDisplayInfos.Url) || DataUrl.IsDataUrl(fileDisplayInfos.Url) || fileDisplayInfos.Url.StartsWith("blob:", StringComparison.InvariantCultureIgnoreCase))
                return false;
   
            var absoluteUrl = await fileService.ToAbsoluteUrlAsync(fileDisplayInfos.Url);
            
            var urlIsExternalAvailable = Uri.TryCreate(absoluteUrl, UriKind.Absolute, out var uriResult) && !uriResult.Host.Equals("localhost", StringComparison.CurrentCultureIgnoreCase) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            if (!urlIsExternalAvailable)
                return false;
            return MimeType.Matches(fileDisplayInfos.ContentType, MimeType.OfficeTypes);
        }

        public Task<IDictionary<string, object>> FileMetaInformationAsync(IMudExFileDisplayInfos fileDisplayInfos)
        {
            return Task.FromResult<IDictionary<string, object>>(new Dictionary<string, object>());
        }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            var srcUpdated = parameters.TryGetValue<string>(nameof(Src), out var src) && src != Src && !string.IsNullOrEmpty(src);
            await base.SetParametersAsync(parameters);

            try
            {
                if (srcUpdated)
                {
                    await UpdateUrl();
                }
            }
            catch (Exception e)
            {
                MudExFileDisplay?.ShowError(e.Message);
                Console.WriteLine(e);
                Value = string.Empty;
            }
        }

        private async Task UpdateUrl()
        {
            //var url = await FileService.ToDataUrlAsync(Src, FileDisplayInfos?.ContentType);
            _iframeUrl = $"https://view.officeapps.live.com/op/view.aspx?src={Src}";
            await InvokeAsync(StateHasChanged);
        }
    }
}


