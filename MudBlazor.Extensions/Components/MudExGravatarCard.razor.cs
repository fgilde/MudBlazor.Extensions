using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Attribute;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Core.Css;
using MudBlazor.Extensions.Helper;

namespace MudBlazor.Extensions.Components;

public partial class MudExGravatarCard
{
    private bool _isLoading = true;
    private string _error;
    private string _cardUrl;
    private string _email;
    private GravatarProfile _profileData;
    private GravatarCardMode _mode = GravatarCardMode.Custom;

    [Parameter]
    public string Email
    {
        get => _email;
        set
        {
            if (_email != value)
            {
                _email = value;
                _ = LoadGravatarDataAsync();
            }
        }
    }

    [Parameter]
    public GravatarCardMode Mode
    {
        get => _mode;
        set
        {
            if (_mode != value)
            {
                _mode = value;
                _ = LoadGravatarDataAsync();
            }
        }
    }

    [Parameter] 
    public JustifyContent JustifyContent { get; set; }

    [Parameter]
    public MudExSize<double> Width { get; set; } = 400;

    [Parameter]
    public MudExSize<double> Height { get; set; } = 400;

    [Parameter]
    public int Elevation { get; set; } = 2;

    [Parameter]
    public string CardClass { get; set; }

    [Parameter]
    public Color LoadingColor { get; set; } = Color.Primary;

    [Parameter]
    public bool ShowAvatar { get; set; } = true;

    [Parameter]
    public Size AvatarSize { get; set; } = Size.Large;

    [Parameter]
    public bool ShowDisplayName { get; set; } = true;

    [Parameter]
    public bool ShowPronouns { get; set; } = true;

    [Parameter]
    public bool ShowJobTitle { get; set; } = true;

    [Parameter]
    public bool ShowCompany { get; set; } = true;

    [Parameter]
    public bool ShowLocation { get; set; } = true;

    [Parameter]
    public bool ShowDescription { get; set; } = true;

    [Parameter]
    public bool ShowVerifiedAccounts { get; set; } = true;

    [Parameter]
    public bool ShowInterests { get; set; } = true;

    [Parameter]
    public bool ShowLinks { get; set; } = true;

    [Parameter]
    public bool ShowHeaderImage { get; set; } = false;

    [Parameter]
    public int HeaderImageHeight { get; set; } = 150;

    [Parameter]
    public bool ShowProfileLink { get; set; } = true;

    private string ContainerStyle => MudExStyleBuilder.FromStyle(Style)
        .WithWidth(Width)
        .WithHeight(Height, Mode == GravatarCardMode.Iframe)
        .WithHeight("auto", Mode == GravatarCardMode.Custom)
        .Build();

    [Parameter, SafeCategory("Avatar Image")]
    public string AvatarImageStyle { get; set; }

    [Parameter, SafeCategory("Avatar Image")]
    public string AvatarImageSize { get; set; }

    [Parameter, SafeCategory("Avatar Image")]
    public int AvatarElevation { get; set; }

    [Parameter, SafeCategory("Avatar Image")]
    public Variant AvatarImageVariant { get; set; }

    [Parameter, SafeCategory("Avatar Image")]
    public Color AvatarImageColor { get; set; }

    [Parameter, SafeCategory("Avatar Image")]
    public bool AvatarImageRounded { get; set; }

    [Parameter, SafeCategory("Avatar Image")]
    public bool AvatarImageSquare { get; set; }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
            await LoadGravatarDataAsync();
    }

    private async Task<string> HashAsync(string emailAddress)
    {
        return await JsRuntime.InvokeAsync<string>("MudExNumber.md5", emailAddress);
    }

    private async Task LoadGravatarDataAsync()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            _isLoading = false;
            return;
        }

        _isLoading = true;
        _error = null;
        await InvokeAsync(StateHasChanged);

        try
        {
            var hash = await HashAsync(Email.Trim().ToLowerInvariant());

            if (Mode == GravatarCardMode.Iframe)
            {
                _cardUrl = $"https://gravatar.com/{hash}.card";
            }
            else if (Mode == GravatarCardMode.Custom)
            {
                await LoadProfileDataAsync(hash);
            }
        }
        catch (Exception ex)
        {
            _error = TryLocalize("Error loading Gravatar profile {0}", ex.Message);
        }
        finally
        {
            _isLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task LoadProfileDataAsync(string hash)
    {
        try
        {
            var url = $"https://api.gravatar.com/v3/profiles/{hash}";
            using var http = new HttpClient();
            var response = await http.GetStringAsync(url);
            _profileData = System.Text.Json.JsonSerializer.Deserialize<GravatarProfile>(response);
        }
        catch (Exception ex)
        {
            _error = TryLocalize("Profile data could not be loaded", ex.Message);
        }
    }
}