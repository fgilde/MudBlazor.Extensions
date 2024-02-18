using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Components.ObjectEdit.Options;

/// <summary>
/// The interface for a render data instance, providing a set of properties and methods to configure the render data.
/// </summary>
public interface IRenderData : ICloneable
{
    /// <summary>
    /// Wrapper component for the current render data.
    /// </summary>
    public IRenderData Wrapper { get; set; }
    
    /// <summary>
    /// Render data for the component to be added before the current render data.
    /// </summary>
    public IList<IRenderData> RenderDataBeforeComponent { get; }
    
    /// <summary>
    /// Render data for the component to be added after the current render data.
    /// </summary>
    public IList<IRenderData> RenderDataAfterComponent { get; }
    
    /// <summary>
    /// Returns true if the given key is a valid parameter attribute for the current component type.
    /// </summary>
    public bool IsValidParameterAttribute(string key, object value);
    
    /// <summary>
    /// Component type to be rendered.
    /// </summary>
    public Type ComponentType { get; }
    
    /// <summary>
    /// Attributes to be used when rendering the component.
    /// </summary>
    public IDictionary<string, object> Attributes { get; }
    
    /// <summary>
    /// Valid attributes for the current component type.
    /// </summary>
    public IDictionary<string, object> ValidAttributes { get; }
    
    /// <summary>
    /// Initializes the value binding for the current render data.
    /// </summary>
    public IRenderData InitValueBinding(ObjectEditPropertyMeta propertyMeta, Func<Task> valueChanged);
    
    /// <summary>
    /// A custom renderer to be used when rendering the component.
    /// </summary>
    public ICustomRenderer CustomRenderer { get; set; }
    
    /// <summary>
    /// Updates all conditional settings for the current render data.
    /// </summary>
    public void UpdateConditionalSettings<TModel>(TModel model);
    
    /// <summary>
    /// Adds a condition to the current render data.
    /// </summary>
    void AddCondition<TModel>(Func<TModel, bool> condition, Action<IRenderData> trueFn, Action<IRenderData> falseFn);
    
    /// <summary>
    /// Try to set the given attribute if it is allowed for the current component type.
    /// </summary>
    public IRenderData TrySetAttributeIfAllowed(string key, Func<object> valueFn, bool condition = true);
    
    /// <summary>
    /// Try to set the given attribute if it is allowed for the current component type.
    /// </summary>
    public IRenderData TrySetAttributeIfAllowed(string key, object value, bool condition = true);
    
    /// <summary>
    /// Set the given attributes for the current render data.
    /// </summary>
    public IRenderData SetAttributes(IDictionary<string, object> attributes);
    
    /// <summary>
    /// Adds the given attributes to the current render data.
    /// </summary>
    public IRenderData AddAttributes(bool overwriteExisting, params KeyValuePair<string, object>[] attributes);
    
    /// <summary>
    /// Adds the given attributes to the current render data if the given condition is true.
    /// </summary>
    public IRenderData AddAttributesIf<TModel>(Func<TModel, bool> condition, bool overwriteExisting, params KeyValuePair<string, object>[] attributes);
    
    /// <summary>
    /// Sets the given attributes for the current render data if the given condition is true.
    /// </summary>
    public IRenderData SetAttributesIf<TModel>(Func<TModel, bool> condition, IDictionary<string, object> attributes);
    
    /// <summary>
    /// Converts the given value to a property value.
    /// </summary>
    public object ConvertToPropertyValue(object value);
    
    /// <summary>
    /// Disables value binding for the current render data.
    /// </summary>
    public bool DisableValueBinding { get; set; }
    
    /// <summary>
    /// Set the value for the current render data.
    /// </summary>
    public void SetValue(object value);
    
    /// <summary>
    /// Value field to be used on the component for the current render data.
    /// </summary>
    public string ValueField { get; }

    /// <summary>
    /// Callback to be used when the reference is rendered.
    /// </summary>
    public void OnRendered<TComponent>(Action<TComponent> onReferenceSet) where TComponent : class, IComponent;
}