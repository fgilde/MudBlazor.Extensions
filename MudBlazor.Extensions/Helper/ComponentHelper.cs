using System.Collections.Concurrent;
using System.Reflection;
using Nextended.Core.Extensions;
using Microsoft.AspNetCore.Components;
using Nextended.Core.Helper;

namespace MudBlazor.Extensions.Helper;

public static class ComponentHelper
{
    private static readonly FieldInfo RenderHandleField = typeof(ComponentBase)
        .GetField("_renderHandle", BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly ConcurrentDictionary<Type, MethodInfo> StateHasChangedMethodCache = new();
    private static readonly ConcurrentDictionary<Type, MethodInfo> InvokeAsyncMethodCache = new();
    private static readonly ConcurrentDictionary<Type, MethodInfo> GetDialogReferenceMethodCache = new();

    public static bool IsRenderHandleAssigned(this IComponent component)
    {
        try
        {
            if (component is ComponentBase baseComponent)
            {
                var value = RenderHandleField?.GetValue(baseComponent);
                var handleValue = value as RenderHandle? ?? default;
                return handleValue.IsInitialized;
            }
        }
        catch (Exception e)
        {}

        return true;
    }

    internal static Task CallReflectionStateHasChanged(this IComponent cmp)
    {
        var type = cmp.GetType();
        var stateHasChangedMethod = StateHasChangedMethodCache.GetOrAdd(type,
            t => t.GetMethod("StateHasChanged", BindingFlags.Instance | BindingFlags.NonPublic));
        var invokeAsyncMethod = InvokeAsyncMethodCache.GetOrAdd(type,
            t => t.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .FirstOrDefault(m => m.Name == "InvokeAsync" && m.GetParameters()?.FirstOrDefault()?.ParameterType == typeof(Action)));

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

    internal static IMudDialogInstance FindMudDialogInstance(this IComponent component) 
        => component.AllOf<IMudDialogInstance>(ReflectReadSettings.AllWithHierarchyTraversal).FirstOrDefault();

    internal static MudDialogProvider FindMudDialogProvider(this IComponent component)
    {
        return component is IMudDialogInstance instance
            ? FindMudDialogProvider(instance)
            : component.FindMudDialogInstance()?.FindMudDialogProvider();
    }

    internal static MudDialogProvider FindMudDialogProvider(this IMudDialogInstance component)
        => component.AllOf<MudDialogProvider>(ReflectReadSettings.AllWithHierarchyTraversal).FirstOrDefault();

    internal static MudDialogProvider FindMudDialogProvider(this IDialogReference component)
    {
        var result = component.AllOf<MudDialogProvider>(ReflectReadSettings.AllWithHierarchyTraversal).FirstOrDefault();
        if (result == null)
        {
            if(component.Dialog is IMudDialogInstance instance)
                result = instance.FindMudDialogProvider();
            else if (component.Dialog is ComponentBase cmp)
                result = cmp.FindMudDialogProvider();
        }
        return result;
    }

    internal static IDialogReference GetDialogReference(this IMudDialogInstance dlgInstance)
    {
        var provider = dlgInstance.FindMudDialogProvider();
        var providerType = provider.GetType();
        var getDialog = GetDialogReferenceMethodCache.GetOrAdd(providerType,
            t => t.GetMethod("GetDialogReference", BindingFlags.NonPublic | BindingFlags.Instance));
        IDialogReference? dialogRef = (IDialogReference)getDialog.Invoke(provider, new object[] { dlgInstance.Id });
        return dialogRef;
    }
}