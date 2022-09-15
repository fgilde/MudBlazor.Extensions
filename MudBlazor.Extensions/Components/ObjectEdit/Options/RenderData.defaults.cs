using System.Globalization;
using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Utilities;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components.ObjectEdit.Options;

public static class RenderDataDefaults
{
    private static readonly Dictionary<Type, IRenderData> _renderData = new();
    private static readonly List<IDefaultRenderDataProvider> _providers = new();
    private static bool _registeredServicesAdded = false;

    public static void AddRenderDataProvider(IServiceProvider serviceProvider)
    {
        if (!_registeredServicesAdded)
        {
            var defaultRenderDataProviders = serviceProvider?.GetServices<IDefaultRenderDataProvider>().ToArray();
            AddRenderDataProvider(defaultRenderDataProviders);
            _registeredServicesAdded = true;
        }
    }

    public static void AddRenderDataProvider(params IDefaultRenderDataProvider[] provider) => _providers.AddRange(provider.EmptyIfNull());
    public static void RemoveRenderDataProvider(params IDefaultRenderDataProvider[] provider) => _providers.RemoveRange(provider.EmptyIfNull());

    static RenderDataDefaults()
    {
        RegisterDefault<string, MudTextField<string>>(f => f.Value);

        RegisterDefault<int, MudNumericField<int>>(f => f.Value);
        RegisterDefault<double, MudNumericField<double>>(f => f.Value);
        RegisterDefault<float, MudNumericField<float>>(f => f.Value);

        RegisterDefault<DateTime?, MudDatePicker>(f => f.Date, DatePickerOptions(true));
        RegisterDefault<DateTime, DateTime?, MudDatePicker>(f => f.Date, DatePickerOptions(false));
        RegisterDefault<DateOnly, DateTime?, MudDatePicker>(f => f.Date, DatePickerOptions(false));
        RegisterDefault<DateOnly?, DateTime?, MudDatePicker>(f => f.Date, DatePickerOptions(true));

        RegisterDefault<TimeOnly, TimeSpan?, MudTimePicker>(f => f.Time, TimePickerOptions());
        RegisterDefault<TimeOnly?, TimeSpan?, MudTimePicker>(f => f.Time, TimePickerOptions());
        RegisterDefault<TimeSpan, TimeSpan?, MudTimePicker>(f => f.Time, TimePickerOptions());
        RegisterDefault<TimeSpan?, MudTimePicker>(f => f.Time, TimePickerOptions());

        RegisterDefault<MudColor, MudColor, MudColorPicker>(f => f.Value, ColorPickerOptions(), c => c, c => c);
        RegisterDefault<System.Drawing.Color, MudColor, MudColorPicker>(f => f.Value, ColorPickerOptions(), c => new MudColor(c.R, c.G, c.B, c.A), mc => System.Drawing.Color.FromArgb(mc.A, mc.R, mc.G, mc.B));

        //RegisterDefault<bool, MudSwitch<bool>>(s => s.Checked, s => s.Color = MudBlazor.Color.Warning);
        RegisterDefault<bool, MudCheckBox<bool>>(s => s.Checked, box =>
        {
            box.TriState = false;
            box.UnCheckedColor = MudBlazor.Color.Default;
            box.Color = MudBlazor.Color.Warning;
        });
        RegisterDefault<bool?, MudCheckBox<bool?>>(s => s.Checked, box =>
        {
            box.TriState = true;
            box.UnCheckedColor = MudBlazor.Color.Default;
            box.Color = MudBlazor.Color.Warning;
        });

        RegisterDefault<ICollection<string>, MudExCollectionEditor<string>>(f => f.Items);
    }

    private static Dictionary<string, object> ColorPickerOptions()
    {
        return new Dictionary<string, object>
        {
            {nameof(MudColorPicker.Editable), true},
            {nameof(MudColorPicker.DisableToolbar), false},
            {nameof(MudColorPicker.PickerVariant), PickerVariant.Inline},
        };
    }

    private static Dictionary<string, object> TimePickerOptions()
    {
        var currentCulture = CultureInfo.CurrentCulture;
        var timeFormat = currentCulture.DateTimeFormat.ShortTimePattern;
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
        if (withPattern)
            res.Add(nameof(MudDatePicker.Mask), new DateMask(dateFormat));
        return res;
    }

    internal static bool HasRenderDataForType(Type type) => _renderData.ContainsKey(type);
    
    public static IRenderData GetRenderData(ObjectEditPropertyMeta propertyMeta)
        => FindFromProvider(propertyMeta) ?? (_renderData.ContainsKey(propertyMeta.PropertyInfo.PropertyType) ? _renderData[propertyMeta.PropertyInfo.PropertyType].Clone() as IRenderData : TryFindDynamicRenderData(propertyMeta));

    public static RenderData<TPropertyType, TPropertyType> RegisterDefault<TPropertyType, TComponent>(Action<TComponent> options) where TComponent : new()
        => RegisterDefault(typeof(TPropertyType), RenderData.For(options)) as RenderData<TPropertyType, TPropertyType>;

