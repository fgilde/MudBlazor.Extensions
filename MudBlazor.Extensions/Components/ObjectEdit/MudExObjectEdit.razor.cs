using System.ComponentModel;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.CompilerServices;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor.Extensions.Components.ObjectEdit.Options;
using MudBlazor.Extensions.Core;
using MudBlazor.Extensions.Helper;
using MudBlazor.Extensions.Helper.Internal;
using MudBlazor.Extensions.Options;
using Newtonsoft.Json;
using Nextended.Blazor.Models;
using Nextended.Core;
using Nextended.Core.Extensions;
using Nextended.Core.Helper;
using Nextended.Core.Scopes;
using IComponent = Microsoft.AspNetCore.Components.IComponent;

namespace MudBlazor.Extensions.Components.ObjectEdit;

/// <summary>
/// A powerful component to edit an object and its properties.
/// </summary>
/// <typeparam name="T"></typeparam>
//[HasDocumentation("ObjectEdit.md")]
public partial class MudExObjectEdit<T>
{
    private IObjectMetaConfiguration<T> ConfigService => Get<IObjectMetaConfiguration<T>>();
    private Color _importButtonColor;
    private bool _restoreCalled;
    private T _value;
    private List<MudExpansionPanel> _groups = new();

    /// <summary>
    /// Is true if currently is a internal Bulk running. Like reset or clear etc..
    /// </summary>
    protected bool IsInternalLoading;
    
    /// <summary>
    /// ToolBarContent
    /// </summary>
    protected virtual RenderFragment InternalToolBarContent => null;
    
    /// <summary>
    /// Is true if the value is a primitive type
    /// </summary>
    protected bool Primitive => IsPrimitive();


    #region Parameters

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
    /// The object to be edited by the component.
    /// </summary>
    [Parameter]
    public T Value
    {
        get => _value;
        set => SetValue(value);
    }

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
    [Parameter] public bool AddScrollToTop { get; set; } = true;

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
    [Parameter] public string ToolBarClass { get; set; }

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
    /// Called when the edited value is changed.
    /// </summary>
    [Parameter] public EventCallback<T> ValueChanged { get; set; }

    /// <summary>
    /// Called when a property of the edited object is changed.
    /// </summary>
    [Parameter] public EventCallback<ObjectEditPropertyMeta> PropertyChanged { get; set; }

    /// <summary>
    /// The object edit metadata for the component.
    /// </summary>
    [Parameter] public ObjectEditMeta<T> MetaInformation { get; set; }

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
    public bool SearchActive
    {
        get => _searchActive;
        set
        {
            _searchActive = value;
            SearchActiveChanged.InvokeAsync(value);
        }
    }

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

    /// <summary>
    /// Is true if currently is a internal Bulk running. Like reset or clear etc..
    /// </summary>
    public bool BulkActionRunning { get; protected set; }

    /// <summary>
    /// All rendered editors
    /// </summary>
    public List<MudExPropertyEdit> Editors = new();

    private bool _searchActive;

    /// <summary>
    /// ExpansionPanels
    /// </summary>
    public MudExpansionPanel ExpansionPanel { set => _groups.Add(value); }

    /// <summary>
    /// References to editors
    /// </summary>
    public MudExPropertyEdit Ref { set => Editors.Add(value); }

    internal static bool IsPrimitive() => MudExObjectEditHelper.HandleAsPrimitive(typeof(T));
    
