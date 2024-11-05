using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Options;
using MudBlazor.Utilities;
using Nextended.Core.Extensions;
using Nextended.Core.Helper;

namespace MudBlazor.Extensions.Components.ObjectEdit.Options;

/// <summary>
/// Class containing default render data for various types of properties.
/// </summary>
public static class RenderDataDefaults
{
    private static readonly Dictionary<Type, IRenderData> RenderData = new();
    private static readonly List<IDefaultRenderDataProvider> Providers = new();
    private static bool _registeredServicesAdded;

    /// <summary>
    /// Adds a default render data providers to the list of providers.
    /// </summary>
    public static void AddRenderDataProvider(IServiceProvider serviceProvider)
    {
        if (!_registeredServicesAdded)
        {
            var defaultRenderDataProviders = serviceProvider?.GetServices<IDefaultRenderDataProvider>().ToArray();
            AddRenderDataProvider(defaultRenderDataProviders);
            _registeredServicesAdded = true;
        }
    }

    /// <summary>
    /// Adds a default render data providers to the list of providers.
    /// </summary>
    public static void AddRenderDataProvider(params IDefaultRenderDataProvider[] provider) 
        => Providers.AddRange(provider.EmptyIfNull().Where(p => !Providers.Contains(p)));

    /// <summary>
    /// Removes a default render data providers from the list of providers.
    /// </summary>
    public static void RemoveRenderDataProvider(params IDefaultRenderDataProvider[] provider) => Providers.RemoveRange(provider.EmptyIfNull());

