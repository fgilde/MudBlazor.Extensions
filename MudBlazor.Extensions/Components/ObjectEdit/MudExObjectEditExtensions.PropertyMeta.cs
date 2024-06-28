using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Helper.Internal;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components.ObjectEdit;

public static partial class MudExObjectEditExtensions
{

    ///<summary>
    /// Renders the ObjectEditPropertyMeta with a MudAutocomplete component.
    ///</summary>
    ///<typeparam name="TPropertyType">The type of the property.</typeparam>
    ///<param name="meta">The ObjectEditPropertyMeta to render.</param>
    ///<param name="items">The array of items for autocomplete suggestions.</param>
    ///<returns>The rendered ObjectEditPropertyMeta.</returns>
    public static ObjectEditPropertyMeta RenderWithMudAutocomplete<TPropertyType>(this ObjectEditPropertyMeta meta, TPropertyType[] items)
        => meta.RenderWithMudAutocomplete(_ => { }, false, true, items);

    ///<summary>
    /// Renders the ObjectEditPropertyMeta with a MudAutocomplete component.
    ///</summary>
    ///<typeparam name="TPropertyType">The type of the property.</typeparam>
    ///<param name="meta">The ObjectEditPropertyMeta to render.</param>
    ///<param name="requireValueFromSuggestion">A boolean indicating whether the value must come from the suggestions.</param>
    ///<param name="items">The array of items for autocomplete suggestions.</param>
    ///<returns>The rendered ObjectEditPropertyMeta.</returns>
    public static ObjectEditPropertyMeta RenderWithMudAutocomplete<TPropertyType>(this ObjectEditPropertyMeta meta, bool requireValueFromSuggestion, TPropertyType[] items)
        => meta.RenderWithMudAutocomplete(_ => { }, requireValueFromSuggestion, true, items);

    ///<summary>
    /// Renders the ObjectEditPropertyMeta with a MudAutocomplete component.
    ///</summary>
    ///<typeparam name="TPropertyType">The type of the property.</typeparam>
    ///<param name="meta">The ObjectEditPropertyMeta to render.</param>
    ///<param name="requireValueFromSuggestion">A boolean indicating whether the value must come from the suggestions.</param>
    ///<param name="filterSuggestionForValue">A boolean indicating whether to filter suggestions based on the input value.</param>
    ///<param name="items">The array of items for autocomplete suggestions.</param>
    ///<returns>The rendered ObjectEditPropertyMeta.</returns>
    public static ObjectEditPropertyMeta RenderWithMudAutocomplete<TPropertyType>(this ObjectEditPropertyMeta meta, bool requireValueFromSuggestion, bool filterSuggestionForValue, TPropertyType[] items)
        => meta.RenderWithMudAutocomplete(_ => { }, requireValueFromSuggestion, filterSuggestionForValue, items);

    ///<summary>
    /// Renders the ObjectEditPropertyMeta with a MudAutocomplete component.
    ///</summary>
    ///<typeparam name="TPropertyType">The type of the property.</typeparam>
    ///<param name="meta">The ObjectEditPropertyMeta to render.</param>
    ///<param name="suggestionsFromEnum">The type of enum for suggestions.</param>
    ///<param name="requireValueFromSuggestion">A boolean indicating whether the value must come from the suggestions.</param>
    ///<param name="filterSuggestionForValue">A boolean indicating whether to filter suggestions based on the input value.</param>
    ///<returns>The rendered ObjectEditPropertyMeta.</returns>
    public static ObjectEditPropertyMeta RenderWithMudAutocomplete<TPropertyType>(this ObjectEditPropertyMeta meta, Type suggestionsFromEnum, bool requireValueFromSuggestion, bool filterSuggestionForValue = true)
        => meta.RenderWithMudAutocomplete<TPropertyType>(suggestionsFromEnum, null, requireValueFromSuggestion, filterSuggestionForValue);

    ///<summary>
    /// Renders the ObjectEditPropertyMeta with a MudAutocomplete component.
    ///</summary>
    ///<typeparam name="TPropertyType">The type of the property.</typeparam>
    ///<param name="meta">The ObjectEditPropertyMeta to render.</param>
    ///<param name="suggestionsFromEnum">The type of enum for suggestions.</param>
    ///<param name="options">Additional options for configuring the MudAutocomplete component.</param>
    ///<param name="requireValueFromSuggestion">A boolean indicating whether the value must come from the suggestions.</param>
    ///<param name="filterSuggestionForValue">A boolean indicating whether to filter suggestions based on the input value.</param>
    ///<returns>The rendered ObjectEditPropertyMeta.</returns>
    public static ObjectEditPropertyMeta RenderWithMudAutocomplete<TPropertyType>(this ObjectEditPropertyMeta meta, Type suggestionsFromEnum, Action<MudAutocomplete<TPropertyType>> options = null, bool requireValueFromSuggestion = true, bool filterSuggestionForValue = true)
    {
        var suggestionItems = suggestionsFromEnum.IsNullableEnum() ? Nullable.GetUnderlyingType(suggestionsFromEnum)!.GetEnumValues().MapElementsTo<TPropertyType>().ToArray() : suggestionsFromEnum.GetEnumValues().MapElementsTo<TPropertyType>().ToArray();
        return meta.RenderWithMudAutocomplete(options, requireValueFromSuggestion, filterSuggestionForValue, suggestionItems);
    }

