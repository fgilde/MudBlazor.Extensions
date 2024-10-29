using Nextended.Blazor.Helper;
using Nextended.Core.Extensions;
using Nextended.Core.Helper;
using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Components.ObjectEdit.Options;

/// <summary>
/// Render data for a component to be rendered.
/// </summary>
public partial class RenderData : IRenderData
{
    /// <summary>
    /// Property meta for the current render data.
    /// </summary>
    protected ObjectEditPropertyMeta PropertyMeta;
    
    /// <summary>
    /// Wrapper component as IRenderData for the current render data.
    /// </summary>
    public IRenderData Wrapper { get; set; }
    
    /// <summary>
    /// Render data to be rendered before the current component.
    /// </summary>
    public IList<IRenderData> RenderDataBeforeComponent { get; set; } = new List<IRenderData>();
    
    /// <summary>
    /// Render data to be rendered after the current component.
    /// </summary>
    public IList<IRenderData> RenderDataAfterComponent { get; set; } = new List<IRenderData>();
    
    /// <summary>
    /// Type of the component to be rendered.
    /// </summary>
    public Type ComponentType { get; set; }
    
    /// <summary>
    /// Attributes to be passed to the component to be rendered.
    /// </summary>
    public IDictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();
    
    /// <summary>
    /// Custom renderer for the component to be rendered.
    /// </summary>
    public ICustomRenderer CustomRenderer { get; set; }
    
    /// <summary>
    /// Initializes the value binding for the current render data.
    /// </summary>
    public virtual IRenderData InitValueBinding(ObjectEditPropertyMeta propertyMeta, Func<Task> valueChanged)
    {
        PropertyMeta = propertyMeta;
        return this;
    }

    /// <summary>
    /// If this is set to true, the binding will only be done once at first render otherwise StateChange updates binding as well.
    /// </summary>
    public bool OneTimeBinding { get; set; }

    /// <summary>
    /// If this is set to true and field and property type are equal it will anyhow map the propertyType to the fieldType what creates a clone of an object.
    /// </summary>
    public bool AlwaysMapToField { get; set; }

    /// <summary>
    /// If this is set to true and field and property type are equal it will anyhow map the fieldType to the propertyType what creates a clone of an object.
    /// </summary>
    public bool AlwaysMapToProperty { get; set; }

    private Action<object> _onReferenceSet;
    
    /// <summary>
    /// Component reference for the current render data.
    /// </summary>
    public object ComponentReference
    {
        get => _componentReference;
        internal set
        {
            _componentReference = value;
            _onReferenceSet?.Invoke(value);
        }
    }

    /// <summary>
    /// Callback for when the component reference is set.
    /// </summary>
    public void OnRendered<TComponent>(Action<TComponent> onReferenceSet) where TComponent : class, IComponent
    {
        if(ComponentReference != null)
            onReferenceSet(ComponentReference as TComponent);
        _onReferenceSet = o => onReferenceSet(o as TComponent);
    }

    /// <summary>
    /// Returns whether the given key and value are valid parameters for the current component type.
    /// </summary>
    public bool IsValidParameterAttribute(string key, object value) => ComponentRenderHelper.IsValidParameter(ComponentType, key, value);

