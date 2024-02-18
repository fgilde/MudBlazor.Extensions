using System.Linq.Expressions;
using System.Reflection;
using Nextended.Core;
using Nextended.Core.Extensions;
using Nextended.Core.Helper;

namespace MudBlazor.Extensions.Components.ObjectEdit.Options;

/// <summary>
/// Object edit metadata for a specific object type.
/// </summary>
public sealed class ObjectEditMeta<T> : ObjectEditMeta
{
    /// <summary>
    /// Value of the object to be edited.
    /// </summary>
    public T Value { get; set; }
    
    /// <summary>
    /// Property resolver functions to be used to determine which properties to include in the edit.
    /// </summary>
    public List<Func<PropertyInfo, bool>> PropertyResolverFunctions { get; set; } = new();

    private List<ObjectEditPropertyMeta> _properties;
    internal Func<ObjectEditPropertyMeta, object> OrderFn { get; set; } = meta => meta.Settings.Order;
    internal bool OrderAscending { get; set; } = true;

    /// <summary>
    /// Creates a new instance of ObjectEditMeta for the given object.
    /// </summary>
    /// <param name="value"></param>
    public ObjectEditMeta(T value)
    {
        Value = value;
    }

    internal ObjectEditMeta<T> SetValue(T value)
    {
        AllProperties.Apply(pm => pm.SetReferenceHolder(pm.ReferenceHolder == (object)Value ? value : pm.ReferenceHolder));
        Value = value;
        return this;
    }
    
    /// <summary>
    /// All properties of the object to be edited.
    /// </summary>
    public override IList<ObjectEditPropertyMeta> AllProperties => Ordered((_properties ??= ReadProperties()).Recursive(m => m.Children)).ToList();
    
    internal IEnumerable<ObjectEditPropertyMeta> Ordered(IEnumerable<ObjectEditPropertyMeta> entries)
        => OrderAscending ? entries.OrderBy(OrderFn) : entries.OrderByDescending(OrderFn);

    /// <summary>
    /// All properties of the object to be edited with excepting the given.
    /// </summary>
    public IEnumerable<ObjectEditPropertyMetaOf<T>> PropertiesExcept(params Expression<Func<T, object>>[] expressionsToExcept)
        => Properties().Where(p => !expressionsToExcept.Select(Property).Contains(p));

    /// <summary>
    /// Returns the properties of the object to be edited with given expression.
    /// </summary>
    public IEnumerable<ObjectEditPropertyMetaOf<T>> Properties(params Expression<Func<T, object>>[] expressions)
        => expressions.Any() ? expressions.Select(Property) : AllProperties.Cast<ObjectEditPropertyMetaOf<T>>();

    /// <summary>
    /// All properties of the object to be edited with given type.
    /// </summary>
    public IEnumerable<ObjectEditPropertyMetaOf<T>> Properties<TPropertyType>()
        => AllProperties.Where(m => m.PropertyInfo.PropertyType == typeof(TPropertyType)).Cast<ObjectEditPropertyMetaOf<T>>();

    /// <summary>
    /// Returns the property of the object to be edited with given expression.
    /// </summary>
    public ObjectEditPropertyMetaOf<T> Property(Expression<Func<T, object>> expression) // TODO: Expression<Func<T, TProperty>> returns ObjectEditPropertyMetaOf<TModel, TProperty>
    {
        //var infos = PropertyPath<T>.Get(expression).ToArray();
        var infos = expression.GetMemberInfosPaths().ToArray();
        var name = string.Join(".", infos.Select(i => i.Name));

        //return (ObjectEditPropertyMetaOf<T>) (Property(infos) ?? Property(name));
        return (ObjectEditPropertyMetaOf<T>) (Property(name) ?? Property(infos));
    }
    
    private IEnumerable<ObjectEditPropertyMeta> GetProperties(Type type, object value, ObjectEditMeta owner = null)
    {
        var res = new List<ObjectEditPropertyMeta>();
        foreach (var propertyInfo in (value?.GetType() ?? type).GetProperties(BindingFlags).Where(ShouldResolve))
        {
            try
            {
                var t = propertyInfo.PropertyType;
                var editPropertyMeta = new ObjectEditPropertyMetaOf<T>(owner ?? this, propertyInfo, value);
                if (IsEditableSubObject(t)) // object in object
                {
                    var reference = Check.TryCatch<object, Exception>(() => propertyInfo.GetValue(value));
                    reference ??= Check.TryCatch<object, Exception>(() =>
                    {
                        propertyInfo.SetValue(value, ReflectionHelper.CreateInstance(propertyInfo.PropertyType));
                        return propertyInfo.GetValue(value);
                    });
                    
                    if(reference != null) {
                        var instance = ((ObjectEditMeta)Activator.CreateInstance(typeof(ObjectEditMeta<>).MakeGenericType(t), reference)).SetProperties(m => m.Parent = owner ?? this);
                        var groupName = editPropertyMeta.Settings.LabelFor(null);
                        editPropertyMeta.Children.AddRange(GetProperties(t, reference, instance).Apply(meta => meta.WithGroup(groupName).Parent = editPropertyMeta));
                    }
                }
                res.Add(editPropertyMeta);
            }
            catch (Exception e)
            {
                var format = $"Error '{e.Message}' while reading property {propertyInfo.Name} of type {propertyInfo.PropertyType.Name} declared on {propertyInfo.DeclaringType?.Name}.";
                Console.WriteLine(format);
                throw;
            }
        }
        return res;
    }

    private bool ShouldResolve(PropertyInfo arg) =>
        PropertyResolverFunctions is not {Count: > 0} 
        || PropertyResolverFunctions.All(func => func?.Invoke(arg) ?? true);

    private bool IsEditableSubObject(Type t)
    {
        if (t.IsKeyValuePair())
        {
            // TODO: Implement KeyValuePair support
        }
        return !t.IsValueType && !t.IsPrimitive && t != typeof(decimal) && t != typeof(string) &&
               !t.IsEnumerableOrArray();
    }

    private List<ObjectEditPropertyMeta> ReadProperties() => GetProperties(typeof(T), Value).ToList();

    /// <summary>
    /// Updates the conditional settings of all properties of the object to be edited.
    /// </summary>
    public ObjectEditMeta<T> UpdateAllConditionalSettings(T value)
    {
        AllProperties.Apply(p => p?.UpdateConditionalSettings(value));
        return this;
    }

    /// <summary>
    /// Updates the conditional settings of all properties of the object to be edited.
    /// </summary>
    public ObjectEditMeta<T> UpdateAllConditionalSettings()
    {
        return UpdateAllConditionalSettings(Value);
    }
}