    ///<summary>
    /// Renders the ObjectEditPropertyMeta with a MudAutocomplete component.
    ///</summary>
    ///<typeparam name="TPropertyType">The type of the property.</typeparam>
    ///<param name="meta">The ObjectEditPropertyMeta to render.</param>
    ///<param name="options">Additional options for configuring the MudAutocomplete component.</param>
    ///<param name="requireValueFromSuggestion">A boolean indicating whether the value must come from the suggestions.</param>
    ///<param name="filterSuggestionForValue">A boolean indicating whether to filter suggestions based on the input value.</param>
    ///<param name="suggestionItems">The array of items for autocomplete suggestions.</param>
    ///<returns>The rendered ObjectEditPropertyMeta.</returns>
    public static ObjectEditPropertyMeta RenderWithMudAutocomplete<TPropertyType>(this ObjectEditPropertyMeta meta, Action<MudAutocomplete<TPropertyType>> options, bool requireValueFromSuggestion, bool filterSuggestionForValue, params TPropertyType[] suggestionItems)
    {
        if ((typeof(TPropertyType).IsEnum || typeof(TPropertyType).IsNullableEnum()) && !suggestionItems.Any())
            return meta.RenderWithMudAutocomplete(typeof(TPropertyType), options, requireValueFromSuggestion);

        return meta.RenderWith<MudAutocomplete<TPropertyType>, TPropertyType>(ac => ac.Value, ac =>
        {
            ac.CoerceValue = !requireValueFromSuggestion;
            ac.SearchFunc = (s, token) => Task.Run(() =>
            {
                var res = !filterSuggestionForValue ? suggestionItems : suggestionItems.Where(x => string.IsNullOrWhiteSpace(s) || x?.ToString()?.Contains(s, StringComparison.InvariantCultureIgnoreCase) == true);
                return !requireValueFromSuggestion ? res.Prepend(s.MapTo<TPropertyType>()).Distinct() : res;
            }, token);
            options?.Invoke(ac);
        });
    }

    ///<summary>
    /// Wraps the ObjectEditPropertyMeta in the specified rendering data.
    ///</summary>
    ///<param name="meta">The ObjectEditPropertyMeta to wrap.</param>
    ///<param name="wrappingRenderData">The rendering data to wrap the ObjectEditPropertyMeta in.</param>
    ///<returns>The wrapped rendering data.</returns>
    public static IRenderData WrapIn(this ObjectEditPropertyMeta meta, IRenderData wrappingRenderData)
        => meta?.RenderData?.WrapIn(wrappingRenderData);

    ///<summary>
    /// Renders the ObjectEditPropertyMeta with the specified component, value field, and options.
    ///</summary>
    ///<typeparam name="TComponent">The type of the component to render.</typeparam>
    ///<typeparam name="TPropertyType">The type of the property.</typeparam>
    ///<typeparam name="TFieldType">The type of the field.</typeparam>
    ///<param name="meta">The ObjectEditPropertyMeta to render.</param>
    ///<param name="valueField">The expression representing the value field.</param>
    ///<param name="options">Additional options for configuring the component.</param>
    ///<param name="toFieldTypeConverter">A function to convert the property type to the field type.</param>
    ///<param name="toPropertyTypeConverter">A function to convert the field type to the property type.</param>
    ///<returns>The rendered ObjectEditPropertyMeta.</returns>
    public static ObjectEditPropertyMeta RenderWith<TComponent, TPropertyType, TFieldType>(this ObjectEditPropertyMeta meta, Expression<Func<TComponent, TFieldType>> valueField, Action<TComponent> options, Func<TPropertyType, TFieldType> toFieldTypeConverter = null, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null) where TComponent : new()
    => meta?.SetProperties(p => p.RenderData = RenderData.For(valueField, options, toFieldTypeConverter, toPropertyTypeConverter));

