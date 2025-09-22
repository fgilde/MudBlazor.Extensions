"use strict";
var __extends = (this && this.__extends) || (function () {
    var extendStatics = function (d, b) {
        extendStatics = Object.setPrototypeOf ||
            ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
            function (d, b) { for (var p in b) if (Object.prototype.hasOwnProperty.call(b, p)) d[p] = b[p]; };
        return extendStatics(d, b);
    };
    return function (d, b) {
        if (typeof b !== "function" && b !== null)
            throw new TypeError("Class extends value " + String(b) + " is not a constructor or null");
        extendStatics(d, b);
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };
})();
var __read = (this && this.__read) || function (o, n) {
    var m = typeof Symbol === "function" && o[Symbol.iterator];
    if (!m) return o;
    var i = m.call(o), r, ar = [], e;
    try {
        while ((n === void 0 || n-- > 0) && !(r = i.next()).done) ar.push(r.value);
    }
    catch (error) { e = { error: error }; }
    finally {
        try {
            if (r && !r.done && (m = i["return"])) m.call(i);
        }
        finally { if (e) throw e.error; }
    }
    return ar;
};
var __spreadArray = (this && this.__spreadArray) || function (to, from, pack) {
    if (pack || arguments.length === 2) for (var i = 0, l = from.length, ar; i < l; i++) {
        if (ar || !(i in from)) {
            if (!ar) ar = Array.prototype.slice.call(from, 0, i);
            ar[i] = from[i];
        }
    }
    return to.concat(ar || Array.prototype.slice.call(from));
};
var __values = (this && this.__values) || function(o) {
    var s = typeof Symbol === "function" && Symbol.iterator, m = s && o[s], i = 0;
    if (m) return m.call(o);
    if (o && typeof o.length === "number") return {
        next: function () {
            if (o && i >= o.length) o = void 0;
            return { value: o && o[i++], done: !o };
        }
    };
    throw new TypeError(s ? "Object is not iterable." : "Symbol.iterator is not defined.");
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.DockviewGroupPanelModel = exports.DockviewWillDropEvent = exports.DockviewDidDropEvent = void 0;
var component_api_1 = require("../api/component.api");
var dataTransfer_1 = require("../dnd/dataTransfer");
var dom_1 = require("../dom");
var events_1 = require("../events");
var events_2 = require("./events");
var lifecycle_1 = require("../lifecycle");
var content_1 = require("./components/panel/content");
var tabsContainer_1 = require("./components/titlebar/tabsContainer");
var options_1 = require("./options");
var DockviewDidDropEvent = /** @class */ (function (_super) {
    __extends(DockviewDidDropEvent, _super);
    function DockviewDidDropEvent(options) {
        var _this = _super.call(this) || this;
        _this.options = options;
        return _this;
    }
    Object.defineProperty(DockviewDidDropEvent.prototype, "nativeEvent", {
        get: function () {
            return this.options.nativeEvent;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewDidDropEvent.prototype, "position", {
        get: function () {
            return this.options.position;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewDidDropEvent.prototype, "panel", {
        get: function () {
            return this.options.panel;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewDidDropEvent.prototype, "group", {
        get: function () {
            return this.options.group;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewDidDropEvent.prototype, "api", {
        get: function () {
            return this.options.api;
        },
        enumerable: false,
        configurable: true
    });
    DockviewDidDropEvent.prototype.getData = function () {
        return this.options.getData();
    };
    return DockviewDidDropEvent;
}(events_1.DockviewEvent));
exports.DockviewDidDropEvent = DockviewDidDropEvent;
var DockviewWillDropEvent = /** @class */ (function (_super) {
    __extends(DockviewWillDropEvent, _super);
    function DockviewWillDropEvent(options) {
        var _this = _super.call(this, options) || this;
        _this._kind = options.kind;
        return _this;
    }
    Object.defineProperty(DockviewWillDropEvent.prototype, "kind", {
        get: function () {
            return this._kind;
        },
        enumerable: false,
        configurable: true
    });
    return DockviewWillDropEvent;
}(DockviewDidDropEvent));
exports.DockviewWillDropEvent = DockviewWillDropEvent;
var DockviewGroupPanelModel = /** @class */ (function (_super) {
    __extends(DockviewGroupPanelModel, _super);
    function DockviewGroupPanelModel(container, accessor, id, options, groupPanel) {
        var _a;
        var _this = _super.call(this) || this;
        _this.container = container;
        _this.accessor = accessor;
        _this.id = id;
        _this.options = options;
        _this.groupPanel = groupPanel;
        _this._isGroupActive = false;
        _this._locked = false;
        _this._location = { type: 'grid' };
        _this.mostRecentlyUsed = [];
        _this._overwriteRenderContainer = null;
        _this._overwriteDropTargetContainer = null;
        _this._onDidChange = new events_1.Emitter();
        _this.onDidChange = _this._onDidChange.event;
        _this._width = 0;
        _this._height = 0;
        _this._panels = [];
        _this._panelDisposables = new Map();
        _this._onMove = new events_1.Emitter();
        _this.onMove = _this._onMove.event;
        _this._onDidDrop = new events_1.Emitter();
        _this.onDidDrop = _this._onDidDrop.event;
        _this._onWillDrop = new events_1.Emitter();
        _this.onWillDrop = _this._onWillDrop.event;
        _this._onWillShowOverlay = new events_1.Emitter();
        _this.onWillShowOverlay = _this._onWillShowOverlay.event;
        _this._onTabDragStart = new events_1.Emitter();
        _this.onTabDragStart = _this._onTabDragStart.event;
        _this._onGroupDragStart = new events_1.Emitter();
        _this.onGroupDragStart = _this._onGroupDragStart.event;
        _this._onDidAddPanel = new events_1.Emitter();
        _this.onDidAddPanel = _this._onDidAddPanel.event;
        _this._onDidPanelTitleChange = new events_1.Emitter();
        _this.onDidPanelTitleChange = _this._onDidPanelTitleChange.event;
        _this._onDidPanelParametersChange = new events_1.Emitter();
        _this.onDidPanelParametersChange = _this._onDidPanelParametersChange.event;
        _this._onDidRemovePanel = new events_1.Emitter();
        _this.onDidRemovePanel = _this._onDidRemovePanel.event;
        _this._onDidActivePanelChange = new events_1.Emitter();
        _this.onDidActivePanelChange = _this._onDidActivePanelChange.event;
        _this._onUnhandledDragOverEvent = new events_1.Emitter();
        _this.onUnhandledDragOverEvent = _this._onUnhandledDragOverEvent.event;
        (0, dom_1.toggleClass)(_this.container, 'dv-groupview', true);
        _this._api = new component_api_1.DockviewApi(_this.accessor);
        _this.tabsContainer = new tabsContainer_1.TabsContainer(_this.accessor, _this.groupPanel);
        _this.contentContainer = new content_1.ContentContainer(_this.accessor, _this);
        container.append(_this.tabsContainer.element, _this.contentContainer.element);
        _this.header.hidden = !!options.hideHeader;
        _this.locked = (_a = options.locked) !== null && _a !== void 0 ? _a : false;
        _this.addDisposables(_this._onTabDragStart, _this._onGroupDragStart, _this._onWillShowOverlay, _this.tabsContainer.onTabDragStart(function (event) {
            _this._onTabDragStart.fire(event);
        }), _this.tabsContainer.onGroupDragStart(function (event) {
            _this._onGroupDragStart.fire(event);
        }), _this.tabsContainer.onDrop(function (event) {
            _this.handleDropEvent('header', event.event, 'center', event.index);
        }), _this.contentContainer.onDidFocus(function () {
            _this.accessor.doSetGroupActive(_this.groupPanel);
        }), _this.contentContainer.onDidBlur(function () {
            // noop
        }), _this.contentContainer.dropTarget.onDrop(function (event) {
            _this.handleDropEvent('content', event.nativeEvent, event.position);
        }), _this.tabsContainer.onWillShowOverlay(function (event) {
            _this._onWillShowOverlay.fire(event);
        }), _this.contentContainer.dropTarget.onWillShowOverlay(function (event) {
            _this._onWillShowOverlay.fire(new events_2.WillShowOverlayLocationEvent(event, {
                kind: 'content',
                panel: _this.activePanel,
                api: _this._api,
                group: _this.groupPanel,
                getData: dataTransfer_1.getPanelData,
            }));
        }), _this._onMove, _this._onDidChange, _this._onDidDrop, _this._onWillDrop, _this._onDidAddPanel, _this._onDidRemovePanel, _this._onDidActivePanelChange, _this._onUnhandledDragOverEvent, _this._onDidPanelTitleChange, _this._onDidPanelParametersChange);
        return _this;
    }
    Object.defineProperty(DockviewGroupPanelModel.prototype, "element", {
        get: function () {
            throw new Error('dockview: not supported');
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewGroupPanelModel.prototype, "activePanel", {
        get: function () {
            return this._activePanel;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewGroupPanelModel.prototype, "locked", {
        get: function () {
            return this._locked;
        },
        set: function (value) {
            this._locked = value;
            (0, dom_1.toggleClass)(this.container, 'dv-locked-groupview', value === 'no-drop-target' || value);
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewGroupPanelModel.prototype, "isActive", {
        get: function () {
            return this._isGroupActive;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewGroupPanelModel.prototype, "panels", {
        get: function () {
            return this._panels;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewGroupPanelModel.prototype, "size", {
        get: function () {
            return this._panels.length;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewGroupPanelModel.prototype, "isEmpty", {
        get: function () {
            return this._panels.length === 0;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewGroupPanelModel.prototype, "hasWatermark", {
        get: function () {
            return !!(this.watermark && this.container.contains(this.watermark.element));
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewGroupPanelModel.prototype, "header", {
        get: function () {
            return this.tabsContainer;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewGroupPanelModel.prototype, "isContentFocused", {
        get: function () {
            if (!document.activeElement) {
                return false;
            }
            return (0, dom_1.isAncestor)(document.activeElement, this.contentContainer.element);
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewGroupPanelModel.prototype, "location", {
        get: function () {
            return this._location;
        },
        set: function (value) {
            this._location = value;
            (0, dom_1.toggleClass)(this.container, 'dv-groupview-floating', false);
            (0, dom_1.toggleClass)(this.container, 'dv-groupview-popout', false);
            switch (value.type) {
                case 'grid':
                    this.contentContainer.dropTarget.setTargetZones([
                        'top',
                        'bottom',
                        'left',
                        'right',
                        'center',
                    ]);
                    break;
                case 'floating':
                    this.contentContainer.dropTarget.setTargetZones(['center']);
                    this.contentContainer.dropTarget.setTargetZones(value
                        ? ['center']
                        : ['top', 'bottom', 'left', 'right', 'center']);
                    (0, dom_1.toggleClass)(this.container, 'dv-groupview-floating', true);
                    break;
                case 'popout':
                    this.contentContainer.dropTarget.setTargetZones(['center']);
                    (0, dom_1.toggleClass)(this.container, 'dv-groupview-popout', true);
                    break;
            }
            this.groupPanel.api._onDidLocationChange.fire({
                location: this.location,
            });
        },
        enumerable: false,
        configurable: true
    });
    DockviewGroupPanelModel.prototype.focusContent = function () {
        this.contentContainer.element.focus();
    };
    Object.defineProperty(DockviewGroupPanelModel.prototype, "renderContainer", {
        get: function () {
            var _a;
            return ((_a = this._overwriteRenderContainer) !== null && _a !== void 0 ? _a : this.accessor.overlayRenderContainer);
        },
        set: function (value) {
            var _this = this;
            this.panels.forEach(function (panel) {
                _this.renderContainer.detatch(panel);
            });
            this._overwriteRenderContainer = value;
            this.panels.forEach(function (panel) {
                _this.rerender(panel);
            });
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewGroupPanelModel.prototype, "dropTargetContainer", {
        get: function () {
            var _a;
            return ((_a = this._overwriteDropTargetContainer) !== null && _a !== void 0 ? _a : this.accessor.rootDropTargetContainer);
        },
        set: function (value) {
            this._overwriteDropTargetContainer = value;
        },
        enumerable: false,
        configurable: true
    });
    DockviewGroupPanelModel.prototype.initialize = function () {
        var _this = this;
        if (this.options.panels) {
            this.options.panels.forEach(function (panel) {
                _this.doAddPanel(panel);
            });
        }
        if (this.options.activePanel) {
            this.openPanel(this.options.activePanel);
        }
        // must be run after the constructor otherwise this.parent may not be
        // correctly initialized
        this.setActive(this.isActive, true);
        this.updateContainer();
        if (this.accessor.options.createRightHeaderActionComponent) {
            this._rightHeaderActions =
                this.accessor.options.createRightHeaderActionComponent(this.groupPanel);
            this.addDisposables(this._rightHeaderActions);
            this._rightHeaderActions.init({
                containerApi: this._api,
                api: this.groupPanel.api,
                group: this.groupPanel,
            });
            this.tabsContainer.setRightActionsElement(this._rightHeaderActions.element);
        }
        if (this.accessor.options.createLeftHeaderActionComponent) {
            this._leftHeaderActions =
                this.accessor.options.createLeftHeaderActionComponent(this.groupPanel);
            this.addDisposables(this._leftHeaderActions);
            this._leftHeaderActions.init({
                containerApi: this._api,
                api: this.groupPanel.api,
                group: this.groupPanel,
            });
            this.tabsContainer.setLeftActionsElement(this._leftHeaderActions.element);
        }
        if (this.accessor.options.createPrefixHeaderActionComponent) {
            this._prefixHeaderActions =
                this.accessor.options.createPrefixHeaderActionComponent(this.groupPanel);
            this.addDisposables(this._prefixHeaderActions);
            this._prefixHeaderActions.init({
                containerApi: this._api,
                api: this.groupPanel.api,
                group: this.groupPanel,
            });
            this.tabsContainer.setPrefixActionsElement(this._prefixHeaderActions.element);
        }
    };
    DockviewGroupPanelModel.prototype.rerender = function (panel) {
        this.contentContainer.renderPanel(panel, { asActive: false });
    };
    DockviewGroupPanelModel.prototype.indexOf = function (panel) {
        return this.tabsContainer.indexOf(panel.id);
    };
    DockviewGroupPanelModel.prototype.toJSON = function () {
        var _a;
        var result = {
            views: this.tabsContainer.panels,
            activeView: (_a = this._activePanel) === null || _a === void 0 ? void 0 : _a.id,
            id: this.id,
        };
        if (this.locked !== false) {
            result.locked = this.locked;
        }
        if (this.header.hidden) {
            result.hideHeader = true;
        }
        return result;
    };
    DockviewGroupPanelModel.prototype.moveToNext = function (options) {
        if (!options) {
            options = {};
        }
        if (!options.panel) {
            options.panel = this.activePanel;
        }
        var index = options.panel ? this.panels.indexOf(options.panel) : -1;
        var normalizedIndex;
        if (index < this.panels.length - 1) {
            normalizedIndex = index + 1;
        }
        else if (!options.suppressRoll) {
            normalizedIndex = 0;
        }
        else {
            return;
        }
        this.openPanel(this.panels[normalizedIndex]);
    };
    DockviewGroupPanelModel.prototype.moveToPrevious = function (options) {
        if (!options) {
            options = {};
        }
        if (!options.panel) {
            options.panel = this.activePanel;
        }
        if (!options.panel) {
            return;
        }
        var index = this.panels.indexOf(options.panel);
        var normalizedIndex;
        if (index > 0) {
            normalizedIndex = index - 1;
        }
        else if (!options.suppressRoll) {
            normalizedIndex = this.panels.length - 1;
        }
        else {
            return;
        }
        this.openPanel(this.panels[normalizedIndex]);
    };
    DockviewGroupPanelModel.prototype.containsPanel = function (panel) {
        return this.panels.includes(panel);
    };
    DockviewGroupPanelModel.prototype.init = function (_params) {
        //noop
    };
    DockviewGroupPanelModel.prototype.update = function (_params) {
        //noop
    };
    DockviewGroupPanelModel.prototype.focus = function () {
        var _a;
        (_a = this._activePanel) === null || _a === void 0 ? void 0 : _a.focus();
    };
    DockviewGroupPanelModel.prototype.openPanel = function (panel, options) {
        /**
         * set the panel group
         * add the panel
         * check if group active
         * check if panel active
         */
        if (options === void 0) { options = {}; }
        if (typeof options.index !== 'number' ||
            options.index > this.panels.length) {
            options.index = this.panels.length;
        }
        var skipSetActive = !!options.skipSetActive;
        // ensure the group is updated before we fire any events
        panel.updateParentGroup(this.groupPanel, {
            skipSetActive: options.skipSetActive,
        });
        this.doAddPanel(panel, options.index, {
            skipSetActive: skipSetActive,
        });
        if (this._activePanel === panel) {
            this.contentContainer.renderPanel(panel, { asActive: true });
            return;
        }
        if (!skipSetActive) {
            this.doSetActivePanel(panel);
        }
        if (!options.skipSetGroupActive) {
            this.accessor.doSetGroupActive(this.groupPanel);
        }
        if (!options.skipSetActive) {
            this.updateContainer();
        }
    };
    DockviewGroupPanelModel.prototype.removePanel = function (groupItemOrId, options) {
        if (options === void 0) { options = {
            skipSetActive: false,
        }; }
        var id = typeof groupItemOrId === 'string'
            ? groupItemOrId
            : groupItemOrId.id;
        var panelToRemove = this._panels.find(function (panel) { return panel.id === id; });
        if (!panelToRemove) {
            throw new Error('invalid operation');
        }
        return this._removePanel(panelToRemove, options);
    };
    DockviewGroupPanelModel.prototype.closeAllPanels = function () {
        var e_1, _a;
        if (this.panels.length > 0) {
            // take a copy since we will be edting the array as we iterate through
            var arrPanelCpy = __spreadArray([], __read(this.panels), false);
            try {
                for (var arrPanelCpy_1 = __values(arrPanelCpy), arrPanelCpy_1_1 = arrPanelCpy_1.next(); !arrPanelCpy_1_1.done; arrPanelCpy_1_1 = arrPanelCpy_1.next()) {
                    var panel = arrPanelCpy_1_1.value;
                    this.doClose(panel);
                }
            }
            catch (e_1_1) { e_1 = { error: e_1_1 }; }
            finally {
                try {
                    if (arrPanelCpy_1_1 && !arrPanelCpy_1_1.done && (_a = arrPanelCpy_1.return)) _a.call(arrPanelCpy_1);
                }
                finally { if (e_1) throw e_1.error; }
            }
        }
        else {
            this.accessor.removeGroup(this.groupPanel);
        }
    };
    DockviewGroupPanelModel.prototype.closePanel = function (panel) {
        this.doClose(panel);
    };
    DockviewGroupPanelModel.prototype.doClose = function (panel) {
        var isLast = this.panels.length === 1 && this.accessor.groups.length === 1;
        this.accessor.removePanel(panel, isLast && this.accessor.options.noPanelsOverlay === 'emptyGroup'
            ? { removeEmptyGroup: false }
            : undefined);
    };
    DockviewGroupPanelModel.prototype.isPanelActive = function (panel) {
        return this._activePanel === panel;
    };
    DockviewGroupPanelModel.prototype.updateActions = function (element) {
        this.tabsContainer.setRightActionsElement(element);
    };
    DockviewGroupPanelModel.prototype.setActive = function (isGroupActive, force) {
        if (force === void 0) { force = false; }
        if (!force && this.isActive === isGroupActive) {
            return;
        }
        this._isGroupActive = isGroupActive;
        (0, dom_1.toggleClass)(this.container, 'dv-active-group', isGroupActive);
        (0, dom_1.toggleClass)(this.container, 'dv-inactive-group', !isGroupActive);
        this.tabsContainer.setActive(this.isActive);
        if (!this._activePanel && this.panels.length > 0) {
            this.doSetActivePanel(this.panels[0]);
        }
        this.updateContainer();
    };
    DockviewGroupPanelModel.prototype.layout = function (width, height) {
        var _a;
        this._width = width;
        this._height = height;
        this.contentContainer.layout(this._width, this._height);
        if ((_a = this._activePanel) === null || _a === void 0 ? void 0 : _a.layout) {
            this._activePanel.layout(this._width, this._height);
        }
    };
    DockviewGroupPanelModel.prototype._removePanel = function (panel, options) {
        var isActivePanel = this._activePanel === panel;
        this.doRemovePanel(panel);
        if (isActivePanel && this.panels.length > 0) {
            var nextPanel = this.mostRecentlyUsed[0];
            this.openPanel(nextPanel, {
                skipSetActive: options.skipSetActive,
                skipSetGroupActive: options.skipSetActiveGroup,
            });
        }
        if (this._activePanel && this.panels.length === 0) {
            this.doSetActivePanel(undefined);
        }
        if (!options.skipSetActive) {
            this.updateContainer();
        }
        return panel;
    };
    DockviewGroupPanelModel.prototype.doRemovePanel = function (panel) {
        var index = this.panels.indexOf(panel);
        if (this._activePanel === panel) {
            this.contentContainer.closePanel();
        }
        this.tabsContainer.delete(panel.id);
        this._panels.splice(index, 1);
        if (this.mostRecentlyUsed.includes(panel)) {
            var index_1 = this.mostRecentlyUsed.indexOf(panel);
            this.mostRecentlyUsed.splice(index_1, 1);
        }
        var disposable = this._panelDisposables.get(panel.id);
        if (disposable) {
            disposable.dispose();
            this._panelDisposables.delete(panel.id);
        }
        this._onDidRemovePanel.fire({ panel: panel });
    };
    DockviewGroupPanelModel.prototype.doAddPanel = function (panel, index, options) {
        var _this = this;
        if (index === void 0) { index = this.panels.length; }
        if (options === void 0) { options = { skipSetActive: false }; }
        var existingPanel = this._panels.indexOf(panel);
        var hasExistingPanel = existingPanel > -1;
        this.tabsContainer.show();
        this.contentContainer.show();
        this.tabsContainer.openPanel(panel, index);
        if (!options.skipSetActive) {
            this.contentContainer.openPanel(panel);
        }
        if (hasExistingPanel) {
            // TODO - need to ensure ordering hasn't changed and if it has need to re-order this.panels
            return;
        }
        this.updateMru(panel);
        this.panels.splice(index, 0, panel);
        this._panelDisposables.set(panel.id, new lifecycle_1.CompositeDisposable(panel.api.onDidTitleChange(function (event) {
            return _this._onDidPanelTitleChange.fire(event);
        }), panel.api.onDidParametersChange(function (event) {
            return _this._onDidPanelParametersChange.fire(event);
        })));
        this._onDidAddPanel.fire({ panel: panel });
    };
    DockviewGroupPanelModel.prototype.doSetActivePanel = function (panel) {
        if (this._activePanel === panel) {
            return;
        }
        this._activePanel = panel;
        if (panel) {
            this.tabsContainer.setActivePanel(panel);
            panel.layout(this._width, this._height);
            this.updateMru(panel);
            this._onDidActivePanelChange.fire({
                panel: panel,
            });
        }
    };
    DockviewGroupPanelModel.prototype.updateMru = function (panel) {
        if (this.mostRecentlyUsed.includes(panel)) {
            this.mostRecentlyUsed.splice(this.mostRecentlyUsed.indexOf(panel), 1);
        }
        this.mostRecentlyUsed = __spreadArray([panel], __read(this.mostRecentlyUsed), false);
    };
    DockviewGroupPanelModel.prototype.updateContainer = function () {
        var _this = this;
        var _a, _b;
        this.panels.forEach(function (panel) { return panel.runEvents(); });
        if (this.isEmpty && !this.watermark) {
            var watermark = this.accessor.createWatermarkComponent();
            watermark.init({
                containerApi: this._api,
                group: this.groupPanel,
            });
            this.watermark = watermark;
            (0, events_1.addDisposableListener)(this.watermark.element, 'pointerdown', function () {
                if (!_this.isActive) {
                    _this.accessor.doSetGroupActive(_this.groupPanel);
                }
            });
            this.contentContainer.element.appendChild(this.watermark.element);
        }
        if (!this.isEmpty && this.watermark) {
            this.watermark.element.remove();
            (_b = (_a = this.watermark).dispose) === null || _b === void 0 ? void 0 : _b.call(_a);
            this.watermark = undefined;
        }
    };
    DockviewGroupPanelModel.prototype.canDisplayOverlay = function (event, position, target) {
        var firedEvent = new options_1.DockviewUnhandledDragOverEvent(event, target, position, dataTransfer_1.getPanelData, this.accessor.getPanel(this.id));
        this._onUnhandledDragOverEvent.fire(firedEvent);
        return firedEvent.isAccepted;
    };
    DockviewGroupPanelModel.prototype.handleDropEvent = function (type, event, position, index) {
        if (this.locked === 'no-drop-target') {
            return;
        }
        function getKind() {
            switch (type) {
                case 'header':
                    return typeof index === 'number' ? 'tab' : 'header_space';
                case 'content':
                    return 'content';
            }
        }
        var panel = typeof index === 'number' ? this.panels[index] : undefined;
        var willDropEvent = new DockviewWillDropEvent({
            nativeEvent: event,
            position: position,
            panel: panel,
            getData: function () { return (0, dataTransfer_1.getPanelData)(); },
            kind: getKind(),
            group: this.groupPanel,
            api: this._api,
        });
        this._onWillDrop.fire(willDropEvent);
        if (willDropEvent.defaultPrevented) {
            return;
        }
        var data = (0, dataTransfer_1.getPanelData)();
        if (data && data.viewId === this.accessor.id) {
            if (type === 'content') {
                if (data.groupId === this.id) {
                    // don't allow to drop on self for center position
                    if (position === 'center') {
                        return;
                    }
                    if (data.panelId === null) {
                        // don't allow group move to drop anywhere on self
                        return;
                    }
                }
            }
            if (type === 'header') {
                if (data.groupId === this.id) {
                    if (data.panelId === null) {
                        return;
                    }
                }
            }
            if (data.panelId === null) {
                // this is a group move dnd event
                var groupId_1 = data.groupId;
                this._onMove.fire({
                    target: position,
                    groupId: groupId_1,
                    index: index,
                });
                return;
            }
            var fromSameGroup = this.tabsContainer.indexOf(data.panelId) !== -1;
            if (fromSameGroup && this.tabsContainer.size === 1) {
                return;
            }
            var groupId = data.groupId, panelId = data.panelId;
            var isSameGroup = this.id === groupId;
            if (isSameGroup && !position) {
                var oldIndex = this.tabsContainer.indexOf(panelId);
                if (oldIndex === index) {
                    return;
                }
            }
            this._onMove.fire({
                target: position,
                groupId: data.groupId,
                itemId: data.panelId,
                index: index,
            });
        }
        else {
            this._onDidDrop.fire(new DockviewDidDropEvent({
                nativeEvent: event,
                position: position,
                panel: panel,
                getData: function () { return (0, dataTransfer_1.getPanelData)(); },
                group: this.groupPanel,
                api: this._api,
            }));
        }
    };
    DockviewGroupPanelModel.prototype.updateDragAndDropState = function () {
        this.tabsContainer.updateDragAndDropState();
    };
    DockviewGroupPanelModel.prototype.dispose = function () {
        var e_2, _a;
        var _b, _c, _d;
        _super.prototype.dispose.call(this);
        (_b = this.watermark) === null || _b === void 0 ? void 0 : _b.element.remove();
        (_d = (_c = this.watermark) === null || _c === void 0 ? void 0 : _c.dispose) === null || _d === void 0 ? void 0 : _d.call(_c);
        this.watermark = undefined;
        try {
            for (var _e = __values(this.panels), _f = _e.next(); !_f.done; _f = _e.next()) {
                var panel = _f.value;
                panel.dispose();
            }
        }
        catch (e_2_1) { e_2 = { error: e_2_1 }; }
        finally {
            try {
                if (_f && !_f.done && (_a = _e.return)) _a.call(_e);
            }
            finally { if (e_2) throw e_2.error; }
        }
        this.tabsContainer.dispose();
        this.contentContainer.dispose();
    };
    return DockviewGroupPanelModel;
}(lifecycle_1.CompositeDisposable));
exports.DockviewGroupPanelModel = DockviewGroupPanelModel;
