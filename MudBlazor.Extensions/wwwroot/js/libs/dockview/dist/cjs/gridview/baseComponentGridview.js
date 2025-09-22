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
exports.BaseGrid = exports.toTarget = void 0;
var events_1 = require("../events");
var gridview_1 = require("./gridview");
var lifecycle_1 = require("../lifecycle");
var math_1 = require("../math");
var splitview_1 = require("../splitview/splitview");
var resizable_1 = require("../resizable");
var dom_1 = require("../dom");
var nextLayoutId = (0, math_1.sequentialNumberGenerator)();
function toTarget(direction) {
    switch (direction) {
        case 'left':
            return 'left';
        case 'right':
            return 'right';
        case 'above':
            return 'top';
        case 'below':
            return 'bottom';
        case 'within':
        default:
            return 'center';
    }
}
exports.toTarget = toTarget;
var BaseGrid = /** @class */ (function (_super) {
    __extends(BaseGrid, _super);
    function BaseGrid(container, options) {
        var _a;
        var _this = _super.call(this, document.createElement('div'), options.disableAutoResizing) || this;
        _this._id = nextLayoutId.next();
        _this._groups = new Map();
        _this._onDidRemove = new events_1.Emitter();
        _this.onDidRemove = _this._onDidRemove.event;
        _this._onDidAdd = new events_1.Emitter();
        _this.onDidAdd = _this._onDidAdd.event;
        _this._onDidMaximizedChange = new events_1.Emitter();
        _this.onDidMaximizedChange = _this._onDidMaximizedChange.event;
        _this._onDidActiveChange = new events_1.Emitter();
        _this.onDidActiveChange = _this._onDidActiveChange.event;
        _this._bufferOnDidLayoutChange = new events_1.AsapEvent();
        _this.onDidLayoutChange = _this._bufferOnDidLayoutChange.onEvent;
        _this._onDidViewVisibilityChangeMicroTaskQueue = new events_1.AsapEvent();
        _this.onDidViewVisibilityChangeMicroTaskQueue = _this._onDidViewVisibilityChangeMicroTaskQueue.onEvent;
        _this.element.style.height = '100%';
        _this.element.style.width = '100%';
        _this._classNames = new dom_1.Classnames(_this.element);
        _this._classNames.setClassNames((_a = options.className) !== null && _a !== void 0 ? _a : '');
        // the container is owned by the third-party, do not modify/delete it
        container.appendChild(_this.element);
        _this.gridview = new gridview_1.Gridview(!!options.proportionalLayout, options.styles, options.orientation, options.locked, options.margin);
        _this.gridview.locked = !!options.locked;
        _this.element.appendChild(_this.gridview.element);
        _this.layout(0, 0, true); // set some elements height/widths
        _this.addDisposables(_this.gridview.onDidMaximizedNodeChange(function (event) {
            _this._onDidMaximizedChange.fire({
                panel: event.view,
                isMaximized: event.isMaximized,
            });
        }), _this.gridview.onDidViewVisibilityChange(function () {
            return _this._onDidViewVisibilityChangeMicroTaskQueue.fire();
        }), _this.onDidViewVisibilityChangeMicroTaskQueue(function () {
            _this.layout(_this.width, _this.height, true);
        }), lifecycle_1.Disposable.from(function () {
            var _a;
            (_a = _this.element.parentElement) === null || _a === void 0 ? void 0 : _a.removeChild(_this.element);
        }), _this.gridview.onDidChange(function () {
            _this._bufferOnDidLayoutChange.fire();
        }), events_1.Event.any(_this.onDidAdd, _this.onDidRemove, _this.onDidActiveChange)(function () {
            _this._bufferOnDidLayoutChange.fire();
        }), _this._onDidMaximizedChange, _this._onDidViewVisibilityChangeMicroTaskQueue, _this._bufferOnDidLayoutChange);
        return _this;
    }
    Object.defineProperty(BaseGrid.prototype, "id", {
        get: function () {
            return this._id;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(BaseGrid.prototype, "size", {
        get: function () {
            return this._groups.size;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(BaseGrid.prototype, "groups", {
        get: function () {
            return Array.from(this._groups.values()).map(function (_) { return _.value; });
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(BaseGrid.prototype, "width", {
        get: function () {
            return this.gridview.width;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(BaseGrid.prototype, "height", {
        get: function () {
            return this.gridview.height;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(BaseGrid.prototype, "minimumHeight", {
        get: function () {
            return this.gridview.minimumHeight;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(BaseGrid.prototype, "maximumHeight", {
        get: function () {
            return this.gridview.maximumHeight;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(BaseGrid.prototype, "minimumWidth", {
        get: function () {
            return this.gridview.minimumWidth;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(BaseGrid.prototype, "maximumWidth", {
        get: function () {
            return this.gridview.maximumWidth;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(BaseGrid.prototype, "activeGroup", {
        get: function () {
            return this._activeGroup;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(BaseGrid.prototype, "locked", {
        get: function () {
            return this.gridview.locked;
        },
        set: function (value) {
            this.gridview.locked = value;
        },
        enumerable: false,
        configurable: true
    });
    BaseGrid.prototype.setVisible = function (panel, visible) {
        this.gridview.setViewVisible((0, gridview_1.getGridLocation)(panel.element), visible);
        this._bufferOnDidLayoutChange.fire();
    };
    BaseGrid.prototype.isVisible = function (panel) {
        return this.gridview.isViewVisible((0, gridview_1.getGridLocation)(panel.element));
    };
    BaseGrid.prototype.updateOptions = function (options) {
        var _a, _b, _c, _d;
        if (typeof options.proportionalLayout === 'boolean') {
            // this.gridview.proportionalLayout = options.proportionalLayout; // not supported
        }
        if (options.orientation) {
            this.gridview.orientation = options.orientation;
        }
        if ('styles' in options) {
            // this.gridview.styles = options.styles; // not supported
        }
        if ('disableResizing' in options) {
            this.disableResizing = (_a = options.disableAutoResizing) !== null && _a !== void 0 ? _a : false;
        }
        if ('locked' in options) {
            this.locked = (_b = options.locked) !== null && _b !== void 0 ? _b : false;
        }
        if ('margin' in options) {
            this.gridview.margin = (_c = options.margin) !== null && _c !== void 0 ? _c : 0;
        }
        if ('className' in options) {
            this._classNames.setClassNames((_d = options.className) !== null && _d !== void 0 ? _d : '');
        }
    };
    BaseGrid.prototype.maximizeGroup = function (panel) {
        this.gridview.maximizeView(panel);
        this.doSetGroupActive(panel);
    };
    BaseGrid.prototype.isMaximizedGroup = function (panel) {
        return this.gridview.maximizedView() === panel;
    };
    BaseGrid.prototype.exitMaximizedGroup = function () {
        this.gridview.exitMaximizedView();
    };
    BaseGrid.prototype.hasMaximizedGroup = function () {
        return this.gridview.hasMaximizedView();
    };
    BaseGrid.prototype.doAddGroup = function (group, location, size) {
        if (location === void 0) { location = [0]; }
        this.gridview.addView(group, size !== null && size !== void 0 ? size : splitview_1.Sizing.Distribute, location);
        this._onDidAdd.fire(group);
    };
    BaseGrid.prototype.doRemoveGroup = function (group, options) {
        if (!this._groups.has(group.id)) {
            throw new Error('invalid operation');
        }
        var item = this._groups.get(group.id);
        var view = this.gridview.remove(group, splitview_1.Sizing.Distribute);
        if (item && !(options === null || options === void 0 ? void 0 : options.skipDispose)) {
            item.disposable.dispose();
            item.value.dispose();
            this._groups.delete(group.id);
            this._onDidRemove.fire(group);
        }
        if (!(options === null || options === void 0 ? void 0 : options.skipActive) && this._activeGroup === group) {
            var groups = Array.from(this._groups.values());
            this.doSetGroupActive(groups.length > 0 ? groups[0].value : undefined);
        }
        return view;
    };
    BaseGrid.prototype.getPanel = function (id) {
        var _a;
        return (_a = this._groups.get(id)) === null || _a === void 0 ? void 0 : _a.value;
    };
    BaseGrid.prototype.doSetGroupActive = function (group) {
        if (this._activeGroup === group) {
            return;
        }
        if (this._activeGroup) {
            this._activeGroup.setActive(false);
        }
        if (group) {
            group.setActive(true);
        }
        this._activeGroup = group;
        this._onDidActiveChange.fire(group);
    };
    BaseGrid.prototype.removeGroup = function (group) {
        this.doRemoveGroup(group);
    };
    BaseGrid.prototype.moveToNext = function (options) {
        var _a;
        if (!options) {
            options = {};
        }
        if (!options.group) {
            if (!this.activeGroup) {
                return;
            }
            options.group = this.activeGroup;
        }
        var location = (0, gridview_1.getGridLocation)(options.group.element);
        var next = (_a = this.gridview.next(location)) === null || _a === void 0 ? void 0 : _a.view;
        this.doSetGroupActive(next);
    };
    BaseGrid.prototype.moveToPrevious = function (options) {
        var _a;
        if (!options) {
            options = {};
        }
        if (!options.group) {
            if (!this.activeGroup) {
                return;
            }
            options.group = this.activeGroup;
        }
        var location = (0, gridview_1.getGridLocation)(options.group.element);
        var next = (_a = this.gridview.previous(location)) === null || _a === void 0 ? void 0 : _a.view;
        this.doSetGroupActive(next);
    };
    BaseGrid.prototype.layout = function (width, height, forceResize) {
        var different = forceResize || width !== this.width || height !== this.height;
        if (!different) {
            return;
        }
        this.gridview.element.style.height = "".concat(height, "px");
        this.gridview.element.style.width = "".concat(width, "px");
        this.gridview.layout(width, height);
    };
    BaseGrid.prototype.dispose = function () {
        var e_1, _a;
        this._onDidActiveChange.dispose();
        this._onDidAdd.dispose();
        this._onDidRemove.dispose();
        try {
            for (var _b = __values(this.groups), _c = _b.next(); !_c.done; _c = _b.next()) {
                var group = _c.value;
                group.dispose();
            }
        }
        catch (e_1_1) { e_1 = { error: e_1_1 }; }
        finally {
            try {
                if (_c && !_c.done && (_a = _b.return)) _a.call(_b);
            }
            finally { if (e_1) throw e_1.error; }
        }
        this.gridview.dispose();
        _super.prototype.dispose.call(this);
    };
    return BaseGrid;
}(resizable_1.Resizable));
exports.BaseGrid = BaseGrid;