    ///<summary>
    /// Renders the ObjectEditPropertyMeta with the specified component, value field, and instance for attributes.
    ///</summary>
    ///<typeparam name="TComponent">The type of the component to render.</typeparam>
    ///<typeparam name="TPropertyType">The type of the property.</typeparam>
    ///<typeparam name="TFieldType">The type of the field.</typeparam>
    ///<param name="meta">The ObjectEditPropertyMeta to render.</param>
    ///<param name="valueField">The expression representing the value field.</param>
    ///<param name="instanceForAttributes">The instance for attributes.</param>
    ///<param name="toFieldTypeConverter">A function to convert the property type to the field type.</param>
    ///<param name="toPropertyTypeConverter">A function to convert the field type to the property type.</param>
    ///<returns>The rendered ObjectEditPropertyMeta.</returns>
    public static ObjectEditPropertyMeta RenderWith<TComponent, TPropertyType, TFieldType>(this ObjectEditPropertyMeta meta, Expression<Func<TComponent, TFieldType>> valueField, TComponent instanceForAttributes, Func<TPropertyType, TFieldType> toFieldTypeConverter = null, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null) where TComponent : new()
        => meta?.SetProperties(p => p.RenderData = RenderData.For(valueField, instanceForAttributes, toFieldTypeConverter, toPropertyTypeConverter));

    ///<summary>
    /// Extends ObjectEditPropertyMeta to render a property using a specified component and field converters.
    /// <typeparam name="TComponent">The component type used for rendering.</typeparam>
    /// <typeparam name="TPropertyType">The type of the property being edited.</typeparam>
    /// <typeparam name="TFieldType">The field type used within the rendering component.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMeta instance to extend.</param>
    /// <param name="valueField">An expression pointing to the component's field to bind.</param>
    /// <param name="toFieldTypeConverter">An optional converter from TPropertyType to TFieldType.</param>
    /// <param name="toPropertyTypeConverter">An optional converter from TFieldType back to TPropertyType.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta RenderWith<TComponent, TPropertyType, TFieldType>(this ObjectEditPropertyMeta meta, Expression<Func<TComponent, TFieldType>> valueField, Func<TPropertyType, TFieldType> toFieldTypeConverter = null, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null)
        => meta?.SetProperties(p => p.RenderData = RenderData.For(valueField, toFieldTypeConverter, toPropertyTypeConverter));

    ///<summary>
    /// Extends ObjectEditPropertyMeta to render a property using a specified component and additional options.
    /// <typeparam name="TComponent">The component type used for rendering where TComponent must be a class with a parameterless constructor.</typeparam>
    /// <typeparam name="TPropertyType">The type of the property being edited.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMeta instance to extend.</param>
    /// <param name="valueField">An expression pointing to the component's field to bind.</param>
    /// <param name="options">An action to configure additional options on the component.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta RenderWith<TComponent, TPropertyType>(this ObjectEditPropertyMeta meta, Expression<Func<TComponent, TPropertyType>> valueField, Action<TComponent> options) where TComponent : new()
        => meta?.SetProperties(p => p.RenderData = RenderData.For(valueField, options));

    ///<summary>
    /// Extends ObjectEditPropertyMeta to render a property using a specified component and an instance for attribute values.
    /// <typeparam name="TComponent">The component type used for rendering where TComponent must be a class with a parameterless constructor.</typeparam>
    /// <typeparam name="TPropertyType">The type of the property being edited.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMeta instance to extend.</param>
    /// <param name="valueField">An expression pointing to the component's field to bind.</param>
    /// <param name="instanceForAttributes">An instance of TComponent for extracting attribute values.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta RenderWith<TComponent, TPropertyType>(this ObjectEditPropertyMeta meta, Expression<Func<TComponent, TPropertyType>> valueField, TComponent instanceForAttributes) where TComponent : new()
        => meta?.SetProperties(p => p.RenderData = RenderData.For(valueField, instanceForAttributes));

    ///<summary>
    /// Extends ObjectEditPropertyMeta to render a property using a specified component.
    /// <typeparam name="TComponent">The component type used for rendering.</typeparam>
    /// <typeparam name="TPropertyType">The type of the property being edited.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMeta instance to extend.</param>
    /// <param name="valueField">An expression pointing to the component's field to bind.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta RenderWith<TComponent, TPropertyType>(this ObjectEditPropertyMeta meta, Expression<Func<TComponent, TPropertyType>> valueField)
        => meta?.SetProperties(p => p.RenderData = RenderData.For(valueField));

    ///<summary>
    /// Extends ObjectEditPropertyMeta to render a property using a specified component with additional options.
    /// <typeparam name="TComponent">The component type used for rendering where TComponent must be a class with a parameterless constructor.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMeta instance to extend.</param>
    /// <param name="options">An action to configure additional options on the component.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta RenderWith<TComponent>(this ObjectEditPropertyMeta meta, Action<TComponent> options) where TComponent : new()
        => meta?.RenderWith(typeof(TComponent), PropertyHelper.ValuesDictionary(options, true));

