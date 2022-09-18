using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Localization;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using Nextended.Core.Extensions;
using Nextended.Core.Helper;

namespace MudBlazor.Extensions.Components.ObjectEdit;

public static partial class MudExObjectEditExtensions
{
    // Unfinished
    public static ObjectEditPropertyMeta RenderWithMudAutocomplete<TPropertyType>(this ObjectEditPropertyMeta meta,  TPropertyType[] items)
        => meta.RenderWithMudAutocomplete(_ => { }, false, true, items);
    public static ObjectEditPropertyMeta RenderWithMudAutocomplete<TPropertyType>(this ObjectEditPropertyMeta meta, bool requireValueFromSuggestion, TPropertyType[] items)
        => meta.RenderWithMudAutocomplete(_ => { }, requireValueFromSuggestion, true, items);
    public static ObjectEditPropertyMeta RenderWithMudAutocomplete<TPropertyType>(this ObjectEditPropertyMeta meta, bool requireValueFromSuggestion, bool filterSuggestionForValue, TPropertyType[] items)
        => meta.RenderWithMudAutocomplete(_ => { }, requireValueFromSuggestion, filterSuggestionForValue, items);

    public static ObjectEditPropertyMeta RenderWithMudAutocomplete<TPropertyType>(this ObjectEditPropertyMeta meta, Type suggesttionsFromEnum, bool requireValueFromSuggestion, bool filterSuggestionForValue = true)
        => meta.RenderWithMudAutocomplete<TPropertyType>(suggesttionsFromEnum, null, requireValueFromSuggestion, filterSuggestionForValue);
    public static ObjectEditPropertyMeta RenderWithMudAutocomplete<TPropertyType>(this ObjectEditPropertyMeta meta, Type suggesttionsFromEnum, Action<MudAutocomplete<TPropertyType>> options = null, bool requireValueFromSuggestion = true, bool filterSuggestionForValue = true)
    {
        var suggestionItems = suggesttionsFromEnum.IsNullableEnum() ? Nullable.GetUnderlyingType(suggesttionsFromEnum).GetEnumValues().MapElementsTo<TPropertyType>().ToArray() : suggesttionsFromEnum.GetEnumValues().MapElementsTo<TPropertyType>().ToArray();
        return meta.RenderWithMudAutocomplete(options, requireValueFromSuggestion, filterSuggestionForValue, suggestionItems);
    }
    public static ObjectEditPropertyMeta RenderWithMudAutocomplete<TPropertyType>(this ObjectEditPropertyMeta meta, Action<MudAutocomplete<TPropertyType>> options, bool requireValueFromSuggestion, bool filterSuggestionForValue,  params TPropertyType[] suggestionItems)
    {
        if ((typeof(TPropertyType).IsEnum || typeof(TPropertyType).IsNullableEnum()) && !suggestionItems.Any())
            return meta.RenderWithMudAutocomplete(typeof(TPropertyType), options, requireValueFromSuggestion);

        return meta.RenderWith<MudAutocomplete<TPropertyType>, TPropertyType>(ac => ac.Value, ac =>
        {
            ac.CoerceValue = !requireValueFromSuggestion;
            ac.SearchFunc = s => Task.Run(() =>
            {
                var res = !filterSuggestionForValue ? suggestionItems : suggestionItems.Where(x => string.IsNullOrWhiteSpace(s) || x?.ToString()?.Contains(s, StringComparison.InvariantCultureIgnoreCase) == true);
                if (!requireValueFromSuggestion)
                    return res.Prepend(s.MapTo<TPropertyType>()).Distinct();
                return res;
            });
            options?.Invoke(ac);
        });
    }

    public static IRenderData WrapIn(this ObjectEditPropertyMeta meta, IRenderData wrappingRenderData)
        => meta?.RenderData?.WrapIn(wrappingRenderData);

    // End Unfinished