    static RenderDataDefaults()
    {
        RegisterDefault<string, MudTextField<string>>(f => f.Value);

        RegisterDefault<int, MudNumericField<int>>(f => f.Value);
        RegisterDefault<decimal, MudNumericField<decimal>>(f => f.Value);
        RegisterDefault<double, MudNumericField<double>>(f => f.Value);
        RegisterDefault<float, MudNumericField<float>>(f => f.Value);

        RegisterDefault<int?, MudNumericField<int?>>(f => f.Value, field => { field.Clearable = true; });
        RegisterDefault<decimal?, MudNumericField<decimal?>>(f => f.Value, field => { field.Clearable = true; });
        RegisterDefault<double?, MudNumericField<double?>>(f => f.Value, field => { field.Clearable = true; });
        RegisterDefault<float?, MudNumericField<float?>>(f => f.Value, field => { field.Clearable = true; });


        RegisterDefault<DateTime?, MudDatePicker>(f => f.Date, DatePickerOptions(true));
        RegisterDefault<DateTime, DateTime?, MudDatePicker>(f => f.Date, DatePickerOptions(false));
        RegisterDefault<DateOnly, DateTime?, MudDatePicker>(f => f.Date, DatePickerOptions(false));
        RegisterDefault<DateOnly?, DateTime?, MudDatePicker>(f => f.Date, DatePickerOptions(true));

        RegisterDefault<TimeOnly, TimeSpan?, MudTimePicker>(f => f.Time, TimePickerOptions());
        RegisterDefault<TimeOnly?, TimeSpan?, MudTimePicker>(f => f.Time, TimePickerOptions());
        RegisterDefault<TimeSpan, TimeSpan?, MudTimePicker>(f => f.Time, TimePickerOptions());
        RegisterDefault<TimeSpan?, MudTimePicker>(f => f.Time, TimePickerOptions());


        RegisterMudExColorEditForColors();
        //RegisterMudColorPickerForColors();
        
        RegisterDefault<MudExColor, MudExColor, MudExColorEdit>(f => f.Value);
        RegisterDefault<MudExColor?, MudExColor, MudExColorEdit>(f => f.Value);


        //RegisterDefault<bool, MudSwitch<bool>>(s => s.Checked, s => s.Color = MudBlazor.Color.Warning);
        RegisterDefault<bool, MudExCheckBox<bool>>(s => s.Value, box =>
        {
            box.TriState = false;
            box.UncheckedColor = Color.Default;
            box.Color = Color.Warning;
        });
        RegisterDefault<bool?, MudExCheckBox<bool?>>(s => s.Value, box =>
        {
            box.TriState = true;
            box.UncheckedColor = Color.Default;
            box.Color = Color.Warning;
        });

        RegisterDefault<ICollection<string>, MudExCollectionEditor<string>>(f => f.Items);
        RegisterDefault<CultureInfo, MudExCultureSelect>(s => s.Value);

        RegisterDefault<IEnumerable<UploadableFile>, MudExUploadEdit<UploadableFile>>(edit => edit.UploadRequests);
        RegisterDefault<UploadableFile[], IList<UploadableFile>, MudExUploadEdit<UploadableFile>>(edit => edit.UploadRequests, requests => requests?.ToList() ?? new List<UploadableFile>(), requests => requests?.ToArray() ?? Array.Empty<UploadableFile>());
        RegisterDefault<IList<UploadableFile>, MudExUploadEdit<UploadableFile>>(edit => edit.UploadRequests);
        RegisterDefault<UploadableFile, MudExUploadEdit<UploadableFile>>(edit => edit.UploadRequest, edit => edit.AllowMultiple = false);


        RegisterDefault<DialogOptionsEx, MudExObjectEditPicker<DialogOptionsEx>>(picker =>
        {
            picker.DialogOptions = DialogOptionsEx.DefaultDialogOptions.CloneOptions().SetProperties(o => o.Resizeable = true);
            picker.PickerVariant = PickerVariant.Dialog;
        });

        RegisterAsSelection<VideoDevice>(select => select.AvailableItemsLoadFunc = LoadFromJsFunc<VideoDevice>("MudExCapture.getAvailableVideoDevices"));
        RegisterAsSelection<AudioDevice>(select => select.AvailableItemsLoadFunc = LoadFromJsFunc<AudioDevice>("MudExCapture.getAvailableAudioDevices"));
        
        RegisterAsMultiSelection<VideoDevice, List<VideoDevice>>(list => list, items => items.ToList(),
            select =>
            {
                select.MultiSelection = true;
                select.AvailableItemsLoadFunc = LoadFromJsFunc<VideoDevice>("MudExCapture.getAvailableVideoDevices");
            });
        RegisterAsMultiSelection<VideoDevice, IList<VideoDevice>>(list => list, items => items.ToList(),
            select =>
            {
                select.MultiSelection = true;
                select.AvailableItemsLoadFunc = LoadFromJsFunc<VideoDevice>("MudExCapture.getAvailableVideoDevices");
            });
        RegisterAsMultiSelection<VideoDevice, VideoDevice[]>(list => list, items => items.ToArray(),
            select =>
            {
                select.MultiSelection = true;
                select.AvailableItemsLoadFunc = LoadFromJsFunc<VideoDevice>("MudExCapture.getAvailableVideoDevices");
            });

        RegisterAsMultiSelection<AudioDevice, List<AudioDevice>>(list => list, items => items.ToList(),
            select =>
            {
                select.MultiSelection = true;
                select.AvailableItemsLoadFunc = LoadFromJsFunc<AudioDevice>("MudExCapture.getAvailableAudioDevices");
            });
        RegisterAsMultiSelection<AudioDevice, IList<AudioDevice>>(list => list, items => items.ToList(),
            select =>
            {
                select.MultiSelection = true;
                select.AvailableItemsLoadFunc = LoadFromJsFunc<AudioDevice>("MudExCapture.getAvailableAudioDevices");
            });
        RegisterAsMultiSelection<AudioDevice, AudioDevice[]>(list => list, items => items.ToArray(),
            select =>
            {
                select.MultiSelection = true;
                select.AvailableItemsLoadFunc = LoadFromJsFunc<AudioDevice>("MudExCapture.getAvailableAudioDevices");
            });
    }

    public static Func<CancellationToken, Task<IList<T>>> LoadFromJsFunc<T>(string identifier, IJSRuntime js = null) 
        => token => (js ?? JsImportHelper.GetInitializedJsRuntime()).InvokeAsync<IList<T>>(identifier, token, null).AsTask();

    public static void RegisterAsSelection<T>(Action<MudExSelect<T>> configure) {
        RegisterDefault(s => s.Value, configure);
    }