    public static RenderData<TPropertyType, TPropertyType> RegisterDefault<TPropertyType, TFieldType, TComponent>(Expression<Func<TComponent, TFieldType>> valueField, Func<TPropertyType, TFieldType> toFieldTypeConverter = null, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null) where TComponent : new()
        => RegisterDefault(typeof(TPropertyType), RenderData.For(valueField, toFieldTypeConverter, toPropertyTypeConverter)) as RenderData<TPropertyType, TPropertyType>;
    public static RenderData<TPropertyType, TPropertyType> RegisterDefault<TPropertyType, TFieldType, TComponent>(Expression<Func<TComponent, TFieldType>> valueField, Action<TComponent> options, Func<TPropertyType, TFieldType> toFieldTypeConverter = null, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null) where TComponent : new()
        => RegisterDefault(typeof(TPropertyType), RenderData.For(valueField, options, toFieldTypeConverter, toPropertyTypeConverter)) as RenderData<TPropertyType, TPropertyType>;

    public static RenderData<TPropertyType, TPropertyType> RegisterDefault<TPropertyType, TFieldType, TComponent>(Expression<Func<TComponent, TFieldType>> valueField, Dictionary<string, object> options, Func<TPropertyType, TFieldType> toFieldTypeConverter = null, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null) where TComponent : new()
        => RegisterDefault(typeof(TPropertyType), RenderData.For(valueField, toFieldTypeConverter, toPropertyTypeConverter).AddAttributes(false, options?.ToArray())) as RenderData<TPropertyType, TPropertyType>;

    public static RenderData<TPropertyType, TPropertyType> RegisterDefault<TPropertyType, TComponent>(Expression<Func<TComponent, TPropertyType>> valueField) where TComponent : new()
        => RegisterDefault(typeof(TPropertyType), RenderData.For(valueField)) as RenderData<TPropertyType, TPropertyType>;

    public static RenderData<TPropertyType, TPropertyType> RegisterDefault<TPropertyType, TComponent>(Expression<Func<TComponent, TPropertyType>> valueField, Action<TComponent> options) where TComponent : new()
        => RegisterDefault(typeof(TPropertyType), RenderData.For(valueField, options)) as RenderData<TPropertyType, TPropertyType>;

    public static RenderData<TPropertyType, TPropertyType> RegisterDefault<TPropertyType, TComponent>(Expression<Func<TComponent, TPropertyType>> valueField, Dictionary<string, object> options) where TComponent : new()
        => RegisterDefault(typeof(TPropertyType), RenderData.For(valueField).AddAttributes(false, options?.ToArray())) as RenderData<TPropertyType, TPropertyType>;

    public static RenderData<TPropertyType, TPropertyType> RegisterDefault<TPropertyType, TComponent>(Expression<Func<TComponent, TPropertyType>> valueField, TComponent instanceForAttributes) where TComponent : new()
        => RegisterDefault(typeof(TPropertyType), RenderData.For(valueField, instanceForAttributes)) as RenderData<TPropertyType, TPropertyType>;

    public static IRenderData RegisterDefault(Type propertyType, IRenderData renderData)
    {
        _renderData.AddOrUpdate(propertyType, renderData);
        return renderData;
    }

    private static IRenderData TryFindDynamicRenderData(ObjectEditPropertyMeta propertyMeta)
    {
        var propertyType = propertyMeta.PropertyInfo.PropertyType;
        if (propertyType.IsEnum || propertyType.IsNullableEnum()) // Enum support
        {
            var values = propertyType.IsNullableEnum() ? Nullable.GetUnderlyingType(propertyType).GetEnumValues().Cast<object>().ToList() : propertyType.GetEnumValues().Cast<object>().ToList();
            return RenderData.For<MudExChipSelect<object>, object, IEnumerable<object>>(s => s.Selected, s =>
            {
                s.AvailableItems = values;
                s.MultiSelect = false;
                s.ViewMode = ViewMode.NoChips;
            }, s => s.AsEnumerable(), x => x.Select(o => o.MapTo(propertyType)).FirstOrDefault());
        }

        if (IsCollection(propertyMeta.PropertyInfo.PropertyType) || IsEnumerable(propertyMeta.PropertyInfo.PropertyType)) // Collection support
        { // TODO: When IEnumerable maybe disable add button
            try
            {
                var collectionType = propertyType.GetGenericArguments().FirstOrDefault() ?? propertyType.GetElementType();
                var renderDataType = typeof(RenderData<,>).MakeGenericType(propertyMeta.PropertyInfo.PropertyType, typeof(ICollection<>).MakeGenericType(collectionType));
                var type = typeof(MudExCollectionEditor<>).MakeGenericType(collectionType);
                return Activator.CreateInstance(renderDataType, nameof(MudExCollectionEditor<object>.Items), type, new Dictionary<string, object>()) as IRenderData;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        return null;
    }

    private static bool IsCollection(Type type)
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ICollection<>) || type.GetInterfaces().Any(IsCollection);
    private static bool IsEnumerable(Type type)
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>) || type.GetInterfaces().Any(IsCollection);

    private static IRenderData FindFromProvider(ObjectEditPropertyMeta propertyMeta)
        => _providers.Select(provider => provider.GetRenderData(propertyMeta)).FirstOrDefault(renderData => renderData != null);
}