    #region Overrides

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        _importButtonColor = ToolbarButtonColor;
        await base.OnParametersSetAsync();
        ResetConfirmationMessageBoxOptions ??= new MessageBoxOptions
        {
            Message = TryLocalize("Reset all properties?"),
            Title = TryLocalize("Reset all"),
            CancelText = TryLocalize("Cancel"),
            YesText = TryLocalize("Reset")
        };
            
    }

    /// <inheritdoc/>
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        bool valueUpdate = parameters.TryGetValue<T>(nameof(Value), out var value) && !Equals(Value, value);
        await base.SetParametersAsync(parameters);
        if (valueUpdate)
        {
            await CreateMetaIfNotExists();
            if (Value is IEditableObject editable)
                editable.BeginEdit();
        }
    }

    /// <summary>
    /// Updates all conditions on meta settings
    /// </summary>
    public virtual void UpdateAllConditions()
    {
        if (BulkActionRunning)
            return;
        MetaInformation?.UpdateAllConditionalSettings(Value);
    }

    /// <summary>
    /// Updates all conditions on meta settings
    /// </summary>
    internal virtual bool UpdateConditions()
    {
        if (AutoUpdateConditions)
        {
            UpdateAllConditions();
            return true;
        }
        return false;
    }

    /// <inheritdoc/>
    protected override async Task OnFinishedRenderAsync()
    {
        await base.OnFinishedRenderAsync();
        if (await RestoreState())
        {
            UpdateConditions();
            CallStateHasChanged();
        }
    }

    #endregion

    /// <summary>
    /// Returns the current value independent of disabled value bindings
    /// </summary>
    /// <returns></returns>
    public T GetUpdatedValue()
    {
        foreach (var meta in MetaInformation.AllProperties.Where(p => p?.RenderData?.DisableValueBinding == true))
        {
            var editor = Editors.FirstOrDefault(edit => edit.PropertyMeta == meta);
            if (editor != null)
                meta.RenderData.SetValue(editor.GetCurrentValue());
        }
        return Value;
    }

    /// <summary>
    /// Resets all values
    /// </summary>
    /// <returns></returns>
    public async Task Reset()
    {
        if (CustomResetFunction == null)
        {
            using (new ActionScope(() => BulkActionRunning = true, () => BulkActionRunning = false))
            {
                await Task.WhenAll(Editors.Select(e => e.ResetAsync()));
                if (Value is IEditableObject editable)
                    editable.CancelEdit();
            }
        }
        else
        {
            await CustomResetFunction();
        }

        UpdateConditions();
    }

    /// <summary>
    /// Clears all input fields
    /// </summary>
    /// <returns></returns>
    public async Task Clear()
    {
        using (new ActionScope(() => BulkActionRunning = true, () => BulkActionRunning = false))
        {
            await Task.WhenAll(Editors.Select(e => e.ClearAsync()));
        }
        UpdateConditions();
    }


    /// <summary>
    /// Restore state if available and returns true if state was restored otherwise false.
    /// </summary>
    public virtual async Task<bool> RestoreState(bool force = false)
    {
        if (force || (AutoSaveRestoreState && !_restoreCalled))
        {
            _restoreCalled = true;
            string json = await JsRuntime.InvokeAsync<string>($"{StateTargetStorage.GetDescription()}.getItem", StateKey);
            if (!string.IsNullOrWhiteSpace(json))
            {
                var toImport = new ImportData<T>
                { Json = json, Value = Value, TriggerdFrom = DataChangeTrigger.StateSaveLoad };
                await BeforeImport.InvokeAsync(toImport);
                if (toImport.Cancel)
                    return false;

                await LoadFromJson(toImport.Json, false);
                await DeleteState();
                await AfterImport.InvokeAsync(new ImportedData<T> { Json = toImport.Json, Value = Value, TriggerdFrom = DataChangeTrigger.StateSaveLoad });
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Deletes the current state
    /// </summary>
    /// <returns></returns>
    public virtual async Task DeleteState()
    {
        await JsRuntime.InvokeVoidAsync($"{StateTargetStorage.GetDescription()}.setItem", StateKey, string.Empty);
    }

    /// <summary>
    /// Saves the current state
    /// </summary>
    /// <returns></returns>
    public virtual async Task SaveState()
    {
        //string json = JsonConvert.SerializeObject(Value);
        string json = await ToJsonAsync();
        var exportData = new ExportData<T> { Json = json, Value = Value, TriggerdFrom = DataChangeTrigger.StateSaveLoad };
        await BeforeExport.InvokeAsync(exportData);
        if (exportData.Cancel)
            return;
        await JsRuntime.InvokeVoidAsync($"{StateTargetStorage.GetDescription()}.setItem", StateKey, exportData.Json);
        await AfterExport.InvokeAsync(exportData);
    }

    /// <summary>
    /// Updates the component state
    /// </summary>
    /// <param name="useRefresh"></param>
    public void Invalidate(bool useRefresh = false)
    {
        if (useRefresh)
            Refresh();
        else
            CallStateHasChanged();
        Editors.Apply(e => e.Invalidate(useRefresh));
    }

    /// <summary>
    /// Raises the ValueChanged event for all editors
    /// </summary>
    public Task RaiseAllEditorsValueChanged() => Task.WhenAll(Editors.Select(edit => edit.RaiseValueChanged()));

    /// <summary>
    /// Raises the ValueChanged event
    /// </summary>
    public Task RaiseValueChanged() => ValueChanged.InvokeAsync(Value);

    /// <summary>
    /// Called when a property value is changed
    /// </summary>
    protected virtual async Task OnPropertyChange(ObjectEditPropertyMeta property)
    {
        if (!IsRendered)
            return;
        
        if (AutoSaveRestoreState)
            _ = Task.Run(SaveState);

        if (Value != null)
            UpdateConditions();
        await PropertyChanged.InvokeAsync(property);
        await ValueChanged.InvokeAsync(Value);
        if (Value is IComponent c)
        {
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object> { { property.PropertyName, property.Value } });
            await c.SetParametersAsync(parameters);
        }
    }

    private async void SetValue(T value)
    {
        _value = value;
        MetaInformation?.SetValue(value);
        await CreateMetaIfNotExists();
        Invalidate();
    }

    private bool IsInFilter(ObjectEditPropertyMeta propertyMeta)
    {
        var allFilters = (!string.IsNullOrEmpty(Filter) ? new[] { Filter } : Enumerable.Empty<string>()).Concat(Filters ?? Enumerable.Empty<string>()).Distinct().ToList();

        // No filters, nothing to filter against, so return true
        return !allFilters.Any() ||
               // Loop through each filter in allFilters
               (from filter in allFilters where !string.IsNullOrWhiteSpace(filter) 
                select propertyMeta.Settings.LabelFor(LocalizerToUse).Contains(filter, StringComparison.InvariantCultureIgnoreCase) 
                || propertyMeta.Settings.DescriptionFor(LocalizerToUse).Contains(filter, StringComparison.InvariantCultureIgnoreCase) 
                || propertyMeta.PropertyInfo.Name.Contains(filter, StringComparison.InvariantCultureIgnoreCase) 
                || (propertyMeta.Value?.ToString()?.Contains(filter, StringComparison.InvariantCultureIgnoreCase) == true) 
                || (propertyMeta.GroupInfo?.Name?.Contains(filter, StringComparison.InvariantCultureIgnoreCase) == true) 
                || (propertyMeta.RenderData?.Attributes.Values.OfType<string>().Any(x => x.Contains(filter, StringComparison.InvariantCultureIgnoreCase)) == true))
                .Any(matchesCurrentFilter => matchesCurrentFilter);
    }



    private bool ShouldAddGrid(IEnumerable<ObjectEditPropertyMeta> meta) => WrapInMudGrid ?? ContainsMudItemInWrapper(meta);
    private string CssClassName => GroupingStyle == GroupingStyle.Flat ? $"mud-ex-object-edit-group-flat {(!GroupsCollapsible ? "mud-ex-hide-expand-btn" : "")}" : string.Empty;

    
    private List<IGrouping<string, ObjectEditPropertyMeta>> DefaultGroupedMetaPropertyInfos() // Here we filter ignore directly
        => MetaInformation?.AllProperties?.EmptyIfNull()
            .Where(m => m.ShouldRender() && IsInFilter(m) && (!AutoHideDisabledFields || m.Settings.IsEditable))
            .GroupBy(m => !DisableGrouping ? m.GroupInfo?.Name : string.Empty)
            .ToList();

    private List<IGrouping<string, ObjectEditPropertyMeta>> AllGroupedMetaPropertyInfos() // Here we don't filter ignores and give them to MudExPropertyEdit to keep reference
        => MetaInformation?.AllProperties?.EmptyIfNull()
            .Where(m => IsInFilter(m) && (!AutoHideDisabledFields || m.Settings.IsEditable))
            .GroupBy(m => !DisableGrouping ? m.GroupInfo?.Name : string.Empty)
            .ToList();

    
    private List<IGrouping<string, ObjectEditPropertyMeta>> GroupedMetaPropertyInfos() 
        => !RenderIgnoredReferences ? DefaultGroupedMetaPropertyInfos() : AllGroupedMetaPropertyInfos();


    private bool ContainsMudItemInWrapper(IEnumerable<ObjectEditPropertyMeta> meta)
        => meta.Where(p => p.RenderData?.Wrapper != null)
            .Select(p => p.RenderData.Wrapper)
            .Recursive(w => w.Wrapper == null ? Enumerable.Empty<IRenderData>() : new[] { w.Wrapper })
            .Any(d => d.ComponentType == typeof(MudItem));

    /// <summary>
    /// Base meta configuration can be overridden to have meta configuration for all ObjectEditPropertyMeta
    /// </summary>
    protected virtual ObjectEditMeta<T> ConfigureMetaBase(ObjectEditMeta<T> meta)
    {
        return meta;
    }

    /// <summary>
    /// Creates and updates meta configs
    /// </summary>
    protected virtual async Task CreateMetaIfNotExists(bool? reconfigure = null)
    {
        RenderDataDefaults.AddRenderDataProvider(ServiceProvider);
        Action<ObjectEditMeta<T>> c = MetaConfigurationAsync != null ? await MetaConfigurationAsync : MetaConfiguration;
        bool metaNeedsConfig = MetaInformation == null || (reconfigure ?? false); // If not configured or not manually bypassed wer configure the Meta
        MetaInformation ??= (Value ??= typeof(T).CreateInstance<T>()).ObjectEditMeta();
        if (metaNeedsConfig || ConfigureMetaInformationAlways)
        {
            if (ConfigService != null && ConfigureBehaviourForRegisteredConfigurations == RegisteredConfigurationBehaviour.ExecutedBefore)
                await ConfigService.ConfigureAsync(MetaInformation);

            // TODO: Currently RenderWithAttribute isn't finished 
            //foreach (var meta in MetaInformation.AllProperties.Where(m =>
            //             m.PropertyInfo.GetCustomAttribute<RenderWithBaseAttribute>(true) != null))
            //{
            //    meta.PropertyInfo.GetCustomAttribute<RenderWithBaseAttribute>(true)?.Apply(meta);
            //}


            if (c != null)
                await Task.Run(() => c.Invoke(ConfigureMetaBase(MetaInformation)));
            else
                await Task.Run(() => ConfigureMetaBase(MetaInformation));     
            
            if (ConfigService != null && ConfigureBehaviourForRegisteredConfigurations == RegisteredConfigurationBehaviour.ExecutedAfter)
                await ConfigService.ConfigureAsync(MetaInformation);
            
            UpdateConditions(); 
        }
    }
    
    private async Task OnResetClick(MouseEventArgs arg)
    {
        if (GlobalResetSettings.RequiresConfirmation && DialogService != null && !(await ShowConfirmationBox()))
            return;
        await Reset();
        CallStateHasChanged();
    }
    
    private async Task<bool> ShowConfirmationBox()
    {
        ResetConfirmationDialogOptions ??= new DialogOptionsEx
        {
            ShowAtCursor = true,
            CursorPositionOrigin = ToolBarActionAlignment == ActionAlignment.Right ? Origin.CenterRight : Origin.CenterLeft,
            Animations = new[] { AnimationType.LightSpeed },
            Position = ToolBarActionAlignment == ActionAlignment.Right ? DialogPosition.TopRight : DialogPosition.TopLeft,
            DragMode = MudDialogDragMode.Simple,
            CloseButton = false,
            DialogAppearance = MudExAppearance.FromCss(MudExCss.Classes.Dialog._Initial)
        };
        return JsRuntime != null
            ? await DialogService.ShowConfirmationDialogAsync(ResetConfirmationMessageBoxOptions, ResetConfirmationDialogOptions)
            : (await DialogService.ShowMessageBox(ResetConfirmationMessageBoxOptions, ResetConfirmationDialogOptions) ?? false);
    }

    private string GetStyle()
    {
        return MudExStyleBuilder.GenerateStyleString(new
        {
            Height,
            MaxHeight,
        }, SizeUnit, Style);
    }

    private bool HasFixedHeight() => Height != null || MaxHeight != null || Style?.ToLower().Contains("height") == true;

    private string GetToolbarStickyTop()
    {
        if (!string.IsNullOrEmpty(StickyToolbarTop))
            return StickyToolbarTop;
        return HasFixedHeight() ? "0" : "var(--mud-appbar-height)";
    }

    private string GetOverflowClass() => HasFixedHeight() ? "mud-ex-overflow" : "";

    private string ToolbarStyle()
    {
        var res = string.Empty;
        if (StickyToolbar && StickyToolbarTop != null)
            res += $"top: {GetToolbarStickyTop()};";
        if (!ToolbarColor.Is(Color.Inherit))
            res += $"background-color: {ToolbarColor.ToCssStringValue()};";
        return res;
    }

    private Task ExpandCollapse()
    {
        var collapse = _groups[0].Expanded;
        return Task.WhenAll(_groups.Select(g => collapse ? g.CollapseAsync() : g.ExpandAsync()));
    }

    private IDictionary<string, object> GetAttributesForPrimitive()
    {
        string[] ignore = { nameof(Value), nameof(Localizer), nameof(ValueChanged), nameof(MetaConfiguration), nameof(MetaConfigurationAsync), nameof(MetaInformation) };
        var props = typeof(MudExObjectEdit<ModelForPrimitive<T>>).GetProperties().Select(p => p.Name); // Only allow props on this type and not on derived types
        var res = (from prop in GetType().GetProperties().Where(p => props.Contains(p.Name))
                   let attr = prop.GetCustomAttribute<ParameterAttribute>()
                   where attr != null && !ignore.Contains(prop.Name) && prop.CanWrite
                   select prop).ToDictionary(prop => prop.Name, prop => prop.GetValue(this));
        var primitiveWrapper = new ModelForPrimitive<T>(Value);
        res.AddOrUpdate(nameof(Primitive), false);
        res.AddOrUpdate(nameof(Value), primitiveWrapper);
        res.AddOrUpdate(nameof(ValueChanged), RuntimeHelpers.TypeCheck(
            EventCallback.Factory.Create(
                this,
                EventCallback.Factory.CreateInferred(
                    this, x =>
                    {
                        Value = x.Value;
                        ValueChanged.InvokeAsync(Value);
                    },
                    primitiveWrapper
                )
            )
        ));

        return res;
    }

    /// <summary>
    /// Exports the current value as json
    /// </summary>
    protected async Task Export()
    {
        var exported = new ExportData<T> { Value = Value, Json = await ToJsonAsync() };
        await BeforeExport.InvokeAsync(exported);
        
        if(exported.Cancel)
            return;

        IsInternalLoading = true;
        try
        {
            var json = exported.Json;
            var url = await DataUrl.GetDataUrlAsync(await Task.Run(() => Encoding.UTF8.GetBytes(json)), "application/json");
            await JsRuntime.InvokeVoidAsync("MudBlazorExtensions.downloadFile", new
            {
                Url = url,
                FileName = !string.IsNullOrWhiteSpace(ExportFileName) ? ExportFileName : $"{Value.GetType().Name}_{DateTime.Now}.json",
                MimeType = "application/json"
            });
            await AfterExport.InvokeAsync(exported);
        }
        finally
        {
            IsInternalLoading = false;
        }
    }

    /// <summary>
    /// Returns the current value as json
    /// </summary>
    public Task<string> ToJsonAsync()
    {
        return Task.Run(ToJson);
    }

    /// <summary>
    /// Returns the json
    /// </summary>
    /// <returns></returns>
    public string ToJson()
    {
        var ignored = MetaInformation.Properties().Where(p => p.Settings.IgnoreOnExport).Select(m => m.PropertyName).ToArray();
        
        var json = JsonConvert.SerializeObject(Value, new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented
        });
        return ignored.Any() ? MudExJsonHelper.RemovePropertiesFromJson(json, ignored) : json;
    }

    private async Task<bool> ShouldCancelImportAsync(string json, string fileName)
    {
        var mimeType = "application/json";
        var url = await DataUrl.GetDataUrlAsync(Encoding.UTF8.GetBytes(json), mimeType);
        var cancelled = ImportNeedsConfirmation && (await (await DialogService.ShowFileDisplayDialog(url, fileName, mimeType, op =>
        {
            op.MaxWidth = MaxWidth.Large;
            op.FullWidth = false;
            op.FullHeight = false;
            op.Position = DialogPosition.TopCenter;
        }, new DialogParameters
        {
            { nameof(MudExFileDisplayDialog.AllowDownload), false },
            { nameof(MudExFileDisplayDialog.Style), "height:700px; width: 900px;" },
            { nameof(MudExFileDisplayDialog.ClassContent), "full-height-90" },
            { nameof(MudExFileDisplayDialog.Buttons), new[]
            {
                new MudExDialogResultAction
                {
                    Label = TryLocalize(ImportCancelText),
                    Variant = Variant.Text,
                    Result = DialogResult.Cancel()
                },
                new MudExDialogResultAction
                {
                    Label = TryLocalize(ImportConfirmText),
                    Color = Color.Error,
                    Variant = Variant.Filled,
                    Result = DialogResult.Ok(true)
                },
            } }
        })).Result).Canceled;
        return cancelled;
    }

    private async Task Import(InputFileChangeEventArgs e)
    {
        if (e.File.ContentType != "application/json")
        {
            ErrorMessage = $"Invalid file type: {e.File.ContentType} only JSON files are supported";
            return;
        }
        try
        {
            var buffer = new byte[e.File.Size];
            await e.File.OpenReadStream(e.File.Size).ReadAsync(buffer);
            
            var toImport = new ImportData<T> { Json = Encoding.UTF8.GetString(buffer), Value = Value };
            await BeforeImport.InvokeAsync(toImport);
            if (toImport.Cancel || await ShouldCancelImportAsync(toImport.Json, e.File.Name))
                return;

            IsInternalLoading = true;


            await LoadFromJson(toImport.Json, RemoveIgnoredFromImport);
            await ImportSuccessUi();
            await AfterImport.InvokeAsync(new ImportedData<T> {Json = toImport.Json, Value = Value});
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsInternalLoading = false;
        }
    }

    private async Task ImportSuccessUi()
    {
        var icon = ImportIcon;
        _importButtonColor = Color.Success;
        ImportIcon = Icons.Material.Filled.Check;
        CallStateHasChanged();
        await Task.Delay(2500);
        _importButtonColor = ToolbarButtonColor;
        ImportIcon = icon;
    }

    /// <summary>
    /// Imports a Json value
    /// </summary>
    /// <param name="json"></param>
    /// <param name="removeIgnoredImports"></param>
    /// <returns></returns>
    public Task LoadFromJson(string json, bool removeIgnoredImports)
    {
        return Task.Run(() =>
        {
            var ignored = removeIgnoredImports ? MetaInformation.Properties().Where(p => p.Settings.IgnoreOnImport).Select(m => m.PropertyName).ToHashSet() : new HashSet<string>();
            if (ignored.Count <= 0)
            {
                SetValueAfterImport(JsonConvert.DeserializeObject<T>(json));
                return;
            }
            
            var obj = JsonConvert.DeserializeObject(json, Value.GetType());
            var notIgnored = obj.ToFlatDictionary().Where(kvp => !ignored.Contains(kvp.Key) && !ignored.Any(path => PropertyHelper.IsPropertyPathSubPropertyOf(kvp.Key, path))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // Keep current values for ignored import properties
            var full = Value.ToFlatDictionary();
            var missing = full.Where(kvp => !notIgnored.ContainsKey(kvp.Key)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            notIgnored.AddRange(missing);
            var cleaned = JsonDictionaryConverter.Unflatten(notIgnored);
            SetValueAfterImport(cleaned.ToObject<T>());

            foreach (var prop in MetaInformation.AllProperties.Where(p => p.PropertyInfo.CanWrite)) // TODO: Get rid of this hack, but otherwise currently not all UI values are updated
                Check.TryCatch<Exception>(() => { prop.Value = notIgnored.TryGetValue(prop.PropertyName, out var val) ? val.MapTo(prop.PropertyInfo.PropertyType) : prop.Value; });
        });
    }
    private void SetValueAfterImport(T value)
    {
        if(SetPropertiesAfterImport)
            SetEditorValueProperties(value);
        else
            Value = value;
    }

    private void SetEditorValueProperties(T value)
    {
        if (value == null) return;

        foreach (var prop in GetPropertiesWithPaths(value))
        {
            var meta = MetaInformation.AllProperties.FirstOrDefault(m => m.PropertyName == prop.Key);
            if (meta != null)
            {
                meta.Value = prop.Value;
                //meta.RenderData?.SetValue(prop.Value);
            }
        }
    }

    private IDictionary<string, object> GetPropertiesWithPaths(object obj, string currentPath = "")
    {
        if (obj == null) return new Dictionary<string, object>();

        var result = new Dictionary<string, object>();
        foreach (var prop in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var propValue = prop.GetValue(obj);
            var newPath = string.IsNullOrEmpty(currentPath) ? prop.Name : $"{currentPath}.{prop.Name}";

            if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string))
            {
                var subProperties = GetPropertiesWithPaths(propValue, newPath);
                foreach (var subProp in subProperties)
                    result.Add(subProp.Key, subProp.Value);
            }
            else
                result.Add(newPath, propValue);
        }

        return result;
    }
}