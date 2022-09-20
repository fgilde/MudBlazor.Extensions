using System.ComponentModel;

namespace MudBlazor.Extensions.Components.ObjectEdit;

public enum GroupingStyle
{
    Flat,
    DefaultExpansionPanel
}

public enum PropertyFilterMode
{
    Toggleable,
    AlwaysVisible,
    Disabled
}
public enum ActionAlignment
{
    Right,
    Left
}

public enum RegisteredConfigurationBehaviour
{
    ExecutedBefore,
    ExecutedAfter,
    IgnoreRegisteredConfigurations
}

public enum PathDisplayMode
{
    DisplaySeparate,
    DisplayAsGroupName,
    None
}

public enum StateTarget
{
    [Description("localStorage")]
    LocalStorage,
    [Description("sessionStorage")]
    SessionStorage
}

public class ModelForPrimitive<T>
{
    public ModelForPrimitive()
    { }

    public ModelForPrimitive(T value)
    {
        Value = value;
    }

    public T Value { get; set; }
}