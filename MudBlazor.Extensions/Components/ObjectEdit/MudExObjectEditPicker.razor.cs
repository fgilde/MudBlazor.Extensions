using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Options;

namespace MudBlazor.Extensions.Components.ObjectEdit;

public partial class MudExObjectEditPicker<T>
{

    /// <summary>
    /// Function to display a string representation of the object
    /// </summary>
    [Parameter]
    public Func<T, string> ToStringFunc { get; set; } = x => x?.ToString();

    #region Simple Delegates

    /// <summary>
    /// The object edit metadata for the component.
    /// </summary>
    [Parameter] public ObjectEditMeta<T> MetaInformation { get; set; }

    /// <summary>
    /// Set to true to allow multiple values
    /// </summary>
    [Parameter] public bool MultiSearch { get; set; }

    /// <summary>
    /// If you need the reference for dynamic ignored fields for example because of Model Validation or resets you should set this to true
    /// </summary>
    [Parameter] public bool RenderIgnoredReferences { get; set; }


    /// <summary>
    /// Whether the component should show a loading indicator.
    /// </summary>
    [Parameter] public bool IsLoading { get; set; }

    /// <summary>
    /// Whether the component should automatically update all registered Conditions.
    /// Otherwise you need to call UpdateAllConditions on your own.
    /// </summary>
    [Parameter]
    public bool AutoUpdateConditions { get; set; } = true;

    /// <summary>
    /// The height of the component.
    /// </summary>
    [Parameter] public int? Height { get; set; }

    /// <summary>
    /// The maximum height of the component.
    /// </summary>
    [Parameter] public int? MaxHeight { get; set; }

    /// <summary>
    /// The size unit of the component.
    /// </summary>
    [Parameter] public CssUnit SizeUnit { get; set; } = CssUnit.Pixels;

    /// <summary>
    /// Whether the import action needs confirmation.
    /// </summary>
    [Parameter] public bool ImportNeedsConfirmation { get; set; }

    /// <summary>
    /// If this setting is true, all properties which are ignored by meta config will be removed
    /// </summary>
    [Parameter] public bool RemoveIgnoredFromImport { get; set; } = true;

    /// <summary>
    /// If this setting is true, after import all properties are set instead of full value assignment
    /// </summary>
    [Obsolete("This will hopefully be removed in future versions. Only use it if you have problems with the import or restore feature")]
    [Parameter] public bool SetPropertiesAfterImport { get; set; }

    /// <summary>
    /// The state key for saving and restoring the component state.
    /// </summary>
    [Parameter] public string StateKey { get; set; } = $"mud-ex-object-edit-{typeof(T).FullName}";

    /// <summary>
    /// The text for confirming an import action.
    /// </summary>
    [Parameter] public string ImportConfirmText { get; set; } = "Import";

    /// <summary>
    /// The text for cancelling an import action.
    /// </summary>
    [Parameter] public string ImportCancelText { get; set; } = "Cancel";

    /// <summary>
    /// Whether the component should be virtualized.
    /// </summary>
    [Parameter] public bool Virtualize { get; set; } = false;

    /// <summary>
    /// Whether the component should have a light overlay loading background.
    /// </summary>
    [Parameter] public bool LightOverlayLoadingBackground { get; set; } = true;

    /// <summary>
    /// Whether the component should automatically display an overlay when loading.
    /// </summary>
    [Parameter] public bool AutoOverlay { get; set; } = true;

    /// <summary>
    /// The color of the toolbar buttons.
    /// </summary>
    [Parameter] public Color ToolbarButtonColor { get; set; } = Color.Inherit;

    /// <summary>
    /// Whether the component should add a scroll to top button.
    /// </summary>
    [Parameter] public bool AddScrollToTop { get; set; }

    /// <summary>
    /// The position of the scroll to top button.
    /// </summary>
    [Parameter] public DialogPosition ScrollToTopPosition { get; set; } = DialogPosition.BottomRight;

    /// <summary>
    /// Whether search functionality is enabled.
    /// </summary>
    [Parameter] public bool AllowSearch { get; set; } = true;

    /// <summary>
    /// Whether export functionality is enabled.
    /// </summary>
    [Parameter] public bool AllowExport { get; set; }

    /// <summary>
    /// Whether import functionality is enabled.
    /// </summary>
    [Parameter] public bool AllowImport { get; set; }

    /// <summary>
    /// Whether the component should automatically save and restore its state.
    /// </summary>
    [Parameter] public bool AutoSaveRestoreState { get; set; }