    ///<summary>
    /// Extends ObjectEditPropertyMeta to render a property using a specified component with a dictionary of attributes.
    /// <typeparam name="TComponent">The component type used for rendering.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMeta instance to extend.</param>
    /// <param name="attributes">A dictionary of attributes to apply to the component.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta RenderWith<TComponent>(this ObjectEditPropertyMeta meta, IDictionary<string, object> attributes = null)
        => meta?.RenderWith(typeof(TComponent), attributes);

    ///<summary>
    /// Extends ObjectEditPropertyMeta to render a property using a specified component type with a dictionary of attributes.
    /// <param name="meta">The ObjectEditPropertyMeta instance to extend.</param>
    /// <param name="componentType">The Type of the component used for rendering.</param>
    /// <param name="attributes">A dictionary of attributes to apply to the component.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta RenderWith(this ObjectEditPropertyMeta meta, Type componentType, IDictionary<string, object> attributes = null)
        => meta?.SetProperties(p => p.RenderData = RenderData.For(componentType, attributes));

    ///<summary>
    /// Extends ObjectEditPropertyMeta to render a property using a custom renderer.
    /// <param name="meta">The ObjectEditPropertyMeta instance to extend.</param>
    /// <param name="renderer">The custom renderer to use for rendering the property.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta RenderWith(this ObjectEditPropertyMeta meta, ICustomRenderer renderer)
        => meta?.SetProperties(m => (m.RenderData ??= new RenderData(null)).SetProperties(d => d.CustomRenderer = renderer));

    ///<summary>
    /// Marks an ObjectEditPropertyMeta as read-only.
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <param name="isReadOnly">Indicates whether the property should be read-only.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta AsReadOnly(this ObjectEditPropertyMeta meta, bool isReadOnly = true)
        => meta?.SetProperties(p => p.Settings.IsEditable = !isReadOnly, p => p?.RenderData?.AddAttributes(true, new KeyValuePair<string, object>(nameof(MudBaseInput<string>.ReadOnly), isReadOnly)));

    ///<summary>
    /// Disables the underline for an ObjectEditPropertyMeta.
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <param name="disableUnderline">Indicates whether the underline should be disabled.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta DisableUnderline(this ObjectEditPropertyMeta meta, bool underline = false)
        => meta?.SetProperties(p => p?.RenderData?.AddAttributes(true, new KeyValuePair<string, object>(nameof(MudBaseInput<string>.Underline), underline)));

    ///<summary>
    /// Ignores an ObjectEditPropertyMeta in the UI rendering process.
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <param name="ignore">Indicates whether the property should be ignored.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta Ignore(this ObjectEditPropertyMeta meta, bool ignore = true)
        => meta?.WithSettings(s => s.Ignored = ignore);

    ///<summary>
    /// Configures reset options for an ObjectEditPropertyMeta.
    /// <param name="meta">The ObjectEditPropertyMeta instance to configure.</param>
    /// <param name="allowReset">Indicates whether resetting is allowed.</param>
    /// <param name="resetIcon">The icon to use for the reset button.</param>
    /// <param name="showResetText">Indicates whether to show reset text.</param>
    /// <param name="resetText">The text to display for the reset option.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta WithResetOptions(this ObjectEditPropertyMeta meta, bool allowReset, string resetIcon, bool showResetText, string resetText)
        => meta?.WithResetOptions(new PropertyResetSettings() { AllowReset = allowReset, ResetIcon = resetIcon, ResetText = resetText, ShowResetText = showResetText });

    ///<summary>
    /// Applies a provided set of reset settings to an ObjectEditPropertyMeta.
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <param name="resetSettings">The reset settings to apply.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta WithResetOptions(this ObjectEditPropertyMeta meta, PropertyResetSettings resetSettings)
        => meta?.WithSettings(s => s.ResetSettings = resetSettings);

    ///<summary>
    /// Configures reset options for an ObjectEditPropertyMeta using a delegate.
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <param name="resetSettingsAction">A delegate to configure the reset settings.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta WithResetOptions(this ObjectEditPropertyMeta meta, Action<PropertyResetSettings> resetSettingsAction)
        => meta?.WithSettings(s => resetSettingsAction(s.ResetSettings ??= new PropertyResetSettings()));

    ///<summary>
    /// Sets whether an ObjectEditPropertyMeta is resettable.
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <param name="resettable">Indicates whether the property should be resettable.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta IsResettable(this ObjectEditPropertyMeta meta, bool resettable = true)
        => meta?.WithResetOptions(s => s.AllowReset = resettable);

    ///<summary>
    /// Sets an ObjectEditPropertyMeta as not resettable.
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance with reset disabled.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta NotResettable(this ObjectEditPropertyMeta meta)
        => meta?.IsResettable(false);

