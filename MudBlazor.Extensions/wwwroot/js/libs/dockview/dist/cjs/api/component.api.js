"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.DockviewApi = exports.GridviewApi = exports.PaneviewApi = exports.SplitviewApi = void 0;
var SplitviewApi = /** @class */ (function () {
    function SplitviewApi(component) {
        this.component = component;
    }
    Object.defineProperty(SplitviewApi.prototype, "minimumSize", {
        /**
         * The minimum size  the component can reach where size is measured in the direction of orientation provided.
         */
        get: function () {
            return this.component.minimumSize;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(SplitviewApi.prototype, "maximumSize", {
        /**
         * The maximum size the component can reach where size is measured in the direction of orientation provided.
         */
        get: function () {
            return this.component.maximumSize;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(SplitviewApi.prototype, "width", {
        /**
         * Width of the component.
         */
        get: function () {
            return this.component.width;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(SplitviewApi.prototype, "height", {
        /**
         * Height of the component.
         */
        get: function () {
            return this.component.height;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(SplitviewApi.prototype, "length", {
        /**
         * The current number of panels.
         */
        get: function () {
            return this.component.length;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(SplitviewApi.prototype, "orientation", {
        /**
         * The current orientation of the component.
         */
        get: function () {
            return this.component.orientation;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(SplitviewApi.prototype, "panels", {
        /**
         * The list of current panels.
         */
        get: function () {
            return this.component.panels;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(SplitviewApi.prototype, "onDidLayoutFromJSON", {
        /**
         * Invoked after a layout is loaded through the `fromJSON` method.
         */
        get: function () {
            return this.component.onDidLayoutFromJSON;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(SplitviewApi.prototype, "onDidLayoutChange", {
        /**
         * Invoked whenever any aspect of the layout changes.
         * If listening to this event it may be worth debouncing ouputs.
         */
        get: function () {
            return this.component.onDidLayoutChange;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(SplitviewApi.prototype, "onDidAddView", {
        /**
         * Invoked when a view is added.
         */
        get: function () {
            return this.component.onDidAddView;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(SplitviewApi.prototype, "onDidRemoveView", {
        /**
         * Invoked when a view is removed.
         */
        get: function () {
            return this.component.onDidRemoveView;
        },
        enumerable: false,
        configurable: true
    });
    /**
     * Removes an existing panel and optionally provide a `Sizing` method
     * for the subsequent resize.
     */
    SplitviewApi.prototype.removePanel = function (panel, sizing) {
        this.component.removePanel(panel, sizing);
    };
    /**
     * Focus the component.
     */
    SplitviewApi.prototype.focus = function () {
        this.component.focus();
    };
    /**
     * Get the reference to a panel given it's `string` id.
     */
    SplitviewApi.prototype.getPanel = function (id) {
        return this.component.getPanel(id);
    };
    /**
     * Layout the panel with a width and height.
     */
    SplitviewApi.prototype.layout = function (width, height) {
        return this.component.layout(width, height);
    };
    /**
     * Add a new panel and return the created instance.
     */
    SplitviewApi.prototype.addPanel = function (options) {
        return this.component.addPanel(options);
    };
    /**
     * Move a panel given it's current and desired index.
     */
    SplitviewApi.prototype.movePanel = function (from, to) {
        this.component.movePanel(from, to);
    };
    /**
     * Deserialize a layout to built a splitivew.
     */
    SplitviewApi.prototype.fromJSON = function (data) {
        this.component.fromJSON(data);
    };
    /** Serialize a layout */
    SplitviewApi.prototype.toJSON = function () {
        return this.component.toJSON();
    };
    /**
     * Remove all panels and clear the component.
     */
    SplitviewApi.prototype.clear = function () {
        this.component.clear();
    };
    /**
     * Update configuratable options.
     */
    SplitviewApi.prototype.updateOptions = function (options) {
        this.component.updateOptions(options);
    };
    /**
     * Release resources and teardown component. Do not call when using framework versions of dockview.
     */
    SplitviewApi.prototype.dispose = function () {
        this.component.dispose();
    };
    return SplitviewApi;
}());
exports.SplitviewApi = SplitviewApi;
var PaneviewApi = /** @class */ (function () {
    function PaneviewApi(component) {
        this.component = component;
    }
    Object.defineProperty(PaneviewApi.prototype, "minimumSize", {
        /**
         * The minimum size  the component can reach where size is measured in the direction of orientation provided.
         */
        get: function () {
            return this.component.minimumSize;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(PaneviewApi.prototype, "maximumSize", {
        /**
         * The maximum size the component can reach where size is measured in the direction of orientation provided.
         */
        get: function () {
            return this.component.maximumSize;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(PaneviewApi.prototype, "width", {
        /**
         * Width of the component.
         */
        get: function () {
            return this.component.width;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(PaneviewApi.prototype, "height", {
        /**
         * Height of the component.
         */
        get: function () {
            return this.component.height;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(PaneviewApi.prototype, "panels", {
        /**
         * All panel objects.
         */
        get: function () {
            return this.component.panels;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(PaneviewApi.prototype, "onDidLayoutChange", {
        /**
         * Invoked when any layout change occures, an aggregation of many events.
         */
        get: function () {
            return this.component.onDidLayoutChange;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(PaneviewApi.prototype, "onDidLayoutFromJSON", {
        /**
         * Invoked after a layout is deserialzied using the `fromJSON` method.
         */
        get: function () {
            return this.component.onDidLayoutFromJSON;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(PaneviewApi.prototype, "onDidAddView", {
        /**
         * Invoked when a panel is added. May be called multiple times when moving panels.
         */
        get: function () {
            return this.component.onDidAddView;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(PaneviewApi.prototype, "onDidRemoveView", {
        /**
         * Invoked when a panel is removed. May be called multiple times when moving panels.
         */
        get: function () {
            return this.component.onDidRemoveView;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(PaneviewApi.prototype, "onDidDrop", {
        /**
         * Invoked when a Drag'n'Drop event occurs that the component was unable to handle. Exposed for custom Drag'n'Drop functionality.
         */
        get: function () {
            return this.component.onDidDrop;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(PaneviewApi.prototype, "onUnhandledDragOverEvent", {
        get: function () {
            return this.component.onUnhandledDragOverEvent;
        },
        enumerable: false,
        configurable: true
    });
    /**
     * Remove a panel given the panel object.
     */
    PaneviewApi.prototype.removePanel = function (panel) {
        this.component.removePanel(panel);
    };
    /**
     * Get a panel object given a `string` id. May return `undefined`.
     */
    PaneviewApi.prototype.getPanel = function (id) {
        return this.component.getPanel(id);
    };
    /**
     * Move a panel given it's current and desired index.
     */
    PaneviewApi.prototype.movePanel = function (from, to) {
        this.component.movePanel(from, to);
    };
    /**
     *  Focus the component. Will try to focus an active panel if one exists.
     */
    PaneviewApi.prototype.focus = function () {
        this.component.focus();
    };
    /**
     * Force resize the component to an exact width and height. Read about auto-resizing before using.
     */
    PaneviewApi.prototype.layout = function (width, height) {
        this.component.layout(width, height);
    };
    /**
     * Add a panel and return the created object.
     */
    PaneviewApi.prototype.addPanel = function (options) {
        return this.component.addPanel(options);
    };
    /**
     * Create a component from a serialized object.
     */
    PaneviewApi.prototype.fromJSON = function (data) {
        this.component.fromJSON(data);
    };
    /**
     * Create a serialized object of the current component.
     */
    PaneviewApi.prototype.toJSON = function () {
        return this.component.toJSON();
    };
    /**
     * Reset the component back to an empty and default state.
     */
    PaneviewApi.prototype.clear = function () {
        this.component.clear();
    };
    /**
     * Update configuratable options.
     */
    PaneviewApi.prototype.updateOptions = function (options) {
        this.component.updateOptions(options);
    };
    /**
     * Release resources and teardown component. Do not call when using framework versions of dockview.
     */
    PaneviewApi.prototype.dispose = function () {
        this.component.dispose();
    };
    return PaneviewApi;
}());
exports.PaneviewApi = PaneviewApi;
var GridviewApi = /** @class */ (function () {
    function GridviewApi(component) {
        this.component = component;
    }
    Object.defineProperty(GridviewApi.prototype, "width", {
        /**
         * Width of the component.
         */
        get: function () {
            return this.component.width;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(GridviewApi.prototype, "height", {
        /**
         * Height of the component.
         */
        get: function () {
            return this.component.height;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(GridviewApi.prototype, "minimumHeight", {
        /**
         * Minimum height of the component.
         */
        get: function () {
            return this.component.minimumHeight;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(GridviewApi.prototype, "maximumHeight", {
        /**
         * Maximum height of the component.
         */
        get: function () {
            return this.component.maximumHeight;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(GridviewApi.prototype, "minimumWidth", {
        /**
         * Minimum width of the component.
         */
        get: function () {
            return this.component.minimumWidth;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(GridviewApi.prototype, "maximumWidth", {
        /**
         * Maximum width of the component.
         */
        get: function () {
            return this.component.maximumWidth;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(GridviewApi.prototype, "onDidLayoutChange", {
        /**
         * Invoked when any layout change occures, an aggregation of many events.
         */
        get: function () {
            return this.component.onDidLayoutChange;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(GridviewApi.prototype, "onDidAddPanel", {
        /**
         * Invoked when a panel is added. May be called multiple times when moving panels.
         */
        get: function () {
            return this.component.onDidAddGroup;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(GridviewApi.prototype, "onDidRemovePanel", {
        /**
         * Invoked when a panel is removed. May be called multiple times when moving panels.
         */
        get: function () {
            return this.component.onDidRemoveGroup;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(GridviewApi.prototype, "onDidActivePanelChange", {
        /**
         * Invoked when the active panel changes. May be undefined if no panel is active.
         */
        get: function () {
            return this.component.onDidActiveGroupChange;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(GridviewApi.prototype, "onDidLayoutFromJSON", {
        /**
         * Invoked after a layout is deserialzied using the `fromJSON` method.
         */
        get: function () {
            return this.component.onDidLayoutFromJSON;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(GridviewApi.prototype, "panels", {
        /**
         * All panel objects.
         */
        get: function () {
            return this.component.groups;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(GridviewApi.prototype, "orientation", {
        /**
         * Current orientation. Can be changed after initialization.
         */
        get: function () {
            return this.component.orientation;
        },
        set: function (value) {
            this.component.updateOptions({ orientation: value });
        },
        enumerable: false,
        configurable: true
    });
    /**
     *  Focus the component. Will try to focus an active panel if one exists.
     */
    GridviewApi.prototype.focus = function () {
        this.component.focus();
    };
    /**
     * Force resize the component to an exact width and height. Read about auto-resizing before using.
     */
    GridviewApi.prototype.layout = function (width, height, force) {
        if (force === void 0) { force = false; }
        this.component.layout(width, height, force);
    };
    /**
     * Add a panel and return the created object.
     */
    GridviewApi.prototype.addPanel = function (options) {
        return this.component.addPanel(options);
    };
    /**
     * Remove a panel given the panel object.
     */
    GridviewApi.prototype.removePanel = function (panel, sizing) {
        this.component.removePanel(panel, sizing);
    };
    /**
     * Move a panel in a particular direction relative to another panel.
     */
    GridviewApi.prototype.movePanel = function (panel, options) {
        this.component.movePanel(panel, options);
    };
    /**
     * Get a panel object given a `string` id. May return `undefined`.
     */
    GridviewApi.prototype.getPanel = function (id) {
        return this.component.getPanel(id);
    };
    /**
     * Create a component from a serialized object.
     */
    GridviewApi.prototype.fromJSON = function (data) {
        return this.component.fromJSON(data);
    };
    /**
     * Create a serialized object of the current component.
     */
    GridviewApi.prototype.toJSON = function () {
        return this.component.toJSON();
    };
    /**
     * Reset the component back to an empty and default state.
     */
    GridviewApi.prototype.clear = function () {
        this.component.clear();
    };
    GridviewApi.prototype.updateOptions = function (options) {
        this.component.updateOptions(options);
    };
    /**
     * Release resources and teardown component. Do not call when using framework versions of dockview.
     */
    GridviewApi.prototype.dispose = function () {
        this.component.dispose();
    };
    return GridviewApi;
}());
exports.GridviewApi = GridviewApi;
var DockviewApi = /** @class */ (function () {
    function DockviewApi(component) {
        this.component = component;
    }
    Object.defineProperty(DockviewApi.prototype, "id", {
        /**
         * The unique identifier for this instance. Used to manage scope of Drag'n'Drop events.
         */
        get: function () {
            return this.component.id;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewApi.prototype, "width", {
        /**
         * Width of the component.
         */
        get: function () {
            return this.component.width;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewApi.prototype, "height", {
        /**
         * Height of the component.
         */
        get: function () {
            return this.component.height;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewApi.prototype, "minimumHeight", {
        /**
         * Minimum height of the component.
         */
        get: function () {
            return this.component.minimumHeight;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewApi.prototype, "maximumHeight", {
        /**
         * Maximum height of the component.
         */
        get: function () {
            return this.component.maximumHeight;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewApi.prototype, "minimumWidth", {
        /**
         * Minimum width of the component.
         */
        get: function () {
            return this.component.minimumWidth;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewApi.prototype, "maximumWidth", {
        /**
         * Maximum width of the component.
         */
        get: function () {
            return this.component.maximumWidth;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewApi.prototype, "size", {
        /**
         * Total number of groups.
         */
        get: function () {
            return this.component.size;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewApi.prototype, "totalPanels", {
        /**
         * Total number of panels.
         */
        get: function () {
            return this.component.totalPanels;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewApi.prototype, "onDidActiveGroupChange", {
        /**
         * Invoked when the active group changes. May be undefined if no group is active.
         */
        get: function () {
            return this.component.onDidActiveGroupChange;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewApi.prototype, "onDidAddGroup", {
        /**
         * Invoked when a group is added. May be called multiple times when moving groups.
         */
        get: function () {
            return this.component.onDidAddGroup;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewApi.prototype, "onDidRemoveGroup", {
        /**
         * Invoked when a group is removed. May be called multiple times when moving groups.
         */
        get: function () {
            return this.component.onDidRemoveGroup;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewApi.prototype, "onDidActivePanelChange", {
        /**
         * Invoked when the active panel changes. May be undefined if no panel is active.
         */
        get: function () {
            return this.component.onDidActivePanelChange;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewApi.prototype, "onDidAddPanel", {
        /**
         * Invoked when a panel is added. May be called multiple times when moving panels.
         */
        get: function () {
            return this.component.onDidAddPanel;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewApi.prototype, "onDidRemovePanel", {
        /**
         * Invoked when a panel is removed. May be called multiple times when moving panels.
         */
        get: function () {
            return this.component.onDidRemovePanel;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewApi.prototype, "onDidMovePanel", {
        get: function () {
            return this.component.onDidMovePanel;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewApi.prototype, "onDidLayoutFromJSON", {
        /**
         * Invoked after a layout is deserialzied using the `fromJSON` method.
         */
        get: function () {
            return this.component.onDidLayoutFromJSON;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewApi.prototype, "onDidLayoutChange", {
        /**
         * Invoked when any layout change occures, an aggregation of many events.
         */
        get: function () {
            return this.component.onDidLayoutChange;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewApi.prototype, "onDidDrop", {
        /**
         * Invoked when a Drag'n'Drop event occurs that the component was unable to handle. Exposed for custom Drag'n'Drop functionality.
         */
        get: function () {
            return this.component.onDidDrop;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewApi.prototype, "onWillDrop", {
        /**
         * Invoked when a Drag'n'Drop event occurs but before dockview handles it giving the user an opportunity to intecept and
         * prevent the event from occuring using the standard `preventDefault()` syntax.
         *
         * Preventing certain events may causes unexpected behaviours, use carefully.
         */
        get: function () {
            return this.component.onWillDrop;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewApi.prototype, "onWillShowOverlay", {
        /**
         * Invoked before an overlay is shown indicating a drop target.
         *
         * Calling `event.preventDefault()` will prevent the overlay being shown and prevent
         * the any subsequent drop event.
         */
        get: function () {
            return this.component.onWillShowOverlay;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewApi.prototype, "onWillDragGroup", {
        /**
         * Invoked before a group is dragged.
         *
         * Calling `event.nativeEvent.preventDefault()` will prevent the group drag starting.
         *
         */
        get: function () {
            return this.component.onWillDragGroup;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewApi.prototype, "onWillDragPanel", {
        /**
         * Invoked before a panel is dragged.
         *
         * Calling `event.nativeEvent.preventDefault()` will prevent the panel drag starting.
         */
        get: function () {
            return this.component.onWillDragPanel;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewApi.prototype, "onUnhandledDragOverEvent", {
        get: function () {
            return this.component.onUnhandledDragOverEvent;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewApi.prototype, "onDidPopoutGroupSizeChange", {
        get: function () {
            return this.component.onDidPopoutGroupSizeChange;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewApi.prototype, "onDidPopoutGroupPositionChange", {
        get: function () {
            return this.component.onDidPopoutGroupPositionChange;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewApi.prototype, "onDidOpenPopoutWindowFail", {
        get: function () {
            return this.component.onDidOpenPopoutWindowFail;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewApi.prototype, "panels", {
        /**
         * All panel objects.
         */
        get: function () {
            return this.component.panels;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewApi.prototype, "groups", {
        /**
         * All group objects.
         */
        get: function () {
            return this.component.groups;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewApi.prototype, "activePanel", {
        /**
         *  Active panel object.
         */
        get: function () {
            return this.component.activePanel;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewApi.prototype, "activeGroup", {
        /**
         * Active group object.
         */
        get: function () {
            return this.component.activeGroup;
        },
        enumerable: false,
        configurable: true
    });
    /**
     *  Focus the component. Will try to focus an active panel if one exists.
     */
    DockviewApi.prototype.focus = function () {
        this.component.focus();
    };
    /**
     * Get a panel object given a `string` id. May return `undefined`.
     */
    DockviewApi.prototype.getPanel = function (id) {
        return this.component.getGroupPanel(id);
    };
    /**
     * Force resize the component to an exact width and height. Read about auto-resizing before using.
     */
    DockviewApi.prototype.layout = function (width, height, force) {
        if (force === void 0) { force = false; }
        this.component.layout(width, height, force);
    };
    /**
     * Add a panel and return the created object.
     */
    DockviewApi.prototype.addPanel = function (options) {
        return this.component.addPanel(options);
    };
    /**
     * Remove a panel given the panel object.
     */
    DockviewApi.prototype.removePanel = function (panel) {
        this.component.removePanel(panel);
    };
    /**
     * Add a group and return the created object.
     */
    DockviewApi.prototype.addGroup = function (options) {
        return this.component.addGroup(options);
    };
    /**
     * Close all groups and panels.
     */
    DockviewApi.prototype.closeAllGroups = function () {
        return this.component.closeAllGroups();
    };
    /**
     * Remove a group and any panels within the group.
     */
    DockviewApi.prototype.removeGroup = function (group) {
        this.component.removeGroup(group);
    };
    /**
     * Get a group object given a `string` id. May return undefined.
     */
    DockviewApi.prototype.getGroup = function (id) {
        return this.component.getPanel(id);
    };
    /**
     * Add a floating group
     */
    DockviewApi.prototype.addFloatingGroup = function (item, options) {
        return this.component.addFloatingGroup(item, options);
    };
    /**
     * Create a component from a serialized object.
     */
    DockviewApi.prototype.fromJSON = function (data) {
        this.component.fromJSON(data);
    };
    /**
     * Create a serialized object of the current component.
     */
    DockviewApi.prototype.toJSON = function () {
        return this.component.toJSON();
    };
    /**
     * Reset the component back to an empty and default state.
     */
    DockviewApi.prototype.clear = function () {
        this.component.clear();
    };
    /**
     * Move the focus progmatically to the next panel or group.
     */
    DockviewApi.prototype.moveToNext = function (options) {
        this.component.moveToNext(options);
    };
    /**
     * Move the focus progmatically to the previous panel or group.
     */
    DockviewApi.prototype.moveToPrevious = function (options) {
        this.component.moveToPrevious(options);
    };
    DockviewApi.prototype.maximizeGroup = function (panel) {
        this.component.maximizeGroup(panel.group);
    };
    DockviewApi.prototype.hasMaximizedGroup = function () {
        return this.component.hasMaximizedGroup();
    };
    DockviewApi.prototype.exitMaximizedGroup = function () {
        this.component.exitMaximizedGroup();
    };
    Object.defineProperty(DockviewApi.prototype, "onDidMaximizedGroupChange", {
        get: function () {
            return this.component.onDidMaximizedGroupChange;
        },
        enumerable: false,
        configurable: true
    });
    /**
     * Add a popout group in a new Window
     */
    DockviewApi.prototype.addPopoutGroup = function (item, options) {
        return this.component.addPopoutGroup(item, options);
    };
    DockviewApi.prototype.updateOptions = function (options) {
        this.component.updateOptions(options);
    };
    /**
     * Release resources and teardown component. Do not call when using framework versions of dockview.
     */
    DockviewApi.prototype.dispose = function () {
        this.component.dispose();
    };
    return DockviewApi;
}());
exports.DockviewApi = DockviewApi;
