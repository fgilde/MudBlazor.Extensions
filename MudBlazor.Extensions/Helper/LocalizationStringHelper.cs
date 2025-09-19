using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace MudBlazor.Extensions.Helper;

public static class LocalizationStringHelper
{
    public static MarkupString ToMarkupString(this LocalizedString str)
    {
        return new MarkupString(str);
    }
}