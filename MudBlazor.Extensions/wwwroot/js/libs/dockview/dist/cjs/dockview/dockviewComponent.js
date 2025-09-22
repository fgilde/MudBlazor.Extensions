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
var __assign = (this && this.__assign) || function () {
    __assign = Object.assign || function(t) {
        for (var s, i = 1, n = arguments.length; i < n; i++) {
            s = arguments[i];
            for (var p in s) if (Object.prototype.hasOwnProperty.call(s, p))
                t[p] = s[p];
        }
        return t;
    };
    return __assign.apply(this, arguments);
};
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
exports.DockviewComponent = void 0;
var gridview_1 = require("../gridview/gridview");
var droptarget_1 = require("../dnd/droptarget");
var array_1 = require("../array");
var dockviewPanel_1 = require("./dockviewPanel");
var lifecycle_1 = require("../lifecycle");
var events_1 = require("../events");
var watermark_1 = require("./components/watermark/watermark");
var math_1 = require("../math");
var deserializer_1 = require("./deserializer");
var options_1 = require("./options");
var baseComponentGridview_1 = require("../gridview/baseComponentGridview");
var component_api_1 = require("../api/component.api");
var splitview_1 = require("../splitview/splitview");
var dockviewGroupPanelModel_1 = require("./dockviewGroupPanelModel");
var events_2 = require("./events");
var dockviewGroupPanel_1 = require("./dockviewGroupPanel");
var dockviewPanelModel_1 = require("./dockviewPanelModel");
var dataTransfer_1 = require("../dnd/dataTransfer");
var overlay_1 = require("../overlay/overlay");
var dom_1 = require("../dom");
var dockviewFloatingGroupPanel_1 = require("./dockviewFloatingGroupPanel");
var constants_1 = require("../constants");
var overlayRenderContainer_1 = require("../overlay/overlayRenderContainer");
var popoutWindow_1 = require("../popoutWindow");
var strictEventsSequencing_1 = require("./strictEventsSequencing");
var popupService_1 = require("./components/popupService");
var dropTargetAnchorContainer_1 = require("../dnd/dropTargetAnchorContainer");
var theme_1 = require("./theme");
var DEFAULT_ROOT_OVERLAY_MODEL = {
    activationSize: { type: 'pixels', value: 10 },
    size: { type: 'pixels', value: 20 },
};
function moveGroupWithoutDestroying(options) {
    var activePanel = options.from.activePanel;
    var panels = __spreadArray([], __read(options.from.panels), false).map(function (panel) {
        var removedPanel = options.from.model.removePanel(panel);
        options.from.model.renderContainer.detatch(panel);
        return removedPanel;
    });
    panels.forEach(function (panel) {
        options.to.model.openPanel(panel, {
            skipSetActive: activePanel !== panel,
            skipSetGroupActive: true,
        });
    });
}
var DockviewComponent = /** @class */ (function (_super) {
    __extends(DockviewComponent, _super);
    function DockviewComponent(container, options) {
        var _a, _b, _c;
        var _this = _super.call(this, container, {
            proportionalLayout: true,
            orientation: splitview_1.Orientation.HORIZONTAL,
            styles: options.hideBorders
                ? { separatorBorder: 'transparent' }
                : undefined,
            disableAutoResizing: options.disableAutoResizing,
            locked: options.locked,
            margin: (_b = (_a = options.theme) === null || _a === void 0 ? void 0 : _a.gap) !== null && _b !== void 0 ? _b : 0,
            className: options.className,
        }) || this;
        _this.nextGroupId = (0, math_1.sequentialNumberGenerator)();
        _this._deserializer = new deserializer_1.DefaultDockviewDeserialzier(_this);
        _this._watermark = null;
        _this._onWillDragPanel = new events_1.Emitter();
        _this.onWillDragPanel = _this._onWillDragPanel.event;
        _this._onWillDragGroup = new events_1.Emitter();
        _this.onWillDragGroup = _this._onWillDragGroup.event;
        _this._onDidDrop = new events_1.Emitter();
        _this.onDidDrop = _this._onDidDrop.event;
        _this._onWillDrop = new events_1.Emitter();
        _this.onWillDrop = _this._onWillDrop.event;
        _this._onWillShowOverlay = new events_1.Emitter();
        _this.onWillShowOverlay = _this._onWillShowOverlay.event;
        _this._onUnhandledDragOverEvent = new events_1.Emitter();
        _this.onUnhandledDragOverEvent = _this._onUnhandledDragOverEvent.event;
        _this._onDidRemovePanel = new events_1.Emitter();
        _this.onDidRemovePanel = _this._onDidRemovePanel.event;
        _this._onDidAddPanel = new events_1.Emitter();
        _this.onDidAddPanel = _this._onDidAddPanel.event;
        _this._onDidPopoutGroupSizeChange = new events_1.Emitter();
        _this.onDidPopoutGroupSizeChange = _this._onDidPopoutGroupSizeChange.event;
        _this._onDidPopoutGroupPositionChange = new events_1.Emitter();
        _this.onDidPopoutGroupPositionChange = _this._onDidPopoutGroupPositionChange.event;
        _this._onDidOpenPopoutWindowFail = new events_1.Emitter();
        _this.onDidOpenPopoutWindowFail = _this._onDidOpenPopoutWindowFail.event;
        _this._onDidLayoutFromJSON = new events_1.Emitter();
        _this.onDidLayoutFromJSON = _this._onDidLayoutFromJSON.event;
        _this._onDidActivePanelChange = new events_1.Emitter({ replay: true });
        _this.onDidActivePanelChange = _this._onDidActivePanelChange.event;
        _this._onDidMovePanel = new events_1.Emitter();
        _this.onDidMovePanel = _this._onDidMovePanel.event;
        _this._onDidMaximizedGroupChange = new events_1.Emitter();
        _this.onDidMaximizedGroupChange = _this._onDidMaximizedGroupChange.event;
        _this._floatingGroups = [];
        _this._popoutGroups = [];
        _this._popoutRestorationPromise = Promise.resolve();
        _this._onDidRemoveGroup = new events_1.Emitter();
        _this.onDidRemoveGroup = _this._onDidRemoveGroup.event;
        _this._onDidAddGroup = new events_1.Emitter();
        _this.onDidAddGroup = _this._onDidAddGroup.event;
        _this._onDidOptionsChange = new events_1.Emitter();
        _this.onDidOptionsChange = _this._onDidOptionsChange.event;
        _this._onDidActiveGroupChange = new events_1.Emitter();
        _this.onDidActiveGroupChange = _this._onDidActiveGroupChange.event;
        _this._moving = false;
        _this._options = options;
        _this.popupService = new popupService_1.PopupService(_this.element);
        _this._themeClassnames = new dom_1.Classnames(_this.element);
        _this._api = new component_api_1.DockviewApi(_this);
        _this.rootDropTargetContainer = new dropTargetAnchorContainer_1.DropTargetAnchorContainer(_this.element, { disabled: true });
        _this.overlayRenderContainer = new overlayRenderContainer_1.OverlayRenderContainer(_this.gridview.element, _this);
        _this._rootDropTarget = new droptarget_1.Droptarget(_this.element, {
            className: 'dv-drop-target-edge',
            canDisplayOverlay: function (event, position) {
                var data = (0, dataTransfer_1.getPanelData)();
                if (data) {
                    if (data.viewId !== _this.id) {
                        return false;
                    }
                    if (position === 'center') {
                        // center drop target is only allowed if there are no panels in the grid
                        // floating panels are allowed
                        return _this.gridview.length === 0;
                    }
                    return true;
                }
                if (position === 'center' && _this.gridview.length !== 0) {
                    /**
                     * for external events only show the four-corner drag overlays, disable
                     * the center position so that external drag events can fall through to the group
                     * and panel drop target handlers
                     */
                    return false;
                }
                var firedEvent = new options_1.DockviewUnhandledDragOverEvent(event, 'edge', position, dataTransfer_1.getPanelData);
                _this._onUnhandledDragOverEvent.fire(firedEvent);
                return firedEvent.isAccepted;
            },
            acceptedTargetZones: ['top', 'bottom', 'left', 'right', 'center'],
            overlayModel: (_c = options.rootOverlayModel) !== null && _c !== void 0 ? _c : DEFAULT_ROOT_OVERLAY_MODEL,
            getOverrideTarget: function () { var _a; return (_a = _this.rootDropTargetContainer) === null || _a === void 0 ? void 0 : _a.model; },
        });
        _this.updateDropTargetModel(options);
        (0, dom_1.toggleClass)(_this.gridview.element, 'dv-dockview', true);
        (0, dom_1.toggleClass)(_this.element, 'dv-debug', !!options.debug);
        _this.updateTheme();
        _this.updateWatermark();
        if (options.debug) {
            _this.addDisposables(new strictEventsSequencing_1.StrictEventsSequencing(_this));
        }
        _this.addDisposables(_this.rootDropTargetContainer, _this.overlayRenderContainer, _this._onWillDragPanel, _this._onWillDragGroup, _this._onWillShowOverlay, _this._onDidActivePanelChange, _this._onDidAddPanel, _this._onDidRemovePanel, _this._onDidLayoutFromJSON, _this._onDidDrop, _this._onWillDrop, _this._onDidMovePanel, _this._onDidAddGroup, _this._onDidRemoveGroup, _this._onDidActiveGroupChange, _this._onUnhandledDragOverEvent, _this._onDidMaximizedGroupChange, _this._onDidOptionsChange, _this._onDidPopoutGroupSizeChange, _this._onDidPopoutGroupPositionChange, _this._onDidOpenPopoutWindowFail, _this.onDidViewVisibilityChangeMicroTaskQueue(function () {
            _this.updateWatermark();
        }), _this.onDidAdd(function (event) {
            if (!_this._moving) {
                _this._onDidAddGroup.fire(event);
            }
        }), _this.onDidRemove(function (event) {
            if (!_this._moving) {
                _this._onDidRemoveGroup.fire(event);
            }
        }), _this.onDidActiveChange(function (event) {
            if (!_this._moving) {
                _this._onDidActiveGroupChange.fire(event);
            }
        }), _this.onDidMaximizedChange(function (event) {
            _this._onDidMaximizedGroupChange.fire({
                group: event.panel,
                isMaximized: event.isMaximized,
            });
        }), events_1.Event.any(_this.onDidAdd, _this.onDidRemove)(function () {
            _this.updateWatermark();
        }), events_1.Event.any(_this.onDidAddPanel, _this.onDidRemovePanel, _this.onDidAddGroup, _this.onDidRemove, _this.onDidMovePanel, _this.onDidActivePanelChange, _this.onDidPopoutGroupPositionChange, _this.onDidPopoutGroupSizeChange)(function () {
            _this._bufferOnDidLayoutChange.fire();
        }), lifecycle_1.Disposable.from(function () {
            var e_1, _a, e_2, _b;
            try {
                // iterate over a copy of the array since .dispose() mutates the original array
                for (var _c = __values(__spreadArray([], __read(_this._floatingGroups), false)), _d = _c.next(); !_d.done; _d = _c.next()) {
                    var group = _d.value;
                    group.dispose();
                }
            }
            catch (e_1_1) { e_1 = { error: e_1_1 }; }
            finally {
                try {
                    if (_d && !_d.done && (_a = _c.return)) _a.call(_c);
                }
                finally { if (e_1) throw e_1.error; }
            }
            try {
                // iterate over a copy of the array since .dispose() mutates the original array
                for (var _e = __values(__spreadArray([], __read(_this._popoutGroups), false)), _f = _e.next(); !_f.done; _f = _e.next()) {
                    var group = _f.value;
                    group.disposable.dispose();
                }
            }
            catch (e_2_1) { e_2 = { error: e_2_1 }; }
            finally {
                try {
                    if (_f && !_f.done && (_b = _e.return)) _b.call(_e);
                }
                finally { if (e_2) throw e_2.error; }
            }
        }), _this._rootDropTarget, _this._rootDropTarget.onWillShowOverlay(function (event) {
            if (_this.gridview.length > 0 && event.position === 'center') {
                // option only available when no panels in primary grid
                return;
            }
            _this._onWillShowOverlay.fire(new events_2.WillShowOverlayLocationEvent(event, {
                kind: 'edge',
                panel: undefined,
                api: _this._api,
                group: undefined,
                getData: dataTransfer_1.getPanelData,
            }));
        }), _this._rootDropTarget.onDrop(function (event) {
            var _a;
            var willDropEvent = new dockviewGroupPanelModel_1.DockviewWillDropEvent({
                nativeEvent: event.nativeEvent,
                position: event.position,
                panel: undefined,
                api: _this._api,
                group: undefined,
                getData: dataTransfer_1.getPanelData,
                kind: 'edge',
            });
            _this._onWillDrop.fire(willDropEvent);
            if (willDropEvent.defaultPrevented) {
                return;
            }
            var data = (0, dataTransfer_1.getPanelData)();
            if (data) {
                _this.moveGroupOrPanel({
                    from: {
                        groupId: data.groupId,
                        panelId: (_a = data.panelId) !== null && _a !== void 0 ? _a : undefined,
                    },
                    to: {
                        group: _this.orthogonalize(event.position),
                        position: 'center',
                    },
                });
            }
            else {
                _this._onDidDrop.fire(new dockviewGroupPanelModel_1.DockviewDidDropEvent({
                    nativeEvent: event.nativeEvent,
                    position: event.position,
                    panel: undefined,
                    api: _this._api,
                    group: undefined,
                    getData: dataTransfer_1.getPanelData,
                }));
            }
        }), _this._rootDropTarget);
        return _this;
    }
    Object.defineProperty(DockviewComponent.prototype, "orientation", {
        get: function () {
            return this.gridview.orientation;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewComponent.prototype, "totalPanels", {
        get: function () {
            return this.panels.length;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewComponent.prototype, "panels", {
        get: function () {
            return this.groups.flatMap(function (group) { return group.panels; });
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewComponent.prototype, "options", {
        get: function () {
            return this._options;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewComponent.prototype, "activePanel", {
        get: function () {
            var activeGroup = this.activeGroup;
            if (!activeGroup) {
                return undefined;
            }
            return activeGroup.activePanel;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewComponent.prototype, "renderer", {
        get: function () {
            var _a;
            return (_a = this.options.defaultRenderer) !== null && _a !== void 0 ? _a : 'onlyWhenVisible';
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewComponent.prototype, "api", {
        get: function () {
            return this._api;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewComponent.prototype, "floatingGroups", {
        get: function () {
            return this._floatingGroups;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewComponent.prototype, "popoutRestorationPromise", {
        /**
         * Promise that resolves when all popout groups from the last fromJSON call are restored.
         * Useful for tests that need to wait for delayed popout creation.
         */
        get: function () {
            return this._popoutRestorationPromise;
        },
        enumerable: false,
        configurable: true
    });
    DockviewComponent.prototype.setVisible = function (panel, visible) {
        switch (panel.api.location.type) {
            case 'grid':
                _super.prototype.setVisible.call(this, panel, visible);
                break;
            case 'floating': {
                var item = this.floatingGroups.find(function (floatingGroup) { return floatingGroup.group === panel; });
                if (item) {
                    item.overlay.setVisible(visible);
                    panel.api._onDidVisibilityChange.fire({
                        isVisible: visible,
                    });
                }
                break;
            }
            case 'popout':
                console.warn('dockview: You cannot hide a group that is in a popout window');
                break;
        }
    };
    DockviewComponent.prototype.addPopoutGroup = function (itemToPopout, options) {
        var _this = this;
        var _a, _b, _c, _d, _e;
        if (itemToPopout instanceof dockviewPanel_1.DockviewPanel &&
            itemToPopout.group.size === 1) {
            return this.addPopoutGroup(itemToPopout.group, options);
        }
        var theme = (0, dom_1.getDockviewTheme)(this.gridview.element);
        var element = this.element;
        function getBox() {
            if (options === null || options === void 0 ? void 0 : options.position) {
                return options.position;
            }
            if (itemToPopout instanceof dockviewGroupPanel_1.DockviewGroupPanel) {
                return itemToPopout.element.getBoundingClientRect();
            }
            if (itemToPopout.group) {
                return itemToPopout.group.element.getBoundingClientRect();
            }
            return element.getBoundingClientRect();
        }
        var box = getBox();
        var groupId = (_b = (_a = options === null || options === void 0 ? void 0 : options.overridePopoutGroup) === null || _a === void 0 ? void 0 : _a.id) !== null && _b !== void 0 ? _b : this.getNextGroupId();
        var _window = new popoutWindow_1.PopoutWindow("".concat(this.id, "-").concat(groupId), // unique id
        theme !== null && theme !== void 0 ? theme : '', {
            url: (_e = (_c = options === null || options === void 0 ? void 0 : options.popoutUrl) !== null && _c !== void 0 ? _c : (_d = this.options) === null || _d === void 0 ? void 0 : _d.popoutUrl) !== null && _e !== void 0 ? _e : '/popout.html',
            left: window.screenX + box.left,
            top: window.screenY + box.top,
            width: box.width,
            height: box.height,
            onDidOpen: options === null || options === void 0 ? void 0 : options.onDidOpen,
            onWillClose: options === null || options === void 0 ? void 0 : options.onWillClose,
        });
        var popoutWindowDisposable = new lifecycle_1.CompositeDisposable(_window, _window.onDidClose(function () {
            popoutWindowDisposable.dispose();
        }));
        return _window
            .open()
            .then(function (popoutContainer) {
            var _a;
            if (_window.isDisposed) {
                return false;
            }
            var referenceGroup = (options === null || options === void 0 ? void 0 : options.referenceGroup)
                ? options.referenceGroup
                : itemToPopout instanceof dockviewPanel_1.DockviewPanel
                    ? itemToPopout.group
                    : itemToPopout;
            var referenceLocation = itemToPopout.api.location.type;
            /**
             * The group that is being added doesn't already exist within the DOM, the most likely occurrence
             * of this case is when being called from the `fromJSON(...)` method
             */
            var isGroupAddedToDom = referenceGroup.element.parentElement !== null;
            var group;
            if (!isGroupAddedToDom) {
                group = referenceGroup;
            }
            else if (options === null || options === void 0 ? void 0 : options.overridePopoutGroup) {
                group = options.overridePopoutGroup;
            }
            else {
                group = _this.createGroup({ id: groupId });
                if (popoutContainer) {
                    _this._onDidAddGroup.fire(group);
                }
            }
            if (popoutContainer === null) {
                console.error('dockview: failed to create popout. perhaps you need to allow pop-ups for this website');
                popoutWindowDisposable.dispose();
                _this._onDidOpenPopoutWindowFail.fire();
                // if the popout window was blocked, we need to move the group back to the reference group
                // and set it to visible
                _this.movingLock(function () {
                    return moveGroupWithoutDestroying({
                        from: group,
                        to: referenceGroup,
                    });
                });
                if (!referenceGroup.api.isVisible) {
                    referenceGroup.api.setVisible(true);
                }
                return false;
            }
            var gready = document.createElement('div');
            gready.className = 'dv-overlay-render-container';
            var overlayRenderContainer = new overlayRenderContainer_1.OverlayRenderContainer(gready, _this);
            group.model.renderContainer = overlayRenderContainer;
            group.layout(_window.window.innerWidth, _window.window.innerHeight);
            var floatingBox;
            if (!(options === null || options === void 0 ? void 0 : options.overridePopoutGroup) && isGroupAddedToDom) {
                if (itemToPopout instanceof dockviewPanel_1.DockviewPanel) {
                    _this.movingLock(function () {
                        var panel = referenceGroup.model.removePanel(itemToPopout);
                        group.model.openPanel(panel);
                    });
                }
                else {
                    _this.movingLock(function () {
                        return moveGroupWithoutDestroying({
                            from: referenceGroup,
                            to: group,
                        });
                    });
                    switch (referenceLocation) {
                        case 'grid':
                            referenceGroup.api.setVisible(false);
                            break;
                        case 'floating':
                        case 'popout':
                            floatingBox = (_a = _this._floatingGroups
                                .find(function (value) {
                                return value.group.api.id ===
                                    itemToPopout.api.id;
                            })) === null || _a === void 0 ? void 0 : _a.overlay.toJSON();
                            _this.removeGroup(referenceGroup);
                            break;
                    }
                }
            }
            popoutContainer.classList.add('dv-dockview');
            popoutContainer.style.overflow = 'hidden';
            popoutContainer.appendChild(gready);
            popoutContainer.appendChild(group.element);
            var anchor = document.createElement('div');
            var dropTargetContainer = new dropTargetAnchorContainer_1.DropTargetAnchorContainer(anchor, { disabled: _this.rootDropTargetContainer.disabled });
            popoutContainer.appendChild(anchor);
            group.model.dropTargetContainer = dropTargetContainer;
            group.model.location = {
                type: 'popout',
                getWindow: function () { return _window.window; },
                popoutUrl: options === null || options === void 0 ? void 0 : options.popoutUrl,
            };
            if (isGroupAddedToDom &&
                itemToPopout.api.location.type === 'grid') {
                itemToPopout.api.setVisible(false);
            }
            _this.doSetGroupAndPanelActive(group);
            popoutWindowDisposable.addDisposables(group.api.onDidActiveChange(function (event) {
                var _a;
                if (event.isActive) {
                    (_a = _window.window) === null || _a === void 0 ? void 0 : _a.focus();
                }
            }), group.api.onWillFocus(function () {
                var _a;
                (_a = _window.window) === null || _a === void 0 ? void 0 : _a.focus();
            }));
            var returnedGroup;
            var isValidReferenceGroup = isGroupAddedToDom &&
                referenceGroup &&
                _this.getPanel(referenceGroup.id);
            var value = {
                window: _window,
                popoutGroup: group,
                referenceGroup: isValidReferenceGroup
                    ? referenceGroup.id
                    : undefined,
                disposable: {
                    dispose: function () {
                        popoutWindowDisposable.dispose();
                        return returnedGroup;
                    },
                },
            };
            var _onDidWindowPositionChange = (0, dom_1.onDidWindowMoveEnd)(_window.window);
            popoutWindowDisposable.addDisposables(_onDidWindowPositionChange, (0, dom_1.onDidWindowResizeEnd)(_window.window, function () {
                _this._onDidPopoutGroupSizeChange.fire({
                    width: _window.window.innerWidth,
                    height: _window.window.innerHeight,
                    group: group,
                });
            }), _onDidWindowPositionChange.event(function () {
                _this._onDidPopoutGroupPositionChange.fire({
                    screenX: _window.window.screenX,
                    screenY: _window.window.screenX,
                    group: group,
                });
            }), 
            /**
             * ResizeObserver seems slow here, I do not know why but we don't need it
             * since we can reply on the window resize event as we will occupy the full
             * window dimensions
             */
            (0, events_1.addDisposableListener)(_window.window, 'resize', function () {
                group.layout(_window.window.innerWidth, _window.window.innerHeight);
            }), overlayRenderContainer, lifecycle_1.Disposable.from(function () {
                if (_this.isDisposed) {
                    return; // cleanup may run after instance is disposed
                }
                if (isGroupAddedToDom &&
                    _this.getPanel(referenceGroup.id)) {
                    _this.movingLock(function () {
                        return moveGroupWithoutDestroying({
                            from: group,
                            to: referenceGroup,
                        });
                    });
                    if (!referenceGroup.api.isVisible) {
                        referenceGroup.api.setVisible(true);
                    }
                    if (_this.getPanel(group.id)) {
                        _this.doRemoveGroup(group, {
                            skipPopoutAssociated: true,
                        });
                    }
                }
                else if (_this.getPanel(group.id)) {
                    group.model.renderContainer =
                        _this.overlayRenderContainer;
                    group.model.dropTargetContainer =
                        _this.rootDropTargetContainer;
                    returnedGroup = group;
                    var alreadyRemoved = !_this._popoutGroups.find(function (p) { return p.popoutGroup === group; });
                    if (alreadyRemoved) {
                        /**
                         * If this popout group was explicitly removed then we shouldn't run the additional
                         * steps. To tell if the running of this disposable is the result of this popout group
                         * being explicitly removed we can check if this popout group is still referenced in
                         * the `this._popoutGroups` list.
                         */
                        return;
                    }
                    if (floatingBox) {
                        _this.addFloatingGroup(group, {
                            height: floatingBox.height,
                            width: floatingBox.width,
                            position: floatingBox,
                        });
                    }
                    else {
                        _this.doRemoveGroup(group, {
                            skipDispose: true,
                            skipActive: true,
                            skipPopoutReturn: true,
                        });
                        group.model.location = { type: 'grid' };
                        _this.movingLock(function () {
                            // suppress group add events since the group already exists
                            _this.doAddGroup(group, [0]);
                        });
                    }
                    _this.doSetGroupAndPanelActive(group);
                }
            }));
            _this._popoutGroups.push(value);
            _this.updateWatermark();
            return true;
        })
            .catch(function (err) {
            console.error('dockview: failed to create popout.', err);
            return false;
        });
    };
    DockviewComponent.prototype.addFloatingGroup = function (item, options) {
        var _this = this;
        var _a, _b, _c, _d, _e;
        var group;
        if (item instanceof dockviewPanel_1.DockviewPanel) {
            group = this.createGroup();
            this._onDidAddGroup.fire(group);
            this.movingLock(function () {
                return _this.removePanel(item, {
                    removeEmptyGroup: true,
                    skipDispose: true,
                    skipSetActiveGroup: true,
                });
            });
            this.movingLock(function () {
                return group.model.openPanel(item, { skipSetGroupActive: true });
            });
        }
        else {
            group = item;
            var popoutReferenceGroupId = (_a = this._popoutGroups.find(function (_) { return _.popoutGroup === group; })) === null || _a === void 0 ? void 0 : _a.referenceGroup;
            var popoutReferenceGroup_1 = popoutReferenceGroupId
                ? this.getPanel(popoutReferenceGroupId)
                : undefined;
            var skip = typeof (options === null || options === void 0 ? void 0 : options.skipRemoveGroup) === 'boolean' &&
                options.skipRemoveGroup;
            if (!skip) {
                if (popoutReferenceGroup_1) {
                    this.movingLock(function () {
                        return moveGroupWithoutDestroying({
                            from: item,
                            to: popoutReferenceGroup_1,
                        });
                    });
                    this.doRemoveGroup(item, {
                        skipPopoutReturn: true,
                        skipPopoutAssociated: true,
                    });
                    this.doRemoveGroup(popoutReferenceGroup_1, {
                        skipDispose: true,
                    });
                    group = popoutReferenceGroup_1;
                }
                else {
                    this.doRemoveGroup(item, {
                        skipDispose: true,
                        skipPopoutReturn: true,
                        skipPopoutAssociated: false,
                    });
                }
            }
        }
        function getAnchoredBox() {
            if (options === null || options === void 0 ? void 0 : options.position) {
                var result = {};
                if ('left' in options.position) {
                    result.left = Math.max(options.position.left, 0);
                }
                else if ('right' in options.position) {
                    result.right = Math.max(options.position.right, 0);
                }
                else {
                    result.left = constants_1.DEFAULT_FLOATING_GROUP_POSITION.left;
                }
                if ('top' in options.position) {
                    result.top = Math.max(options.position.top, 0);
                }
                else if ('bottom' in options.position) {
                    result.bottom = Math.max(options.position.bottom, 0);
                }
                else {
                    result.top = constants_1.DEFAULT_FLOATING_GROUP_POSITION.top;
                }
                if (typeof options.width === 'number') {
                    result.width = Math.max(options.width, 0);
                }
                else {
                    result.width = constants_1.DEFAULT_FLOATING_GROUP_POSITION.width;
                }
                if (typeof options.height === 'number') {
                    result.height = Math.max(options.height, 0);
                }
                else {
                    result.height = constants_1.DEFAULT_FLOATING_GROUP_POSITION.height;
                }
                return result;
            }
            return {
                left: typeof (options === null || options === void 0 ? void 0 : options.x) === 'number'
                    ? Math.max(options.x, 0)
                    : constants_1.DEFAULT_FLOATING_GROUP_POSITION.left,
                top: typeof (options === null || options === void 0 ? void 0 : options.y) === 'number'
                    ? Math.max(options.y, 0)
                    : constants_1.DEFAULT_FLOATING_GROUP_POSITION.top,
                width: typeof (options === null || options === void 0 ? void 0 : options.width) === 'number'
                    ? Math.max(options.width, 0)
                    : constants_1.DEFAULT_FLOATING_GROUP_POSITION.width,
                height: typeof (options === null || options === void 0 ? void 0 : options.height) === 'number'
                    ? Math.max(options.height, 0)
                    : constants_1.DEFAULT_FLOATING_GROUP_POSITION.height,
            };
        }
        var anchoredBox = getAnchoredBox();
        var overlay = new overlay_1.Overlay(__assign(__assign({ container: this.gridview.element, content: group.element }, anchoredBox), { minimumInViewportWidth: this.options.floatingGroupBounds === 'boundedWithinViewport'
                ? undefined
                : (_c = (_b = this.options.floatingGroupBounds) === null || _b === void 0 ? void 0 : _b.minimumWidthWithinViewport) !== null && _c !== void 0 ? _c : constants_1.DEFAULT_FLOATING_GROUP_OVERFLOW_SIZE, minimumInViewportHeight: this.options.floatingGroupBounds === 'boundedWithinViewport'
                ? undefined
                : (_e = (_d = this.options.floatingGroupBounds) === null || _d === void 0 ? void 0 : _d.minimumHeightWithinViewport) !== null && _e !== void 0 ? _e : constants_1.DEFAULT_FLOATING_GROUP_OVERFLOW_SIZE }));
        var el = group.element.querySelector('.dv-void-container');
        if (!el) {
            throw new Error('failed to find drag handle');
        }
        overlay.setupDrag(el, {
            inDragMode: typeof (options === null || options === void 0 ? void 0 : options.inDragMode) === 'boolean'
                ? options.inDragMode
                : false,
        });
        var floatingGroupPanel = new dockviewFloatingGroupPanel_1.DockviewFloatingGroupPanel(group, overlay);
        var disposable = new lifecycle_1.CompositeDisposable(group.api.onDidActiveChange(function (event) {
            if (event.isActive) {
                overlay.bringToFront();
            }
        }), (0, dom_1.watchElementResize)(group.element, function (entry) {
            var _a = entry.contentRect, width = _a.width, height = _a.height;
            group.layout(width, height); // let the group know it's size is changing so it can fire events to the panel
        }));
        floatingGroupPanel.addDisposables(overlay.onDidChange(function () {
            // this is either a resize or a move
            // to inform the panels .layout(...) the group with it's current size
            // don't care about resize since the above watcher handles that
            group.layout(group.width, group.height);
        }), overlay.onDidChangeEnd(function () {
            _this._bufferOnDidLayoutChange.fire();
        }), group.onDidChange(function (event) {
            overlay.setBounds({
                height: event === null || event === void 0 ? void 0 : event.height,
                width: event === null || event === void 0 ? void 0 : event.width,
            });
        }), {
            dispose: function () {
                disposable.dispose();
                (0, array_1.remove)(_this._floatingGroups, floatingGroupPanel);
                group.model.location = { type: 'grid' };
                _this.updateWatermark();
            },
        });
        this._floatingGroups.push(floatingGroupPanel);
        group.model.location = { type: 'floating' };
        if (!(options === null || options === void 0 ? void 0 : options.skipActiveGroup)) {
            this.doSetGroupAndPanelActive(group);
        }
        this.updateWatermark();
    };
    DockviewComponent.prototype.orthogonalize = function (position, options) {
        this.gridview.normalize();
        switch (position) {
            case 'top':
            case 'bottom':
                if (this.gridview.orientation === splitview_1.Orientation.HORIZONTAL) {
                    // we need to add to a vertical splitview but the current root is a horizontal splitview.
                    // insert a vertical splitview at the root level and add the existing view as a child
                    this.gridview.insertOrthogonalSplitviewAtRoot();
                }
                break;
            case 'left':
            case 'right':
                if (this.gridview.orientation === splitview_1.Orientation.VERTICAL) {
                    // we need to add to a horizontal splitview but the current root is a vertical splitview.
                    // insert a horiziontal splitview at the root level and add the existing view as a child
                    this.gridview.insertOrthogonalSplitviewAtRoot();
                }
                break;
            default:
                break;
        }
        switch (position) {
            case 'top':
            case 'left':
            case 'center':
                return this.createGroupAtLocation([0], undefined, options); // insert into first position
            case 'bottom':
            case 'right':
                return this.createGroupAtLocation([this.gridview.length], undefined, options); // insert into last position
            default:
                throw new Error("unsupported position ".concat(position));
        }
    };
    DockviewComponent.prototype.updateOptions = function (options) {
        var e_3, _a;
        var _b, _c;
        _super.prototype.updateOptions.call(this, options);
        if ('floatingGroupBounds' in options) {
            try {
                for (var _d = __values(this._floatingGroups), _e = _d.next(); !_e.done; _e = _d.next()) {
                    var group = _e.value;
                    switch (options.floatingGroupBounds) {
                        case 'boundedWithinViewport':
                            group.overlay.minimumInViewportHeight = undefined;
                            group.overlay.minimumInViewportWidth = undefined;
                            break;
                        case undefined:
                            group.overlay.minimumInViewportHeight =
                                constants_1.DEFAULT_FLOATING_GROUP_OVERFLOW_SIZE;
                            group.overlay.minimumInViewportWidth =
                                constants_1.DEFAULT_FLOATING_GROUP_OVERFLOW_SIZE;
                            break;
                        default:
                            group.overlay.minimumInViewportHeight =
                                (_b = options.floatingGroupBounds) === null || _b === void 0 ? void 0 : _b.minimumHeightWithinViewport;
                            group.overlay.minimumInViewportWidth =
                                (_c = options.floatingGroupBounds) === null || _c === void 0 ? void 0 : _c.minimumWidthWithinViewport;
                    }
                    group.overlay.setBounds();
                }
            }
            catch (e_3_1) { e_3 = { error: e_3_1 }; }
            finally {
                try {
                    if (_e && !_e.done && (_a = _d.return)) _a.call(_d);
                }
                finally { if (e_3) throw e_3.error; }
            }
        }
        this.updateDropTargetModel(options);
        var oldDisableDnd = this.options.disableDnd;
        this._options = __assign(__assign({}, this.options), options);
        var newDisableDnd = this.options.disableDnd;
        if (oldDisableDnd !== newDisableDnd) {
            this.updateDragAndDropState();
        }
        if ('theme' in options) {
            this.updateTheme();
        }
        this.layout(this.gridview.width, this.gridview.height, true);
    };
    DockviewComponent.prototype.layout = function (width, height, forceResize) {
        var e_4, _a;
        _super.prototype.layout.call(this, width, height, forceResize);
        if (this._floatingGroups) {
            try {
                for (var _b = __values(this._floatingGroups), _c = _b.next(); !_c.done; _c = _b.next()) {
                    var floating = _c.value;
                    // ensure floting groups stay within visible boundaries
                    floating.overlay.setBounds();
                }
            }
            catch (e_4_1) { e_4 = { error: e_4_1 }; }
            finally {
                try {
                    if (_c && !_c.done && (_a = _b.return)) _a.call(_b);
                }
                finally { if (e_4) throw e_4.error; }
            }
        }
    };
    DockviewComponent.prototype.updateDragAndDropState = function () {
        var e_5, _a;
        try {
            // Update draggable state for all tabs and void containers
            for (var _b = __values(this.groups), _c = _b.next(); !_c.done; _c = _b.next()) {
                var group = _c.value;
                group.model.updateDragAndDropState();
            }
        }
        catch (e_5_1) { e_5 = { error: e_5_1 }; }
        finally {
            try {
                if (_c && !_c.done && (_a = _b.return)) _a.call(_b);
            }
            finally { if (e_5) throw e_5.error; }
        }
    };
    DockviewComponent.prototype.focus = function () {
        var _a;
        (_a = this.activeGroup) === null || _a === void 0 ? void 0 : _a.focus();
    };
    DockviewComponent.prototype.getGroupPanel = function (id) {
        return this.panels.find(function (panel) { return panel.id === id; });
    };
    DockviewComponent.prototype.setActivePanel = function (panel) {
        panel.group.model.openPanel(panel);
        this.doSetGroupAndPanelActive(panel.group);
    };
    DockviewComponent.prototype.moveToNext = function (options) {
        var _a;
        if (options === void 0) { options = {}; }
        if (!options.group) {
            if (!this.activeGroup) {
                return;
            }
            options.group = this.activeGroup;
        }
        if (options.includePanel && options.group) {
            if (options.group.activePanel !==
                options.group.panels[options.group.panels.length - 1]) {
                options.group.model.moveToNext({ suppressRoll: true });
                return;
            }
        }
        var location = (0, gridview_1.getGridLocation)(options.group.element);
        var next = (_a = this.gridview.next(location)) === null || _a === void 0 ? void 0 : _a.view;
        this.doSetGroupAndPanelActive(next);
    };
    DockviewComponent.prototype.moveToPrevious = function (options) {
        var _a;
        if (options === void 0) { options = {}; }
        if (!options.group) {
            if (!this.activeGroup) {
                return;
            }
            options.group = this.activeGroup;
        }
        if (options.includePanel && options.group) {
            if (options.group.activePanel !== options.group.panels[0]) {
                options.group.model.moveToPrevious({ suppressRoll: true });
                return;
            }
        }
        var location = (0, gridview_1.getGridLocation)(options.group.element);
        var next = (_a = this.gridview.previous(location)) === null || _a === void 0 ? void 0 : _a.view;
        if (next) {
            this.doSetGroupAndPanelActive(next);
        }
    };
    /**
     * Serialize the current state of the layout
     *
     * @returns A JSON respresentation of the layout
     */
    DockviewComponent.prototype.toJSON = function () {
        var _a;
        var data = this.gridview.serialize();
        var panels = this.panels.reduce(function (collection, panel) {
            collection[panel.id] = panel.toJSON();
            return collection;
        }, {});
        var floats = this._floatingGroups.map(function (group) {
            return {
                data: group.group.toJSON(),
                position: group.overlay.toJSON(),
            };
        });
        var popoutGroups = this._popoutGroups.map(function (group) {
            return {
                data: group.popoutGroup.toJSON(),
                gridReferenceGroup: group.referenceGroup,
                position: group.window.dimensions(),
                url: group.popoutGroup.api.location.type === 'popout'
                    ? group.popoutGroup.api.location.popoutUrl
                    : undefined,
            };
        });
        var result = {
            grid: data,
            panels: panels,
            activeGroup: (_a = this.activeGroup) === null || _a === void 0 ? void 0 : _a.id,
        };
        if (floats.length > 0) {
            result.floatingGroups = floats;
        }
        if (popoutGroups.length > 0) {
            result.popoutGroups = popoutGroups;
        }
        return result;
    };
    DockviewComponent.prototype.fromJSON = function (data) {
        var e_6, _a, e_7, _b, e_8, _c, e_9, _d, e_10, _e, e_11, _f;
        var _this = this;
        var _g, _h;
        this.clear();
        if (typeof data !== 'object' || data === null) {
            throw new Error('serialized layout must be a non-null object');
        }
        var grid = data.grid, panels = data.panels, activeGroup = data.activeGroup;
        if (grid.root.type !== 'branch' || !Array.isArray(grid.root.data)) {
            throw new Error('root must be of type branch');
        }
        try {
            // take note of the existing dimensions
            var width = this.width;
            var height = this.height;
            var createGroupFromSerializedState_1 = function (data) {
                var e_12, _a;
                var id = data.id, locked = data.locked, hideHeader = data.hideHeader, views = data.views, activeView = data.activeView;
                if (typeof id !== 'string') {
                    throw new Error('group id must be of type string');
                }
                var group = _this.createGroup({
                    id: id,
                    locked: !!locked,
                    hideHeader: !!hideHeader,
                });
                _this._onDidAddGroup.fire(group);
                var createdPanels = [];
                try {
                    for (var views_1 = __values(views), views_1_1 = views_1.next(); !views_1_1.done; views_1_1 = views_1.next()) {
                        var child = views_1_1.value;
                        /**
                         * Run the deserializer step seperately since this may fail to due corrupted external state.
                         * In running this section first we avoid firing lots of 'add' events in the event of a failure
                         * due to a corruption of input data.
                         */
                        var panel = _this._deserializer.fromJSON(panels[child], group);
                        createdPanels.push(panel);
                    }
                }
                catch (e_12_1) { e_12 = { error: e_12_1 }; }
                finally {
                    try {
                        if (views_1_1 && !views_1_1.done && (_a = views_1.return)) _a.call(views_1);
                    }
                    finally { if (e_12) throw e_12.error; }
                }
                for (var i = 0; i < views.length; i++) {
                    var panel = createdPanels[i];
                    var isActive = typeof activeView === 'string' &&
                        activeView === panel.id;
                    group.model.openPanel(panel, {
                        skipSetActive: !isActive,
                        skipSetGroupActive: true,
                    });
                }
                if (!group.activePanel && group.panels.length > 0) {
                    group.model.openPanel(group.panels[group.panels.length - 1], {
                        skipSetGroupActive: true,
                    });
                }
                return group;
            };
            this.gridview.deserialize(grid, {
                fromJSON: function (node) {
                    return createGroupFromSerializedState_1(node.data);
                },
            });
            this.layout(width, height, true);
            var serializedFloatingGroups = (_g = data.floatingGroups) !== null && _g !== void 0 ? _g : [];
            try {
                for (var serializedFloatingGroups_1 = __values(serializedFloatingGroups), serializedFloatingGroups_1_1 = serializedFloatingGroups_1.next(); !serializedFloatingGroups_1_1.done; serializedFloatingGroups_1_1 = serializedFloatingGroups_1.next()) {
                    var serializedFloatingGroup = serializedFloatingGroups_1_1.value;
                    var data_1 = serializedFloatingGroup.data, position = serializedFloatingGroup.position;
                    var group = createGroupFromSerializedState_1(data_1);
                    this.addFloatingGroup(group, {
                        position: position,
                        width: position.width,
                        height: position.height,
                        skipRemoveGroup: true,
                        inDragMode: false,
                    });
                }
            }
            catch (e_6_1) { e_6 = { error: e_6_1 }; }
            finally {
                try {
                    if (serializedFloatingGroups_1_1 && !serializedFloatingGroups_1_1.done && (_a = serializedFloatingGroups_1.return)) _a.call(serializedFloatingGroups_1);
                }
                finally { if (e_6) throw e_6.error; }
            }
            var serializedPopoutGroups = (_h = data.popoutGroups) !== null && _h !== void 0 ? _h : [];
            // Create a promise that resolves when all popout groups are created
            var popoutPromises_1 = [];
            // Queue popup group creation with delays to avoid browser blocking
            serializedPopoutGroups.forEach(function (serializedPopoutGroup, index) {
                var data = serializedPopoutGroup.data, position = serializedPopoutGroup.position, gridReferenceGroup = serializedPopoutGroup.gridReferenceGroup, url = serializedPopoutGroup.url;
                var group = createGroupFromSerializedState_1(data);
                // Add a small delay for each popup after the first to avoid browser popup blocking
                var popoutPromise = new Promise(function (resolve) {
                    setTimeout(function () {
                        _this.addPopoutGroup(group, {
                            position: position !== null && position !== void 0 ? position : undefined,
                            overridePopoutGroup: gridReferenceGroup ? group : undefined,
                            referenceGroup: gridReferenceGroup
                                ? _this.getPanel(gridReferenceGroup)
                                : undefined,
                            popoutUrl: url,
                        });
                        resolve();
                    }, index * constants_1.DESERIALIZATION_POPOUT_DELAY_MS); // 100ms delay between each popup
                });
                popoutPromises_1.push(popoutPromise);
            });
            // Store the promise for tests to wait on
            this._popoutRestorationPromise = Promise.all(popoutPromises_1).then(function () { return void 0; });
            try {
                for (var _j = __values(this._floatingGroups), _k = _j.next(); !_k.done; _k = _j.next()) {
                    var floatingGroup = _k.value;
                    floatingGroup.overlay.setBounds();
                }
            }
            catch (e_7_1) { e_7 = { error: e_7_1 }; }
            finally {
                try {
                    if (_k && !_k.done && (_b = _j.return)) _b.call(_j);
                }
                finally { if (e_7) throw e_7.error; }
            }
            if (typeof activeGroup === 'string') {
                var panel = this.getPanel(activeGroup);
                if (panel) {
                    this.doSetGroupAndPanelActive(panel);
                }
            }
        }
        catch (err) {
            console.error('dockview: failed to deserialize layout. Reverting changes', err);
            try {
                /**
                 * Takes all the successfully created groups and remove all of their panels.
                 */
                for (var _l = __values(this.groups), _m = _l.next(); !_m.done; _m = _l.next()) {
                    var group = _m.value;
                    try {
                        for (var _o = (e_9 = void 0, __values(group.panels)), _p = _o.next(); !_p.done; _p = _o.next()) {
                            var panel = _p.value;
                            this.removePanel(panel, {
                                removeEmptyGroup: false,
                                skipDispose: false,
                            });
                        }
                    }
                    catch (e_9_1) { e_9 = { error: e_9_1 }; }
                    finally {
                        try {
                            if (_p && !_p.done && (_d = _o.return)) _d.call(_o);
                        }
                        finally { if (e_9) throw e_9.error; }
                    }
                }
            }
            catch (e_8_1) { e_8 = { error: e_8_1 }; }
            finally {
                try {
                    if (_m && !_m.done && (_c = _l.return)) _c.call(_l);
                }
                finally { if (e_8) throw e_8.error; }
            }
            try {
                /**
                 * To remove a group we cannot call this.removeGroup(...) since this makes assumptions about
                 * the underlying HTMLElement existing in the Gridview.
                 */
                for (var _q = __values(this.groups), _r = _q.next(); !_r.done; _r = _q.next()) {
                    var group = _r.value;
                    group.dispose();
                    this._groups.delete(group.id);
                    this._onDidRemoveGroup.fire(group);
                }
            }
            catch (e_10_1) { e_10 = { error: e_10_1 }; }
            finally {
                try {
                    if (_r && !_r.done && (_e = _q.return)) _e.call(_q);
                }
                finally { if (e_10) throw e_10.error; }
            }
            try {
                // iterate over a reassigned array since original array will be modified
                for (var _s = __values(__spreadArray([], __read(this._floatingGroups), false)), _t = _s.next(); !_t.done; _t = _s.next()) {
                    var floatingGroup = _t.value;
                    floatingGroup.dispose();
                }
            }
            catch (e_11_1) { e_11 = { error: e_11_1 }; }
            finally {
                try {
                    if (_t && !_t.done && (_f = _s.return)) _f.call(_s);
                }
                finally { if (e_11) throw e_11.error; }
            }
            // fires clean-up events and clears the underlying HTML gridview.
            this.clear();
            /**
             * even though we have cleaned-up we still want to inform the caller of their error
             * and we'll do this through re-throwing the original error since afterall you would
             * expect trying to load a corrupted layout to result in an error and not silently fail...
             */
            throw err;
        }
        this.updateWatermark();
        // Force position updates for always visible panels after DOM layout is complete
        requestAnimationFrame(function () {
            _this.overlayRenderContainer.updateAllPositions();
        });
        this._onDidLayoutFromJSON.fire();
    };
    DockviewComponent.prototype.clear = function () {
        var e_13, _a;
        var groups = Array.from(this._groups.values()).map(function (_) { return _.value; });
        var hasActiveGroup = !!this.activeGroup;
        try {
            for (var groups_1 = __values(groups), groups_1_1 = groups_1.next(); !groups_1_1.done; groups_1_1 = groups_1.next()) {
                var group = groups_1_1.value;
                // remove the group will automatically remove the panels
                this.removeGroup(group, { skipActive: true });
            }
        }
        catch (e_13_1) { e_13 = { error: e_13_1 }; }
        finally {
            try {
                if (groups_1_1 && !groups_1_1.done && (_a = groups_1.return)) _a.call(groups_1);
            }
            finally { if (e_13) throw e_13.error; }
        }
        if (hasActiveGroup) {
            this.doSetGroupAndPanelActive(undefined);
        }
        this.gridview.clear();
    };
    DockviewComponent.prototype.closeAllGroups = function () {
        var e_14, _a;
        try {
            for (var _b = __values(this._groups.entries()), _c = _b.next(); !_c.done; _c = _b.next()) {
                var entry = _c.value;
                var _d = __read(entry, 2), _ = _d[0], group = _d[1];
                group.value.model.closeAllPanels();
            }
        }
        catch (e_14_1) { e_14 = { error: e_14_1 }; }
        finally {
            try {
                if (_c && !_c.done && (_a = _b.return)) _a.call(_b);
            }
            finally { if (e_14) throw e_14.error; }
        }
    };
    DockviewComponent.prototype.addPanel = function (options) {
        var _a, _b;
        if (this.panels.find(function (_) { return _.id === options.id; })) {
            throw new Error("panel with id ".concat(options.id, " already exists"));
        }
        var referenceGroup;
        if (options.position && options.floating) {
            throw new Error('you can only provide one of: position, floating as arguments to .addPanel(...)');
        }
        var initial = {
            width: options.initialWidth,
            height: options.initialHeight,
        };
        var index;
        if (options.position) {
            if ((0, options_1.isPanelOptionsWithPanel)(options.position)) {
                var referencePanel = typeof options.position.referencePanel === 'string'
                    ? this.getGroupPanel(options.position.referencePanel)
                    : options.position.referencePanel;
                index = options.position.index;
                if (!referencePanel) {
                    throw new Error("referencePanel '".concat(options.position.referencePanel, "' does not exist"));
                }
                referenceGroup = this.findGroup(referencePanel);
            }
            else if ((0, options_1.isPanelOptionsWithGroup)(options.position)) {
                referenceGroup =
                    typeof options.position.referenceGroup === 'string'
                        ? (_a = this._groups.get(options.position.referenceGroup)) === null || _a === void 0 ? void 0 : _a.value
                        : options.position.referenceGroup;
                index = options.position.index;
                if (!referenceGroup) {
                    throw new Error("referenceGroup '".concat(options.position.referenceGroup, "' does not exist"));
                }
            }
            else {
                var group = this.orthogonalize((0, droptarget_1.directionToPosition)(options.position.direction));
                var panel_1 = this.createPanel(options, group);
                group.model.openPanel(panel_1, {
                    skipSetActive: options.inactive,
                    skipSetGroupActive: options.inactive,
                    index: index,
                });
                if (!options.inactive) {
                    this.doSetGroupAndPanelActive(group);
                }
                group.api.setSize({
                    height: initial === null || initial === void 0 ? void 0 : initial.height,
                    width: initial === null || initial === void 0 ? void 0 : initial.width,
                });
                return panel_1;
            }
        }
        else {
            referenceGroup = this.activeGroup;
        }
        var panel;
        if (referenceGroup) {
            var target = (0, baseComponentGridview_1.toTarget)(((_b = options.position) === null || _b === void 0 ? void 0 : _b.direction) || 'within');
            if (options.floating) {
                var group = this.createGroup();
                this._onDidAddGroup.fire(group);
                var floatingGroupOptions = typeof options.floating === 'object' &&
                    options.floating !== null
                    ? options.floating
                    : {};
                this.addFloatingGroup(group, __assign(__assign({}, floatingGroupOptions), { inDragMode: false, skipRemoveGroup: true, skipActiveGroup: true }));
                panel = this.createPanel(options, group);
                group.model.openPanel(panel, {
                    skipSetActive: options.inactive,
                    skipSetGroupActive: options.inactive,
                    index: index,
                });
            }
            else if (referenceGroup.api.location.type === 'floating' ||
                target === 'center') {
                panel = this.createPanel(options, referenceGroup);
                referenceGroup.model.openPanel(panel, {
                    skipSetActive: options.inactive,
                    skipSetGroupActive: options.inactive,
                    index: index,
                });
                referenceGroup.api.setSize({
                    width: initial === null || initial === void 0 ? void 0 : initial.width,
                    height: initial === null || initial === void 0 ? void 0 : initial.height,
                });
                if (!options.inactive) {
                    this.doSetGroupAndPanelActive(referenceGroup);
                }
            }
            else {
                var location_1 = (0, gridview_1.getGridLocation)(referenceGroup.element);
                var relativeLocation = (0, gridview_1.getRelativeLocation)(this.gridview.orientation, location_1, target);
                var group = this.createGroupAtLocation(relativeLocation, this.orientationAtLocation(relativeLocation) ===
                    splitview_1.Orientation.VERTICAL
                    ? initial === null || initial === void 0 ? void 0 : initial.height
                    : initial === null || initial === void 0 ? void 0 : initial.width);
                panel = this.createPanel(options, group);
                group.model.openPanel(panel, {
                    skipSetActive: options.inactive,
                    skipSetGroupActive: options.inactive,
                    index: index,
                });
                if (!options.inactive) {
                    this.doSetGroupAndPanelActive(group);
                }
            }
        }
        else if (options.floating) {
            var group = this.createGroup();
            this._onDidAddGroup.fire(group);
            var coordinates = typeof options.floating === 'object' &&
                options.floating !== null
                ? options.floating
                : {};
            this.addFloatingGroup(group, __assign(__assign({}, coordinates), { inDragMode: false, skipRemoveGroup: true, skipActiveGroup: true }));
            panel = this.createPanel(options, group);
            group.model.openPanel(panel, {
                skipSetActive: options.inactive,
                skipSetGroupActive: options.inactive,
                index: index,
            });
        }
        else {
            var group = this.createGroupAtLocation([0], this.gridview.orientation === splitview_1.Orientation.VERTICAL
                ? initial === null || initial === void 0 ? void 0 : initial.height
                : initial === null || initial === void 0 ? void 0 : initial.width);
            panel = this.createPanel(options, group);
            group.model.openPanel(panel, {
                skipSetActive: options.inactive,
                skipSetGroupActive: options.inactive,
                index: index,
            });
            if (!options.inactive) {
                this.doSetGroupAndPanelActive(group);
            }
        }
        return panel;
    };
    DockviewComponent.prototype.removePanel = function (panel, options) {
        if (options === void 0) { options = {
            removeEmptyGroup: true,
        }; }
        var group = panel.group;
        if (!group) {
            throw new Error("cannot remove panel ".concat(panel.id, ". it's missing a group."));
        }
        group.model.removePanel(panel, {
            skipSetActiveGroup: options.skipSetActiveGroup,
        });
        if (!options.skipDispose) {
            panel.group.model.renderContainer.detatch(panel);
            panel.dispose();
        }
        if (group.size === 0 && options.removeEmptyGroup) {
            this.removeGroup(group, { skipActive: options.skipSetActiveGroup });
        }
    };
    DockviewComponent.prototype.createWatermarkComponent = function () {
        if (this.options.createWatermarkComponent) {
            return this.options.createWatermarkComponent();
        }
        return new watermark_1.Watermark();
    };
    DockviewComponent.prototype.updateWatermark = function () {
        var _a, _b;
        if (this.groups.filter(function (x) { return x.api.location.type === 'grid' && x.api.isVisible; }).length === 0) {
            if (!this._watermark) {
                this._watermark = this.createWatermarkComponent();
                this._watermark.init({
                    containerApi: new component_api_1.DockviewApi(this),
                });
                var watermarkContainer = document.createElement('div');
                watermarkContainer.className = 'dv-watermark-container';
                (0, dom_1.addTestId)(watermarkContainer, 'watermark-component');
                watermarkContainer.appendChild(this._watermark.element);
                this.gridview.element.appendChild(watermarkContainer);
            }
        }
        else if (this._watermark) {
            this._watermark.element.parentElement.remove();
            (_b = (_a = this._watermark).dispose) === null || _b === void 0 ? void 0 : _b.call(_a);
            this._watermark = null;
        }
    };
    DockviewComponent.prototype.addGroup = function (options) {
        var _a;
        if (options) {
            var referenceGroup = void 0;
            if ((0, options_1.isGroupOptionsWithPanel)(options)) {
                var referencePanel = typeof options.referencePanel === 'string'
                    ? this.panels.find(function (panel) { return panel.id === options.referencePanel; })
                    : options.referencePanel;
                if (!referencePanel) {
                    throw new Error("reference panel ".concat(options.referencePanel, " does not exist"));
                }
                referenceGroup = this.findGroup(referencePanel);
                if (!referenceGroup) {
                    throw new Error("reference group for reference panel ".concat(options.referencePanel, " does not exist"));
                }
            }
            else if ((0, options_1.isGroupOptionsWithGroup)(options)) {
                referenceGroup =
                    typeof options.referenceGroup === 'string'
                        ? (_a = this._groups.get(options.referenceGroup)) === null || _a === void 0 ? void 0 : _a.value
                        : options.referenceGroup;
                if (!referenceGroup) {
                    throw new Error("reference group ".concat(options.referenceGroup, " does not exist"));
                }
            }
            else {
                var group_1 = this.orthogonalize((0, droptarget_1.directionToPosition)(options.direction), options);
                if (!options.skipSetActive) {
                    this.doSetGroupAndPanelActive(group_1);
                }
                return group_1;
            }
            var target = (0, baseComponentGridview_1.toTarget)(options.direction || 'within');
            var location_2 = (0, gridview_1.getGridLocation)(referenceGroup.element);
            var relativeLocation = (0, gridview_1.getRelativeLocation)(this.gridview.orientation, location_2, target);
            var group = this.createGroup(options);
            var size = this.getLocationOrientation(relativeLocation) ===
                splitview_1.Orientation.VERTICAL
                ? options.initialHeight
                : options.initialWidth;
            this.doAddGroup(group, relativeLocation, size);
            if (!options.skipSetActive) {
                this.doSetGroupAndPanelActive(group);
            }
            return group;
        }
        else {
            var group = this.createGroup(options);
            this.doAddGroup(group);
            this.doSetGroupAndPanelActive(group);
            return group;
        }
    };
    DockviewComponent.prototype.getLocationOrientation = function (location) {
        return location.length % 2 == 0 &&
            this.gridview.orientation === splitview_1.Orientation.HORIZONTAL
            ? splitview_1.Orientation.HORIZONTAL
            : splitview_1.Orientation.VERTICAL;
    };
    DockviewComponent.prototype.removeGroup = function (group, options) {
        this.doRemoveGroup(group, options);
    };
    DockviewComponent.prototype.doRemoveGroup = function (group, options) {
        var e_15, _a;
        var _b;
        var panels = __spreadArray([], __read(group.panels), false); // reassign since group panels will mutate
        if (!(options === null || options === void 0 ? void 0 : options.skipDispose)) {
            try {
                for (var panels_1 = __values(panels), panels_1_1 = panels_1.next(); !panels_1_1.done; panels_1_1 = panels_1.next()) {
                    var panel = panels_1_1.value;
                    this.removePanel(panel, {
                        removeEmptyGroup: false,
                        skipDispose: (_b = options === null || options === void 0 ? void 0 : options.skipDispose) !== null && _b !== void 0 ? _b : false,
                    });
                }
            }
            catch (e_15_1) { e_15 = { error: e_15_1 }; }
            finally {
                try {
                    if (panels_1_1 && !panels_1_1.done && (_a = panels_1.return)) _a.call(panels_1);
                }
                finally { if (e_15) throw e_15.error; }
            }
        }
        var activePanel = this.activePanel;
        if (group.api.location.type === 'floating') {
            var floatingGroup = this._floatingGroups.find(function (_) { return _.group === group; });
            if (floatingGroup) {
                if (!(options === null || options === void 0 ? void 0 : options.skipDispose)) {
                    floatingGroup.group.dispose();
                    this._groups.delete(group.id);
                    this._onDidRemoveGroup.fire(group);
                }
                (0, array_1.remove)(this._floatingGroups, floatingGroup);
                floatingGroup.dispose();
                if (!(options === null || options === void 0 ? void 0 : options.skipActive) && this._activeGroup === group) {
                    var groups = Array.from(this._groups.values());
                    this.doSetGroupAndPanelActive(groups.length > 0 ? groups[0].value : undefined);
                }
                return floatingGroup.group;
            }
            throw new Error('failed to find floating group');
        }
        if (group.api.location.type === 'popout') {
            var selectedGroup = this._popoutGroups.find(function (_) { return _.popoutGroup === group; });
            if (selectedGroup) {
                if (!(options === null || options === void 0 ? void 0 : options.skipDispose)) {
                    if (!(options === null || options === void 0 ? void 0 : options.skipPopoutAssociated)) {
                        var refGroup = selectedGroup.referenceGroup
                            ? this.getPanel(selectedGroup.referenceGroup)
                            : undefined;
                        if (refGroup && refGroup.panels.length === 0) {
                            this.removeGroup(refGroup);
                        }
                    }
                    selectedGroup.popoutGroup.dispose();
                    this._groups.delete(group.id);
                    this._onDidRemoveGroup.fire(group);
                }
                (0, array_1.remove)(this._popoutGroups, selectedGroup);
                var removedGroup = selectedGroup.disposable.dispose();
                if (!(options === null || options === void 0 ? void 0 : options.skipPopoutReturn) && removedGroup) {
                    this.doAddGroup(removedGroup, [0]);
                    this.doSetGroupAndPanelActive(removedGroup);
                }
                if (!(options === null || options === void 0 ? void 0 : options.skipActive) && this._activeGroup === group) {
                    var groups = Array.from(this._groups.values());
                    this.doSetGroupAndPanelActive(groups.length > 0 ? groups[0].value : undefined);
                }
                this.updateWatermark();
                return selectedGroup.popoutGroup;
            }
            throw new Error('failed to find popout group');
        }
        var re = _super.prototype.doRemoveGroup.call(this, group, options);
        if (!(options === null || options === void 0 ? void 0 : options.skipActive)) {
            if (this.activePanel !== activePanel) {
                this._onDidActivePanelChange.fire(this.activePanel);
            }
        }
        return re;
    };
    DockviewComponent.prototype.movingLock = function (func) {
        var isMoving = this._moving;
        try {
            this._moving = true;
            return func();
        }
        finally {
            this._moving = isMoving;
        }
    };
    DockviewComponent.prototype.moveGroupOrPanel = function (options) {
        var _this = this;
        var _a;
        var destinationGroup = options.to.group;
        var sourceGroupId = options.from.groupId;
        var sourceItemId = options.from.panelId;
        var destinationTarget = options.to.position;
        var destinationIndex = options.to.index;
        var sourceGroup = sourceGroupId
            ? (_a = this._groups.get(sourceGroupId)) === null || _a === void 0 ? void 0 : _a.value
            : undefined;
        if (!sourceGroup) {
            throw new Error("Failed to find group id ".concat(sourceGroupId));
        }
        if (sourceItemId === undefined) {
            /**
             * Moving an entire group into another group
             */
            this.moveGroup({
                from: { group: sourceGroup },
                to: {
                    group: destinationGroup,
                    position: destinationTarget,
                },
                skipSetActive: options.skipSetActive,
            });
            return;
        }
        if (!destinationTarget || destinationTarget === 'center') {
            /**
             * Dropping a panel within another group
             */
            var removedPanel_1 = this.movingLock(function () {
                return sourceGroup.model.removePanel(sourceItemId, {
                    skipSetActive: false,
                    skipSetActiveGroup: true,
                });
            });
            if (!removedPanel_1) {
                throw new Error("No panel with id ".concat(sourceItemId));
            }
            if (sourceGroup.model.size === 0) {
                // remove the group and do not set a new group as active
                this.doRemoveGroup(sourceGroup, { skipActive: true });
            }
            // Check if destination group is empty - if so, force render the component
            var isDestinationGroupEmpty_1 = destinationGroup.model.size === 0;
            this.movingLock(function () {
                var _a;
                return destinationGroup.model.openPanel(removedPanel_1, {
                    index: destinationIndex,
                    skipSetActive: ((_a = options.skipSetActive) !== null && _a !== void 0 ? _a : false) && !isDestinationGroupEmpty_1,
                    skipSetGroupActive: true,
                });
            });
            if (!options.skipSetActive) {
                this.doSetGroupAndPanelActive(destinationGroup);
            }
            this._onDidMovePanel.fire({
                panel: removedPanel_1,
                from: sourceGroup,
            });
        }
        else {
            /**
             * Dropping a panel to the extremities of a group which will place that panel
             * into an adjacent group
             */
            var referenceLocation = (0, gridview_1.getGridLocation)(destinationGroup.element);
            var targetLocation = (0, gridview_1.getRelativeLocation)(this.gridview.orientation, referenceLocation, destinationTarget);
            if (sourceGroup.size < 2) {
                /**
                 * If we are moving from a group which only has one panel left we will consider
                 * moving the group itself rather than moving the panel into a newly created group
                 */
                var _b = __read((0, array_1.tail)(targetLocation), 2), targetParentLocation = _b[0], to = _b[1];
                if (sourceGroup.api.location.type === 'grid') {
                    var sourceLocation = (0, gridview_1.getGridLocation)(sourceGroup.element);
                    var _c = __read((0, array_1.tail)(sourceLocation), 2), sourceParentLocation = _c[0], from = _c[1];
                    if ((0, array_1.sequenceEquals)(sourceParentLocation, targetParentLocation)) {
                        // special case when 'swapping' two views within same grid location
                        // if a group has one tab - we are essentially moving the 'group'
                        // which is equivalent to swapping two views in this case
                        this.gridview.moveView(sourceParentLocation, from, to);
                        this._onDidMovePanel.fire({
                            panel: this.getGroupPanel(sourceItemId),
                            from: sourceGroup,
                        });
                        return;
                    }
                }
                if (sourceGroup.api.location.type === 'popout') {
                    /**
                     * the source group is a popout group with a single panel
                     *
                     * 1. remove the panel from the group without triggering any events
                     * 2. remove the popout group
                     * 3. create a new group at the requested location and add that panel
                     */
                    var popoutGroup_1 = this._popoutGroups.find(function (group) { return group.popoutGroup === sourceGroup; });
                    var removedPanel_2 = this.movingLock(function () {
                        return popoutGroup_1.popoutGroup.model.removePanel(popoutGroup_1.popoutGroup.panels[0], {
                            skipSetActive: true,
                            skipSetActiveGroup: true,
                        });
                    });
                    this.doRemoveGroup(sourceGroup, { skipActive: true });
                    var newGroup_1 = this.createGroupAtLocation(targetLocation);
                    this.movingLock(function () {
                        return newGroup_1.model.openPanel(removedPanel_2);
                    });
                    this.doSetGroupAndPanelActive(newGroup_1);
                    this._onDidMovePanel.fire({
                        panel: this.getGroupPanel(sourceItemId),
                        from: sourceGroup,
                    });
                    return;
                }
                // source group will become empty so delete the group
                var targetGroup_1 = this.movingLock(function () {
                    return _this.doRemoveGroup(sourceGroup, {
                        skipActive: true,
                        skipDispose: true,
                    });
                });
                // after deleting the group we need to re-evaulate the ref location
                var updatedReferenceLocation = (0, gridview_1.getGridLocation)(destinationGroup.element);
                var location_3 = (0, gridview_1.getRelativeLocation)(this.gridview.orientation, updatedReferenceLocation, destinationTarget);
                this.movingLock(function () { return _this.doAddGroup(targetGroup_1, location_3); });
                this.doSetGroupAndPanelActive(targetGroup_1);
                this._onDidMovePanel.fire({
                    panel: this.getGroupPanel(sourceItemId),
                    from: sourceGroup,
                });
            }
            else {
                /**
                 * The group we are removing from has many panels, we need to remove the panels we are moving,
                 * create a new group, add the panels to that new group and add the new group in an appropiate position
                 */
                var removedPanel_3 = this.movingLock(function () {
                    return sourceGroup.model.removePanel(sourceItemId, {
                        skipSetActive: false,
                        skipSetActiveGroup: true,
                    });
                });
                if (!removedPanel_3) {
                    throw new Error("No panel with id ".concat(sourceItemId));
                }
                var dropLocation = (0, gridview_1.getRelativeLocation)(this.gridview.orientation, referenceLocation, destinationTarget);
                var group_2 = this.createGroupAtLocation(dropLocation);
                this.movingLock(function () {
                    return group_2.model.openPanel(removedPanel_3, {
                        skipSetGroupActive: true,
                    });
                });
                this.doSetGroupAndPanelActive(group_2);
                this._onDidMovePanel.fire({
                    panel: removedPanel_3,
                    from: sourceGroup,
                });
            }
        }
    };
    DockviewComponent.prototype.moveGroup = function (options) {
        var _this = this;
        var from = options.from.group;
        var to = options.to.group;
        var target = options.to.position;
        if (target === 'center') {
            var activePanel_1 = from.activePanel;
            var panels_2 = this.movingLock(function () {
                return __spreadArray([], __read(from.panels), false).map(function (p) {
                    return from.model.removePanel(p.id, {
                        skipSetActive: true,
                    });
                });
            });
            if ((from === null || from === void 0 ? void 0 : from.model.size) === 0) {
                this.doRemoveGroup(from, { skipActive: true });
            }
            this.movingLock(function () {
                var e_16, _a;
                try {
                    for (var panels_3 = __values(panels_2), panels_3_1 = panels_3.next(); !panels_3_1.done; panels_3_1 = panels_3.next()) {
                        var panel = panels_3_1.value;
                        to.model.openPanel(panel, {
                            skipSetActive: panel !== activePanel_1,
                            skipSetGroupActive: true,
                        });
                    }
                }
                catch (e_16_1) { e_16 = { error: e_16_1 }; }
                finally {
                    try {
                        if (panels_3_1 && !panels_3_1.done && (_a = panels_3.return)) _a.call(panels_3);
                    }
                    finally { if (e_16) throw e_16.error; }
                }
            });
            // Ensure group becomes active after move
            if (options.skipSetActive !== true) {
                // For center moves (merges), we need to ensure the target group is active
                // unless explicitly told not to (skipSetActive: true)
                this.doSetGroupAndPanelActive(to);
            }
            else if (!this.activePanel) {
                // Even with skipSetActive: true, ensure there's an active panel if none exists
                // This maintains basic functionality while respecting skipSetActive
                this.doSetGroupAndPanelActive(to);
            }
        }
        else {
            switch (from.api.location.type) {
                case 'grid':
                    this.gridview.removeView((0, gridview_1.getGridLocation)(from.element));
                    break;
                case 'floating': {
                    var selectedFloatingGroup = this._floatingGroups.find(function (x) { return x.group === from; });
                    if (!selectedFloatingGroup) {
                        throw new Error('failed to find floating group');
                    }
                    selectedFloatingGroup.dispose();
                    break;
                }
                case 'popout': {
                    var selectedPopoutGroup = this._popoutGroups.find(function (x) { return x.popoutGroup === from; });
                    if (!selectedPopoutGroup) {
                        throw new Error('failed to find popout group');
                    }
                    // Remove from popout groups list to prevent automatic restoration
                    var index = this._popoutGroups.indexOf(selectedPopoutGroup);
                    if (index >= 0) {
                        this._popoutGroups.splice(index, 1);
                    }
                    // Clean up the reference group (ghost) if it exists and is hidden
                    if (selectedPopoutGroup.referenceGroup) {
                        var referenceGroup = this.getPanel(selectedPopoutGroup.referenceGroup);
                        if (referenceGroup && !referenceGroup.api.isVisible) {
                            this.doRemoveGroup(referenceGroup, {
                                skipActive: true,
                            });
                        }
                    }
                    // Manually dispose the window without triggering restoration
                    selectedPopoutGroup.window.dispose();
                    // Update group's location and containers for target
                    if (to.api.location.type === 'grid') {
                        from.model.renderContainer =
                            this.overlayRenderContainer;
                        from.model.dropTargetContainer =
                            this.rootDropTargetContainer;
                        from.model.location = { type: 'grid' };
                    }
                    else if (to.api.location.type === 'floating') {
                        from.model.renderContainer =
                            this.overlayRenderContainer;
                        from.model.dropTargetContainer =
                            this.rootDropTargetContainer;
                        from.model.location = { type: 'floating' };
                    }
                    break;
                }
            }
            // For moves to grid locations
            if (to.api.location.type === 'grid') {
                var referenceLocation = (0, gridview_1.getGridLocation)(to.element);
                var dropLocation = (0, gridview_1.getRelativeLocation)(this.gridview.orientation, referenceLocation, target);
                // Add to grid for all moves targeting grid location
                var size = void 0;
                switch (this.gridview.orientation) {
                    case splitview_1.Orientation.VERTICAL:
                        size =
                            referenceLocation.length % 2 == 0
                                ? from.api.width
                                : from.api.height;
                        break;
                    case splitview_1.Orientation.HORIZONTAL:
                        size =
                            referenceLocation.length % 2 == 0
                                ? from.api.height
                                : from.api.width;
                        break;
                }
                this.gridview.addView(from, size, dropLocation);
            }
            else if (to.api.location.type === 'floating') {
                // For moves to floating locations, add as floating group
                // Get the position/size from the target floating group
                var targetFloatingGroup = this._floatingGroups.find(function (x) { return x.group === to; });
                if (targetFloatingGroup) {
                    var box = targetFloatingGroup.overlay.toJSON();
                    // Calculate position based on available properties
                    var left = void 0, top_1;
                    if ('left' in box) {
                        left = box.left + 50;
                    }
                    else if ('right' in box) {
                        left = Math.max(0, box.right - box.width - 50);
                    }
                    else {
                        left = 50; // Default fallback
                    }
                    if ('top' in box) {
                        top_1 = box.top + 50;
                    }
                    else if ('bottom' in box) {
                        top_1 = Math.max(0, box.bottom - box.height - 50);
                    }
                    else {
                        top_1 = 50; // Default fallback
                    }
                    this.addFloatingGroup(from, {
                        height: box.height,
                        width: box.width,
                        position: {
                            left: left,
                            top: top_1,
                        },
                    });
                }
            }
        }
        from.panels.forEach(function (panel) {
            _this._onDidMovePanel.fire({ panel: panel, from: from });
        });
        // Ensure group becomes active after move
        if (options.skipSetActive === false) {
            // Only activate when explicitly requested (skipSetActive: false)
            // Use 'to' group for non-center moves since 'from' may have been destroyed
            var targetGroup = to !== null && to !== void 0 ? to : from;
            this.doSetGroupAndPanelActive(targetGroup);
        }
    };
    DockviewComponent.prototype.doSetGroupActive = function (group) {
        _super.prototype.doSetGroupActive.call(this, group);
        var activePanel = this.activePanel;
        if (!this._moving &&
            activePanel !== this._onDidActivePanelChange.value) {
            this._onDidActivePanelChange.fire(activePanel);
        }
    };
    DockviewComponent.prototype.doSetGroupAndPanelActive = function (group) {
        _super.prototype.doSetGroupActive.call(this, group);
        var activePanel = this.activePanel;
        if (group &&
            this.hasMaximizedGroup() &&
            !this.isMaximizedGroup(group)) {
            this.exitMaximizedGroup();
        }
        if (!this._moving &&
            activePanel !== this._onDidActivePanelChange.value) {
            this._onDidActivePanelChange.fire(activePanel);
        }
    };
    DockviewComponent.prototype.getNextGroupId = function () {
        var id = this.nextGroupId.next();
        while (this._groups.has(id)) {
            id = this.nextGroupId.next();
        }
        return id;
    };
    DockviewComponent.prototype.createGroup = function (options) {
        var _this = this;
        if (!options) {
            options = {};
        }
        var id = options === null || options === void 0 ? void 0 : options.id;
        if (id && this._groups.has(options.id)) {
            console.warn("dockview: Duplicate group id ".concat(options === null || options === void 0 ? void 0 : options.id, ". reassigning group id to avoid errors"));
            id = undefined;
        }
        if (!id) {
            id = this.nextGroupId.next();
            while (this._groups.has(id)) {
                id = this.nextGroupId.next();
            }
        }
        var view = new dockviewGroupPanel_1.DockviewGroupPanel(this, id, options);
        view.init({ params: {}, accessor: this });
        if (!this._groups.has(view.id)) {
            var disposable = new lifecycle_1.CompositeDisposable(view.model.onTabDragStart(function (event) {
                _this._onWillDragPanel.fire(event);
            }), view.model.onGroupDragStart(function (event) {
                _this._onWillDragGroup.fire(event);
            }), view.model.onMove(function (event) {
                var groupId = event.groupId, itemId = event.itemId, target = event.target, index = event.index;
                _this.moveGroupOrPanel({
                    from: { groupId: groupId, panelId: itemId },
                    to: {
                        group: view,
                        position: target,
                        index: index,
                    },
                });
            }), view.model.onDidDrop(function (event) {
                _this._onDidDrop.fire(event);
            }), view.model.onWillDrop(function (event) {
                _this._onWillDrop.fire(event);
            }), view.model.onWillShowOverlay(function (event) {
                if (_this.options.disableDnd) {
                    event.preventDefault();
                    return;
                }
                _this._onWillShowOverlay.fire(event);
            }), view.model.onUnhandledDragOverEvent(function (event) {
                _this._onUnhandledDragOverEvent.fire(event);
            }), view.model.onDidAddPanel(function (event) {
                if (_this._moving) {
                    return;
                }
                _this._onDidAddPanel.fire(event.panel);
            }), view.model.onDidRemovePanel(function (event) {
                if (_this._moving) {
                    return;
                }
                _this._onDidRemovePanel.fire(event.panel);
            }), view.model.onDidActivePanelChange(function (event) {
                if (_this._moving) {
                    return;
                }
                if (event.panel !== _this.activePanel) {
                    return;
                }
                if (_this._onDidActivePanelChange.value !== event.panel) {
                    _this._onDidActivePanelChange.fire(event.panel);
                }
            }), events_1.Event.any(view.model.onDidPanelTitleChange, view.model.onDidPanelParametersChange)(function () {
                _this._bufferOnDidLayoutChange.fire();
            }));
            this._groups.set(view.id, { value: view, disposable: disposable });
        }
        // TODO: must be called after the above listeners have been setup, not an ideal pattern
        view.initialize();
        return view;
    };
    DockviewComponent.prototype.createPanel = function (options, group) {
        var _a, _b, _c;
        var contentComponent = options.component;
        var tabComponent = (_a = options.tabComponent) !== null && _a !== void 0 ? _a : this.options.defaultTabComponent;
        var view = new dockviewPanelModel_1.DockviewPanelModel(this, options.id, contentComponent, tabComponent);
        var panel = new dockviewPanel_1.DockviewPanel(options.id, contentComponent, tabComponent, this, this._api, group, view, {
            renderer: options.renderer,
            minimumWidth: options.minimumWidth,
            minimumHeight: options.minimumHeight,
            maximumWidth: options.maximumWidth,
            maximumHeight: options.maximumHeight,
        });
        panel.init({
            title: (_b = options.title) !== null && _b !== void 0 ? _b : options.id,
            params: (_c = options === null || options === void 0 ? void 0 : options.params) !== null && _c !== void 0 ? _c : {},
        });
        return panel;
    };
    DockviewComponent.prototype.createGroupAtLocation = function (location, size, options) {
        var group = this.createGroup(options);
        this.doAddGroup(group, location, size);
        return group;
    };
    DockviewComponent.prototype.findGroup = function (panel) {
        var _a;
        return (_a = Array.from(this._groups.values()).find(function (group) {
            return group.value.model.containsPanel(panel);
        })) === null || _a === void 0 ? void 0 : _a.value;
    };
    DockviewComponent.prototype.orientationAtLocation = function (location) {
        var rootOrientation = this.gridview.orientation;
        return location.length % 2 == 1
            ? rootOrientation
            : (0, gridview_1.orthogonal)(rootOrientation);
    };
    DockviewComponent.prototype.updateDropTargetModel = function (options) {
        if ('dndEdges' in options) {
            this._rootDropTarget.disabled =
                typeof options.dndEdges === 'boolean' &&
                    options.dndEdges === false;
            if (typeof options.dndEdges === 'object' &&
                options.dndEdges !== null) {
                this._rootDropTarget.setOverlayModel(options.dndEdges);
            }
            else {
                this._rootDropTarget.setOverlayModel(DEFAULT_ROOT_OVERLAY_MODEL);
            }
        }
        if ('rootOverlayModel' in options) {
            this.updateDropTargetModel({ dndEdges: options.dndEdges });
        }
    };
    DockviewComponent.prototype.updateTheme = function () {
        var _a, _b;
        var theme = (_a = this._options.theme) !== null && _a !== void 0 ? _a : theme_1.themeAbyss;
        this._themeClassnames.setClassNames(theme.className);
        this.gridview.margin = (_b = theme.gap) !== null && _b !== void 0 ? _b : 0;
        switch (theme.dndOverlayMounting) {
            case 'absolute':
                this.rootDropTargetContainer.disabled = false;
                break;
            case 'relative':
            default:
                this.rootDropTargetContainer.disabled = true;
                break;
        }
    };
    return DockviewComponent;
}(baseComponentGridview_1.BaseGrid));
exports.DockviewComponent = DockviewComponent;
