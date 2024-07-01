using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Helper;

public static class RenderFragmentExtensions
{
    public static RenderFragment CombineWith(this RenderFragment first, params RenderFragment[] fragments)
    {
        return builder =>
        {
            if(first != null)
                first(builder);
            foreach (var fragment in fragments.Where(f => f != null))
            {
                fragment(builder);
            }
        };
    }
}