    ///<summary>
    /// Applies custom settings to an ObjectEditPropertyMeta.
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <param name="settingsAction">A delegate to configure the settings.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta WithSettings(this ObjectEditPropertyMeta meta, Action<ObjectEditPropertyMetaSettings> settingsAction)
        => meta?.SetProperties(p => settingsAction(p.Settings ??= new ObjectEditPropertyMetaSettings(meta)));

    ///<summary>
    /// Configures an ObjectEditPropertyMeta to use separate components for the label and the editor.
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance with separate label component enabled.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta WithSeparateLabelComponent(this ObjectEditPropertyMeta meta)
        => meta?.WithSettings(s => s.LabelBehaviour = LabelBehaviour.Both);

    ///<summary>
    /// Configures an ObjectEditPropertyMeta to use a separate validation component.
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance with separate validation component enabled.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta WithSeparateValidationComponent(this ObjectEditPropertyMeta meta)
        => meta?.WithSettings(s => s.ValidationComponent = true);

    ///<summary>
    /// Configures an ObjectEditPropertyMeta to not display a label.
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance without a label.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta WithoutLabel(this ObjectEditPropertyMeta meta)
        => meta?.WithSettings(s => s.LabelBehaviour = LabelBehaviour.NoLabel);

    ///<summary>
    /// Configures an ObjectEditPropertyMeta to only use a separate label component.
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance with only a separate label component.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta WithSeparateLabelComponentOnly(this ObjectEditPropertyMeta meta)
        => meta?.WithSettings(s => s.LabelBehaviour = LabelBehaviour.SeparateLabelComponentOnly);

    ///<summary>
    /// Sets the default labeling behavior for an ObjectEditPropertyMeta.
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance with default component labeling.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta WithDefaultLabeling(this ObjectEditPropertyMeta meta)
        => meta?.WithSettings(s => s.LabelBehaviour = LabelBehaviour.DefaultComponentLabeling);

    ///<summary>
    /// Sets the order of an ObjectEditPropertyMeta.
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <param name="order">The order index.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance with the specified order.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta WithOrder(this ObjectEditPropertyMeta meta, int order)
        => meta?.SetProperties(p => p.Settings.Order = order);

    ///<summary>
    /// Sets the label for an ObjectEditPropertyMeta.
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <param name="label">The label text.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance with the specified label.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta WithLabel(this ObjectEditPropertyMeta meta, string label)
        => meta?.SetProperties(p => p.Settings.LabelResolverFn = _ => label);

    ///<summary>
    /// Sets the description for an ObjectEditPropertyMeta.
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <param name="description">The description text.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance with the specified description.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta WithDescription(this ObjectEditPropertyMeta meta, string description)
        => meta?.SetProperties(p => p.Settings.DescriptionResolverFn = _ => description);

    ///<summary>
    /// Sets a custom label resolver function for an ObjectEditPropertyMeta.
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <param name="resolverFunc">The function to resolve label text.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance with the custom label resolver.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta WithLabelResolver(this ObjectEditPropertyMeta meta, Func<PropertyInfo, string> resolverFunc)
        => meta?.SetProperties(p => p.Settings.LabelResolverFn = resolverFunc);

    ///<summary>
    /// Sets a custom description resolver function for an ObjectEditPropertyMeta.
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <param name="resolverFunc">The function to resolve description text.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance with the custom description resolver.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta WithDescriptionResolver(this ObjectEditPropertyMeta meta, Func<PropertyInfo, string> resolverFunc)
        => meta?.SetProperties(p => p.Settings.DescriptionResolverFn = resolverFunc);

    ///<summary>
    /// Sets a label localizer pattern for an ObjectEditPropertyMeta.
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <param name="pattern">The pattern to use for localization, e.g., "Label_{0}".</param>
    /// <param name="localizer">The IStringLocalizer instance to use for localization.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance with the specified localizer pattern for labels.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta WithLabelLocalizerPattern(this ObjectEditPropertyMeta meta, string pattern = "Label_{0}", IStringLocalizer localizer = null)
        => meta?.SetProperties(p => p.Settings.Localizer = localizer).WithLabelResolver(info => string.Format(pattern, info.Name));

    ///<summary>
    /// Sets a description localizer pattern for an ObjectEditPropertyMeta.
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <param name="pattern">The pattern to use for localization, e.g., "Description_{0}".</param>
    /// <param name="localizer">The IStringLocalizer instance to use for localization.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance with the specified localizer pattern for descriptions.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta WithDescriptionLocalizerPattern(this ObjectEditPropertyMeta meta, string pattern = "Description_{0}", IStringLocalizer localizer = null)
        => meta?.SetProperties(p => p.Settings.Localizer = localizer).WithDescriptionResolver(info => string.Format(pattern, info.Name));

