using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Helper;

public static class JsonHelper
{
    public static TTheme CloneTheme<TTheme>(this TTheme theme) where TTheme : MudTheme
    {
        var json = JsonConvert.SerializeObject(theme);
        return JsonConvert.DeserializeObject<TTheme>(SimplifyMudColorInJson(json));
    }

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