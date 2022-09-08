# MudExObjectEdit
`MudExObjectEdit` is a powerfull component to edit objects. 
You can also use the `MudExObjectEditForm` to have automatic validation and submit.
Validation works automatically for DataAnnotation Validations or fluent registered validations for your model.
The easiest way to use it is to use the `MudExObjectEditForm` and pass your model to it.
```csharp
<MudExObjectEditForm OnValidSubmit="@OnSubmit" Value="@MyModel"></MudExObjectEditForm>
```

You can also use the `MudExObjectEditDialog` to edit you model in a dialog. The easieest way to do this is to use the extension method `EditObject` on the `IDialogService`.
```csharp
dialogService.EditObject(User, "Dialog Title", dialogOptionsEx);
```

Ok but what if you want to customize the layout or behaviour of your properties.
Here you can use the Parameter `MetaConfiguration` or implement the interface `IObjectMetaConfiguration`.
```csharp
public class MyModelMetaConfiguration : IObjectMetaConfiguration<MyModel>
{

    public Task ConfigureAsync(ObjectEditMeta<MyModel> meta)
    {
        // Configure here        
        return Task.CompletedTask;
    }
```

The meta configuration is very powerfull you can render every property as you want and have many features like Conditional updtaes, unlimited configuratable wrapper components, label and description resolving and much much more.
Also there is no requirement to use MudBlazor Components. For example lets configure a property to render with the Syncfusion RichTextEditor

```csharp
public class MyModel {
	...
	public string Name { get; set; }
    public string Text { get; set; }
    public string Text2 { get; set; }
    public string Text3 { get; set; }
	
	public bool CanEdit() => true;
}
public class MyModelMetaConfiguration : IObjectMetaConfiguration<MyModel>
{

    public Task ConfigureAsync(ObjectEditMeta<MyModel> meta)
    {
		...
		// Notice you can also configure multiple properties with meta.Properties(m => m.Text, m => m.Text2, m => m.Text3).RenderWith(e => e.Value, RichTextOptions()).WithSeparateLabelComponent(); 
        meta.Property(m => m.Text)
            .WithGroup("HTML Text")
            .RenderWith(e => e.Value, RichTextOptions()).WithSeparateLabelComponent(); 
        return Task.CompletedTask;
    }

    private static Action<SfRichTextEditor> RichTextOptions()
    {
        return e =>
        {
            e.EnableResize = true;
            e.Height = "250px";
        };
    }	
	
```


## Conditions: 
Each property can have all options or attributes the used component supports. The Attributes can set as Action<TComponent> or as Dictionary<string,object> and all options can also set with conditions. 
Use IgnoreIf for example then if the condition is not met the property will not be rendered. In this example we have a method CanEdit for example and setting some options only if this returns falls
```csharp
            meta.Properties(m => m.Text, m => m.Text2, m => m.Text3)
                .WithAttributesIf(m => !m.CanEdit(),
                        new KeyValuePair<string, object>(nameof(SfRichTextEditor.CssClass), "sf-rich-text-editor-disabled"),
                        new KeyValuePair<string, object>(nameof(SfRichTextEditor.EnableResize), false),
                        new KeyValuePair<string, object>(nameof(SfRichTextEditor.Readonly), true));
			// Same code different syntax
            meta.Properties(m => m.Text, m => m.Text2, m => m.Text3)
                .WithAttributesIf<CaseDto, SfRichTextEditor>(m => !CanEdit(m), editor =>
                {
                    editor.CssClass = "sf-rich-text-editor-disabled";
                    editor.EnableResize = false;
                    editor.Readonly = true;
                }); 
```

Instead of using the model type you can also use the component type, the property type (or the field type if you've used ConverterFn) for conditions
```csharp
        // Here the editor will be readonly of the value will be 'hello''
        meta.Property(m => m.Text).WithAttributesIf<SfRichTextEditor, SfRichTextEditor>(editor => editor.Value == "Hello", editor =>
        {
            editor.CssClass = "sf-rich-text-editor-disabled";
            editor.EnableResize = false;
            editor.Readonly = true;
        }); 
        // Same code different syntax using propertytype
        meta.Property(m => m.Text).WithAttributesIf<string, SfRichTextEditor>(s => s == "Hello", editor =>
        {
            editor.CssClass = "sf-rich-text-editor-disabled";
            editor.EnableResize = false;
            editor.Readonly = true;
        }); 
```

## Layout: 
You can also layout your editor behavior. For example you can set a property to render with a wrapper component. 
```csharp
		meta.Property(m => m.Text).WrapIn<ComponentToWrapIn>(e =>
		{
			e.SettingForWrapper = value
		}).WrapIn<AnotherWrapperArroundTheWrapper>(e => ...);
```
The most common usecase in MudBlazor is to wrap the whole Editor in a grid and each Item in a MudItem.
The `MudObjectEdit` automatically detects if it should add a MudGrid arround but this behavior can also overwritten by setting the Parameter `WrapInMudGrid`
For the property or the hole meta is also an easy extension to simply add MudItem
```csharp
		meta.Property(m => m.Text).WrapInMudItem(i => i.xs = 12);
		meta.WrapEachInMudItem(i => i.xs = 6);
```