    /// <summary>
    /// If this is true, the component adds the value if possible to url and reads it automatically if its present in Url
    /// </summary>
    [Parameter] public bool StoreAndReadValueFromUrl { get; set; }

    /// <summary>
    /// The storage location for saving and restoring component state.
    /// </summary>
    [Parameter] public StateTarget StateTargetStorage { get; set; } = StateTarget.SessionStorage;

    /// <summary>
    /// The file name for exporting the component data.
    /// </summary>
    [Parameter] public string ExportFileName { get; set; }

    /// <summary>
    /// The icon to display for the import action.
    /// </summary>
    [Parameter] public string ImportIcon { get; set; } = Icons.Material.Outlined.FileOpen;

    /// <summary>
    /// The icon to display for the search action.
    /// </summary>
    [Parameter] public string SearchIcon { get; set; } = Icons.Material.Outlined.Search;

    /// <summary>
    /// The icon to display for expanding and collapsing.
    /// </summary>
    [Parameter] public string ExpandCollapseIcon { get; set; } = Icons.Material.Filled.Expand;

    /// <summary>
    /// The icon to display for the export action.
    /// </summary>
    [Parameter] public string ExportIcon { get; set; } = Icons.Material.Filled.Save;

    /// <summary>
    /// Whether to automatically display the skeleton loading state on component load.
    /// </summary>
    [Parameter] public bool AutoSkeletonOnLoad { get; set; }

    /// <summary>
    /// The color of the toolbar.
    /// </summary>
    [Parameter] public MudExColor ToolbarColor { get; set; } = Color.Default;

    /// <summary>
    /// The color of the group lines in the component.
    /// </summary>
    [Parameter] public MudExColor GroupLineColor { get; set; } = Color.Secondary;

    /// <summary>
    /// The elevation of the group.
    /// </summary>
    [Parameter] public int? GroupElevation { get; set; }

    /// <summary>
    /// The elevation of the toolbar.
    /// </summary>
    [Parameter] public int ToolBarElevation { get; set; }

    /// <summary>
    /// A CSS class for the component toolbar.
    /// </summary>
    [Parameter] public string ToolBarClassObjectEdit { get; set; }

    /// <summary>
    /// A CSS class for the component toolbar paper.
    /// </summary>
    [Parameter] public string ToolBarPaperClass { get; set; }

    /// <summary>
    /// Whether the toolbar should be sticky to the top of the component.
    /// </summary>
    [Parameter] public bool StickyToolbar { get; set; }

    /// <summary>
    /// The positioning CSS value for a sticky toolbar.
    /// </summary>
    [Parameter] public string StickyToolbarTop { get; set; } = string.Empty;

    /// <summary>
    /// Called after the component has imported data.
    /// </summary>
    [Parameter] public EventCallback<ImportedData<T>> AfterImport { get; set; }

    /// <summary>
    /// Called after the component has exported data.
    /// </summary>
    [Parameter] public EventCallback<ExportedData<T>> AfterExport { get; set; }

    /// <summary>
    /// Called before the component export's data. Provides the export data to be manipulated.
    /// If you need to change content of parameter to manipulate export data you can do it here
    /// </summary>
    [Parameter] public EventCallback<ExportData<T>> BeforeExport { get; set; }

    /// <summary>
    /// Called before the component import's data. Provides the import data to be manipulated.
    /// Here you can change content of parameter to manipulate import data
    /// For example you can remove some properties or change the values
    /// This is called before the import is executed
    /// importData.Json = "{\"FirstName\": \"Changed Test\"}";
    /// </summary>
    [Parameter] public EventCallback<ImportData<T>> BeforeImport { get; set; }


    /// <summary>
    /// Called when a property of the edited object is changed.
    /// </summary>
    [Parameter] public EventCallback<ObjectEditPropertyMeta> PropertyChanged { get; set; }

    /// <summary>
    /// Whether to show the property path as title for each property.
    /// </summary>
    [Parameter] public bool ShowPathAsTitleForEachProperty { get; set; }

    /// <summary>
    /// The path display mode for the component.
    /// </summary>
    [Parameter] public PathDisplayMode PathDisplayMode { get; set; }

    /// <summary>
    /// The grouping style for the component.
    /// </summary>
    [Parameter] public GroupingStyle GroupingStyle { get; set; }

    /// <summary>
    /// The filter mode for the component.
    /// </summary>
    [Parameter] public PropertyFilterMode FilterMode { get; set; } = PropertyFilterMode.Toggleable;