    ///<summary>
    /// Groups an ObjectEditPropertyMeta under a specified group name.
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <param name="groupName">The name of the group.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance grouped under the specified name.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta WithGroup(this ObjectEditPropertyMeta meta, string groupName)
        => meta?.WithGroup(new ObjectEditPropertyMetaGroupInfo { Name = groupName });

    ///<summary>
    /// Groups an ObjectEditPropertyMeta using a specified group info object.
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <param name="groupInfo">The ObjectEditPropertyMetaGroupInfo instance to use for grouping.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance grouped according to the specified group info.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta WithGroup(this ObjectEditPropertyMeta meta, ObjectEditPropertyMetaGroupInfo groupInfo)
        => meta?.SetProperties(p => p.GroupInfo = groupInfo);

    ///<summary>
    /// Wraps the rendering of an ObjectEditPropertyMeta in a specified wrapper component with configuration options.
    /// <typeparam name="TWrapperComponent">The wrapper component type, which must have a parameterless constructor.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMeta instance to be wrapped.</param>
    /// <param name="options">Actions to configure the wrapper component.</param>
    /// <returns>The modified IRenderData with the wrapper component applied.</returns>
    ///</summary>
    public static IRenderData WrapIn<TWrapperComponent>(this ObjectEditPropertyMeta meta, params Action<TWrapperComponent>[] options) where TWrapperComponent : new()
        => meta?.RenderData?.WrapIn(options);

    ///<summary>
    /// Wraps the rendering of an ObjectEditPropertyMeta in a MudExValidationWrapper with a specified expression for the property.
    /// <typeparam name="TPropertyType">The property type for validation.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMeta instance to be wrapped.</param>
    /// <param name="forExpression">An expression indicating the property to validate.</param>
    /// <returns>The modified IRenderData with the MudExValidationWrapper applied.</returns>
    ///</summary>
    public static IRenderData WrapInValidationWrapper<TPropertyType>(this ObjectEditPropertyMeta meta, Expression<Func<TPropertyType>> forExpression) =>
        meta.WrapIn<MudExValidationWrapper<TPropertyType>>(w => w.For = forExpression);

    ///<summary>
    /// Wraps the rendering of an ObjectEditPropertyMeta in a MudExValidationWrapper with configuration options.
    /// <typeparam name="TPropertyType">The property type for validation.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMeta instance to be wrapped.</param>
    /// <param name="options">Actions to configure the MudExValidationWrapper.</param>
    /// <returns>The modified IRenderData with the MudExValidationWrapper applied.</returns>
    ///</summary>
    public static IRenderData WrapInValidationWrapper<TPropertyType>(this ObjectEditPropertyMeta meta, Action<MudExValidationWrapper<TPropertyType>>[] options)
        => meta.WrapIn(options);

    ///<summary>
    /// Wraps the rendering of an ObjectEditPropertyMeta in a MudItem component with configuration options.
    /// <param name="meta">The ObjectEditPropertyMeta instance to be wrapped.</param>
    /// <param name="options">Actions to configure the MudItem component.</param>
    /// <returns>The modified IRenderData with the MudItem component applied.</returns>
    ///</summary>
    public static IRenderData WrapInMudItem(this ObjectEditPropertyMeta meta, params Action<MudItem>[] options)
        => meta?.WrapIn(options);

    ///<summary>
    /// Adds additional attributes to the rendering of an ObjectEditPropertyMeta.
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <param name="attributes">Key-value pairs of attributes to add.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance with additional attributes applied.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta WithAdditionalAttributes(this ObjectEditPropertyMeta meta, params KeyValuePair<string, object>[] attributes)
        => meta?.WithAdditionalAttributes(false, attributes);

    ///<summary>
    /// Adds additional attributes to the rendering of an ObjectEditPropertyMeta, with an option to overwrite existing attributes.
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <param name="overwriteExisting">Indicates whether to overwrite existing attributes.</param>
    /// <param name="attributes">Key-value pairs of attributes to add.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance with additional attributes applied.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta WithAdditionalAttributes(this ObjectEditPropertyMeta meta, bool overwriteExisting, params KeyValuePair<string, object>[] attributes)
        => meta?.SetProperties(p => p.RenderData?.AddAttributes(overwriteExisting, attributes));

    ///<summary>
    /// Adds additional attributes to the rendering of an ObjectEditPropertyMeta from a dictionary, with an option to overwrite existing attributes.
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <param name="attributes">A dictionary of attributes to add.</param>
    /// <param name="overwriteExisting">Indicates whether to overwrite existing attributes.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance with additional attributes applied.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta WithAdditionalAttributes(this ObjectEditPropertyMeta meta, IDictionary<string, object> attributes, bool overwriteExisting = false)
        => meta?.WithAdditionalAttributes(overwriteExisting, attributes.ToArray());

