using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Simple extension for MudAvatar to load automatically a profile image for an email address using Gravatar
/// </summary>
public partial class MudExGravatar : IMudExComponent
{
    [Inject] private IJSRuntime JsRuntime { get; set; }
    private string _email;
    private RenderFragment Inherited() => builder => base.BuildRenderTree(builder);

    /// <summary>
    /// Here you can get the current used Url for the image
    /// </summary>
    public string GravatarUrl { get; private set; }

    /// <summary>
    /// Specify the size of the Gravatar image to load. The default is 80px.
    /// Please note this is not the size of the displayed image, but the size of the image behind.
    /// </summary>
    [Parameter]
    public string GravatarImageSize { get; set; } = "80";

    /// <summary>
    /// The email address to load the Gravatar image for
    /// </summary>
    [Parameter]
    public string Email
    {
        get => _email;
        set
        {
            if (_email != value)
            {
                _email = value;
                _ = LoadGravatarAsync();
            }
        }
    }


    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
            _ = LoadGravatarAsync();
    }

    private async Task<string> HashAsync(string emailAddress)
    {
        return await JsRuntime.InvokeAsync<string>("MudExNumber.md5", emailAddress);
    }

    private async Task LoadGravatarAsync()
    {
        GravatarUrl = string.Empty;
        if (!string.IsNullOrEmpty(Email))
        {
            ChildContent = RenderLoading();
            await InvokeAsync(StateHasChanged);            
            var hash = await HashAsync(Email);
            GravatarUrl = $"https://www.gravatar.com/avatar/{hash}?s={GravatarImageSize}&d=identicon";
            ChildContent = RenderGravatarImage();
            await InvokeAsync(StateHasChanged);
        }
    }

    private Color GetLoadColor() => Color == Color.Info ? Color.Success : Color.Info;
}