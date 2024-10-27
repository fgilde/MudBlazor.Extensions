using System.Text.Json;
using System.Text.Json.Serialization;

namespace MudBlazor.Extensions.Helper.JsonConverter
{
    public class LowercaseEnumConverter : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsEnum;
        }

        public override System.Text.Json.Serialization.JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            return new JsonStringEnumConverter(JsonNamingPolicy.CamelCase).CreateConverter(typeToConvert, options);
        }
    }
}