    /// <summary>
    /// Set of valid attributes for the current component type.
    /// </summary>
    public IDictionary<string, object> ValidAttributes => Attributes.Where(kvp => IsValidParameterAttribute(kvp.Key, kvp.Value)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    
    /// <summary>
    /// Try to set the given attribute if it is allowed for the current component type.
    /// </summary>
    public IRenderData TrySetAttributeIfAllowed(string key, Func<object> valueFn, bool condition = true) => TrySetAttributeIfAllowed(key, valueFn(), condition);
    
    /// <summary>
    /// Convert the given value to a property value.
    /// </summary>
    public virtual object ConvertToPropertyValue(object value) => value;
    
    /// <summary>
    /// Set to true to disable value binding for the current render data.
    /// </summary>
    public bool DisableValueBinding { get; set; }
    
    /// <summary>
    /// Set the value for the current render data.
    /// </summary>
    public virtual void SetValue(object value) { }

    /// <summary>
    /// Value field for the component used in current render data.
    /// </summary>
    public string ValueField { get; set; }

    /// <summary>
    /// Added conditions for the current render data.
    /// </summary>
    protected List<(Type modelType, Func<object, bool> condition, Action<IRenderData> trueFn, Action<IRenderData> falseFn)> Conditions;
    private object _componentReference;

    /// <summary>
    /// Create a new render data for the given component type with the given attributes.
    /// </summary>
    public RenderData(Type componentType, IDictionary<string, object> attributes = null)
    {
        ComponentType = componentType;
        SetAttributes(attributes);
    }

    internal virtual Type FieldType => null;
    internal virtual Type PropertyType => null;

    /// <summary>
    /// Set the attributes for the current render data.
    /// </summary>
    public IRenderData SetAttributes(IDictionary<string, object> attributes)
    {
        if (attributes != null)
        {
            Attributes = attributes;//attributes.ToDictionary(entry => entry.Key, entry => entry.Value);
            RemoveUnsafeAttributes();
        }

        return this;
    }

    /// <summary>
    /// Add the given attributes to the current render data.
    /// </summary>
    public IRenderData AddAttributes(bool overwriteExisting, params KeyValuePair<string, object>[] attributes)
    {
        if (attributes?.Any() == true)
        {
            var attr = attributes; //attributes.ToDictionary(entry => entry.Key, entry => entry.Value);
            if (overwriteExisting)
                attr.Apply(a => Attributes.AddOrUpdate(a.Key, a.Value));
            else
                Attributes.AddRange(attr.Where(a => !Attributes.ContainsKey(a.Key)));
            RemoveUnsafeAttributes();
        }

        return this;
    }

    private void RemoveUnsafeAttributes()
    {
        if (Attributes.ContainsKey(nameof(MudComponentBase.UserAttributes)))
            Attributes.Remove(nameof(MudComponentBase.UserAttributes));
        Attributes.Where(k => !IsValidParameterAttribute(k.Key, k.Value)).Apply(k => Attributes.Remove(k));
    }

    /// <summary>
    /// Try to set the given attribute if it is allowed for the current component type.
    /// </summary>
    public IRenderData TrySetAttributeIfAllowed(string key, object value, bool condition = true)
    {
        //if (condition && value != null && (!Attributes.ContainsKey(key) || Attributes[key] == null) && IsValidParameterAttribute(key))
        if (condition && value != null && IsValidParameterAttribute(key, value))
            Attributes.AddOrUpdate(key, value);
        
        return this;
    }
    
    /// <summary>
    /// Clone the current render data.
    /// </summary>
    public object Clone()
    {
        var result = (RenderData)MemberwiseClone();
        
        result.Attributes = Attributes?.ToDictionary(entry => entry.Key, entry => entry.Value);
        result.RenderDataBeforeComponent = RenderDataBeforeComponent?.Select(r => r.Clone() as IRenderData).ToList();
        result.RenderDataAfterComponent = RenderDataAfterComponent?.Select(r => r.Clone() as IRenderData).ToList();
        result.Wrapper = Wrapper?.Clone() as IRenderData;
        result.Conditions = Conditions?.Select(c => (c.modelType, c.condition, c.trueFn, c.falseFn)).ToList();

        return result;
    }

    /// <summary>
    /// Update the conditional settings for the current render data.
    /// </summary>
    public virtual void UpdateConditionalSettings<TModel>(TModel model)
    {
        Conditions?.Where(c => c.modelType == typeof(TModel)).Apply(condition => (condition.condition(model) ? condition.trueFn : condition.falseFn)(this));
        Conditions?.Where(c => c.modelType == ComponentType).Apply(condition =>
        {
            var componentInstanceClone = Attributes.ToObject(ComponentType);
            (condition.condition(componentInstanceClone) ? condition.trueFn : condition.falseFn)(this);
        });
        if (PropertyMeta != null) {
            Conditions?.Where(c => c.modelType == typeof(ObjectEditPropertyMeta)).Apply(condition => (condition.condition(PropertyMeta) ? condition.trueFn : condition.falseFn)(this));
            Conditions?.Where(c => c.modelType == typeof(PropertyInfo)).Apply(condition => (condition.condition(PropertyMeta.PropertyInfo) ? condition.trueFn : condition.falseFn)(this));
        }
        // Notice if you want to allow more types as condition you need to do it here, and in ObjectEditPropertyMetaSettings.cs as well
        Wrapper?.UpdateConditionalSettings(model);
    }

    /// <summary>
    /// Add a condition for the current render data.
    /// </summary>
    public void AddCondition<TModel>(Func<TModel, bool> condition, Action<IRenderData> trueFn, Action<IRenderData> falseFn)
    {
        Conditions ??= new List<(Type modelType, Func<object, bool> condition, Action<IRenderData> trueFn, Action<IRenderData> falseFn)>();
        Conditions.Add((typeof(TModel), model => condition((TModel) model), trueFn, falseFn));
    }


    /// <summary>
    /// Adds an attribute to the current render data if the given condition becomes true.
    /// </summary>
    public IRenderData AddAttributesIf<TModel>(Func<TModel, bool> condition, bool overwriteExisting, params KeyValuePair<string, object>[] attributes)
    {
        var existing = Attributes.Where(pair => attributes.Any(a => a.Key == pair.Key)).ToArray();
        var toRemove = attributes.Where(pair => existing.All(e => e.Key != pair.Key)).ToArray();
        AddCondition(condition, rd => rd.AddAttributes(overwriteExisting, attributes), rd =>
        {
            rd.AddAttributes(overwriteExisting, existing);
            if (toRemove.Any())
                rd.Attributes.RemoveRange(toRemove);
        });
        return this;
    }

    /// <summary>
    /// Sets the given attributes for the current render data if the given condition becomes true.
    /// </summary>
    public IRenderData SetAttributesIf<TModel>(Func<TModel, bool> condition, IDictionary<string, object> attributes)
    {
        var existing = Attributes?.Any() == true ? new Dictionary<string, object>(Attributes) : new Dictionary<string, object>();
        AddCondition(condition, rd => rd.SetAttributes(attributes), rd => rd.SetAttributes(existing));
        return this;
    }

}