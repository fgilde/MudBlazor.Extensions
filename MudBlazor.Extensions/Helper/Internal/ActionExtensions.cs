using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Helper.Internal;

internal static class ActionExtensions
{
    public static Action<T> CombineWith<T>(this Action<T> lastAction, params Action<T>[] actions)
    {
        return meta =>
        {
            actions.Apply(a => a(meta));      
            lastAction(meta);
        };
    }
}
