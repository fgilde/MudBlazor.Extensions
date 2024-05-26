using Nextended.Core.Extensions;
using Microsoft.AspNetCore.Components;
using Nextended.Core.Helper;

namespace MudBlazor.Extensions.Helper;

public static class ComponentHelper
{
   
    internal static MudDialogInstance FindMudDialogInstance(this IComponent component) 
        => component.AllOf<MudDialogInstance>(ReflectReadSettings.AllWithHierarchyTraversal).FirstOrDefault();

    internal static MudDialogProvider FindMudDialogProvider(this IComponent component)
    {
        return component is MudDialogInstance instance
            ? FindMudDialogProvider(instance)
            : component.FindMudDialogInstance()?.FindMudDialogProvider();
    }

    internal static MudDialogProvider FindMudDialogProvider(this MudDialogInstance component)
        => component.AllOf<MudDialogProvider>(ReflectReadSettings.AllWithHierarchyTraversal).FirstOrDefault();
}