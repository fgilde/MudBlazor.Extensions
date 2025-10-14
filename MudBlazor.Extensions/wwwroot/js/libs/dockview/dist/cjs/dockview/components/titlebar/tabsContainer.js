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
exports.TabsContainer = void 0;
var lifecycle_1 = require("../../../lifecycle");
var events_1 = require("../../../events");
var voidContainer_1 = require("./voidContainer");
var dom_1 = require("../../../dom");
var events_2 = require("../../events");
var dataTransfer_1 = require("../../../dnd/dataTransfer");
var tabs_1 = require("./tabs");
var tabOverflowControl_1 = require("./tabOverflowControl");
var TabsContainer = /** @class */ (function (_super) {
    __extends(TabsContainer, _super);
    function TabsContainer(accessor, group) {
        var _this = _super.call(this) || this;
        _this.accessor = accessor;
        _this.group = group;
        _this._hidden = false;
        _this.dropdownPart = null;
        _this._overflowTabs = [];
        _this._dropdownDisposable = new lifecycle_1.MutableDisposable();
        _this._onDrop = new events_1.Emitter();
        _this.onDrop = _this._onDrop.event;
        _this._onGroupDragStart = new events_1.Emitter();
        _this.onGroupDragStart = _this._onGroupDragStart.event;
        _this._onWillShowOverlay = new events_1.Emitter();
        _this.onWillShowOverlay = _this._onWillShowOverlay.event;
        _this._element = document.createElement('div');
        _this._element.className = 'dv-tabs-and-actions-container';
        (0, dom_1.toggleClass)(_this._element, 'dv-full-width-single-tab', _this.accessor.options.singleTabMode === 'fullwidth');
        _this.rightActionsContainer = document.createElement('div');
        _this.rightActionsContainer.className = 'dv-right-actions-container';
        _this.leftActionsContainer = document.createElement('div');
        _this.leftActionsContainer.className = 'dv-left-actions-container';
        _this.preActionsContainer = document.createElement('div');
        _this.preActionsContainer.className = 'dv-pre-actions-container';
        _this.tabs = new tabs_1.Tabs(group, accessor, {
            showTabsOverflowControl: !accessor.options.disableTabsOverflowList,
        });
        _this.voidContainer = new voidContainer_1.VoidContainer(_this.accessor, _this.group);
        _this._element.appendChild(_this.preActionsContainer);
        _this._element.appendChild(_this.tabs.element);
        _this._element.appendChild(_this.leftActionsContainer);
        _this._element.appendChild(_this.voidContainer.element);
        _this._element.appendChild(_this.rightActionsContainer);
        _this.addDisposables(_this.tabs.onDrop(function (e) { return _this._onDrop.fire(e); }), _this.tabs.onWillShowOverlay(function (e) { return _this._onWillShowOverlay.fire(e); }), accessor.onDidOptionsChange(function () {
            _this.tabs.showTabsOverflowControl =
                !accessor.options.disableTabsOverflowList;
        }), _this.tabs.onOverflowTabsChange(function (event) {
            _this.toggleDropdown(event);
        }), _this.tabs, _this._onWillShowOverlay, _this._onDrop, _this._onGroupDragStart, _this.voidContainer, _this.voidContainer.onDragStart(function (event) {
            _this._onGroupDragStart.fire({
                nativeEvent: event,
                group: _this.group,
            });
        }), _this.voidContainer.onDrop(function (event) {
            _this._onDrop.fire({
                event: event.nativeEvent,
                index: _this.tabs.size,
            });
        }), _this.voidContainer.onWillShowOverlay(function (event) {
            _this._onWillShowOverlay.fire(new events_2.WillShowOverlayLocationEvent(event, {
                kind: 'header_space',
                panel: _this.group.activePanel,
                api: _this.accessor.api,
                group: _this.group,
                getData: dataTransfer_1.getPanelData,
            }));
        }), (0, events_1.addDisposableListener)(_this.voidContainer.element, 'pointerdown', function (event) {
            if (event.defaultPrevented) {
                return;
            }
            var isFloatingGroupsEnabled = !_this.accessor.options.disableFloatingGroups;
            if (isFloatingGroupsEnabled &&
                event.shiftKey &&
                _this.group.api.location.type !== 'floating') {
                event.preventDefault();
                var _a = _this.element.getBoundingClientRect(), top_1 = _a.top, left = _a.left;
                var _b = _this.accessor.element.getBoundingClientRect(), rootTop = _b.top, rootLeft = _b.left;
                _this.accessor.addFloatingGroup(_this.group, {
                    x: left - rootLeft + 20,
                    y: top_1 - rootTop + 20,
                    inDragMode: true,
                });
            }
        }));
        return _this;
    }
    Object.defineProperty(TabsContainer.prototype, "onTabDragStart", {
        get: function () {
            return this.tabs.onTabDragStart;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(TabsContainer.prototype, "panels", {
        get: function () {
            return this.tabs.panels;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(TabsContainer.prototype, "size", {
        get: function () {
            return this.tabs.size;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(TabsContainer.prototype, "hidden", {
        get: function () {
            return this._hidden;
        },
        set: function (value) {
            this._hidden = value;
            this.element.style.display = value ? 'none' : '';
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(TabsContainer.prototype, "element", {
        get: function () {
            return this._element;
        },
        enumerable: false,
        configurable: true
    });
    TabsContainer.prototype.show = function () {
        if (!this.hidden) {
            this.element.style.display = '';
        }
    };
    TabsContainer.prototype.hide = function () {
        this._element.style.display = 'none';
    };
    TabsContainer.prototype.setRightActionsElement = function (element) {
        if (this.rightActions === element) {
            return;
        }
        if (this.rightActions) {
            this.rightActions.remove();
            this.rightActions = undefined;
        }
        if (element) {
            this.rightActionsContainer.appendChild(element);
            this.rightActions = element;
        }
    };
    TabsContainer.prototype.setLeftActionsElement = function (element) {
        if (this.leftActions === element) {
            return;
        }
        if (this.leftActions) {
            this.leftActions.remove();
            this.leftActions = undefined;
        }
        if (element) {
            this.leftActionsContainer.appendChild(element);
            this.leftActions = element;
        }
    };
    TabsContainer.prototype.setPrefixActionsElement = function (element) {
        if (this.preActions === element) {
            return;
        }
        if (this.preActions) {
            this.preActions.remove();
            this.preActions = undefined;
        }
        if (element) {
            this.preActionsContainer.appendChild(element);
            this.preActions = element;
        }
    };
    TabsContainer.prototype.isActive = function (tab) {
        return this.tabs.isActive(tab);
    };
    TabsContainer.prototype.indexOf = function (id) {
        return this.tabs.indexOf(id);
    };
    TabsContainer.prototype.setActive = function (_isGroupActive) {
        // noop
    };
    TabsContainer.prototype.delete = function (id) {
        this.tabs.delete(id);
        this.updateClassnames();
    };
    TabsContainer.prototype.setActivePanel = function (panel) {
        this.tabs.setActivePanel(panel);
    };
    TabsContainer.prototype.openPanel = function (panel, index) {
        if (index === void 0) { index = this.tabs.size; }
        this.tabs.openPanel(panel, index);
        this.updateClassnames();
    };
    TabsContainer.prototype.closePanel = function (panel) {
        this.delete(panel.id);
    };
    TabsContainer.prototype.updateClassnames = function () {
        (0, dom_1.toggleClass)(this._element, 'dv-single-tab', this.size === 1);
    };
    TabsContainer.prototype.toggleDropdown = function (options) {
        var _this = this;
        var tabs = options.reset ? [] : options.tabs;
        this._overflowTabs = tabs;
        if (this._overflowTabs.length > 0 && this.dropdownPart) {
            this.dropdownPart.update({ tabs: tabs.length });
            return;
        }
        if (this._overflowTabs.length === 0) {
            this._dropdownDisposable.dispose();
            return;
        }
        var root = document.createElement('div');
        root.className = 'dv-tabs-overflow-dropdown-root';
        var part = (0, tabOverflowControl_1.createDropdownElementHandle)();
        part.update({ tabs: tabs.length });
        this.dropdownPart = part;
        root.appendChild(part.element);
        this.rightActionsContainer.prepend(root);
        this._dropdownDisposable.value = new lifecycle_1.CompositeDisposable(lifecycle_1.Disposable.from(function () {
            var _a, _b;
            root.remove();
            (_b = (_a = _this.dropdownPart) === null || _a === void 0 ? void 0 : _a.dispose) === null || _b === void 0 ? void 0 : _b.call(_a);
            _this.dropdownPart = null;
        }), (0, events_1.addDisposableListener)(root, 'pointerdown', function (event) {
            event.preventDefault();
        }, { capture: true }), (0, events_1.addDisposableListener)(root, 'click', function (event) {
            var e_1, _a;
            var el = document.createElement('div');
            el.style.overflow = 'auto';
            el.className = 'dv-tabs-overflow-container';
            var _loop_1 = function (tab) {
                var panelObject = _this.group.panels.find(function (panel) { return panel === tab.panel; });
                var tabComponent = panelObject.view.createTabRenderer('headerOverflow');
                var child = tabComponent.element;
                var wrapper = document.createElement('div');
                (0, dom_1.toggleClass)(wrapper, 'dv-tab', true);
                (0, dom_1.toggleClass)(wrapper, 'dv-active-tab', panelObject.api.isActive);
                (0, dom_1.toggleClass)(wrapper, 'dv-inactive-tab', !panelObject.api.isActive);
                wrapper.addEventListener('click', function (event) {
                    _this.accessor.popupService.close();
                    if (event.defaultPrevented) {
                        return;
                    }
                    tab.element.scrollIntoView();
                    tab.panel.api.setActive();
                });
                wrapper.appendChild(child);
                el.appendChild(wrapper);
            };
            try {
                for (var _b = __values(_this.tabs.tabs.filter(function (tab) {
                    return _this._overflowTabs.includes(tab.panel.id);
                })), _c = _b.next(); !_c.done; _c = _b.next()) {
                    var tab = _c.value;
                    _loop_1(tab);
                }
            }
            catch (e_1_1) { e_1 = { error: e_1_1 }; }
            finally {
                try {
                    if (_c && !_c.done && (_a = _b.return)) _a.call(_b);
                }
                finally { if (e_1) throw e_1.error; }
            }
            var relativeParent = (0, dom_1.findRelativeZIndexParent)(root);
            _this.accessor.popupService.openPopover(el, {
                x: event.clientX,
                y: event.clientY,
                zIndex: (relativeParent === null || relativeParent === void 0 ? void 0 : relativeParent.style.zIndex)
                    ? "calc(".concat(relativeParent.style.zIndex, " * 2)")
                    : undefined,
            });
        }));
    };
    TabsContainer.prototype.updateDragAndDropState = function () {
        this.tabs.updateDragAndDropState();
        this.voidContainer.updateDragAndDropState();
    };
    return TabsContainer;
}(lifecycle_1.CompositeDisposable));
exports.TabsContainer = TabsContainer;
