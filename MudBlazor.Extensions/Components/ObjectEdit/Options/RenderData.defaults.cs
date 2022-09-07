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

        RegisterDefault<DateTime?, MudDatePicker>(f => f.Date);
        RegisterDefault<DateTime, DateTime?, MudDatePicker>(f => f.Date);
        RegisterDefault<DateOnly, DateTime?, MudDatePicker>(f => f.Date);
        RegisterDefault<TimeOnly, TimeSpan?, MudTimePicker>(f => f.Time);
        RegisterDefault<DateOnly?, DateTime?, MudDatePicker>(f => f.Date);
        RegisterDefault<TimeOnly?, TimeSpan?, MudTimePicker>(f => f.Time);
        RegisterDefault<TimeSpan, TimeSpan?, MudTimePicker>(f => f.Time);
        RegisterDefault<TimeSpan?, MudTimePicker>(f => f.Time);

        RegisterDefault<MudColor, MudColor, MudColorPicker>(f => f.Value, c => c, c => c);
        RegisterDefault<System.Drawing.Color, MudColor, MudColorPicker>(f => f.Value, c => new MudColor(c.R, c.G, c.B, c.A), mc => System.Drawing.Color.FromArgb(mc.A, mc.R, mc.G, mc.B));

        RegisterDefault<bool, MudSwitch<bool>>(s => s.Checked, s => s.Color = MudBlazor.Color.Warning);
        RegisterDefault<bool?, MudCheckBox<bool?>>(s => s.Checked, box =>
        {
            box.TriState = true;
            box.UnCheckedColor = MudBlazor.Color.Default;
            box.Color = MudBlazor.Color.Warning;
        });

        RegisterDefault<ICollection<string>, MudExCollectionEditor<string>>(f => f.Items);
    }


    public static IRenderData GetRenderData(ObjectEditPropertyMeta propertyMeta) 
        => FindFromProvider(propertyMeta) ?? (_renderData.ContainsKey(propertyMeta.PropertyInfo.PropertyType) ? _renderData[propertyMeta.PropertyInfo.PropertyType].Clone() as IRenderData : TryFindDynamicRenderData(propertyMeta));

    public static RenderData<TPropertyType, TPropertyType> RegisterDefault<TPropertyType, TComponent>(Action<TComponent> options) where TComponent : new() 
        => RegisterDefault(typeof(TPropertyType), RenderData.For(options)) as RenderData<TPropertyType, TPropertyType>;

    public static RenderData<TPropertyType, TPropertyType> RegisterDefault<TPropertyType, TFieldType, TComponent>(Expression<Func<TComponent, TFieldType>> valueField, Func<TPropertyType, TFieldType> toFieldTypeConverter = null, Func<TFieldType, TPropertyType> toPropertyTypeConverter = null) where TComponent : new()
        => RegisterDefault(typeof(TPropertyType), RenderData.For(valueField, toFieldTypeConverter, toPropertyTypeConverter)) as RenderData<TPropertyType, TPropertyType>;

    public static RenderData<TPropertyType, TPropertyType> RegisterDefault<TPropertyType, TComponent>(Expression<Func<TComponent, TPropertyType>> valueField) where TComponent : new() 
        => RegisterDefault(typeof(TPropertyType), RenderData.For(valueField)) as RenderData<TPropertyType, TPropertyType>;

    public static RenderData<TPropertyType, TPropertyType> RegisterDefault<TPropertyType, TComponent>(Expression<Func<TComponent, TPropertyType>> valueField, Action<TComponent> options) where TComponent : new()
        => RegisterDefault(typeof(TPropertyType), RenderData.For(valueField, options)) as RenderData<TPropertyType, TPropertyType>;

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

        if (IsCollection(propertyMeta.PropertyInfo.PropertyType)) // Collection support
        {
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

    private static IRenderData FindFromProvider(ObjectEditPropertyMeta propertyMeta)
        => _providers.Select(provider => provider.GetRenderData(propertyMeta)).FirstOrDefault(renderData => renderData != null);
}