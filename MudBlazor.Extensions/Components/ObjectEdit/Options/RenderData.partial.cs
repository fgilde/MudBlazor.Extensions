using System.Linq.Expressions;
using MudBlazor.Extensions.Helper.Internal;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components.ObjectEdit.Options;

public partial class RenderData
{
    #region Static Factory Methods

    public static IRenderData For(string valueField, Type propertyType, Type controlType)
    {
        return For(valueField, propertyType, propertyType, controlType);
    }

    public static IRenderData For(string valueField, Type propertyType, Type fieldType, Type controlType)
    {
        return typeof(RenderData<,>).MakeGenericType(propertyType, fieldType).CreateInstance<IRenderData>(valueField, controlType, null);
    }


    /// <summary>
    /// Returns a new RenderData instance for the given component type, with the specified options.
    /// </summary>
    public static RenderData<TPropertyType, TFieldType> For<TComponent, TPropertyType, TFieldType>(
        Expression<Func<TComponent, TFieldType>> valueField, Action<TComponent> options,
        Func<TPropertyType, TFieldType> toFieldTypeConverter = null,
        Func<TFieldType, TPropertyType> toPropertyTypeConverter = null) where TComponent : new()
        => new(valueField.GetMemberName(), typeof(TComponent), PropertyHelper.ValuesDictionary(options, true))
        {
            ToFieldTypeConverterFn = toFieldTypeConverter,
            ToPropertyTypeConverterFn = toPropertyTypeConverter
        };

    /// <summary>
    /// Returns a new RenderData instance for the given component type, with the specified options.
    /// </summary>    
    public static RenderData<TPropertyType, TFieldType> For<TComponent, TPropertyType, TFieldType>(
        Expression<Func<TComponent, TFieldType>> valueField, TComponent instanceForAttributes,
        Func<TPropertyType, TFieldType> toFieldTypeConverter = null,
        Func<TFieldType, TPropertyType> toPropertyTypeConverter = null) where TComponent : new()
        => new(valueField.GetMemberName(), typeof(TComponent),
            PropertyHelper.ValuesDictionary(instanceForAttributes, true))
        {
            ToFieldTypeConverterFn = toFieldTypeConverter,
            ToPropertyTypeConverterFn = toPropertyTypeConverter
        };

    /// <summary>
    /// Returns a new RenderData instance for the given component type, with the specified options.
    /// </summary>    
    public static RenderData<TPropertyType, TFieldType> For<TComponent, TPropertyType, TFieldType>(
        Expression<Func<TComponent, TFieldType>> valueField,
        Func<TPropertyType, TFieldType> toFieldTypeConverter = null,
        Func<TFieldType, TPropertyType> toPropertyTypeConverter = null)
        => new(valueField.GetMemberName(), typeof(TComponent))
        {
            ToFieldTypeConverterFn = toFieldTypeConverter,
            ToPropertyTypeConverterFn = toPropertyTypeConverter
        };

    /// <summary>
    /// Returns a new RenderData instance for the given component type, with the specified options.
    /// </summary>    
    public static RenderData<TPropertyType, TPropertyType> For<TComponent, TPropertyType>(
        Expression<Func<TComponent, TPropertyType>> valueField, Action<TComponent> options) where TComponent : new()
        => new(valueField.GetMemberName(), typeof(TComponent), PropertyHelper.ValuesDictionary(options, true));

    /// <summary>
    /// Returns a new RenderData instance for the given component type, with the specified options.
    /// </summary>    
    public static RenderData<TPropertyType, TPropertyType> For<TComponent, TPropertyType>(
        Expression<Func<TComponent, TPropertyType>> valueField, TComponent instanceForAttributes)
        where TComponent : new()
        => new(valueField.GetMemberName(), typeof(TComponent),
            PropertyHelper.ValuesDictionary(instanceForAttributes, true));

    /// <summary>
    /// Returns a new RenderData instance for the given component type, with the specified options.
    /// </summary>    
    public static RenderData<TPropertyType, TPropertyType> For<TComponent, TPropertyType>(
        Expression<Func<TComponent, TPropertyType>> valueField)
        => new(valueField.GetMemberName(), typeof(TComponent));

    /// <summary>
    /// Returns a new RenderData instance for the given component type, with the specified options.
    /// </summary>    
    public static RenderData For<TComponent>(Action<TComponent> options) where TComponent : new()
        => For(typeof(TComponent), PropertyHelper.ValuesDictionary(options, true));

    /// <summary>
    /// Returns a new RenderData instance for the given component type, with the specified options.
    /// </summary>    
    public static RenderData For<TComponent>(IDictionary<string, object> attributes = null)
        => For(typeof(TComponent), attributes);

    /// <summary>
    /// Returns a new RenderData instance for the given component type, with the specified options.
    /// </summary>    
    public static RenderData For(Type componentType,
        IDictionary<string, object> attributes = null) => new(componentType, attributes);

    /// <summary>
    /// Returns a new RenderData instance for the given component type, with the specified options.
    /// </summary>    
    public static RenderData For(ICustomRenderer customRenderer) => new(null) { CustomRenderer = customRenderer };

    #endregion
}