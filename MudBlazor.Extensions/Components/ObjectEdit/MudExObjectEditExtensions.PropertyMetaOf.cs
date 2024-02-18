using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Localization;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Helper.Internal;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components.ObjectEdit;

public static partial class MudExObjectEditExtensions
{

    ///<summary>
    /// Configures a property to be rendered with a specified component, allowing for customization of the component's value field, options, and type converters between the property and field types.
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <typeparam name="TComponent">The component type.</typeparam>
    /// <typeparam name="TPropertyType">The property type.</typeparam>
    /// <typeparam name="TFieldType">The field type within the component.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance for the model.</param>
    /// <param name="valueField">The expression selecting the field within the component.</param>
    /// <param name="options">Action to configure the component instance.</param>
    /// <param name="toFieldTypeConverter">Optional converter from property to field type.</param>
    /// <param name="toPropertyTypeConverter">Optional converter from field to property type.</param>
    ///</summary>
    public static ObjectEditPropertyMetaOf<TModel> RenderWith<TModel, TComponent, TPropertyType, TFieldType>(this ObjectEditPropertyMetaOf<TModel> meta, Expression<Func<TComponent, TFieldType>> valueField, Action<TComponent> options, Func<TPropertyType, TFieldType> toFieldTypeConverter = null, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null) where TComponent : new()
    => meta?.SetProperties(p => p.RenderData = RenderData.For(valueField, options, toFieldTypeConverter, toPropertyTypeConverter));

    ///<summary>
    /// Configures a property to be rendered with a specified component using an instance for attributes, allowing for type conversions between the property and component field types.
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <typeparam name="TComponent">The component type.</typeparam>
    /// <typeparam name="TPropertyType">The property type.</typeparam>
    /// <typeparam name="TFieldType">The field type within the component.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance for the model.</param>
    /// <param name="valueField">The expression selecting the field within the component.</param>
    /// <param name="instanceForAttributes">The component instance for defining attributes.</param>
    /// <param name="toFieldTypeConverter">Optional converter from property to field type.</param>
    /// <param name="toPropertyTypeConverter">Optional converter from field to property type.</param>
    ///</summary>
    public static ObjectEditPropertyMetaOf<TModel> RenderWith<TModel, TComponent, TPropertyType, TFieldType>(this ObjectEditPropertyMetaOf<TModel> meta, Expression<Func<TComponent, TFieldType>> valueField, TComponent instanceForAttributes, Func<TPropertyType, TFieldType> toFieldTypeConverter = null, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null) where TComponent : new()
        => meta?.SetProperties(p => p.RenderData = RenderData.For(valueField, instanceForAttributes, toFieldTypeConverter, toPropertyTypeConverter));

    ///<summary>
    /// Configures a property to be rendered with a specified component, specifying only the value field and optional type converters, without custom options.
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <typeparam name="TComponent">The component type.</typeparam>
    /// <typeparam name="TPropertyType">The property type.</typeparam>
    /// <typeparam name="TFieldType">The field type within the component.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance for the model.</param>
    /// <param name="valueField">The expression selecting the field within the component.</param>
    /// <param name="toFieldTypeConverter">Optional converter from property to field type.</param>
    /// <param name="toPropertyTypeConverter">Optional converter from field to property type.</param>
    ///</summary>
    public static ObjectEditPropertyMetaOf<TModel> RenderWith<TModel, TComponent, TPropertyType, TFieldType>(this ObjectEditPropertyMetaOf<TModel> meta, Expression<Func<TComponent, TFieldType>> valueField, Func<TPropertyType, TFieldType> toFieldTypeConverter = null, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null)
        => meta?.SetProperties(p => p.RenderData = RenderData.For(valueField, toFieldTypeConverter, toPropertyTypeConverter));

