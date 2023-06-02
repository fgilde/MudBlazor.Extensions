using MudBlazor.Extensions.Helper;
using Nextended.Blazor.Helper;
using Nextended.Core.Extensions;
using Nextended.Core.Helper;

namespace MudBlazor.Extensions.Components.ObjectEdit.Options;

public partial class RenderData : IRenderData
{
    public IRenderData Wrapper { get; set; }
    public IList<IRenderData> RenderDataBeforeComponent { get; set; } = new List<IRenderData>();
    public IList<IRenderData> RenderDataAfterComponent { get; set; } = new List<IRenderData>();
    public Type ComponentType { get; set; }
    public IDictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();
    public ICustomRenderer CustomRenderer { get; set; }
    public virtual IRenderData InitValueBinding(ObjectEditPropertyMeta propertyMeta, Func<Task> valueChanged) => this;
    
    //public bool IsValidParameterAttribute(string key, object value) => ComponentRenderHelper.IsValidProperty(ComponentType, key, value);

    public bool IsValidParameterAttribute(string key, object value) => ComponentRenderHelper.IsValidParameter(ComponentType, key, value);

    public IDictionary<string, object> ValidAttributes => Attributes.Where(kvp => IsValidParameterAttribute(kvp.Key, kvp.Value)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    public IRenderData TrySetAttributeIfAllowed(string key, Func<object> valueFn, bool condition = true) => TrySetAttributeIfAllowed(key, valueFn(), condition);
    public virtual object ConvertToPropertyValue(object value) => value;
    public bool DisableValueBinding { get; set; }
    public virtual void SetValue(object value) { }

    public string ValueField { get; set; }

    protected List<(Type modelType, Func<object, bool> condition, Action<IRenderData> trueFn, Action<IRenderData> falseFn)> _conditions;
    
    public RenderData(Type componentType, IDictionary<string, object> attributes = null)
    {
        ComponentType = componentType;
        SetAttributes(attributes);
    }

    internal virtual Type FieldType => null;
    internal virtual Type PropertyType => null;

    public IRenderData SetAttributes(IDictionary<string, object> attributes)
    {
        if (attributes != null)
        {
            Attributes = attributes;//attributes.ToDictionary(entry => entry.Key, entry => entry.Value);
            RemoveUnsafeAttributes();
        }

        return this;
    }

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

    public IRenderData TrySetAttributeIfAllowed(string key, object value, bool condition = true)
    {
        //if (condition && value != null && (!Attributes.ContainsKey(key) || Attributes[key] == null) && IsValidParameterAttribute(key))
        if (condition && value != null && IsValidParameterAttribute(key, value))
            Attributes.AddOrUpdate(key, value);
        
        return this;
    }
    
    public object Clone()
    {
        var result = (RenderData)MemberwiseClone();
        
        result.Attributes = Attributes?.ToDictionary(entry => entry.Key, entry => entry.Value);
        result.RenderDataBeforeComponent = RenderDataBeforeComponent?.Select(r => r.Clone() as IRenderData).ToList();
        result.RenderDataAfterComponent = RenderDataAfterComponent?.Select(r => r.Clone() as IRenderData).ToList();
        result.Wrapper = Wrapper?.Clone() as IRenderData;
        result._conditions = _conditions?.Select(c => (c.modelType, c.condition, c.trueFn, c.falseFn)).ToList();

        return result;
    }


    public virtual void UpdateConditionalSettings<TModel>(TModel model)
    {
        _conditions?.Where(c => c.modelType == typeof(TModel)).Apply(condition => (condition.condition(model) ? condition.trueFn : condition.falseFn)(this));
        _conditions?.Where(c => c.modelType == ComponentType).Apply(condition =>
        {
            var componentInstanceClone = Attributes.ToObject(ComponentType);
            (condition.condition(componentInstanceClone) ? condition.trueFn : condition.falseFn)(this);
        });
        Wrapper?.UpdateConditionalSettings(model);
    }

    public void AddCondition<TModel>(Func<TModel, bool> condition, Action<IRenderData> trueFn, Action<IRenderData> falseFn)
    {
        _conditions ??= new List<(Type modelType, Func<object, bool> condition, Action<IRenderData> trueFn, Action<IRenderData> falseFn)>();
        _conditions.Add((typeof(TModel), model => condition((TModel) model), trueFn, falseFn));
    }


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

    public IRenderData SetAttributesIf<TModel>(Func<TModel, bool> condition, IDictionary<string, object> attributes)
    {
        var existing = Attributes?.Any() == true ? new Dictionary<string, object>(Attributes) : new Dictionary<string, object>();
        AddCondition(condition, rd => rd.SetAttributes(attributes), rd => rd.SetAttributes(existing));
        return this;
    }

}