    ///<summary>
    /// Adds additional attributes to the rendering of an ObjectEditPropertyMeta from a dictionary, with an option to overwrite existing attributes.
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <param name="attributes">A dictionary of attributes to add.</param>
    /// <param name="overwriteExisting">Indicates whether to overwrite existing attributes.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance with additional attributes applied.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta WithAdditionalAttributes(this ObjectEditPropertyMeta meta, Dictionary<string, object> attributes, bool overwriteExisting = false)
        => meta?.WithAdditionalAttributes(overwriteExisting, attributes.ToArray());

    ///<summary>
    /// Adds or overwrites a single additional attribute for the rendering of an ObjectEditPropertyMeta.
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <param name="key">The key of the attribute to add or overwrite.</param>
    /// <param name="value">The value of the attribute.</param>
    /// <param name="overwriteExisting">Indicates whether to overwrite the attribute if it already exists.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance with the additional attribute applied.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta WithAdditionalAttribute(this ObjectEditPropertyMeta meta, string key, object value, bool overwriteExisting = false)
        => meta?.WithAdditionalAttributes(overwriteExisting, new KeyValuePair<string, object>(key, value));

    ///<summary>
    /// Conditionally ignores an ObjectEditPropertyMeta based on a predicate applied to the model.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <param name="condition">The condition that determines whether the property should be ignored.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance with the ignore condition applied.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta IgnoreIf<TModel>(this ObjectEditPropertyMeta meta, Func<TModel, bool> condition)
        => meta?.WithSettings(settings => settings?.AddCondition(condition, s => s.Ignored = true, s => s.Ignored = false));

    ///<summary>
    /// Conditionally sets an ObjectEditPropertyMeta as read-only based on a predicate applied to the model.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <param name="condition">The condition that determines whether the property should be read-only.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance with the read-only condition applied.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta AsReadOnlyIf<TModel>(this ObjectEditPropertyMeta meta, Func<TModel, bool> condition)
        => meta?.WithSettings(settings => settings?.AddCondition(condition, s => s.IsEditable = false, s => s.IsEditable = true)).WithAttributesIf(condition, new KeyValuePair<string, object>(nameof(MudBaseInput<string>.ReadOnly), true));

    ///<summary>
    /// Conditionally adds attributes to an ObjectEditPropertyMeta based on a predicate applied to the model.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <param name="condition">The condition that determines whether the attributes should be added.</param>
    /// <param name="attributes">The attributes to conditionally add.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance with the conditional attributes applied.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta WithAttributesIf<TModel>(this ObjectEditPropertyMeta meta, Func<TModel, bool> condition, params KeyValuePair<string, object>[] attributes)
        => meta?.SetProperties(p => p.RenderData?.AddAttributesIf(condition, true, attributes));

    ///<summary>
    /// Conditionally adds attributes to an ObjectEditPropertyMeta based on a predicate applied to the model, using configuration options for a specific component.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <typeparam name="TComponent">The type of the component for which attributes are being configured.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <param name="condition">The condition that determines whether the attributes should be added.</param>
    /// <param name="options">The configuration options for the component.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance with the conditional attributes applied.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta WithAttributesIf<TModel, TComponent>(this ObjectEditPropertyMeta meta, Func<TModel, bool> condition, params Action<TComponent>[] options) where TComponent : new()
        => meta?.WithAttributesIf(condition, PropertyHelper.ValuesDictionary(true, options));

    ///<summary>
    /// Conditionally adds attributes to an ObjectEditPropertyMeta based on a predicate applied to the model, from a dictionary of attributes.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <param name="condition">The condition that determines whether the attributes should be added.</param>
    /// <param name="attributes">A dictionary of attributes to conditionally add.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance with the conditional attributes applied.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta WithAttributesIf<TModel>(this ObjectEditPropertyMeta meta, Func<TModel, bool> condition, IDictionary<string, object> attributes)
        => meta?.WithAttributesIf(condition, attributes.ToArray());

    ///<summary>
    /// Conditionally adds attributes to an ObjectEditPropertyMeta based on a predicate applied to the model, from a dictionary of attributes.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <param name="condition">The condition that determines whether the attributes should be added.</param>
    /// <param name="attributes">A dictionary of attributes to conditionally add.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance with the conditional attributes applied.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta WithAttributesIf<TModel>(this ObjectEditPropertyMeta meta, Func<TModel, bool> condition, Dictionary<string, object> attributes)
        => meta?.WithAttributesIf(condition, attributes.ToArray());

