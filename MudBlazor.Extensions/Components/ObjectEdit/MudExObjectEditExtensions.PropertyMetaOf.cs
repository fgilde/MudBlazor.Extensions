using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Localization;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using Nextended.Core.Extensions;
using Nextended.Core.Helper;

namespace MudBlazor.Extensions.Components.ObjectEdit;

public static partial class MudExObjectEditExtensions
{
    public static ObjectEditPropertyMetaOf<TModel> RenderWith<TModel, TComponent, TPropertyType, TFieldType>(this ObjectEditPropertyMetaOf<TModel> meta, Expression<Func<TComponent, TFieldType>> valueField, Action<TComponent> options, Func<TPropertyType, TFieldType> toFieldTypeConverter = null, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null) where TComponent : new()
    => meta?.SetProperties(p => p.RenderData = RenderData.For<TComponent, TPropertyType, TFieldType>(valueField, options, toFieldTypeConverter, toPropertyTypeConverter));
    public static ObjectEditPropertyMetaOf<TModel> RenderWith<TModel, TComponent, TPropertyType, TFieldType>(this ObjectEditPropertyMetaOf<TModel> meta, Expression<Func<TComponent, TFieldType>> valueField, TComponent instanceForAttributes, Func<TPropertyType, TFieldType> toFieldTypeConverter = null, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null) where TComponent : new()
        => meta?.SetProperties(p => p.RenderData = RenderData.For<TComponent, TPropertyType, TFieldType>(valueField, instanceForAttributes, toFieldTypeConverter, toPropertyTypeConverter));
    public static ObjectEditPropertyMetaOf<TModel> RenderWith<TModel, TComponent, TPropertyType, TFieldType>(this ObjectEditPropertyMetaOf<TModel> meta, Expression<Func<TComponent, TFieldType>> valueField, Func<TPropertyType, TFieldType> toFieldTypeConverter = null, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null)
        => meta?.SetProperties(p => p.RenderData = RenderData.For<TComponent, TPropertyType, TFieldType>(valueField, toFieldTypeConverter, toPropertyTypeConverter));
    public static ObjectEditPropertyMetaOf<TModel> RenderWith<TModel, TComponent, TPropertyType>(this ObjectEditPropertyMetaOf<TModel> meta, Expression<Func<TComponent, TPropertyType>> valueField, Action<TComponent> options) where TComponent : new()
        => meta?.SetProperties(p => p.RenderData = RenderData.For(valueField, options));
    public static ObjectEditPropertyMetaOf<TModel> RenderWith<TModel, TComponent, TPropertyType>(this ObjectEditPropertyMetaOf<TModel> meta, Expression<Func<TComponent, TPropertyType>> valueField, TComponent instanceForAttributes) where TComponent : new()
        => meta?.SetProperties(p => p.RenderData = RenderData.For(valueField, instanceForAttributes));
    public static ObjectEditPropertyMetaOf<TModel> RenderWith<TModel, TComponent, TPropertyType>(this ObjectEditPropertyMetaOf<TModel> meta, Expression<Func<TComponent, TPropertyType>> valueField)
        => meta?.SetProperties(p => p.RenderData = RenderData.For(valueField));
    public static ObjectEditPropertyMetaOf<TModel> RenderWith<TModel, TComponent>(this ObjectEditPropertyMetaOf<TModel> meta, Action<TComponent> options) where TComponent : new()
        => meta?.RenderWith<TModel>(typeof(TComponent), DictionaryHelper.GetValuesDictionary(options, true));
    public static ObjectEditPropertyMetaOf<TModel> RenderWith<TModel, TComponent>(this ObjectEditPropertyMetaOf<TModel> meta, IDictionary<string, object> attributes = null)
        => meta?.RenderWith(typeof(TComponent), attributes);
    public static ObjectEditPropertyMetaOf<TModel> RenderWith<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, Type componentType, IDictionary<string, object> attributes = null)
        => meta?.SetProperties(p => p.RenderData = RenderData.For(componentType, attributes));
    public static ObjectEditPropertyMetaOf<TModel> RenderWith<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, ICustomRenderer renderer)
        => meta?.SetProperties(m => (m.RenderData ??= new RenderData(null)).SetProperties(d => d.CustomRenderer = renderer));
    public static ObjectEditPropertyMetaOf<TModel> AsReadOnly<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, bool isReadOnly = true)
        => meta?.SetProperties(p => p.Settings.IsEditable = !isReadOnly, p => p?.RenderData?.AddAttributes(true, new KeyValuePair<string, object>(nameof(MudBaseInput<string>.ReadOnly), isReadOnly)));
    public static ObjectEditPropertyMetaOf<TModel> DisableUnderline<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, bool disableUnderline = true)
        => meta?.SetProperties(p => p?.RenderData?.AddAttributes(true, new KeyValuePair<string, object>(nameof(MudBaseInput<string>.DisableUnderLine), disableUnderline)));
    public static ObjectEditPropertyMetaOf<TModel> Ignore<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, bool ignore = true)
        => meta?.WithSettings(s => s.Ignored = ignore);
    public static ObjectEditPropertyMetaOf<TModel> WithResetOptions<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, bool allowReset, string resetIcon, bool showResetText, string resetText)
        => meta?.WithResetOptions(new PropertyResetSettings() { AllowReset = allowReset, ResetIcon = resetIcon, ResetText = resetText, ShowResetText = showResetText });
    public static ObjectEditPropertyMetaOf<TModel> WithResetOptions<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, PropertyResetSettings resetSettings)
        => meta?.WithSettings(s => s.ResetSettings = resetSettings);
    public static ObjectEditPropertyMetaOf<TModel> WithResetOptions<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, Action<PropertyResetSettings> resetSettingsAction)
        => meta?.WithSettings(s => resetSettingsAction(s.ResetSettings ??= new PropertyResetSettings()));
    public static ObjectEditPropertyMetaOf<TModel> IsResettable<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, bool resettable = true)
        => meta?.WithResetOptions(s => s.AllowReset = resettable);
    public static ObjectEditPropertyMetaOf<TModel> NotResettable<TModel>(this ObjectEditPropertyMetaOf<TModel> meta)
        => meta?.IsResettable(false);
    public static ObjectEditPropertyMetaOf<TModel> WithSettings<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, Action<ObjectEditPropertyMetaSettings> settingsAction)
        => meta?.SetProperties(p => settingsAction(p.Settings ??= new ObjectEditPropertyMetaSettings(meta)));
    public static ObjectEditPropertyMetaOf<TModel> WithSeparateLabelComponent<TModel>(this ObjectEditPropertyMetaOf<TModel> meta)
        => meta?.WithSettings(s => s.LabelBehaviour = LabelBehaviour.Both);
    public static ObjectEditPropertyMetaOf<TModel> WithoutLabel<TModel>(this ObjectEditPropertyMetaOf<TModel> meta)
        => meta?.WithSettings(s => s.LabelBehaviour = LabelBehaviour.NoLabel);
    public static ObjectEditPropertyMetaOf<TModel> WithSeparateLabelComponentOnly<TModel>(this ObjectEditPropertyMetaOf<TModel> meta)
        => meta?.WithSettings(s => s.LabelBehaviour = LabelBehaviour.SeparateLabelComponentOnly);
    public static ObjectEditPropertyMetaOf<TModel> WithDefaultLabeling<TModel>(this ObjectEditPropertyMetaOf<TModel> meta)
        => meta?.WithSettings(s => s.LabelBehaviour = LabelBehaviour.DefaultComponentLabeling);
    public static ObjectEditPropertyMetaOf<TModel> WithOrder<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, int order)
        => meta?.SetProperties(p => p.Settings.Order = order);
    public static ObjectEditPropertyMetaOf<TModel> WithLabel<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, string label)
        => meta?.SetProperties(p => p.Settings.LabelResolverFn = _ => label);
    public static ObjectEditPropertyMetaOf<TModel> WithDescription<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, string description)
        => meta?.SetProperties(p => p.Settings.DescriptionResolverFn = _ => description);
    public static ObjectEditPropertyMetaOf<TModel> WithLabelResolver<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, Func<PropertyInfo, string> resolverFunc)
        => meta?.SetProperties(p => p.Settings.LabelResolverFn = resolverFunc);
    public static ObjectEditPropertyMetaOf<TModel> WithDescriptionResolver<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, Func<PropertyInfo, string> resolverFunc)
        => meta?.SetProperties(p => p.Settings.DescriptionResolverFn = resolverFunc);
    public static ObjectEditPropertyMetaOf<TModel> WithLabelLocalizerPattern<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, string pattern = "Label_{0}", IStringLocalizer localizer = null)
        => meta?.SetProperties(p => p.Settings.Localizer = localizer).WithLabelResolver(info => string.Format(pattern, info.Name));
    public static ObjectEditPropertyMetaOf<TModel> WithDescriptionLocalizerPattern<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, string pattern = "Description_{0}", IStringLocalizer localizer = null)
        => meta?.SetProperties(p => p.Settings.Localizer = localizer).WithDescriptionResolver(info => string.Format(pattern, info.Name));
    public static ObjectEditPropertyMetaOf<TModel> WithGroup<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, string groupName)
        => meta?.WithGroup(new ObjectEditPropertyMetaGroupInfo { Name = groupName });
    public static ObjectEditPropertyMetaOf<TModel> WithGroup<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, ObjectEditPropertyMetaGroupInfo groupInfo)
        => meta?.SetProperties(p => p.GroupInfo = groupInfo);
    public static ObjectEditPropertyMetaOf<TModel> WithAdditionalAttributes<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, params KeyValuePair<string, object>[] attributes)
        => meta?.WithAdditionalAttributes(false, attributes);
    public static ObjectEditPropertyMetaOf<TModel> WithAdditionalAttributes<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, bool overwriteExisting, params KeyValuePair<string, object>[] attributes)
        => meta?.SetProperties(p => p.RenderData?.AddAttributes(overwriteExisting, attributes));
    public static ObjectEditPropertyMetaOf<TModel> WithAdditionalAttributes<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, IDictionary<string, object> attributes, bool overwriteExisting = false)
        => meta?.WithAdditionalAttributes(overwriteExisting, attributes.ToArray());
    public static ObjectEditPropertyMetaOf<TModel> WithAdditionalAttributes<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, Dictionary<string, object> attributes, bool overwriteExisting = false)
        => meta?.WithAdditionalAttributes(overwriteExisting, attributes.ToArray());
    public static ObjectEditPropertyMetaOf<TModel> WithAdditionalAttribute<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, string key, object value, bool overwriteExisting = false)
        => meta?.WithAdditionalAttributes(overwriteExisting, new KeyValuePair<string, object>(key, value));
    public static ObjectEditPropertyMetaOf<TModel> WithAdditionalAttributes<TModel, TComponent>(this ObjectEditPropertyMetaOf<TModel> meta, bool overwriteExisting, params Action<TComponent>[] options) where TComponent : new()
        => meta?.WithAdditionalAttributes(DictionaryHelper.GetValuesDictionary(true, options), overwriteExisting);
    public static ObjectEditPropertyMetaOf<TModel> WithAdditionalAttributes<TModel, TComponent>(this ObjectEditPropertyMetaOf<TModel> meta, params Action<TComponent>[] options) where TComponent : new()
        => meta?.WithAdditionalAttributes(DictionaryHelper.GetValuesDictionary(true, options));
    public static ObjectEditPropertyMetaOf<TModel> WithAdditionalAttributes<TModel, TComponent>(this ObjectEditPropertyMetaOf<TModel> meta, TComponent instanceForAttributes, bool overwriteExisting = false) where TComponent : new()
        => meta?.WithAdditionalAttributes(DictionaryHelper.GetValuesDictionary(instanceForAttributes, true), overwriteExisting);
    public static ObjectEditPropertyMetaOf<TModel> IgnoreIf<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, Func<TModel, bool> condition)
        => meta?.WithSettings(settings => settings?.AddCondition(condition, s => s.Ignored = true, s => s.Ignored = false));
    public static ObjectEditPropertyMetaOf<TModel> AsReadOnlyIf<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, Func<TModel, bool> condition)
        => meta?.WithSettings(settings => settings?.AddCondition(condition, s => s.IsEditable = false, s => s.IsEditable = true)).WithAttributesIf(condition, new KeyValuePair<string, object>(nameof(MudBaseInput<string>.ReadOnly), true));
    public static ObjectEditPropertyMetaOf<TModel> WithAttributesIf<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, Func<TModel, bool> condition, params KeyValuePair<string, object>[] attributes)
        => meta?.SetProperties(p => p.RenderData?.AddAttributesIf(condition, true, attributes));
    public static ObjectEditPropertyMetaOf<TModel> WithAttributesIf<TModel, TComponent>(this ObjectEditPropertyMetaOf<TModel> meta, Func<TModel, bool> condition, params Action<TComponent>[] options) where TComponent : new()
        => meta?.WithAttributesIf(condition, DictionaryHelper.GetValuesDictionary(true, options));
    public static ObjectEditPropertyMetaOf<TModel> WithAttributesIf<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, Func<TModel, bool> condition, IDictionary<string, object> attributes)
        => meta?.WithAttributesIf(condition, attributes.ToArray());
    public static ObjectEditPropertyMetaOf<TModel> WithAttributesIf<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, Func<TModel, bool> condition, Dictionary<string, object> attributes)
        => meta?.WithAttributesIf(condition, attributes.ToArray());
    //public static ObjectEditPropertyMetaOf<TModel> WithAttributesIf<TModel, TComponent>(this ObjectEditPropertyMetaOf<TModel> meta, Func<TModel, bool> condition, TComponent instanceForAttributes) where TComponent : new()
    //    => meta?.WithAttributesIf(condition, DictionaryHelper.GetValuesDictionary(instanceForAttributes, true));
}