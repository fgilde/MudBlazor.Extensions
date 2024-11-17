# MudExObjectEdit
## Register component as Editor for a type

To easily register a component as an editor for a type you can implement `IObjectEditorFor<T>` in your component.
Then this component will be the default editor for specified type. 

```csharp
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
```

### Example

```csharp
public class MyModel
{
	public string Name { get; set; }
	public string Text { get; set; }
	public string Text2 { get; set; }
	public string Text3 { get; set; }	
}

public partial class MyModelEditor : IObjectEditorFor<MyModel>
{
    [Parameter] public MyModel Value { get; set; }
	[Parameter] public EventCallback<MyModel> ValueChanged { get; set; } // Notice you dont need to invoke this own your own, this will be done by the ObjectEdit component

}

```

With the code above all properties of type `MyModel` will be rendered with the `MyModelEditor` component regardless of whether they belong to another model or are displayed directly within a MudExObjectEditor.
However, you can still override the editor for a specific property by using the `RenderWith` method in the `IObjectMetaConfiguration` implementation or implement the interface `IObjectEditorWithCustomPropertyRenderDataFor<CaptureOptions>`.

Then you need to additional implement the GetRenderData method to return the render data for the property from the interface `IDefaultRenderDataProvider`.
```csharp
IRenderData GetRenderData(ObjectEditPropertyMeta propertyMeta);
```

### Example
```csharp
/// <inheritdoc />
public IRenderData GetRenderData(ObjectEditPropertyMeta meta)
{
    return RenderData.For<MudExObjectEditPicker<MyModel>, MyModel>(edit => edit.Value, edit =>
    {
        //edit.PickerVariant = PickerVariant.Dialog;
        edit.AllowOpenOnReadOnly = true;
    });
}
```

With the code above the `MudExObjectEditPicker<MyModel>` component will be used to render MyModel types if they are properties inside of another Model. If a `MyModel` object is edited directly within a `MudExObjectEditor` the `MyModelEditor` component will be used.

If you still want to use the default behavior for a property of a Model of type `MyModel` where the properties are spreaded out you can just return null

```csharp
/// <inheritdoc />
public IRenderData GetRenderData(ObjectEditPropertyMeta meta)
{
	return null;
}
```

This will use the default behavior for the property of type `MyModel` inside of another Model. 
If a `MyModel` object is edited directly within a `MudExObjectEditor` the `MyModelEditor` component will be used.