    ///<summary>
    /// Ignores an ObjectEditPropertyMeta during export operations based on a boolean flag.
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <param name="ignore">Indicates whether the property should be ignored on export.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance with the export ignore setting applied.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta IgnoreOnExport(this ObjectEditPropertyMeta meta, bool ignore = true)
        => meta?.WithSettings(s => s.IgnoreOnExport = ignore);

    ///<summary>
    /// Ignores an ObjectEditPropertyMeta during import operations based on a boolean flag.
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <param name="ignore">Indicates whether the property should be ignored on import.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance with the import ignore setting applied.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta IgnoreOnImport(this ObjectEditPropertyMeta meta, bool ignore = true)
        => meta?.WithSettings(s => s.IgnoreOnImport = ignore);

    ///<summary>
    /// Ignores an ObjectEditPropertyMeta during both export and import operations based on a boolean flag.
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <param name="ignore">Indicates whether the property should be ignored on both export and import.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance with both export and import ignore settings applied.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta IgnoreOnExportAndImport(this ObjectEditPropertyMeta meta, bool ignore = true)
        => meta?.WithSettings(s =>
        {
            s.IgnoreOnImport = ignore;
            s.IgnoreOnExport = ignore;
        });

    ///<summary>
    /// Conditionally disables an ObjectEditPropertyMeta based on a predicate applied to the model.
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <param name="condition">The condition that determines whether the property should be disabled.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance with the disabled condition applied.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta AsDisabledIf<TModel>(this ObjectEditPropertyMeta meta, Func<TModel, bool> condition)
        => meta?.WithAttributesIf(condition, new KeyValuePair<string, object>(nameof(MudBaseInput<string>.Disabled), true));

    ///<summary>
    /// Sets a callback to be invoked when the component associated with the ObjectEditPropertyMeta is rendered.
    /// <typeparam name="TComponent">The component type associated with the property.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <param name="onReferenceSet">The action to invoke on component render.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance with the render callback set.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta OnRendered<TComponent>(this ObjectEditPropertyMeta meta, Action<TComponent> onReferenceSet) where TComponent : class, IComponent
        => meta.SetProperties(m => m?.RenderData?.OnRendered(onReferenceSet));

    ///<summary>
    /// Adds additional attributes to the rendering of an ObjectEditPropertyMeta using configuration options for a specific component type, with an option to overwrite existing attributes.
    /// <typeparam name="TComponent">The component type for which the attributes are being configured, must have a parameterless constructor.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <param name="overwriteExisting">Indicates whether to overwrite existing attributes.</param>
    /// <param name="options">Configuration options for the component that define the attributes to add.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance with the additional attributes applied.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta WithAdditionalAttributes<TComponent>(this ObjectEditPropertyMeta meta, bool overwriteExisting, params Action<TComponent>[] options) where TComponent : new()
        => meta?.WithAdditionalAttributes(PropertyHelper.ValuesDictionary(true, options), overwriteExisting);

    ///<summary>
    /// Adds additional attributes to the rendering of an ObjectEditPropertyMeta using configuration options for a specific component type.
    /// <typeparam name="TComponent">The component type for which the attributes are being configured, must have a parameterless constructor.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <param name="options">Configuration options for the component that define the attributes to add.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance with the additional attributes applied.</returns>
    ///</summary>
    public static ObjectEditPropertyMeta WithAdditionalAttributes<TComponent>(this ObjectEditPropertyMeta meta, params Action<TComponent>[] options) where TComponent : new()
        => meta?.WithAdditionalAttributes(PropertyHelper.ValuesDictionary(true, options));

    ///<summary>
    /// Adds additional attributes to the rendering of an ObjectEditPropertyMeta using a specific component instance, potentially with an option to overwrite existing attributes. This method allows for the dynamic generation of attributes based on the state or properties of the provided component instance.
    /// <typeparam name="TComponent">The component type for which the attributes are being configured, must have a parameterless constructor.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMeta instance to modify.</param>
    /// <param name="instanceForAttributes">The instance of TComponent from which to derive the attributes.</param>
    /// <param name="overwriteExisting">Optional. Indicates whether to overwrite existing attributes. Defaults to false if not specified.</param>
    /// <returns>The modified ObjectEditPropertyMeta instance with the additional attributes applied.</returns>
    /// Note: This method signature seems to be incomplete as provided, so the description is based on the visible pattern and assumption of intended functionality.
    ///</summary>
    public static ObjectEditPropertyMeta WithAdditionalAttributes<TComponent>(this ObjectEditPropertyMeta meta, TComponent instanceForAttributes, bool overwriteExisting = false) where TComponent : new()
        => meta?.WithAdditionalAttributes(PropertyHelper.ValuesDictionary(instanceForAttributes, true), overwriteExisting);
}