    ///<summary>
    /// Configures a property to be rendered with a specified component, specifying the value field and an action to customize the component.
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <typeparam name="TComponent">The component type.</typeparam>
    /// <typeparam name="TPropertyType">The property type.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance for the model.</param>
    /// <param name="valueField">The expression selecting the field within the component.</param>
    /// <param name="options">Action to configure the component instance.</param>
    ///</summary>
    public static ObjectEditPropertyMetaOf<TModel> RenderWith<TModel, TComponent, TPropertyType>(this ObjectEditPropertyMetaOf<TModel> meta, Expression<Func<TComponent, TPropertyType>> valueField, Action<TComponent> options) where TComponent : new()
        => meta?.SetProperties(p => p.RenderData = RenderData.For(valueField, options));

    ///<summary>
    /// Configures a property to be rendered with a specified component using an instance for attributes, without needing type conversions.
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <typeparam name="TComponent">The component type.</typeparam>
    /// <typeparam name="TPropertyType">The property type.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance for the model.</param>
    /// <param name="valueField">The expression selecting the field within the component.</param>
    /// <param name="instanceForAttributes">The component instance for defining attributes.</param>
    ///</summary>
    public static ObjectEditPropertyMetaOf<TModel> RenderWith<TModel, TComponent, TPropertyType>(this ObjectEditPropertyMetaOf<TModel> meta, Expression<Func<TComponent, TPropertyType>> valueField, TComponent instanceForAttributes) where TComponent : new()
        => meta?.SetProperties(p => p.RenderData = RenderData.For(valueField, instanceForAttributes));

    ///<summary>
    /// Configures a property to be rendered with a specified component, specifying only the value field without additional options or type conversions.
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <typeparam name="TComponent">The component type.</typeparam>
    /// <typeparam name="TPropertyType">The property type.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance for the model.</param>
    /// <param name="valueField">The expression selecting the field within the component.</param>
    ///</summary>
    public static ObjectEditPropertyMetaOf<TModel> RenderWith<TModel, TComponent, TPropertyType>(this ObjectEditPropertyMetaOf<TModel> meta, Expression<Func<TComponent, TPropertyType>> valueField)
        => meta?.SetProperties(p => p.RenderData = RenderData.For(valueField));

    ///<summary>
    /// Configures a property to be rendered with a specified component, allowing for customization through an action.
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <typeparam name="TComponent">The component type to render with.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance for the model.</param>
    /// <param name="options">Action to configure the component instance.</param>
    ///</summary>
    public static ObjectEditPropertyMetaOf<TModel> RenderWith<TModel, TComponent>(this ObjectEditPropertyMetaOf<TModel> meta, Action<TComponent> options) where TComponent : new()
        => meta?.RenderWith(typeof(TComponent), PropertyHelper.ValuesDictionary(options, true));

    ///<summary>
    /// Configures a property to be rendered with a specified component, optionally using provided attributes.
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <typeparam name="TComponent">The component type to render with.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance for the model.</param>
    /// <param name="attributes">Optional dictionary of attributes to apply to the component.</param>
    ///</summary>
    public static ObjectEditPropertyMetaOf<TModel> RenderWith<TModel, TComponent>(this ObjectEditPropertyMetaOf<TModel> meta, IDictionary<string, object> attributes = null)
        => meta?.RenderWith(typeof(TComponent), attributes);

