namespace MudBlazor.Extensions.Components.ObjectEdit.Options;

public interface IRenderData : ICloneable
{
    bool IsValidParameterAttribute(string key, object value);
    public Type ComponentType { get; }
    public IDictionary<string, object> Attributes { get; }
    public IRenderData InitValueBinding(ObjectEditPropertyMeta propertyMeta, Func<Task> valueChanged);
    public ICustomRenderer CustomRenderer { get; set; }
    public IRenderData Wrapper { get; set; }
    void UpdateConditionalSettings<TModel>(TModel model);
    void AddCondition<TModel>(Func<TModel, bool> condition, Action<IRenderData> trueFn, Action<IRenderData> falseFn);
    IRenderData TrySetAttributeIfAllowed(string key, Func<object> valueFn, bool condition = true);
    IRenderData TrySetAttributeIfAllowed(string key, object value, bool condition = true);
    IRenderData SetAttributes(IDictionary<string, object> attributes);
    IRenderData AddAttributes(bool overwriteExisting, params KeyValuePair<string, object>[] attributes);
    IRenderData AddAttributesIf<TModel>(Func<TModel, bool> condition, bool overwriteExisting, params KeyValuePair<string, object>[] attributes);
    IRenderData SetAttributesIf<TModel>(Func<TModel, bool> condition, IDictionary<string, object> attributes);
    object ConvertToPropertyValue(object value);
}