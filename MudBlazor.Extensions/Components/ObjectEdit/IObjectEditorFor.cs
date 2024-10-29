using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Components.ObjectEdit;

/// <summary>
/// Interface for object editor
/// Just implement this interface on any component to make it as the default object editor for <typeparam name="T"></typeparam>
/// </summary>
public interface IObjectEditorFor<T> : IComponent
{
    /// <summary>
    /// The value to edit. This is the value that will be edited by the component
    /// It's required that this property has the parameter attribute
    /// </summary>
    [Parameter] public T Value { get; set; }

    /// <summary>
    /// The value changed event
    /// It's required that this property has the parameter attribute
    /// </summary>
    [Parameter] public EventCallback<T> ValueChanged { get; set; }
}

/// <summary>
/// Interface for object editor
/// Just implement this interface on any component to make it as the default object editor for <typeparam name="T"></typeparam>
/// Additionally this interface provides a method to get the custom render data for the object editor.
/// This is useful when you want to provide custom render data behavior for your editor if it is used as a property of another object.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IObjectEditorWithCustomRenderDataFor<T> : IObjectEditorFor<T>, IDefaultRenderDataProviderFor<T>
{}