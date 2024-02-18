using System.Reflection;
using Microsoft.AspNetCore.Components;
using Nextended.Core.Extensions;

namespace MudBlazor.Extensions.Components;

/// <summary>
/// Drop down component to select an enum value or multiple on flags enums
/// </summary>
public partial class MudExEnumSelect<TEnum>
{
    bool _isFlagsEnum;

    private RenderFragment Inherited() => builder => base.BuildRenderTree(builder);


    private static IList<TEnum> EnumValueList()
        => EnumOrUnderlyingType().GetEnumValues().Cast<TEnum>().ToList();

    private static Type EnumOrUnderlyingType()
        => IsNullableEnum ? Nullable.GetUnderlyingType(typeof(TEnum)) : typeof(TEnum);

    private static bool IsNullableEnum => typeof(TEnum).IsNullableEnum();

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();        
        _isFlagsEnum = EnumOrUnderlyingType().GetCustomAttribute<FlagsAttribute>() != null;
        MultiSelection = _isFlagsEnum;
        ItemCollection = EnumValueList();
        Clearable = true;
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        if (_isFlagsEnum)
        {
            var valueEnum = Value as Enum;
            var toSelect = ItemCollection.Where(e => valueEnum?.HasFlag((e as Enum)!) == true).ToList();
            SelectedValues = toSelect;
        }
        else if (Value != null)
        {
            SelectedValues = new[] { Value };
        }
    }

    /// <inheritdoc />
    protected override void OnBeforeSelectedChanged(IEnumerable<TEnum> selected)
    {
        if (_isFlagsEnum)
        {
            var selectedAsArray = selected as TEnum[] ?? selected?.ToArray() ?? Array.Empty<TEnum>();
            if (IsNullableEnum && !selectedAsArray.Any())
            {
                Value = default;              
                ValueChanged.InvokeAsync(Value);
                return;
            }
            if (IsNullableEnum && Value == null)
            {
                Value = selectedAsArray.FirstOrDefault();
            }
            foreach (var e in ItemCollection)
                Value = SetFlag(e, false);
            foreach (var e in selectedAsArray)
                Value = SetFlag(e, true);
            
            ValueChanged.InvokeAsync(Value);
        }
    }


    private T SetFlag<T>(T flag, bool set)
    {
        Enum value = Value as Enum;

        var underlyingType = Enum.GetUnderlyingType(EnumOrUnderlyingType());

        // note: AsInt mean: math integer vs enum (not the c# int type)
        dynamic valueAsInt = Convert.ChangeType(value, underlyingType);
        dynamic flagAsInt = Convert.ChangeType(flag, underlyingType);
        if (set)
            valueAsInt |= flagAsInt;
        else
            valueAsInt &= ~flagAsInt;

        return (T)valueAsInt;
    }
}