    ///<summary>
    /// Configures a property to be rendered with a component of a specified type, optionally using provided attributes.
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance for the model.</param>
    /// <param name="componentType">The type of the component to render with.</param>
    /// <param name="attributes">Optional dictionary of attributes to apply to the component.</param>
    ///</summary>
    public static ObjectEditPropertyMetaOf<TModel> RenderWith<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, Type componentType, IDictionary<string, object> attributes = null)
        => meta?.SetProperties(p => p.RenderData = RenderData.For(componentType, attributes));

    ///<summary>
    /// Configures a property to be rendered using a custom renderer.
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance for the model.</param>
    /// <param name="renderer">The custom renderer to use.</param>
    ///</summary>
    public static ObjectEditPropertyMetaOf<TModel> RenderWith<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, ICustomRenderer renderer)
        => meta?.SetProperties(m => (m.RenderData ??= new RenderData(null)).SetProperties(d => d.CustomRenderer = renderer));

    ///<summary>
    /// Marks a property as read-only, optionally based on a condition.
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance for the model.</param>
    /// <param name="isReadOnly">Indicates whether the property is read-only.</param>
    ///</summary>
    public static ObjectEditPropertyMetaOf<TModel> AsReadOnly<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, bool isReadOnly = true)
        => meta?.SetProperties(p => p.Settings.IsEditable = !isReadOnly, p => p?.RenderData?.AddAttributes(true, new KeyValuePair<string, object>(nameof(MudBaseInput<string>.ReadOnly), isReadOnly)));

    ///<summary>
    /// Disables the underline feature of a component, optionally based on a condition.
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance for the model.</param>
    /// <param name="disableUnderline">Indicates whether to disable the underline.</param>
    ///</summary>
    public static ObjectEditPropertyMetaOf<TModel> DisableUnderline<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, bool disableUnderline = true)
        => meta?.SetProperties(p => p?.RenderData?.AddAttributes(true, new KeyValuePair<string, object>(nameof(MudBaseInput<string>.DisableUnderLine), disableUnderline)));

    ///<summary>
    /// Ignores a property for editing purposes, optionally based on a condition.
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance for the model.</param>
    /// <param name="ignore">Indicates whether the property should be ignored.</param>
    ///</summary>
    public static ObjectEditPropertyMetaOf<TModel> Ignore<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, bool ignore = true)
        => meta?.WithSettings(s => s.Ignored = ignore);

    ///<summary>
    /// Configures reset options for a property, allowing for customization of reset behavior.
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance for the model.</param>
    /// <param name="allowReset">Indicates whether resetting is allowed.</param>
    /// <param name="resetIcon">The icon used for the reset button.</param>
    /// <param name="showResetText">Indicates whether to show text next to the reset icon.</param>
    /// <param name="resetText">The text to display next to the reset icon, if applicable.</param>
    ///</summary>
    public static ObjectEditPropertyMetaOf<TModel> WithResetOptions<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, bool allowReset, string resetIcon, bool showResetText, string resetText)
        => meta?.WithResetOptions(new PropertyResetSettings() { AllowReset = allowReset, ResetIcon = resetIcon, ResetText = resetText, ShowResetText = showResetText });

    ///<summary>
    /// Configures reset options for a property using a PropertyResetSettings object.
    /// <typeparam name="TModel">The model type.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance for the model.</param>
    /// <param name="resetSettings">The reset settings to apply.</param>
    ///</summary>
    public static ObjectEditPropertyMetaOf<TModel> WithResetOptions<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, PropertyResetSettings resetSettings)
        => meta?.WithSettings(s => s.ResetSettings = resetSettings);

    /// <summary>
    /// Configures reset options for an ObjectEditPropertyMetaOf instance using a specified settings action.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance to configure.</param>
    /// <param name="resetSettingsAction">An action to configure the reset settings.</param>
    /// <returns>The modified ObjectEditPropertyMetaOf instance.</returns>
    public static ObjectEditPropertyMetaOf<TModel> WithResetOptions<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, Action<PropertyResetSettings> resetSettingsAction)
        => meta?.WithSettings(s => resetSettingsAction(s.ResetSettings ??= new PropertyResetSettings()));

    /// <summary>
    /// Marks an ObjectEditPropertyMetaOf instance as resettable.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance to modify.</param>
    /// <param name="resettable">A boolean indicating if the property is resettable. Defaults to true.</param>
    /// <returns>The modified ObjectEditPropertyMetaOf instance.</returns>
    public static ObjectEditPropertyMetaOf<TModel> IsResettable<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, bool resettable = true)
        => meta?.WithResetOptions(s => s.AllowReset = resettable);

    /// <summary>
    /// Marks an ObjectEditPropertyMetaOf instance as not resettable.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance to modify.</param>
    /// <returns>The modified ObjectEditPropertyMetaOf instance.</returns>
    public static ObjectEditPropertyMetaOf<TModel> NotResettable<TModel>(this ObjectEditPropertyMetaOf<TModel> meta)
        => meta?.IsResettable(false);

    /// <summary>
    /// Applies custom settings to an ObjectEditPropertyMetaOf instance.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance to configure.</param>
    /// <param name="settingsAction">An action to configure the settings.</param>
    /// <returns>The modified ObjectEditPropertyMetaOf instance.</returns>
    public static ObjectEditPropertyMetaOf<TModel> WithSettings<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, Action<ObjectEditPropertyMetaSettings> settingsAction)
        => meta?.SetProperties(p => settingsAction(p.Settings ??= new ObjectEditPropertyMetaSettings(meta)));

    /// <summary>
    /// Configures an ObjectEditPropertyMetaOf instance to use a separate label component.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance to configure.</param>
    /// <returns>The modified ObjectEditPropertyMetaOf instance.</returns>
    public static ObjectEditPropertyMetaOf<TModel> WithSeparateLabelComponent<TModel>(this ObjectEditPropertyMetaOf<TModel> meta)
        => meta?.WithSettings(s => s.LabelBehaviour = LabelBehaviour.Both);

    /// <summary>
    /// Configures an ObjectEditPropertyMetaOf instance to use a separate validation component.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance to configure.</param>
    /// <returns>The modified ObjectEditPropertyMetaOf instance.</returns>
    public static ObjectEditPropertyMetaOf<TModel> WithSeparateValidationComponent<TModel>(this ObjectEditPropertyMetaOf<TModel> meta)
        => meta?.WithSettings(s => s.ValidationComponent = true);

    /// <summary>
    /// Configures an ObjectEditPropertyMetaOf instance to have no label.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance to configure.</param>
    /// <returns>The modified ObjectEditPropertyMetaOf instance.</returns>
    public static ObjectEditPropertyMetaOf<TModel> WithoutLabel<TModel>(this ObjectEditPropertyMetaOf<TModel> meta)
        => meta?.WithSettings(s => s.LabelBehaviour = LabelBehaviour.NoLabel);

    /// <summary>
    /// Configures an ObjectEditPropertyMetaOf instance to use a separate label component only, without integrating it into the main component.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance to configure.</param>
    /// <returns>The modified ObjectEditPropertyMetaOf instance.</returns>
    public static ObjectEditPropertyMetaOf<TModel> WithSeparateLabelComponentOnly<TModel>(this ObjectEditPropertyMetaOf<TModel> meta)
        => meta?.WithSettings(s => s.LabelBehaviour = LabelBehaviour.SeparateLabelComponentOnly);

    /// <summary>
    /// Configures an ObjectEditPropertyMetaOf instance to use default component labeling.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance to configure.</param>
    /// <returns>The modified ObjectEditPropertyMetaOf instance.</returns>
    public static ObjectEditPropertyMetaOf<TModel> WithDefaultLabeling<TModel>(this ObjectEditPropertyMetaOf<TModel> meta)
        => meta?.WithSettings(s => s.LabelBehaviour = LabelBehaviour.DefaultComponentLabeling);

    /// <summary>
    /// Sets the order of an ObjectEditPropertyMetaOf instance.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance to modify.</param>
    /// <param name="order">The order to set.</param>
    /// <returns>The modified ObjectEditPropertyMetaOf instance.</returns>
    public static ObjectEditPropertyMetaOf<TModel> WithOrder<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, int order)
        => meta?.SetProperties(p => p.Settings.Order = order);

    /// <summary>
    /// Sets a static label for an ObjectEditPropertyMetaOf instance.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance to modify.</param>
    /// <param name="label">The label to set.</param>
    /// <returns>The modified ObjectEditPropertyMetaOf instance.</returns>
    public static ObjectEditPropertyMetaOf<TModel> WithLabel<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, string label)
        => meta?.SetProperties(p => p.Settings.LabelResolverFn = _ => label);

    /// <summary>
    /// Sets a static description for an ObjectEditPropertyMetaOf instance.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance to modify.</param>
    /// <param name="description">The description to set.</param>
    /// <returns>The modified ObjectEditPropertyMetaOf instance.</returns>
    public static ObjectEditPropertyMetaOf<TModel> WithDescription<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, string description)
        => meta?.SetProperties(p => p.Settings.DescriptionResolverFn = _ => description);

    /// <summary>
    /// Sets a dynamic label resolver for an ObjectEditPropertyMetaOf instance.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance to modify.</param>
    /// <param name="resolverFunc">The function to resolve labels dynamically.</param>
    /// <returns>The modified ObjectEditPropertyMetaOf instance.</returns>
    public static ObjectEditPropertyMetaOf<TModel> WithLabelResolver<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, Func<PropertyInfo, string> resolverFunc)
        => meta?.SetProperties(p => p.Settings.LabelResolverFn = resolverFunc);

    /// <summary>
    /// Sets a dynamic description resolver for an ObjectEditPropertyMetaOf instance.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance to modify.</param>
    /// <param name="resolverFunc">The function to resolve descriptions dynamically.</param>
    /// <returns>The modified ObjectEditPropertyMetaOf instance.</returns>
    public static ObjectEditPropertyMetaOf<TModel> WithDescriptionResolver<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, Func<PropertyInfo, string> resolverFunc)
        => meta?.SetProperties(p => p.Settings.DescriptionResolverFn = resolverFunc);

    /// <summary>
    /// Configures localization patterns for labels in an ObjectEditPropertyMetaOf instance.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance to configure.</param>
    /// <param name="pattern">The pattern to use for label localization. Defaults to "Label_{0}".</param>
    /// <param name="localizer">The IStringLocalizer instance to use for localization. Optional.</param>
    /// <returns>The modified ObjectEditPropertyMetaOf instance.</returns>
    public static ObjectEditPropertyMetaOf<TModel> WithLabelLocalizerPattern<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, string pattern = "Label_{0}", IStringLocalizer localizer = null)
        => meta?.SetProperties(p => p.Settings.Localizer = localizer).WithLabelResolver(info => string.Format(pattern, info.Name));

    /// <summary>
    /// Configures localization patterns for descriptions in an ObjectEditPropertyMetaOf instance.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance to configure.</param>
    /// <param name="pattern">The pattern to use for description localization. Defaults to "Description_{0}".</param>
    /// <param name="localizer">The IStringLocalizer instance to use for localization. Optional.</param>
    /// <returns>The modified ObjectEditPropertyMetaOf instance.</returns>
    public static ObjectEditPropertyMetaOf<TModel> WithDescriptionLocalizerPattern<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, string pattern = "Description_{0}", IStringLocalizer localizer = null)
        => meta?.SetProperties(p => p.Settings.Localizer = localizer).WithDescriptionResolver(info => string.Format(pattern, info.Name));

    /// <summary>
    /// Adds a group to an ObjectEditPropertyMetaOf instance using a group name.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance to modify.</param>
    /// <param name="groupName">The name of the group to add.</param>
    /// <returns>The modified ObjectEditPropertyMetaOf instance.</returns>
    public static ObjectEditPropertyMetaOf<TModel> WithGroup<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, string groupName)
        => meta?.WithGroup(new ObjectEditPropertyMetaGroupInfo { Name = groupName });

    /// <summary>
    /// Adds a group to an ObjectEditPropertyMetaOf instance using a group info object.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance to modify.</param>
    /// <param name="groupInfo">The ObjectEditPropertyMetaGroupInfo instance representing the group to add.</param>
    /// <returns>The modified ObjectEditPropertyMetaOf instance.</returns>
    public static ObjectEditPropertyMetaOf<TModel> WithGroup<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, ObjectEditPropertyMetaGroupInfo groupInfo)
        => meta?.SetProperties(p => p.GroupInfo = groupInfo);

    /// <summary>
    /// Adds additional attributes to an ObjectEditPropertyMetaOf instance.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance to modify.</param>
    /// <param name="attributes">The attributes to add.</param>
    /// <returns>The modified ObjectEditPropertyMetaOf instance.</returns>
    public static ObjectEditPropertyMetaOf<TModel> WithAdditionalAttributes<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, params KeyValuePair<string, object>[] attributes)
        => meta?.WithAdditionalAttributes(false, attributes);

    /// <summary>
    /// Adds additional attributes to an ObjectEditPropertyMetaOf instance, with an option to overwrite existing attributes.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance to modify.</param>
    /// <param name="overwriteExisting">A boolean indicating whether to overwrite existing attributes.</param>
    /// <param name="attributes">The attributes to add.</param>
    /// <returns>The modified ObjectEditPropertyMetaOf instance.</returns>
    public static ObjectEditPropertyMetaOf<TModel> WithAdditionalAttributes<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, bool overwriteExisting, params KeyValuePair<string, object>[] attributes)
        => meta?.SetProperties(p => p.RenderData?.AddAttributes(overwriteExisting, attributes));

    /// <summary>
    /// Adds additional attributes to an ObjectEditPropertyMetaOf instance, with an option to overwrite existing attributes.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance to modify.</param>
    /// <param name="attributes">A dictionary of attributes to add.</param>
    /// <param name="overwriteExisting">A boolean indicating whether to overwrite existing attributes.</param>
    /// <returns>The modified ObjectEditPropertyMetaOf instance.</returns>
    public static ObjectEditPropertyMetaOf<TModel> WithAdditionalAttributes<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, IDictionary<string, object> attributes, bool overwriteExisting = false)
        => meta?.WithAdditionalAttributes(overwriteExisting, attributes.ToArray());

    /// <summary>
    /// Adds a single additional attribute to an ObjectEditPropertyMetaOf instance, with an option to overwrite existing attributes.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance to modify.</param>
    /// <param name="key">The key of the attribute to add.</param>
    /// <param name="value">The value of the attribute to add.</param>
    /// <param name="overwriteExisting">A boolean indicating whether to overwrite existing attributes.</param>
    /// <returns>The modified ObjectEditPropertyMetaOf instance.</returns>
    public static ObjectEditPropertyMetaOf<TModel> WithAdditionalAttribute<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, string key, object value, bool overwriteExisting = false)
        => meta?.WithAdditionalAttributes(overwriteExisting, new KeyValuePair<string, object>(key, value));

    /// <summary>
    /// Adds additional attributes to an ObjectEditPropertyMetaOf instance using component options, with an option to overwrite existing attributes.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <typeparam name="TComponent">The type of the component for attribute options.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance to modify.</param>
    /// <param name="overwriteExisting">A boolean indicating whether to overwrite existing attributes.</param>
    /// <param name="options">The component options to convert into attributes.</param>
    /// <returns>The modified ObjectEditPropertyMetaOf instance.</returns>
    public static ObjectEditPropertyMetaOf<TModel> WithAdditionalAttributes<TModel, TComponent>(this ObjectEditPropertyMetaOf<TModel> meta, bool overwriteExisting, params Action<TComponent>[] options) where TComponent : new()
        => meta?.WithAdditionalAttributes(PropertyHelper.ValuesDictionary(true, options), overwriteExisting);

    /// <summary>
    /// Adds additional attributes to an ObjectEditPropertyMetaOf instance using component options.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <typeparam name="TComponent">The type of the component for attribute options.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance to modify.</param>
    /// <param name="options">The component options to convert into attributes.</param>
    /// <returns>The modified ObjectEditPropertyMetaOf instance.</returns>
    public static ObjectEditPropertyMetaOf<TModel> WithAdditionalAttributes<TModel, TComponent>(this ObjectEditPropertyMetaOf<TModel> meta, params Action<TComponent>[] options) where TComponent : new()
        => meta?.WithAdditionalAttributes(PropertyHelper.ValuesDictionary(true, options));

    /// <summary>
    /// Adds additional attributes to an ObjectEditPropertyMetaOf instance using an instance of a component for attribute options, with an option to overwrite existing attributes.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <typeparam name="TComponent">The type of the component for attribute options.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance to modify.</param>
    /// <param name="instanceForAttributes">The component instance to use for generating attribute values.</param>
    /// <param name="overwriteExisting">A boolean indicating whether to overwrite existing attributes.</param>
    /// <returns>The modified ObjectEditPropertyMetaOf instance.</returns>
    public static ObjectEditPropertyMetaOf<TModel> WithAdditionalAttributes<TModel, TComponent>(this ObjectEditPropertyMetaOf<TModel> meta, TComponent instanceForAttributes, bool overwriteExisting = false) where TComponent : new()
        => meta?.WithAdditionalAttributes(PropertyHelper.ValuesDictionary(instanceForAttributes, true), overwriteExisting);

    /// <summary>
    /// Ignores an ObjectEditPropertyMetaOf instance if a specified condition is met.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance to modify.</param>
    /// <param name="condition">The condition that determines if the property should be ignored.</param>
    /// <returns>The modified ObjectEditPropertyMetaOf instance.</returns>
    public static ObjectEditPropertyMetaOf<TModel> IgnoreIf<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, Func<TModel, bool> condition)
        => meta?.WithSettings(settings => settings?.AddCondition(condition, s => s.Ignored = true, s => s.Ignored = false));

    /// <summary>
    /// Sets an ObjectEditPropertyMetaOf instance as read-only if a specified condition is met.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance to modify.</param>
    /// <param name="condition">The condition that determines if the property should be read-only.</param>
    /// <returns>The modified ObjectEditPropertyMetaOf instance.</returns>
    public static ObjectEditPropertyMetaOf<TModel> AsReadOnlyIf<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, Func<TModel, bool> condition)
        => meta?.WithSettings(settings => settings?.AddCondition(condition, s => s.IsEditable = false, s => s.IsEditable = true)).WithAttributesIf(condition, new KeyValuePair<string, object>(nameof(MudBaseInput<string>.ReadOnly), true));

    /// <summary>
    /// Adds attributes to an ObjectEditPropertyMetaOf instance if a specified condition is met.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance to modify.</param>
    /// <param name="condition">The condition that determines if the attributes should be added.</param>
    /// <param name="attributes">The attributes to add.</param>
    /// <returns>The modified ObjectEditPropertyMetaOf instance.</returns>
    public static ObjectEditPropertyMetaOf<TModel> WithAttributesIf<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, Func<TModel, bool> condition, params KeyValuePair<string, object>[] attributes)
        => meta?.SetProperties(p => p.RenderData?.AddAttributesIf(condition, true, attributes));

    /// <summary>
    /// Adds attributes to an ObjectEditPropertyMetaOf instance if a specified condition is met, using component options.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <typeparam name="TComponent">The type of the component for attribute options.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance to modify.</param>
    /// <param name="condition">The condition that determines if the attributes should be added.</param>
    /// <param name="options">The component options to convert into attributes.</param>
    /// <returns>The modified ObjectEditPropertyMetaOf instance.</returns>
    public static ObjectEditPropertyMetaOf<TModel> WithAttributesIf<TModel, TComponent>(this ObjectEditPropertyMetaOf<TModel> meta, Func<TModel, bool> condition, params Action<TComponent>[] options) where TComponent : new()
        => meta?.WithAttributesIf(condition, PropertyHelper.ValuesDictionary(true, options));

    /// <summary>
    /// Ignores an ObjectEditPropertyMetaOf instance during export operations if specified.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance to modify.</param>
    /// <param name="ignore">A boolean indicating if the property should be ignored during export. Defaults to true.</param>
    /// <returns>The modified ObjectEditPropertyMetaOf instance.</returns>
    public static ObjectEditPropertyMetaOf<TModel> IgnoreOnExport<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, bool ignore = true)
        => meta?.WithSettings(s => s.IgnoreOnExport = ignore);

    /// <summary>
    /// Ignores an ObjectEditPropertyMetaOf instance during import operations if specified.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance to modify.</param>
    /// <param name="ignore">A boolean indicating if the property should be ignored during import. Defaults to true.</param>
    /// <returns>The modified ObjectEditPropertyMetaOf instance.</returns>
    public static ObjectEditPropertyMetaOf<TModel> IgnoreOnImport<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, bool ignore = true)
        => meta?.WithSettings(s => s.IgnoreOnImport = ignore);

    /// <summary>
    /// Ignores an ObjectEditPropertyMetaOf instance during both export and import operations if specified.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance to modify.</param>
    /// <param name="ignore">A boolean indicating if the property should be ignored during both export and import. Defaults to true.</param>
    /// <returns>The modified ObjectEditPropertyMetaOf instance.</returns>
    public static ObjectEditPropertyMetaOf<TModel> IgnoreOnExportAndImport<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, bool ignore = true)
        => meta?.WithSettings(s =>
        {
            s.IgnoreOnImport = ignore;
            s.IgnoreOnExport = ignore;
        });

    /// <summary>
    /// Disables an ObjectEditPropertyMetaOf instance if a specified condition is met.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance to modify.</param>
    /// <param name="condition">The condition that determines if the property should be disabled.</param>
    /// <returns>The modified ObjectEditPropertyMetaOf instance.</returns>
    public static ObjectEditPropertyMetaOf<TModel> AsDisabledIf<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, Func<TModel, bool> condition)
        => meta?.WithAttributesIf(condition, new KeyValuePair<string, object>(nameof(MudBaseInput<string>.Disabled), true));

    /// <summary>
    /// Adds additional attributes to an ObjectEditPropertyMetaOf instance from a dictionary, with an option to overwrite existing attributes.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance to modify.</param>
    /// <param name="attributes">A dictionary of attributes to add.</param>
    /// <param name="overwriteExisting">A boolean indicating whether to overwrite existing attributes.</param>
    /// <returns>The modified ObjectEditPropertyMetaOf instance.</returns>
    public static ObjectEditPropertyMetaOf<TModel> WithAdditionalAttributes<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, Dictionary<string, object> attributes, bool overwriteExisting = false)
        => meta?.WithAdditionalAttributes(overwriteExisting, attributes.ToArray());

    /// <summary>
    /// Adds attributes to an ObjectEditPropertyMetaOf instance if a specified condition is met, using a dictionary of attributes.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance to modify.</param>
    /// <param name="condition">The condition that determines if the attributes should be added.</param>
    /// <param name="attributes">A dictionary of attributes to add.</param>
    /// <returns>The modified ObjectEditPropertyMetaOf instance.</returns>
    public static ObjectEditPropertyMetaOf<TModel> WithAttributesIf<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, Func<TModel, bool> condition, IDictionary<string, object> attributes)
        => meta?.WithAttributesIf(condition, attributes.ToArray());

    /// <summary>
    /// Adds attributes to an ObjectEditPropertyMetaOf instance if a specified condition is met, using a dictionary of attributes. This overload accepts a Dictionary specifically, offering the same functionality as the IDictionary overload.
    /// </summary>
    /// <typeparam name="TModel">The type of the model.</typeparam>
    /// <param name="meta">The ObjectEditPropertyMetaOf instance to modify.</param>
    /// <param name="condition">The condition that determines if the attributes should be added.</param>
    /// <param name="attributes">A dictionary of attributes to add.</param>
    /// <returns>The modified ObjectEditPropertyMetaOf instance.</returns>
    public static ObjectEditPropertyMetaOf<TModel> WithAttributesIf<TModel>(this ObjectEditPropertyMetaOf<TModel> meta, Func<TModel, bool> condition, Dictionary<string, object> attributes)
        => meta?.WithAttributesIf(condition, attributes.ToArray());

}