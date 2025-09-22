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
Object.defineProperty(exports, "__esModule", { value: true });
exports.Tabs = void 0;
var dataTransfer_1 = require("../../../dnd/dataTransfer");
var dom_1 = require("../../../dom");
var events_1 = require("../../../events");
var lifecycle_1 = require("../../../lifecycle");
var scrollbar_1 = require("../../../scrollbar");
var events_2 = require("../../events");
var tab_1 = require("../tab/tab");
var Tabs = /** @class */ (function (_super) {
    __extends(Tabs, _super);
    function Tabs(group, accessor, options) {
        var _this = _super.call(this) || this;
        _this.group = group;
        _this.accessor = accessor;
        _this._observerDisposable = new lifecycle_1.MutableDisposable();
        _this._tabs = [];
        _this.selectedIndex = -1;
        _this._showTabsOverflowControl = false;
        _this._onTabDragStart = new events_1.Emitter();
        _this.onTabDragStart = _this._onTabDragStart.event;
        _this._onDrop = new events_1.Emitter();
        _this.onDrop = _this._onDrop.event;
        _this._onWillShowOverlay = new events_1.Emitter();
        _this.onWillShowOverlay = _this._onWillShowOverlay.event;
        _this._onOverflowTabsChange = new events_1.Emitter();
        _this.onOverflowTabsChange = _this._onOverflowTabsChange.event;
        _this._tabsList = document.createElement('div');
        _this._tabsList.className = 'dv-tabs-container dv-horizontal';
        _this.showTabsOverflowControl = options.showTabsOverflowControl;
        if (accessor.options.scrollbars === 'native') {
            _this._element = _this._tabsList;
        }
        else {
            var scrollbar = new scrollbar_1.Scrollbar(_this._tabsList);
            _this._element = scrollbar.element;
            _this.addDisposables(scrollbar);
        }
        _this.addDisposables(_this._onOverflowTabsChange, _this._observerDisposable, _this._onWillShowOverlay, _this._onDrop, _this._onTabDragStart, (0, events_1.addDisposableListener)(_this.element, 'pointerdown', function (event) {
            if (event.defaultPrevented) {
                return;
            }
            var isLeftClick = event.button === 0;
            if (isLeftClick) {
                _this.accessor.doSetGroupActive(_this.group);
            }
        }), lifecycle_1.Disposable.from(function () {
            var e_1, _a;
            try {
                for (var _b = __values(_this._tabs), _c = _b.next(); !_c.done; _c = _b.next()) {
                    var _d = _c.value, value = _d.value, disposable = _d.disposable;
                    disposable.dispose();
                    value.dispose();
                }
            }
            catch (e_1_1) { e_1 = { error: e_1_1 }; }
            finally {
                try {
                    if (_c && !_c.done && (_a = _b.return)) _a.call(_b);
                }
                finally { if (e_1) throw e_1.error; }
            }
            _this._tabs = [];
        }));
        return _this;
    }
    Object.defineProperty(Tabs.prototype, "showTabsOverflowControl", {
        get: function () {
            return this._showTabsOverflowControl;
        },
        set: function (value) {
            var _this = this;
            if (this._showTabsOverflowControl == value) {
                return;
            }
            this._showTabsOverflowControl = value;
            if (value) {
                var observer = new dom_1.OverflowObserver(this._tabsList);
                this._observerDisposable.value = new lifecycle_1.CompositeDisposable(observer, observer.onDidChange(function (event) {
                    var hasOverflow = event.hasScrollX || event.hasScrollY;
                    _this.toggleDropdown({ reset: !hasOverflow });
                }), (0, events_1.addDisposableListener)(this._tabsList, 'scroll', function () {
                    _this.toggleDropdown({ reset: false });
                }));
            }
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(Tabs.prototype, "element", {
        get: function () {
            return this._element;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(Tabs.prototype, "panels", {
        get: function () {
            return this._tabs.map(function (_) { return _.value.panel.id; });
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(Tabs.prototype, "size", {
        get: function () {
            return this._tabs.length;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(Tabs.prototype, "tabs", {
        get: function () {
            return this._tabs.map(function (_) { return _.value; });
        },
        enumerable: false,
        configurable: true
    });
    Tabs.prototype.indexOf = function (id) {
        return this._tabs.findIndex(function (tab) { return tab.value.panel.id === id; });
    };
    Tabs.prototype.isActive = function (tab) {
        return (this.selectedIndex > -1 &&
            this._tabs[this.selectedIndex].value === tab);
    };
    Tabs.prototype.setActivePanel = function (panel) {
        var e_2, _a;
        var runningWidth = 0;
        try {
            for (var _b = __values(this._tabs), _c = _b.next(); !_c.done; _c = _b.next()) {
                var tab = _c.value;
                var isActivePanel = panel.id === tab.value.panel.id;
                tab.value.setActive(isActivePanel);
                if (isActivePanel) {
                    var element = tab.value.element;
                    var parentElement = element.parentElement;
                    if (runningWidth < parentElement.scrollLeft ||
                        runningWidth + element.clientWidth >
                            parentElement.scrollLeft + parentElement.clientWidth) {
                        parentElement.scrollLeft = runningWidth;
                    }
                }
                runningWidth += tab.value.element.clientWidth;
            }
        }
        catch (e_2_1) { e_2 = { error: e_2_1 }; }
        finally {
            try {
                if (_c && !_c.done && (_a = _b.return)) _a.call(_b);
            }
            finally { if (e_2) throw e_2.error; }
        }
    };
    Tabs.prototype.openPanel = function (panel, index) {
        var _this = this;
        if (index === void 0) { index = this._tabs.length; }
        if (this._tabs.find(function (tab) { return tab.value.panel.id === panel.id; })) {
            return;
        }
        var tab = new tab_1.Tab(panel, this.accessor, this.group);
        tab.setContent(panel.view.tab);
        var disposable = new lifecycle_1.CompositeDisposable(tab.onDragStart(function (event) {
            _this._onTabDragStart.fire({ nativeEvent: event, panel: panel });
        }), tab.onPointerDown(function (event) {
            if (event.defaultPrevented) {
                return;
            }
            var isFloatingGroupsEnabled = !_this.accessor.options.disableFloatingGroups;
            var isFloatingWithOnePanel = _this.group.api.location.type === 'floating' &&
                _this.size === 1;
            if (isFloatingGroupsEnabled &&
                !isFloatingWithOnePanel &&
                event.shiftKey) {
                event.preventDefault();
                var panel_1 = _this.accessor.getGroupPanel(tab.panel.id);
                var _a = tab.element.getBoundingClientRect(), top_1 = _a.top, left = _a.left;
                var _b = _this.accessor.element.getBoundingClientRect(), rootTop = _b.top, rootLeft = _b.left;
                _this.accessor.addFloatingGroup(panel_1, {
                    x: left - rootLeft,
                    y: top_1 - rootTop,
                    inDragMode: true,
                });
                return;
            }
            switch (event.button) {
                case 0: // left click or touch
                    if (_this.group.activePanel !== panel) {
                        _this.group.model.openPanel(panel);
                    }
                    break;
            }
        }), tab.onDrop(function (event) {
            _this._onDrop.fire({
                event: event.nativeEvent,
                index: _this._tabs.findIndex(function (x) { return x.value === tab; }),
            });
        }), tab.onWillShowOverlay(function (event) {
            _this._onWillShowOverlay.fire(new events_2.WillShowOverlayLocationEvent(event, {
                kind: 'tab',
                panel: _this.group.activePanel,
                api: _this.accessor.api,
                group: _this.group,
                getData: dataTransfer_1.getPanelData,
            }));
        }));
        var value = { value: tab, disposable: disposable };
        this.addTab(value, index);
    };
    Tabs.prototype.delete = function (id) {
        var index = this.indexOf(id);
        var tabToRemove = this._tabs.splice(index, 1)[0];
        var value = tabToRemove.value, disposable = tabToRemove.disposable;
        disposable.dispose();
        value.dispose();
        value.element.remove();
    };
    Tabs.prototype.addTab = function (tab, index) {
        if (index === void 0) { index = this._tabs.length; }
        if (index < 0 || index > this._tabs.length) {
            throw new Error('invalid location');
        }
        this._tabsList.insertBefore(tab.value.element, this._tabsList.children[index]);
        this._tabs = __spreadArray(__spreadArray(__spreadArray([], __read(this._tabs.slice(0, index)), false), [
            tab
        ], false), __read(this._tabs.slice(index)), false);
        if (this.selectedIndex < 0) {
            this.selectedIndex = index;
        }
    };
    Tabs.prototype.toggleDropdown = function (options) {
        var _this = this;
        var tabs = options.reset
            ? []
            : this._tabs
                .filter(function (tab) {
                return !(0, dom_1.isChildEntirelyVisibleWithinParent)(tab.value.element, _this._tabsList);
            })
                .map(function (x) { return x.value.panel.id; });
        this._onOverflowTabsChange.fire({ tabs: tabs, reset: options.reset });
    };
    Tabs.prototype.updateDragAndDropState = function () {
        var e_3, _a;
        try {
            for (var _b = __values(this._tabs), _c = _b.next(); !_c.done; _c = _b.next()) {
                var tab = _c.value;
                tab.value.updateDragAndDropState();
            }
        }
        catch (e_3_1) { e_3 = { error: e_3_1 }; }
        finally {
            try {
                if (_c && !_c.done && (_a = _b.return)) _a.call(_b);
            }
            finally { if (e_3) throw e_3.error; }
        }
    };
    return Tabs;
}(lifecycle_1.CompositeDisposable));
exports.Tabs = Tabs;
