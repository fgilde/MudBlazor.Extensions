using Microsoft.AspNetCore.Components;

namespace MudBlazor.Extensions.Components.ObjectEdit.Options;

public interface IRenderData : ICloneable
{
    public IRenderData Wrapper { get; set; }
    public IList<IRenderData> RenderDataBeforeComponent { get; }
    public IList<IRenderData> RenderDataAfterComponent { get; }
    bool IsValidParameterAttribute(string key, object value);
    public Type ComponentType { get; }
    public IDictionary<string, object> Attributes { get; }
    public IDictionary<string, object> ValidAttributes { get; }
    public IRenderData InitValueBinding(ObjectEditPropertyMeta propertyMeta, Func<Task> valueChanged);
    public ICustomRenderer CustomRenderer { get; set; }
    void UpdateConditionalSettings<TModel>(TModel model);
    void AddCondition<TModel>(Func<TModel, bool> condition, Action<IRenderData> trueFn, Action<IRenderData> falseFn);
    IRenderData TrySetAttributeIfAllowed(string key, Func<object> valueFn, bool condition = true);
    IRenderData TrySetAttributeIfAllowed(string key, object value, bool condition = true);
    IRenderData SetAttributes(IDictionary<string, object> attributes);
    IRenderData AddAttributes(bool overwriteExisting, params KeyValuePair<string, object>[] attributes);
    IRenderData AddAttributesIf<TModel>(Func<TModel, bool> condition, bool overwriteExisting, params KeyValuePair<string, object>[] attributes);
    IRenderData SetAttributesIf<TModel>(Func<TModel, bool> condition, IDictionary<string, object> attributes);
    object ConvertToPropertyValue(object value);
    bool DisableValueBinding { get; set; }
    void SetValue(object value);
    string ValueField { get; }

    public void OnRendered<TComponent>(Action<TComponent> onReferenceSet) where TComponent : class, IComponent;
}