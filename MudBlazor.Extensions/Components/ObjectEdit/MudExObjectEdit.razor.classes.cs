using System.ComponentModel;

namespace MudBlazor.Extensions.Components.ObjectEdit;

/// <summary>
/// Specifies the grouping style.
/// </summary>
public enum GroupingStyle
{
    /// <summary>
    /// Grouped flat with s line
    /// </summary>
    Flat,

    /// <summary>
    /// Use MudBlazor expansion panel
    /// </summary>
    DefaultExpansionPanel
}

/// <summary>
/// Specifies the property filter mode.
/// </summary>
public enum PropertyFilterMode
{
    /// <summary>
    /// Filter is toggable
    /// </summary>
    Toggleable,

    /// <summary>
    /// Filter is always visible
    /// </summary>
    AlwaysVisible,

    /// <summary>
    /// No filter at all
    /// </summary>
    Disabled
}

/// <summary>
/// Specifies the alignment of an action.
/// </summary>
public enum ActionAlignment
{
    /// <summary>
    /// Right
    /// </summary>
    Right,

    /// <summary>
    /// Left
    /// </summary>
    Left
}

/// <summary>
/// Specifies when the registered configurations will applied.
/// </summary>
public enum RegisteredConfigurationBehaviour
{
    /// <summary>
    /// Registered configurations will applied first
    /// </summary>
    ExecutedBefore,

    /// <summary>
    /// Registered configurations will applied after component property configurations
    /// </summary>
    ExecutedAfter,

    /// <summary>
    /// Registered configurations will ignored
    /// </summary>
    IgnoreRegisteredConfigurations
}

/// <summary>
/// Specifies the mode of path display.
/// </summary>
public enum PathDisplayMode
{
    /// <summary>
    /// Display path as separate label
    /// </summary>
    DisplaySeparate,

    /// <summary>
    /// Display path as group
    /// </summary>
    DisplayAsGroupName,

    /// <summary>
    /// Do not display the path
    /// </summary>
    None
}

/// <summary>
/// Specifies the target for state storage.
/// </summary>
public enum StateTarget
{
    /// <summary>
    /// Local storage
    /// </summary>
    [Description("localStorage")]
    LocalStorage,

    /// <summary>
    /// Session storage
    /// </summary>
    [Description("sessionStorage")]
    SessionStorage
}

/// <summary>
/// Specifies the trigger for data change.
/// </summary>
public enum DataChangeTrigger
{
    /// <summary>
    /// Import or export
    /// </summary>
    ExportImport,

    /// <summary>
    /// State restored
    /// </summary>
    StateSaveLoad
}

/// <summary>
/// Represents a model for primitive types.
/// </summary>
public class ModelForPrimitive<T>
{
    /// <summary>
    /// Creates a new instance
    /// </summary>
    public ModelForPrimitive()
    { }

    /// <summary>
    /// Creates a new instance
    /// </summary>
    public ModelForPrimitive(T value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets or sets the value of the model.
    /// </summary>
    public T Value { get; set; }
}

/// <summary>
/// Represents exported data.
/// </summary>
public class ExportedData<T>
{
    /// <summary>
    /// Gets or sets the value of the exported data.
    /// </summary>
    public T Value { get; set; }

    /// <summary>
    /// Gets or sets the JSON representation of the exported data.
    /// </summary>
    public string Json { get; set; }

    /// <summary>
    /// Gets or sets the data change trigger.
    /// </summary>
    public DataChangeTrigger TriggerdFrom { get; set; } = DataChangeTrigger.ExportImport;
}

/// <summary>
/// Represents imported data.
/// </summary>
public class ImportedData<T> : ExportedData<T> { }

/// <summary>
/// Represents data to be imported.
/// </summary>
public class ImportData<T> : ExportData<T> { }

/// <summary>
/// Represents data to be exported.
/// </summary>
public class ExportData<T> : ExportedData<T>
{
    /// <summary>
    /// Gets or sets a value indicating whether the export operation should be cancelled.
    /// </summary>
    public bool Cancel { get; set; }
}
