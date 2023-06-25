using Newtonsoft.Json.Linq;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Helper;

/// <summary>
/// Simple JsonHelper
/// </summary>
internal static class JsonHelper
{

    /// <summary>
    /// Simplifies all MudColors in a given JSON
    /// </summary>
    public static string SimplifyMudColorInJson(string json)
    {
        void SimplifyColorsJToken(JToken token)
        {
            if (token is JProperty jProperty && jProperty.First is JObject && jProperty.First["Value"] != null) jProperty.Value = jProperty.Value["Value"];

            if (token is not JContainer) return;
            token.Children().Apply(SimplifyColorsJToken);
        }
        var jsonObj = JObject.Parse(json);
        SimplifyColorsJToken(jsonObj);
        return jsonObj.ToString();
    }

    /// <summary>
    /// Merges two json files
    /// </summary>
    /// <param name="json"></param>
    /// <param name="other"></param>
    public static string MergeJson(string json, params string[] other)
    {
        var jObject = JObject.Parse(json);
        foreach (var o in other)
        {
            var jObjectOther = JObject.Parse(o);
            jObject.Merge(jObjectOther, new JsonMergeSettings
            {
                MergeArrayHandling = MergeArrayHandling.Union,
                MergeNullValueHandling = MergeNullValueHandling.Ignore
            });
        }
        return jObject.ToString();
    }

    /// <summary>
    /// Removes given properties from given json.
    /// </summary>
    public static string RemovePropertiesFromJson(string json, string[] propertiesToRemove)
    {
        if (string.IsNullOrWhiteSpace(json) || propertiesToRemove == null || !propertiesToRemove.Any())
            return json;
        var jObject = JObject.Parse(json);
        foreach (var property in propertiesToRemove)
        {
            var pathParts = property.Split('.');
            var jToken = jObject;
            for (var i = 0; i < pathParts.Length; i++)
            {
                var pathPart = pathParts[i];
                if (i == pathParts.Length - 1)
                {
                    var jContainer = jToken?[pathPart]?.Parent;
                    jContainer?.Remove();

                    break;
                }

                jToken = (JObject) jToken?[pathPart];
            }
        }
        return jObject.ToString();
    }
}