    public static void RegisterAsMultiSelection<TItem, TCollection>(
        Func<TCollection, IEnumerable<TItem>> toEnumerable,
        Func<IEnumerable<TItem>, TCollection> toCollection,
        Action<MudExSelect<TItem>> configure)
    {
        RegisterDefault<TCollection, IEnumerable<TItem>, MudExSelect<TItem>>(
            s => s.SelectedValues,
            s =>
            {
                s.ValuePresenter = ValuePresenter.Chip;
                configure?.Invoke(s);
            },
            toEnumerable,
            toCollection);
    }



    /// <summary>
    /// Registers the MudExColorEdit component for various color types.
    /// </summary>
    public static void RegisterMudExColorEditForColors()
    {
        //RegisterDefault<MudColor, MudExColor, MudExColorEdit>(f => f.Value, ColorPickerOptions(), c => c, c => c.ToMudColor());
        RegisterDefault<MudColor, string, MudExColorEdit>(f => f.ValueString, ColorPickerOptions());

        RegisterDefault<System.Drawing.Color, MudExColor, MudExColorEdit>(f => f.Value, ColorPickerOptions(), c => c, c => c.ToMudColor().ToDrawingColor());
        RegisterDefault<System.Drawing.Color?, MudExColor, MudExColorEdit>(f => f.Value, ColorPickerOptions(), c => c ?? MudExColor.Default, c => c.ToMudColor().ToDrawingColor());
    }

    /// <summary>
    /// Registers the MudColorPicker component for various color types.
    /// </summary>
    public static void RegisterMudColorPickerForColors()
    {
        RegisterDefault<MudColor, MudColor, MudExColorPicker>(f => f.Value, ColorPickerOptions(), c => c, c => c);
        RegisterDefault<System.Drawing.Color, MudColor, MudExColorPicker>(f => f.Value, ColorPickerOptions(), c => new MudColor(c.R, c.G, c.B, c.A), mc => System.Drawing.Color.FromArgb(mc.A, mc.R, mc.G, mc.B));
    }

    /// <summary>
    /// Returns the meta configuration for rendering a color picker for a string property.
    /// </summary>
    public static Action<ObjectEditMeta<T>> ColorFromStringOptions<T>(KeyValuePair<string, MudColor>[] cssVars = null)
    {
        return meta =>
        {
            var attributes = ColorPickerOptions();
            if (cssVars != null)            
                attributes.AddOrUpdate(nameof(MudExColorEdit.CssVars), cssVars);            
            meta.Properties<string>().Where(p => p?.Value?.ToString()?.StartsWith("rgb") == true || p?.Value?.ToString()?.StartsWith("#") == true)
                .RenderWith<MudExColorEdit, string, string>(edit => edit.ValueString)
                .WithAdditionalAttributes(attributes);
        };
    }

    internal static Dictionary<string, object> ColorPickerOptions()
    {
        return new Dictionary<string, object>
        {
            {nameof(MudExColorEdit.Editable), true},
            {nameof(MudExColorEdit.ForceSelectOfMudColor), true},
            {nameof(MudExColorEdit.ShowToolbar), true},
            {nameof(MudExColorEdit.DelayValueChangeToPickerClose), true},
            {nameof(MudExColorEdit.PickerVariant), PickerVariant.Inline},
            {nameof(MudExColorEdit.PreviewMode), ColorPreviewMode.Icon},
        };
    }

    private static Dictionary<string, object> TimePickerOptions()
    {
        var currentCulture = CultureInfo.CurrentCulture;
        var timeFormat = currentCulture.DateTimeFormat.LongTimePattern;
        return new Dictionary<string, object>
        {
            {nameof(MudTimePicker.Editable), true},
            {nameof(MudTimePicker.TimeFormat), timeFormat},
            {nameof(MudTimePicker.Placeholder), timeFormat},
            {nameof(MudTimePicker.Culture), currentCulture}
        };
    }


