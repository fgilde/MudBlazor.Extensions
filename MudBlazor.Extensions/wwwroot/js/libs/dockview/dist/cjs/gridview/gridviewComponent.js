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
Object.defineProperty(exports, "__esModule", { value: true });
exports.GridviewComponent = void 0;
var gridview_1 = require("./gridview");
var array_1 = require("../array");
var lifecycle_1 = require("../lifecycle");
var baseComponentGridview_1 = require("./baseComponentGridview");
var events_1 = require("../events");
var GridviewComponent = /** @class */ (function (_super) {
    __extends(GridviewComponent, _super);
    function GridviewComponent(container, options) {
        var _a;
        var _this = _super.call(this, container, {
            proportionalLayout: (_a = options.proportionalLayout) !== null && _a !== void 0 ? _a : true,
            orientation: options.orientation,
            styles: options.hideBorders
                ? { separatorBorder: 'transparent' }
                : undefined,
            disableAutoResizing: options.disableAutoResizing,
            className: options.className,
        }) || this;
        _this._onDidLayoutfromJSON = new events_1.Emitter();
        _this.onDidLayoutFromJSON = _this._onDidLayoutfromJSON.event;
        _this._onDidRemoveGroup = new events_1.Emitter();
        _this.onDidRemoveGroup = _this._onDidRemoveGroup.event;
        _this._onDidAddGroup = new events_1.Emitter();
        _this.onDidAddGroup = _this._onDidAddGroup.event;
        _this._onDidActiveGroupChange = new events_1.Emitter();
        _this.onDidActiveGroupChange = _this._onDidActiveGroupChange.event;
        _this._options = options;
        _this.addDisposables(_this._onDidAddGroup, _this._onDidRemoveGroup, _this._onDidActiveGroupChange, _this.onDidAdd(function (event) {
            _this._onDidAddGroup.fire(event);
        }), _this.onDidRemove(function (event) {
            _this._onDidRemoveGroup.fire(event);
        }), _this.onDidActiveChange(function (event) {
            _this._onDidActiveGroupChange.fire(event);
        }));
        return _this;
    }
    Object.defineProperty(GridviewComponent.prototype, "orientation", {
        get: function () {
            return this.gridview.orientation;
        },
        set: function (value) {
            this.gridview.orientation = value;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(GridviewComponent.prototype, "options", {
        get: function () {
            return this._options;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(GridviewComponent.prototype, "deserializer", {
        get: function () {
            return this._deserializer;
        },
        set: function (value) {
            this._deserializer = value;
        },
        enumerable: false,
        configurable: true
    });
    GridviewComponent.prototype.updateOptions = function (options) {
        _super.prototype.updateOptions.call(this, options);
        var hasOrientationChanged = typeof options.orientation === 'string' &&
            this.gridview.orientation !== options.orientation;
        this._options = __assign(__assign({}, this.options), options);
        if (hasOrientationChanged) {
            this.gridview.orientation = options.orientation;
        }
        this.layout(this.gridview.width, this.gridview.height, true);
    };
    GridviewComponent.prototype.removePanel = function (panel) {
        this.removeGroup(panel);
    };
    /**
     * Serialize the current state of the layout
     *
     * @returns A JSON respresentation of the layout
     */
    GridviewComponent.prototype.toJSON = function () {
        var _a;
        var data = this.gridview.serialize();
        return {
            grid: data,
            activePanel: (_a = this.activeGroup) === null || _a === void 0 ? void 0 : _a.id,
        };
    };
    GridviewComponent.prototype.setVisible = function (panel, visible) {
        this.gridview.setViewVisible((0, gridview_1.getGridLocation)(panel.element), visible);
    };
    GridviewComponent.prototype.setActive = function (panel) {
        this._groups.forEach(function (value, _key) {
            value.value.setActive(panel === value.value);
        });
    };
    GridviewComponent.prototype.focus = function () {
        var _a;
        (_a = this.activeGroup) === null || _a === void 0 ? void 0 : _a.focus();
    };
    GridviewComponent.prototype.fromJSON = function (serializedGridview) {
        var e_1, _a;
        var _this = this;
        this.clear();
        var grid = serializedGridview.grid, activePanel = serializedGridview.activePanel;
        try {
            var queue_1 = [];
            // take note of the existing dimensions
            var width = this.width;
            var height = this.height;
            this.gridview.deserialize(grid, {
                fromJSON: function (node) {
                    var data = node.data;
                    var view = _this.options.createComponent({
                        id: data.id,
                        name: data.component,
                    });
                    queue_1.push(function () {
                        return view.init({
                            params: data.params,
                            minimumWidth: data.minimumWidth,
                            maximumWidth: data.maximumWidth,
                            minimumHeight: data.minimumHeight,
                            maximumHeight: data.maximumHeight,
                            priority: data.priority,
                            snap: !!data.snap,
                            accessor: _this,
                            isVisible: node.visible,
                        });
                    });
                    _this._onDidAddGroup.fire(view);
                    _this.registerPanel(view);
                    return view;
                },
            });
            this.layout(width, height, true);
            queue_1.forEach(function (f) { return f(); });
            if (typeof activePanel === 'string') {
                var panel = this.getPanel(activePanel);
                if (panel) {
                    this.doSetGroupActive(panel);
                }
            }
        }
        catch (err) {
            try {
                /**
                 * To remove a group we cannot call this.removeGroup(...) since this makes assumptions about
                 * the underlying HTMLElement existing in the Gridview.
                 */
                for (var _b = __values(this.groups), _c = _b.next(); !_c.done; _c = _b.next()) {
                    var group = _c.value;
                    group.dispose();
                    this._groups.delete(group.id);
                    this._onDidRemoveGroup.fire(group);
                }
            }
            catch (e_1_1) { e_1 = { error: e_1_1 }; }
            finally {
                try {
                    if (_c && !_c.done && (_a = _b.return)) _a.call(_b);
                }
                finally { if (e_1) throw e_1.error; }
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
        this._onDidLayoutfromJSON.fire();
    };
    GridviewComponent.prototype.clear = function () {
        var e_2, _a;
        var hasActiveGroup = this.activeGroup;
        var groups = Array.from(this._groups.values()); // reassign since group panels will mutate
        try {
            for (var groups_1 = __values(groups), groups_1_1 = groups_1.next(); !groups_1_1.done; groups_1_1 = groups_1.next()) {
                var group = groups_1_1.value;
                group.disposable.dispose();
                this.doRemoveGroup(group.value, { skipActive: true });
            }
        }
        catch (e_2_1) { e_2 = { error: e_2_1 }; }
        finally {
            try {
                if (groups_1_1 && !groups_1_1.done && (_a = groups_1.return)) _a.call(groups_1);
            }
            finally { if (e_2) throw e_2.error; }
        }
        if (hasActiveGroup) {
            this.doSetGroupActive(undefined);
        }
        this.gridview.clear();
    };
    GridviewComponent.prototype.movePanel = function (panel, options) {
        var _a;
        var relativeLocation;
        var removedPanel = this.gridview.remove(panel);
        var referenceGroup = (_a = this._groups.get(options.reference)) === null || _a === void 0 ? void 0 : _a.value;
        if (!referenceGroup) {
            throw new Error("reference group ".concat(options.reference, " does not exist"));
        }
        var target = (0, baseComponentGridview_1.toTarget)(options.direction);
        if (target === 'center') {
            throw new Error("".concat(target, " not supported as an option"));
        }
        else {
            var location_1 = (0, gridview_1.getGridLocation)(referenceGroup.element);
            relativeLocation = (0, gridview_1.getRelativeLocation)(this.gridview.orientation, location_1, target);
        }
        this.doAddGroup(removedPanel, relativeLocation, options.size);
    };
    GridviewComponent.prototype.addPanel = function (options) {
        var _a, _b, _c, _d;
        var relativeLocation = (_a = options.location) !== null && _a !== void 0 ? _a : [0];
        if ((_b = options.position) === null || _b === void 0 ? void 0 : _b.referencePanel) {
            var referenceGroup = (_c = this._groups.get(options.position.referencePanel)) === null || _c === void 0 ? void 0 : _c.value;
            if (!referenceGroup) {
                throw new Error("reference group ".concat(options.position.referencePanel, " does not exist"));
            }
            var target = (0, baseComponentGridview_1.toTarget)(options.position.direction);
            if (target === 'center') {
                throw new Error("".concat(target, " not supported as an option"));
            }
            else {
                var location_2 = (0, gridview_1.getGridLocation)(referenceGroup.element);
                relativeLocation = (0, gridview_1.getRelativeLocation)(this.gridview.orientation, location_2, target);
            }
        }
        var view = this.options.createComponent({
            id: options.id,
            name: options.component,
        });
        view.init({
            params: (_d = options.params) !== null && _d !== void 0 ? _d : {},
            minimumWidth: options.minimumWidth,
            maximumWidth: options.maximumWidth,
            minimumHeight: options.minimumHeight,
            maximumHeight: options.maximumHeight,
            priority: options.priority,
            snap: !!options.snap,
            accessor: this,
            isVisible: true,
        });
        this.registerPanel(view);
        this.doAddGroup(view, relativeLocation, options.size);
        this.doSetGroupActive(view);
        return view;
    };
    GridviewComponent.prototype.registerPanel = function (panel) {
        var _this = this;
        var disposable = new lifecycle_1.CompositeDisposable(panel.api.onDidFocusChange(function (event) {
            if (!event.isFocused) {
                return;
            }
            _this._groups.forEach(function (groupItem) {
                var group = groupItem.value;
                if (group !== panel) {
                    group.setActive(false);
                }
                else {
                    group.setActive(true);
                }
            });
        }));
        this._groups.set(panel.id, {
            value: panel,
            disposable: disposable,
        });
    };
    GridviewComponent.prototype.moveGroup = function (referenceGroup, groupId, target) {
        var sourceGroup = this.getPanel(groupId);
        if (!sourceGroup) {
            throw new Error('invalid operation');
        }
        var referenceLocation = (0, gridview_1.getGridLocation)(referenceGroup.element);
        var targetLocation = (0, gridview_1.getRelativeLocation)(this.gridview.orientation, referenceLocation, target);
        var _a = __read((0, array_1.tail)(targetLocation), 2), targetParentLocation = _a[0], to = _a[1];
        var sourceLocation = (0, gridview_1.getGridLocation)(sourceGroup.element);
        var _b = __read((0, array_1.tail)(sourceLocation), 2), sourceParentLocation = _b[0], from = _b[1];
        if ((0, array_1.sequenceEquals)(sourceParentLocation, targetParentLocation)) {
            // special case when 'swapping' two views within same grid location
            // if a group has one tab - we are essentially moving the 'group'
            // which is equivalent to swapping two views in this case
            this.gridview.moveView(sourceParentLocation, from, to);
            return;
        }
        // source group will become empty so delete the group
        var targetGroup = this.doRemoveGroup(sourceGroup, {
            skipActive: true,
            skipDispose: true,
        });
        // after deleting the group we need to re-evaulate the ref location
        var updatedReferenceLocation = (0, gridview_1.getGridLocation)(referenceGroup.element);
        var location = (0, gridview_1.getRelativeLocation)(this.gridview.orientation, updatedReferenceLocation, target);
        this.doAddGroup(targetGroup, location);
    };
    GridviewComponent.prototype.removeGroup = function (group) {
        _super.prototype.removeGroup.call(this, group);
    };
    GridviewComponent.prototype.dispose = function () {
        _super.prototype.dispose.call(this);
        this._onDidLayoutfromJSON.dispose();
    };
    return GridviewComponent;
}(baseComponentGridview_1.BaseGrid));
exports.GridviewComponent = GridviewComponent;