    /// <summary>
    /// Event callback if filter input is toggled
    /// </summary>
    [Parameter] public EventCallback<bool> SearchActiveChanged { get; set; }

    /// <summary>
    /// Search is active
    /// </summary>
    [Parameter]
    public bool SearchActive { get; set; }

    /// <summary>
    /// The filter value for the component.
    /// </summary>
    [Parameter] public string Filter { get; set; }


    /// <summary>
    /// The filter values for the component.
    /// </summary>
    [Parameter] public List<string> Filters { get; set; }

    /// <summary>
    /// Whether to automatically hide disabled fields.
    /// </summary>
    [Parameter] public bool AutoHideDisabledFields { get; set; }

    /// <summary>
    /// Whether to disable grouping.
    /// </summary>
    [Parameter] public bool DisableGrouping { get; set; }

    /// <summary>
    /// The default group name for the component.
    /// </summary>
    [Parameter] public string DefaultGroupName { get; set; }

    /// <summary>
    /// Whether to disable field fallback.
    /// </summary>
    [Parameter] public bool DisableFieldFallback { get; set; }

    /// <summary>
    /// Whether to wrap the component in a MudGrid component.
    /// </summary>
    [Parameter] public bool? WrapInMudGrid { get; set; }

    /// <summary>
    /// Whether groups are collapsible in the component.
    /// </summary>
    [Parameter] public bool GroupsCollapsible { get; set; } = true;

    /// <summary>
    /// The global reset settings for the component.
    /// </summary>
    [Parameter] public GlobalResetSettings GlobalResetSettings { get; set; } = new();

    /// <summary>
    /// The default property reset settings for the component.
    /// </summary>
    [Parameter] public PropertyResetSettings DefaultPropertyResetSettings { get; set; }

    /// <summary>
    /// The message box options for reset confirmation.
    /// </summary>
    [Parameter] public MessageBoxOptions ResetConfirmationMessageBoxOptions { get; set; }

    /// <summary>
    /// The dialog options for reset confirmation.
    /// </summary>
    [Parameter] public DialogOptionsEx ResetConfirmationDialogOptions { get; set; }

    /// <summary>
    /// The action to perform for object metadata configuration.
    /// </summary>
    [Parameter] public Action<ObjectEditMeta<T>> MetaConfiguration { get; set; }

    /// <summary>
    /// The async action to perform for object metadata configuration.
    /// </summary>
    [Parameter] public Task<Action<ObjectEditMeta<T>>> MetaConfigurationAsync { get; set; }

    /// <summary>
    /// The alignment of actions in the component toolbar.
    /// </summary>
    [Parameter] public ActionAlignment ToolBarActionAlignment { get; set; } = ActionAlignment.Right;

    /// <summary>
    /// The content to display in the component toolbar.
    /// </summary>
    [Parameter] public RenderFragment ToolBarContent { get; set; }

    /// <summary>
    /// The behaviour how registered Meta and configured meta should applied
    /// </summary>
    [Parameter] public RegisteredConfigurationBehaviour ConfigureBehaviourForRegisteredConfigurations { get; set; } = RegisteredConfigurationBehaviour.ExecutedBefore;

    /**
     * If this setting is true a manual passed MetaInformation will also re configured
     */
    [Parameter] public bool ConfigureMetaInformationAlways { get; set; }

    /// <summary>
    /// Error message to display
    /// </summary>
    [Parameter] public string ErrorMessage { get; set; }

    /// <summary>
    /// Set this to handle Reset on your own
    /// </summary>
    [Parameter] public Func<Task> CustomResetFunction { get; set; }

    #endregion


    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (!IsOverwritten(nameof(AdornmentIcon)))
            AdornmentIcon = ReadOnly ? Icons.Material.Filled.Search : Icons.Material.Filled.Edit;
        if (!IsOverwritten(nameof(IconSize)))
            IconSize = Size.Medium;
        base.OnInitialized();
    }

    /// <inheritdoc />
    protected override Task OnPickerClosedAsync()
    {
        Text = ToStringFunc(Value);
        return base.OnPickerClosedAsync();
    }

    protected override Task WriteValueAsync(T value)
    {
        Text = ToStringFunc(value);
        return base.WriteValueAsync(value);
    }

    /// <inheritdoc />
    protected override void AfterValueChanged(T from, T to)
    {
        Text = ToStringFunc(to);
        base.AfterValueChanged(from, to);
    }

    private bool? ReadOnlyOverwrite()
    {
        return ReadOnly ? true : null;
    }
}