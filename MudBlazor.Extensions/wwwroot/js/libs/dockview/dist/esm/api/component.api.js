export class SplitviewApi {
    /**
     * The minimum size  the component can reach where size is measured in the direction of orientation provided.
     */
    get minimumSize() {
        return this.component.minimumSize;
    }
    /**
     * The maximum size the component can reach where size is measured in the direction of orientation provided.
     */
    get maximumSize() {
        return this.component.maximumSize;
    }
    /**
     * Width of the component.
     */
    get width() {
        return this.component.width;
    }
    /**
     * Height of the component.
     */
    get height() {
        return this.component.height;
    }
    /**
     * The current number of panels.
     */
    get length() {
        return this.component.length;
    }
    /**
     * The current orientation of the component.
     */
    get orientation() {
        return this.component.orientation;
    }
    /**
     * The list of current panels.
     */
    get panels() {
        return this.component.panels;
    }
    /**
     * Invoked after a layout is loaded through the `fromJSON` method.
     */
    get onDidLayoutFromJSON() {
        return this.component.onDidLayoutFromJSON;
    }
    /**
     * Invoked whenever any aspect of the layout changes.
     * If listening to this event it may be worth debouncing ouputs.
     */
    get onDidLayoutChange() {
        return this.component.onDidLayoutChange;
    }
    /**
     * Invoked when a view is added.
     */
    get onDidAddView() {
        return this.component.onDidAddView;
    }
    /**
     * Invoked when a view is removed.
     */
    get onDidRemoveView() {
        return this.component.onDidRemoveView;
    }
    constructor(component) {
        this.component = component;
    }
    /**
     * Removes an existing panel and optionally provide a `Sizing` method
     * for the subsequent resize.
     */
    removePanel(panel, sizing) {
        this.component.removePanel(panel, sizing);
    }
    /**
     * Focus the component.
     */
    focus() {
        this.component.focus();
    }
    /**
     * Get the reference to a panel given it's `string` id.
     */
    getPanel(id) {
        return this.component.getPanel(id);
    }
    /**
     * Layout the panel with a width and height.
     */
    layout(width, height) {
        return this.component.layout(width, height);
    }
    /**
     * Add a new panel and return the created instance.
     */
    addPanel(options) {
        return this.component.addPanel(options);
    }
    /**
     * Move a panel given it's current and desired index.
     */
    movePanel(from, to) {
        this.component.movePanel(from, to);
    }
    /**
     * Deserialize a layout to built a splitivew.
     */
    fromJSON(data) {
        this.component.fromJSON(data);
    }
    /** Serialize a layout */
    toJSON() {
        return this.component.toJSON();
    }
    /**
     * Remove all panels and clear the component.
     */
    clear() {
        this.component.clear();
    }
    /**
     * Update configuratable options.
     */
    updateOptions(options) {
        this.component.updateOptions(options);
    }
    /**
     * Release resources and teardown component. Do not call when using framework versions of dockview.
     */
    dispose() {
        this.component.dispose();
    }
}
export class PaneviewApi {
    /**
     * The minimum size  the component can reach where size is measured in the direction of orientation provided.
     */
    get minimumSize() {
        return this.component.minimumSize;
    }
    /**
     * The maximum size the component can reach where size is measured in the direction of orientation provided.
     */
    get maximumSize() {
        return this.component.maximumSize;
    }
    /**
     * Width of the component.
     */
    get width() {
        return this.component.width;
    }
    /**
     * Height of the component.
     */
    get height() {
        return this.component.height;
    }
    /**
     * All panel objects.
     */
    get panels() {
        return this.component.panels;
    }
    /**
     * Invoked when any layout change occures, an aggregation of many events.
     */
    get onDidLayoutChange() {
        return this.component.onDidLayoutChange;
    }
    /**
     * Invoked after a layout is deserialzied using the `fromJSON` method.
     */
    get onDidLayoutFromJSON() {
        return this.component.onDidLayoutFromJSON;
    }
    /**
     * Invoked when a panel is added. May be called multiple times when moving panels.
     */
    get onDidAddView() {
        return this.component.onDidAddView;
    }
    /**
     * Invoked when a panel is removed. May be called multiple times when moving panels.
     */
    get onDidRemoveView() {
        return this.component.onDidRemoveView;
    }
    /**
     * Invoked when a Drag'n'Drop event occurs that the component was unable to handle. Exposed for custom Drag'n'Drop functionality.
     */
    get onDidDrop() {
        return this.component.onDidDrop;
    }
    get onUnhandledDragOverEvent() {
        return this.component.onUnhandledDragOverEvent;
    }
    constructor(component) {
        this.component = component;
    }
    /**
     * Remove a panel given the panel object.
     */
    removePanel(panel) {
        this.component.removePanel(panel);
    }
    /**
     * Get a panel object given a `string` id. May return `undefined`.
     */
    getPanel(id) {
        return this.component.getPanel(id);
    }
    /**
     * Move a panel given it's current and desired index.
     */
    movePanel(from, to) {
        this.component.movePanel(from, to);
    }
    /**
     *  Focus the component. Will try to focus an active panel if one exists.
     */
    focus() {
        this.component.focus();
    }
    /**
     * Force resize the component to an exact width and height. Read about auto-resizing before using.
     */
    layout(width, height) {
        this.component.layout(width, height);
    }
    /**
     * Add a panel and return the created object.
     */
    addPanel(options) {
        return this.component.addPanel(options);
    }
    /**
     * Create a component from a serialized object.
     */
    fromJSON(data) {
        this.component.fromJSON(data);
    }
    /**
     * Create a serialized object of the current component.
     */
    toJSON() {
        return this.component.toJSON();
    }
    /**
     * Reset the component back to an empty and default state.
     */
    clear() {
        this.component.clear();
    }
    /**
     * Update configuratable options.
     */
    updateOptions(options) {
        this.component.updateOptions(options);
    }
    /**
     * Release resources and teardown component. Do not call when using framework versions of dockview.
     */
    dispose() {
        this.component.dispose();
    }
}
export class GridviewApi {
    /**
     * Width of the component.
     */
    get width() {
        return this.component.width;
    }
    /**
     * Height of the component.
     */
    get height() {
        return this.component.height;
    }
    /**
     * Minimum height of the component.
     */
    get minimumHeight() {
        return this.component.minimumHeight;
    }
    /**
     * Maximum height of the component.
     */
    get maximumHeight() {
        return this.component.maximumHeight;
    }
    /**
     * Minimum width of the component.
     */
    get minimumWidth() {
        return this.component.minimumWidth;
    }
    /**
     * Maximum width of the component.
     */
    get maximumWidth() {
        return this.component.maximumWidth;
    }
    /**
     * Invoked when any layout change occures, an aggregation of many events.
     */
    get onDidLayoutChange() {
        return this.component.onDidLayoutChange;
    }
    /**
     * Invoked when a panel is added. May be called multiple times when moving panels.
     */
    get onDidAddPanel() {
        return this.component.onDidAddGroup;
    }
    /**
     * Invoked when a panel is removed. May be called multiple times when moving panels.
     */
    get onDidRemovePanel() {
        return this.component.onDidRemoveGroup;
    }
    /**
     * Invoked when the active panel changes. May be undefined if no panel is active.
     */
    get onDidActivePanelChange() {
        return this.component.onDidActiveGroupChange;
    }
    /**
     * Invoked after a layout is deserialzied using the `fromJSON` method.
     */
    get onDidLayoutFromJSON() {
        return this.component.onDidLayoutFromJSON;
    }
    /**
     * All panel objects.
     */
    get panels() {
        return this.component.groups;
    }
    /**
     * Current orientation. Can be changed after initialization.
     */
    get orientation() {
        return this.component.orientation;
    }
    set orientation(value) {
        this.component.updateOptions({ orientation: value });
    }
    constructor(component) {
        this.component = component;
    }
    /**
     *  Focus the component. Will try to focus an active panel if one exists.
     */
    focus() {
        this.component.focus();
    }
    /**
     * Force resize the component to an exact width and height. Read about auto-resizing before using.
     */
    layout(width, height, force = false) {
        this.component.layout(width, height, force);
    }
    /**
     * Add a panel and return the created object.
     */
    addPanel(options) {
        return this.component.addPanel(options);
    }
    /**
     * Remove a panel given the panel object.
     */
    removePanel(panel, sizing) {
        this.component.removePanel(panel, sizing);
    }
    /**
     * Move a panel in a particular direction relative to another panel.
     */
    movePanel(panel, options) {
        this.component.movePanel(panel, options);
    }
    /**
     * Get a panel object given a `string` id. May return `undefined`.
     */
    getPanel(id) {
        return this.component.getPanel(id);
    }
    /**
     * Create a component from a serialized object.
     */
    fromJSON(data) {
        return this.component.fromJSON(data);
    }
    /**
     * Create a serialized object of the current component.
     */
    toJSON() {
        return this.component.toJSON();
    }
    /**
     * Reset the component back to an empty and default state.
     */
    clear() {
        this.component.clear();
    }
    updateOptions(options) {
        this.component.updateOptions(options);
    }
    /**
     * Release resources and teardown component. Do not call when using framework versions of dockview.
     */
    dispose() {
        this.component.dispose();
    }
}
export class DockviewApi {
    /**
     * The unique identifier for this instance. Used to manage scope of Drag'n'Drop events.
     */
    get id() {
        return this.component.id;
    }
    /**
     * Width of the component.
     */
    get width() {
        return this.component.width;
    }
    /**
     * Height of the component.
     */
    get height() {
        return this.component.height;
    }
    /**
     * Minimum height of the component.
     */
    get minimumHeight() {
        return this.component.minimumHeight;
    }
    /**
     * Maximum height of the component.
     */
    get maximumHeight() {
        return this.component.maximumHeight;
    }
    /**
     * Minimum width of the component.
     */
    get minimumWidth() {
        return this.component.minimumWidth;
    }
    /**
     * Maximum width of the component.
     */
    get maximumWidth() {
        return this.component.maximumWidth;
    }
    /**
     * Total number of groups.
     */
    get size() {
        return this.component.size;
    }
    /**
     * Total number of panels.
     */
    get totalPanels() {
        return this.component.totalPanels;
    }
    /**
     * Invoked when the active group changes. May be undefined if no group is active.
     */
    get onDidActiveGroupChange() {
        return this.component.onDidActiveGroupChange;
    }
    /**
     * Invoked when a group is added. May be called multiple times when moving groups.
     */
    get onDidAddGroup() {
        return this.component.onDidAddGroup;
    }
    /**
     * Invoked when a group is removed. May be called multiple times when moving groups.
     */
    get onDidRemoveGroup() {
        return this.component.onDidRemoveGroup;
    }
    /**
     * Invoked when the active panel changes. May be undefined if no panel is active.
     */
    get onDidActivePanelChange() {
        return this.component.onDidActivePanelChange;
    }
    /**
     * Invoked when a panel is added. May be called multiple times when moving panels.
     */
    get onDidAddPanel() {
        return this.component.onDidAddPanel;
    }
    /**
     * Invoked when a panel is removed. May be called multiple times when moving panels.
     */
    get onDidRemovePanel() {
        return this.component.onDidRemovePanel;
    }
    get onDidMovePanel() {
        return this.component.onDidMovePanel;
    }
    /**
     * Invoked after a layout is deserialzied using the `fromJSON` method.
     */
    get onDidLayoutFromJSON() {
        return this.component.onDidLayoutFromJSON;
    }
    /**
     * Invoked when any layout change occures, an aggregation of many events.
     */
    get onDidLayoutChange() {
        return this.component.onDidLayoutChange;
    }
    /**
     * Invoked when a Drag'n'Drop event occurs that the component was unable to handle. Exposed for custom Drag'n'Drop functionality.
     */
    get onDidDrop() {
        return this.component.onDidDrop;
    }
    /**
     * Invoked when a Drag'n'Drop event occurs but before dockview handles it giving the user an opportunity to intecept and
     * prevent the event from occuring using the standard `preventDefault()` syntax.
     *
     * Preventing certain events may causes unexpected behaviours, use carefully.
     */
    get onWillDrop() {
        return this.component.onWillDrop;
    }
    /**
     * Invoked before an overlay is shown indicating a drop target.
     *
     * Calling `event.preventDefault()` will prevent the overlay being shown and prevent
     * the any subsequent drop event.
     */
    get onWillShowOverlay() {
        return this.component.onWillShowOverlay;
    }
    /**
     * Invoked before a group is dragged.
     *
     * Calling `event.nativeEvent.preventDefault()` will prevent the group drag starting.
     *
     */
    get onWillDragGroup() {
        return this.component.onWillDragGroup;
    }
    /**
     * Invoked before a panel is dragged.
     *
     * Calling `event.nativeEvent.preventDefault()` will prevent the panel drag starting.
     */
    get onWillDragPanel() {
        return this.component.onWillDragPanel;
    }
    get onUnhandledDragOverEvent() {
        return this.component.onUnhandledDragOverEvent;
    }
    get onDidPopoutGroupSizeChange() {
        return this.component.onDidPopoutGroupSizeChange;
    }
    get onDidPopoutGroupPositionChange() {
        return this.component.onDidPopoutGroupPositionChange;
    }
    get onDidOpenPopoutWindowFail() {
        return this.component.onDidOpenPopoutWindowFail;
    }
    /**
     * All panel objects.
     */
    get panels() {
        return this.component.panels;
    }
    /**
     * All group objects.
     */
    get groups() {
        return this.component.groups;
    }
    /**
     *  Active panel object.
     */
    get activePanel() {
        return this.component.activePanel;
    }
    /**
     * Active group object.
     */
    get activeGroup() {
        return this.component.activeGroup;
    }
    constructor(component) {
        this.component = component;
    }
    /**
     *  Focus the component. Will try to focus an active panel if one exists.
     */
    focus() {
        this.component.focus();
    }
    /**
     * Get a panel object given a `string` id. May return `undefined`.
     */
    getPanel(id) {
        return this.component.getGroupPanel(id);
    }
    /**
     * Force resize the component to an exact width and height. Read about auto-resizing before using.
     */
    layout(width, height, force = false) {
        this.component.layout(width, height, force);
    }
    /**
     * Add a panel and return the created object.
     */
    addPanel(options) {
        return this.component.addPanel(options);
    }
    /**
     * Remove a panel given the panel object.
     */
    removePanel(panel) {
        this.component.removePanel(panel);
    }
    /**
     * Add a group and return the created object.
     */
    addGroup(options) {
        return this.component.addGroup(options);
    }
    /**
     * Close all groups and panels.
     */
    closeAllGroups() {
        return this.component.closeAllGroups();
    }
    /**
     * Remove a group and any panels within the group.
     */
    removeGroup(group) {
        this.component.removeGroup(group);
    }
    /**
     * Get a group object given a `string` id. May return undefined.
     */
    getGroup(id) {
        return this.component.getPanel(id);
    }
    /**
     * Add a floating group
     */
    addFloatingGroup(item, options) {
        return this.component.addFloatingGroup(item, options);
    }
    /**
     * Create a component from a serialized object.
     */
    fromJSON(data) {
        this.component.fromJSON(data);
    }
    /**
     * Create a serialized object of the current component.
     */
    toJSON() {
        return this.component.toJSON();
    }
    /**
     * Reset the component back to an empty and default state.
     */
    clear() {
        this.component.clear();
    }
    /**
     * Move the focus progmatically to the next panel or group.
     */
    moveToNext(options) {
        this.component.moveToNext(options);
    }
    /**
     * Move the focus progmatically to the previous panel or group.
     */
    moveToPrevious(options) {
        this.component.moveToPrevious(options);
    }
    maximizeGroup(panel) {
        this.component.maximizeGroup(panel.group);
    }
    hasMaximizedGroup() {
        return this.component.hasMaximizedGroup();
    }
    exitMaximizedGroup() {
        this.component.exitMaximizedGroup();
    }
    get onDidMaximizedGroupChange() {
        return this.component.onDidMaximizedGroupChange;
    }
    /**
     * Add a popout group in a new Window
     */
    addPopoutGroup(item, options) {
        return this.component.addPopoutGroup(item, options);
    }
    updateOptions(options) {
        this.component.updateOptions(options);
    }
    /**
     * Release resources and teardown component. Do not call when using framework versions of dockview.
     */
    dispose() {
        this.component.dispose();
    }
}