    private static Dictionary<string, object> DatePickerOptions(bool withPattern)
    {
        var currentCulture = CultureInfo.CurrentCulture;
        var dateFormat = currentCulture.DateTimeFormat.ShortDatePattern;
        var res = new Dictionary<string, object>()
        {
            {nameof(MudDatePicker.Editable), true},
            {nameof(MudDatePicker.DateFormat), dateFormat},
            {nameof(MudDatePicker.Placeholder), dateFormat},
            {nameof(MudDatePicker.Culture), currentCulture}
        };
        //if (withPattern) // TODO: Currently not supported because of value is not set on pick if enabled
        //    res.Add(nameof(MudDatePicker.Mask), new DateMask(dateFormat));
        return res;
    }

    internal static bool HasRenderDataForType(Type type) => RenderData.ContainsKey(type);
    
    /// <summary>
    /// Returns the render data for the given property meta.
    /// </summary>
    public static IRenderData GetRenderData(ObjectEditPropertyMeta propertyMeta)
        => FindFromProvider(propertyMeta) ?? (RenderData.ContainsKey(propertyMeta.PropertyInfo.PropertyType) ? RenderData[propertyMeta.PropertyInfo.PropertyType].Clone() as IRenderData : TryFindDynamicRenderData(propertyMeta));


    ///<summary>
    /// Registers a default RenderData configuration for a property type using a component with additional options.
    /// <typeparam name="TPropertyType">The type of the property to register the default for.</typeparam>
    /// <typeparam name="TComponent">The component type to use for rendering.</typeparam>
    /// <param name="options">An action to configure the component.</param>
    /// <returns>A RenderData instance configured with the specified component and options.</returns>
    ///</summary>
    public static RenderData<TPropertyType, TPropertyType> RegisterDefault<TPropertyType, TComponent>(Action<TComponent> options) where TComponent : new()
        => RegisterDefault(typeof(TPropertyType), Options.RenderData.For(options)) as RenderData<TPropertyType, TPropertyType>;

    ///<summary>
    /// Registers a default RenderData configuration for a property type using a component and field with converters.
    /// <typeparam name="TPropertyType">The type of the property.</typeparam>
    /// <typeparam name="TFieldType">The field type within the component.</typeparam>
    /// <typeparam name="TComponent">The component type for rendering.</typeparam>
    /// <param name="valueField">An expression pointing to the component's field to bind.</param>
    /// <param name="toFieldTypeConverter">Optional converter from property type to field type.</param>
    /// <param name="toPropertyTypeConverter">Optional converter from field type back to property type.</param>
    /// <returns>A RenderData instance configured with the specified component, field, and converters.</returns>
    ///</summary>
    public static RenderData<TPropertyType, TPropertyType> RegisterDefault<TPropertyType, TFieldType, TComponent>(Expression<Func<TComponent, TFieldType>> valueField, Func<TPropertyType, TFieldType> toFieldTypeConverter = null, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null) where TComponent : new()
        => RegisterDefault(typeof(TPropertyType), Options.RenderData.For(valueField, toFieldTypeConverter, toPropertyTypeConverter)) as RenderData<TPropertyType, TPropertyType>;

    ///<summary>
    /// Registers a default RenderData configuration for a property type using a component and field with additional options and converters.
    /// <typeparam name="TPropertyType">The type of the property.</typeparam>
    /// <typeparam name="TFieldType">The field type within the component.</typeparam>
    /// <typeparam name="TComponent">The component type for rendering.</typeparam>
    /// <param name="valueField">An expression pointing to the component's field to bind.</param>
    /// <param name="options">An action to configure the component.</param>
    /// <param name="toFieldTypeConverter">Optional converter from property type to field type.</param>
    /// <param name="toPropertyTypeConverter">Optional converter from field type back to property type.</param>
    /// <returns>A RenderData instance configured with the specified component, field, options, and converters.</returns>
    ///</summary>
    public static RenderData<TPropertyType, TPropertyType> RegisterDefault<TPropertyType, TFieldType, TComponent>(Expression<Func<TComponent, TFieldType>> valueField, Action<TComponent> options, Func<TPropertyType, TFieldType> toFieldTypeConverter = null, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null) where TComponent : new()
        => RegisterDefault(typeof(TPropertyType), Options.RenderData.For(valueField, options, toFieldTypeConverter, toPropertyTypeConverter)) as RenderData<TPropertyType, TPropertyType>;