    public static ObjectEditPropertyMeta RenderWith<TComponent, TPropertyType, TFieldType>(this ObjectEditPropertyMeta meta, Expression<Func<TComponent, TFieldType>> valueField, Action<TComponent> options, Func<TPropertyType, TFieldType> toFieldTypeConverter = null, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null) where TComponent : new()
    => meta?.SetProperties(p => p.RenderData = RenderData.For<TComponent, TPropertyType, TFieldType>(valueField, options, toFieldTypeConverter, toPropertyTypeConverter));
    public static ObjectEditPropertyMeta RenderWith<TComponent, TPropertyType, TFieldType>(this ObjectEditPropertyMeta meta, Expression<Func<TComponent, TFieldType>> valueField, TComponent instanceForAttributes, Func<TPropertyType, TFieldType> toFieldTypeConverter = null, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null) where TComponent : new()
        => meta?.SetProperties(p => p.RenderData = RenderData.For<TComponent, TPropertyType, TFieldType>(valueField, instanceForAttributes, toFieldTypeConverter, toPropertyTypeConverter));
    public static ObjectEditPropertyMeta RenderWith<TComponent, TPropertyType, TFieldType>(this ObjectEditPropertyMeta meta, Expression<Func<TComponent, TFieldType>> valueField, Func<TPropertyType, TFieldType> toFieldTypeConverter = null, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null)
        => meta?.SetProperties(p => p.RenderData = RenderData.For<TComponent, TPropertyType, TFieldType>(valueField, toFieldTypeConverter, toPropertyTypeConverter));
    public static ObjectEditPropertyMeta RenderWith<TComponent, TPropertyType>(this ObjectEditPropertyMeta meta, Expression<Func<TComponent, TPropertyType>> valueField, Action<TComponent> options) where TComponent : new()
        => meta?.SetProperties(p => p.RenderData = RenderData.For(valueField, options));
    public static ObjectEditPropertyMeta RenderWith<TComponent, TPropertyType>(this ObjectEditPropertyMeta meta, Expression<Func<TComponent, TPropertyType>> valueField, TComponent instanceForAttributes) where TComponent : new()
        => meta?.SetProperties(p => p.RenderData = RenderData.For(valueField, instanceForAttributes));
    public static ObjectEditPropertyMeta RenderWith<TComponent, TPropertyType>(this ObjectEditPropertyMeta meta, Expression<Func<TComponent, TPropertyType>> valueField)
        => meta?.SetProperties(p => p.RenderData = RenderData.For(valueField));
    public static ObjectEditPropertyMeta RenderWith<TComponent>(this ObjectEditPropertyMeta meta, Action<TComponent> options) where TComponent : new()
        => meta?.RenderWith(typeof(TComponent), DictionaryHelper.GetValuesDictionary(options, true));
    public static ObjectEditPropertyMeta RenderWith<TComponent>(this ObjectEditPropertyMeta meta, IDictionary<string, object> attributes = null)
        => meta?.RenderWith(typeof(TComponent), attributes);
    public static ObjectEditPropertyMeta RenderWith(this ObjectEditPropertyMeta meta, Type componentType, IDictionary<string, object> attributes = null)
        => meta?.SetProperties(p => p.RenderData = RenderData.For(componentType, attributes));
    public static ObjectEditPropertyMeta RenderWith(this ObjectEditPropertyMeta meta, ICustomRenderer renderer)
        => meta?.SetProperties(m => (m.RenderData ??= new RenderData(null)).SetProperties(d => d.CustomRenderer = renderer));
    public static ObjectEditPropertyMeta AsReadOnly(this ObjectEditPropertyMeta meta, bool isReadOnly = true)
        => meta?.SetProperties(p => p.Settings.IsEditable = !isReadOnly, p => p?.RenderData?.AddAttributes(true, new KeyValuePair<string, object>(nameof(MudBaseInput<string>.ReadOnly), isReadOnly)));
    public static ObjectEditPropertyMeta DisableUnderline(this ObjectEditPropertyMeta meta, bool disableUnderline = true)
        => meta?.SetProperties(p => p?.RenderData?.AddAttributes(true, new KeyValuePair<string, object>(nameof(MudBaseInput<string>.DisableUnderLine), disableUnderline)));
    public static ObjectEditPropertyMeta Ignore(this ObjectEditPropertyMeta meta, bool ignore = true)
        => meta?.WithSettings(s => s.Ignored = ignore);
    public static ObjectEditPropertyMeta WithResetOptions(this ObjectEditPropertyMeta meta, bool allowReset, string resetIcon, bool showResetText, string resetText)
        => meta?.WithResetOptions(new PropertyResetSettings() { AllowReset = allowReset, ResetIcon = resetIcon, ResetText = resetText, ShowResetText = showResetText });
    public static ObjectEditPropertyMeta WithResetOptions(this ObjectEditPropertyMeta meta, PropertyResetSettings resetSettings)
        => meta?.WithSettings(s => s.ResetSettings = resetSettings);
    public static ObjectEditPropertyMeta WithResetOptions(this ObjectEditPropertyMeta meta, Action<PropertyResetSettings> resetSettingsAction)
        => meta?.WithSettings(s => resetSettingsAction(s.ResetSettings ??= new PropertyResetSettings()));
    public static ObjectEditPropertyMeta IsResettable(this ObjectEditPropertyMeta meta, bool resettable = true)
        => meta?.WithResetOptions(s => s.AllowReset = resettable);
    public static ObjectEditPropertyMeta NotResettable(this ObjectEditPropertyMeta meta)
        => meta?.IsResettable(false);
    public static ObjectEditPropertyMeta WithSettings(this ObjectEditPropertyMeta meta, Action<ObjectEditPropertyMetaSettings> settingsAction)
        => meta?.SetProperties(p => settingsAction(p.Settings ??= new ObjectEditPropertyMetaSettings(meta)));
    public static ObjectEditPropertyMeta WithSeparateLabelComponent(this ObjectEditPropertyMeta meta)
        => meta?.WithSettings(s => s.LabelBehaviour = LabelBehaviour.Both);
    public static ObjectEditPropertyMeta WithSeparateValidationComponent(this ObjectEditPropertyMeta meta)
        => meta?.WithSettings(s => s.ValidationComponent = true);
    public static ObjectEditPropertyMeta WithoutLabel(this ObjectEditPropertyMeta meta)
        => meta?.WithSettings(s => s.LabelBehaviour = LabelBehaviour.NoLabel);
    public static ObjectEditPropertyMeta WithSeparateLabelComponentOnly(this ObjectEditPropertyMeta meta)
        => meta?.WithSettings(s => s.LabelBehaviour = LabelBehaviour.SeparateLabelComponentOnly);
    public static ObjectEditPropertyMeta WithDefaultLabeling(this ObjectEditPropertyMeta meta)
        => meta?.WithSettings(s => s.LabelBehaviour = LabelBehaviour.DefaultComponentLabeling);
    public static ObjectEditPropertyMeta WithOrder(this ObjectEditPropertyMeta meta, int order)
        => meta?.SetProperties(p => p.Settings.Order = order);
    public static ObjectEditPropertyMeta WithLabel(this ObjectEditPropertyMeta meta, string label)
        => meta?.SetProperties(p => p.Settings.LabelResolverFn = _ => label);
    public static ObjectEditPropertyMeta WithDescription(this ObjectEditPropertyMeta meta, string description)
        => meta?.SetProperties(p => p.Settings.DescriptionResolverFn = _ => description);
    public static ObjectEditPropertyMeta WithLabelResolver(this ObjectEditPropertyMeta meta, Func<PropertyInfo, string> resolverFunc)
        => meta?.SetProperties(p => p.Settings.LabelResolverFn = resolverFunc);
    public static ObjectEditPropertyMeta WithDescriptionResolver(this ObjectEditPropertyMeta meta, Func<PropertyInfo, string> resolverFunc)
        => meta?.SetProperties(p => p.Settings.DescriptionResolverFn = resolverFunc);
    public static ObjectEditPropertyMeta WithLabelLocalizerPattern(this ObjectEditPropertyMeta meta, string pattern = "Label_{0}", IStringLocalizer localizer = null)
        => meta?.SetProperties(p => p.Settings.Localizer = localizer).WithLabelResolver(info => string.Format(pattern, info.Name));
    public static ObjectEditPropertyMeta WithDescriptionLocalizerPattern(this ObjectEditPropertyMeta meta, string pattern = "Description_{0}", IStringLocalizer localizer = null)
        => meta?.SetProperties(p => p.Settings.Localizer = localizer).WithDescriptionResolver(info => string.Format(pattern, info.Name));
    public static ObjectEditPropertyMeta WithGroup(this ObjectEditPropertyMeta meta, string groupName)
        => meta?.WithGroup(new ObjectEditPropertyMetaGroupInfo { Name = groupName });
    public static ObjectEditPropertyMeta WithGroup(this ObjectEditPropertyMeta meta, ObjectEditPropertyMetaGroupInfo groupInfo)
        => meta?.SetProperties(p => p.GroupInfo = groupInfo);
    public static IRenderData WrapIn<TWrapperComponent>(this ObjectEditPropertyMeta meta, params Action<TWrapperComponent>[] options) where TWrapperComponent : new()
        => meta?.RenderData?.WrapIn(options);
    public static IRenderData WrapInMudItem(this ObjectEditPropertyMeta meta, params Action<MudItem>[] options)
        => meta?.WrapIn(options);
    public static ObjectEditPropertyMeta WithAdditionalAttributes(this ObjectEditPropertyMeta meta, params KeyValuePair<string, object>[] attributes)
        => meta?.WithAdditionalAttributes(false, attributes);
    public static ObjectEditPropertyMeta WithAdditionalAttributes(this ObjectEditPropertyMeta meta, bool overwriteExisting, params KeyValuePair<string, object>[] attributes)
        => meta?.SetProperties(p => p.RenderData?.AddAttributes(overwriteExisting, attributes));
    public static ObjectEditPropertyMeta WithAdditionalAttributes(this ObjectEditPropertyMeta meta, IDictionary<string, object> attributes, bool overwriteExisting = false)
        => meta?.WithAdditionalAttributes(overwriteExisting, attributes.ToArray());
    public static ObjectEditPropertyMeta WithAdditionalAttributes(this ObjectEditPropertyMeta meta, Dictionary<string, object> attributes, bool overwriteExisting = false)
        => meta?.WithAdditionalAttributes(overwriteExisting, attributes.ToArray());
    public static ObjectEditPropertyMeta WithAdditionalAttribute(this ObjectEditPropertyMeta meta, string key, object value, bool overwriteExisting = false)
        => meta?.WithAdditionalAttributes(overwriteExisting, new KeyValuePair<string, object>(key, value));
    public static ObjectEditPropertyMeta WithAdditionalAttributes<TComponent>(this ObjectEditPropertyMeta meta, bool overwriteExisting, params Action<TComponent>[] options) where TComponent : new()
        => meta?.WithAdditionalAttributes(DictionaryHelper.GetValuesDictionary(true, options), overwriteExisting);
    public static ObjectEditPropertyMeta WithAdditionalAttributes<TComponent>(this ObjectEditPropertyMeta meta, params Action<TComponent>[] options) where TComponent : new()
        => meta?.WithAdditionalAttributes(DictionaryHelper.GetValuesDictionary(true, options));
    public static ObjectEditPropertyMeta WithAdditionalAttributes<TComponent>(this ObjectEditPropertyMeta meta, TComponent instanceForAttributes, bool overwriteExisting = false) where TComponent : new()
        => meta?.WithAdditionalAttributes(DictionaryHelper.GetValuesDictionary(instanceForAttributes, true), overwriteExisting);
    public static ObjectEditPropertyMeta IgnoreIf<TModel>(this ObjectEditPropertyMeta meta, Func<TModel, bool> condition)
        => meta?.WithSettings(settings => settings?.AddCondition(condition, s => s.Ignored = true, s => s.Ignored = false));
    public static ObjectEditPropertyMeta AsReadOnlyIf<TModel>(this ObjectEditPropertyMeta meta, Func<TModel, bool> condition)
        => meta?.WithSettings(settings => settings?.AddCondition(condition, s => s.IsEditable = false, s => s.IsEditable = true)).WithAttributesIf(condition, new KeyValuePair<string, object>(nameof(MudBaseInput<string>.ReadOnly), true));
    public static ObjectEditPropertyMeta WithAttributesIf<TModel>(this ObjectEditPropertyMeta meta, Func<TModel, bool> condition, params KeyValuePair<string, object>[] attributes)
        => meta?.SetProperties(p => p.RenderData?.AddAttributesIf(condition, true, attributes));
    public static ObjectEditPropertyMeta WithAttributesIf<TModel, TComponent>(this ObjectEditPropertyMeta meta, Func<TModel, bool> condition, params Action<TComponent>[] options) where TComponent : new()
        => meta?.WithAttributesIf(condition, DictionaryHelper.GetValuesDictionary(true, options));
    public static ObjectEditPropertyMeta WithAttributesIf<TModel>(this ObjectEditPropertyMeta meta, Func<TModel, bool> condition, IDictionary<string, object> attributes)
        => meta?.WithAttributesIf(condition, attributes.ToArray());
    public static ObjectEditPropertyMeta WithAttributesIf<TModel>(this ObjectEditPropertyMeta meta, Func<TModel, bool> condition, Dictionary<string, object> attributes)
        => meta?.WithAttributesIf(condition, attributes.ToArray());
    //public static ObjectEditPropertyMeta WithAttributesIf<TModel, TComponent>(this ObjectEditPropertyMeta meta, Func<TModel, bool> condition, TComponent instanceForAttributes) where TComponent : new()
    //    => meta?.WithAttributesIf(condition, DictionaryHelper.GetValuesDictionary(instanceForAttributes, true));
}