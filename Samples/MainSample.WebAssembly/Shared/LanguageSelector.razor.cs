using System.Globalization;
using System.Reflection;
using AKSoftware.Localization.MultiLanguages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace MainSample.WebAssembly.Shared;

public partial class LanguageSelector
{
    private List<CultureInfo>? _languages; 
    private CultureInfo _currentCulture = CultureInfo.CurrentUICulture;
    
    [Inject ]private IStringLocalizer<LanguageSelector> Localizer { get; set; }

    protected override Task OnInitializedAsync()
    {
        if (_languages == null)
            LoadLanguages();
        _currentCulture = CultureInfo.CurrentUICulture;
        return base.OnInitializedAsync();
    }

    private void SetCulture(CultureInfo info)
    {
        _currentCulture = info;
        MainLayout.Instance?.SetLanguage(info);
        InvokeAsync(StateHasChanged);
    }

    private void LoadLanguages()
    {
        // read all yaml files from embedded resources
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();
        _languages = new List<CultureInfo>();
        foreach (var resourceName in resourceNames)
        {
            if (resourceName.EndsWith(".yml"))
            {
                var name = Path.GetFileNameWithoutExtension(resourceName).Split(".").Last();
                try
                {
                    var culture = CultureInfo.GetCultureInfo(name);
                    if (culture is { IsNeutralCulture: false })
                    {
                        _languages.Add(culture);
                    }
                }
                catch (Exception)
                {
                }
            }
        }
    }

    private string DisplayStr(CultureInfo language, bool addCultureCode = true)
    {
        string display = Localizer[language.Name];
        if (display == language.TwoLetterISOLanguageName)
            display = language.DisplayName;
        if (!addCultureCode)
            return display;
        return $"{display} - {language.Name}";
    }
}