    ///<summary>
    /// Registers a default RenderData configuration for a property type using a component and field with additional options specified as a dictionary and converters.
    /// <typeparam name="TPropertyType">The type of the property.</typeparam>
    /// <typeparam name="TFieldType">The field type within the component.</typeparam>
    /// <typeparam name="TComponent">The component type for rendering.</typeparam>
    /// <param name="valueField">An expression pointing to the component's field to bind.</param>
    /// <param name="options">A dictionary of additional options to configure the component.</param>
    /// <param name="toFieldTypeConverter">Optional converter from property type to field type.</param>
    /// <param name="toPropertyTypeConverter">Optional converter from field type back to property type.</param>
    /// <returns>A RenderData instance configured with the specified component, field, options, and converters.</returns>
    ///</summary>
    public static RenderData<TPropertyType, TPropertyType> RegisterDefault<TPropertyType, TFieldType, TComponent>(Expression<Func<TComponent, TFieldType>> valueField, IDictionary<string, object> options, Func<TPropertyType, TFieldType> toFieldTypeConverter = null, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null) where TComponent : new()
        => RegisterDefault(typeof(TPropertyType), Options.RenderData.For(valueField, toFieldTypeConverter, toPropertyTypeConverter).AddAttributes(false, options?.ToArray())) as RenderData<TPropertyType, TPropertyType>;

    ///<summary>
    /// Registers a default RenderData configuration for a property type using a component with a direct field binding.
    /// <typeparam name="TPropertyType">The type of the property.</typeparam>
    /// <typeparam name="TComponent">The component type for rendering.</typeparam>
    /// <param name="valueField">An expression pointing to the component's field to bind directly to the property type.</param>
    /// <returns>A RenderData instance configured with the specified component and direct field binding.</returns>
    ///</summary>
    public static RenderData<TPropertyType, TPropertyType> RegisterDefault<TPropertyType, TComponent>(Expression<Func<TComponent, TPropertyType>> valueField) where TComponent : new()
        => RegisterDefault(typeof(TPropertyType), Options.RenderData.For(valueField)) as RenderData<TPropertyType, TPropertyType>;

    ///<summary>
    /// Adds a property type registration to the global defaults, associating it with a specific rendering component, field, and instance for attributes configuration.
    /// <typeparam name="TPropertyType">The property type for which the default is registered.</typeparam>
    /// <typeparam name="TComponent">The component type used for rendering.</typeparam>
    /// <param name="valueField">An expression identifying the component's field that binds to the property.</param>
    /// <param name="instanceForAttributes">An instance of the component used to derive additional attributes.</param>
    /// <returns>The registered RenderData instance for the specified property type.</returns>
    ///</summary>
    public static RenderData<TPropertyType, TPropertyType> RegisterDefault<TPropertyType, TComponent>(Expression<Func<TComponent, TPropertyType>> valueField, TComponent instanceForAttributes) where TComponent : new()
        => RegisterDefault(typeof(TPropertyType), Options.RenderData.For(valueField, instanceForAttributes)) as RenderData<TPropertyType, TPropertyType>;

    ///<summary>
    /// Adds or updates a default RenderData association for a given property type.
    /// <param name="propertyType">The type of the property for which the default RenderData is being registered or updated.</param>
    /// <param name="renderData">The RenderData instance to associate with the property type.</param>
    /// <returns>The registered or updated RenderData instance.</returns>
    ///</summary>
    public static IRenderData RegisterDefault(Type propertyType, IRenderData renderData)
    {
        RenderData.AddOrUpdate(propertyType, renderData);
        return renderData;
    }

