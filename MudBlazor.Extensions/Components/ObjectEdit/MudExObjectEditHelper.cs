using MudBlazor.Extensions.Core;
using MudBlazor.Utilities;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components.ObjectEdit;

internal class MudExObjectEditHelper
{
    private static Type[] handleAsPrimitive = { typeof(string), typeof(decimal), typeof(MudExColor), typeof(MudColor), typeof(System.Drawing.Color), typeof(DateTime), typeof(DateTimeOffset), typeof(TimeSpan), typeof(TimeOnly), typeof(DateOnly), typeof(Guid) };

    internal static bool HandleAsPrimitive(Type t)
    {
        var type = t.IsNullable() ? Nullable.GetUnderlyingType(t) ?? t : t;
        return type.IsPrimitive || type.IsEnum || handleAsPrimitive.Contains(type);
    }
}