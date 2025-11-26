using AKSoftware.Localization.MultiLanguages;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json.Linq;
using Nextended.Core.Attributes;
using System.Reflection;

namespace MainSample.WebAssembly;

[RegisterAs(typeof(IStringLocalizer<>), ServiceLifetime = ServiceLifetime.Transient)]
public class YamlLocalizer<T> : IStringLocalizer<T>
{
    private MethodInfo? _getValueMethod;
    private FieldInfo? _keyValuesField;
    private readonly ILanguageContainerService _originalService;
    private readonly ILogger<YamlLocalizer<T>> _logger;

    public YamlLocalizer(ILanguageContainerService originalService, ILogger<YamlLocalizer<T>> logger)
    {
        _originalService = originalService;
        _logger = logger;
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        _keyValuesField ??= _originalService.Keys.GetType().GetField("keyValues", BindingFlags.Instance | BindingFlags.NonPublic);
        if (_keyValuesField != null)
        {
            var keyValues = _keyValuesField.GetValue(_originalService.Keys) as JObject;
            foreach (JProperty property in keyValues.Properties())
            {
                yield return this[property.Name];
            }
        }
    }

    public LocalizedString this[string name] => this[name, null];

    public LocalizedString this[string name, params object[]? arguments]
    {
        get
        {
            var str = Get(name);
            if (arguments != null && arguments.Any())
                str.value = string.Format(str.value, arguments);
                
            return new LocalizedString(name, str.value);
        }
    }

    private (string value, bool found) Get(string key)
    {
        try
        {
            if (_originalService?.Keys != null)
            {
                _getValueMethod ??= _originalService?.Keys?.GetType().GetMethod("GetValue", BindingFlags.Instance | BindingFlags.NonPublic);
                string res = _getValueMethod?.Invoke(_originalService?.Keys, new[] {key})?.ToString();
                res ??= _originalService?[key];
                return CheckStore((res, res != key));
            }
            return CheckStore((key, false));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return CheckStore((key, false));
        }
    }

    private (string value, bool found) CheckStore((string value, bool found) valueTuple)
    {
        if (!valueTuple.found)
        {
            YamlLocalizerExtensions.Store(valueTuple.value);
        }
        return valueTuple;
    }
}

public static class YamlLocalizerExtensions
{
    const bool StoreNotFoundKeys = false;
    internal static readonly HashSet<string> NotFoundKeys = new();

    internal static void Store(string key)
    {
        if(StoreNotFoundKeys)
            NotFoundKeys.Add(key);
    }
    
    public static IServiceCollection AddYamlLocalizer(this IServiceCollection services)
    {
        services.AddLocalization(options =>
        {
            options.ResourcesPath = "Resources";
        });

        services.AddLanguageContainer(typeof(WebAssemblyClassLocator).Assembly);
        return services;
    }
}