    ///<summary>
    /// Registers a default RenderData configuration for a property type using a component with a direct field binding and additional component configuration options.
    /// <typeparam name="TPropertyType">The type of the property.</typeparam>
    /// <typeparam name="TComponent">The component type for rendering.</typeparam>
    /// <param name="valueField">An expression pointing to the component's field to bind directly to the property type.</param>
    /// <param name="options">An action to configure additional options on the component.</param>
    /// <returns>A RenderData instance configured with the specified component, direct field binding, and additional options.</returns>
    ///</summary>
    public static RenderData<TPropertyType, TPropertyType> RegisterDefault<TPropertyType, TComponent>(Expression<Func<TComponent, TPropertyType>> valueField, Action<TComponent> options) where TComponent : new()
        => RegisterDefault(typeof(TPropertyType), Options.RenderData.For(valueField, options)) as RenderData<TPropertyType, TPropertyType>;

    ///<summary>
    /// Registers a default RenderData configuration for a property type using a component with a direct field binding and additional options specified as a dictionary.
    /// <typeparam name="TPropertyType">The type of the property.</typeparam>
    /// <typeparam name="TComponent">The component type for rendering.</typeparam>
    /// <param name="valueField">An expression pointing to the component's field to bind directly to the property type.</param>
    /// <param name="options">A dictionary of additional options to configure the component.</param>
    /// <returns>A RenderData instance configured with the specified component, direct field binding, and additional options provided as a dictionary.</returns>
    ///</summary>
    public static RenderData<TPropertyType, TPropertyType> RegisterDefault<TPropertyType, TComponent>(Expression<Func<TComponent, TPropertyType>> valueField, IDictionary<string, object> options) where TComponent : new()
        => RegisterDefault(typeof(TPropertyType), Options.RenderData.For(valueField).AddAttributes(false, options?.ToArray())) as RenderData<TPropertyType, TPropertyType>;


    private static IRenderData TryFindDynamicRenderData(ObjectEditPropertyMeta propertyMeta)
    {
        var propertyType = propertyMeta.PropertyInfo.PropertyType;
        if (propertyType.IsEnum || propertyType.IsNullableEnum()) // Enum support
        {
            var controlType = typeof(MudExEnumSelect<>).MakeGenericType(propertyType);
            var renderDataType = typeof(RenderData<,>).MakeGenericType(propertyType, propertyType);
            return renderDataType.CreateInstance<IRenderData>(nameof(MudExEnumSelect<object>.Value), controlType, null);
        }
        
        if (propertyMeta.PropertyInfo.PropertyType.IsCollection() || propertyMeta.PropertyInfo.PropertyType.IsEnumerable()) // Collection support
        { // TODO: When IEnumerable maybe disable add button
            try
            {
                var collectionType = propertyType.GetGenericArguments().FirstOrDefault() ?? propertyType.GetElementType();
                var renderDataType = typeof(RenderData<,>).MakeGenericType(propertyMeta.PropertyInfo.PropertyType, typeof(ICollection<>).MakeGenericType(collectionType));
                var type = typeof(MudExCollectionEditor<>).MakeGenericType(collectionType);
                return Activator.CreateInstance(renderDataType, nameof(MudExCollectionEditor<object>.Items), type, new Dictionary<string, object>()
                {
                    //{nameof(MudExCollectionEditor<object>.MaxHeight), 400} // TODO: Make this configurable or think about default setting max height to have the advantages of virtualization
                }) as IRenderData;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        if (propertyType.IsKeyValuePair())
        {
            // TODO: Implement KeyValuePair support
        }

        return null;
    }

    private static IRenderData FindFromProvider(ObjectEditPropertyMeta propertyMeta)
    {
        return Providers.Where( p => ProviderIsValidFor(p, propertyMeta))
            .Select(provider => provider.GetRenderData(propertyMeta))
            .FirstOrDefault(renderData => renderData != null);
    }

    private static bool ProviderIsValidFor(IDefaultRenderDataProvider defaultRenderDataProvider, ObjectEditPropertyMeta propertyMeta)
    {
        var providerType = defaultRenderDataProvider.GetType();
        return providerType.ImplementsInterface(typeof(IDefaultRenderDataProviderFor<>).MakeGenericType(propertyMeta.PropertyInfo.PropertyType)) 
               || !providerType.GetInterfaces().Any(p => p.IsGenericType && p.GetGenericTypeDefinition() == typeof(IDefaultRenderDataProviderFor<>));
    }
}