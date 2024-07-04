using System.Reflection;
using Nextended.Core.Extensions;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Core;
using Nextended.Core.Helper;

namespace MudBlazor.Extensions.Helper;

public static class ComponentHelper
{
    internal static Task CallReflectionStateHasChanged(this IComponent cmp)
    {
        var stateHasChangedMethod = cmp.GetType().GetMethod("StateHasChanged", BindingFlags.Instance | BindingFlags.NonPublic);
        var invokeAsyncMethod = cmp.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .FirstOrDefault(m => m.Name == "InvokeAsync" && m.GetParameters()?.FirstOrDefault()?.ParameterType == typeof(Action));

        if (stateHasChangedMethod == null)
            return Task.CompletedTask;

        if (invokeAsyncMethod == null)
        {
            stateHasChangedMethod.Invoke(cmp, null);
            return Task.CompletedTask;
        }

        Action stateHasChangedAction = (Action)Delegate.CreateDelegate(typeof(Action), cmp, stateHasChangedMethod);

        var res = invokeAsyncMethod.Invoke(cmp, new object[] { stateHasChangedAction });

        return res as Task ?? Task.CompletedTask;
    }

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