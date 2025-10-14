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
exports.SplitviewComponent = void 0;
var lifecycle_1 = require("../lifecycle");
var splitview_1 = require("./splitview");
var events_1 = require("../events");
var resizable_1 = require("../resizable");
var dom_1 = require("../dom");
/**
 * A high-level implementation of splitview that works using 'panels'
 */
var SplitviewComponent = /** @class */ (function (_super) {
    __extends(SplitviewComponent, _super);
    function SplitviewComponent(container, options) {
        var _a;
        var _this = _super.call(this, document.createElement('div'), options.disableAutoResizing) || this;
        _this._splitviewChangeDisposable = new lifecycle_1.MutableDisposable();
        _this._panels = new Map();
        _this._onDidLayoutfromJSON = new events_1.Emitter();
        _this.onDidLayoutFromJSON = _this._onDidLayoutfromJSON.event;
        _this._onDidAddView = new events_1.Emitter();
        _this.onDidAddView = _this._onDidAddView.event;
        _this._onDidRemoveView = new events_1.Emitter();
        _this.onDidRemoveView = _this._onDidRemoveView.event;
        _this._onDidLayoutChange = new events_1.Emitter();
        _this.onDidLayoutChange = _this._onDidLayoutChange.event;
        _this.element.style.height = '100%';
        _this.element.style.width = '100%';
        _this._classNames = new dom_1.Classnames(_this.element);
        _this._classNames.setClassNames((_a = options.className) !== null && _a !== void 0 ? _a : '');
        // the container is owned by the third-party, do not modify/delete it
        container.appendChild(_this.element);
        _this._options = options;
        _this.splitview = new splitview_1.Splitview(_this.element, options);
        _this.addDisposables(_this._onDidAddView, _this._onDidLayoutfromJSON, _this._onDidRemoveView, _this._onDidLayoutChange);
        return _this;
    }
    Object.defineProperty(SplitviewComponent.prototype, "panels", {
        get: function () {
            return this.splitview.getViews();
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(SplitviewComponent.prototype, "options", {
        get: function () {
            return this._options;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(SplitviewComponent.prototype, "length", {
        get: function () {
            return this._panels.size;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(SplitviewComponent.prototype, "orientation", {
        get: function () {
            return this.splitview.orientation;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(SplitviewComponent.prototype, "splitview", {
        get: function () {
            return this._splitview;
        },
        set: function (value) {
            var _this = this;
            if (this._splitview) {
                this._splitview.dispose();
            }
            this._splitview = value;
            this._splitviewChangeDisposable.value = new lifecycle_1.CompositeDisposable(this._splitview.onDidSashEnd(function () {
                _this._onDidLayoutChange.fire(undefined);
            }), this._splitview.onDidAddView(function (e) { return _this._onDidAddView.fire(e); }), this._splitview.onDidRemoveView(function (e) {
                return _this._onDidRemoveView.fire(e);
            }));
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(SplitviewComponent.prototype, "minimumSize", {
        get: function () {
            return this.splitview.minimumSize;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(SplitviewComponent.prototype, "maximumSize", {
        get: function () {
            return this.splitview.maximumSize;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(SplitviewComponent.prototype, "height", {
        get: function () {
            return this.splitview.orientation === splitview_1.Orientation.HORIZONTAL
                ? this.splitview.orthogonalSize
                : this.splitview.size;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(SplitviewComponent.prototype, "width", {
        get: function () {
            return this.splitview.orientation === splitview_1.Orientation.HORIZONTAL
                ? this.splitview.size
                : this.splitview.orthogonalSize;
        },
        enumerable: false,
        configurable: true
    });
    SplitviewComponent.prototype.updateOptions = function (options) {
        var _a, _b;
        if ('className' in options) {
            this._classNames.setClassNames((_a = options.className) !== null && _a !== void 0 ? _a : '');
        }
        if ('disableResizing' in options) {
            this.disableResizing = (_b = options.disableAutoResizing) !== null && _b !== void 0 ? _b : false;
        }
        if (typeof options.orientation === 'string') {
            this.splitview.orientation = options.orientation;
        }
        this._options = __assign(__assign({}, this.options), options);
        this.splitview.layout(this.splitview.size, this.splitview.orthogonalSize);
    };
    SplitviewComponent.prototype.focus = function () {
        var _a;
        (_a = this._activePanel) === null || _a === void 0 ? void 0 : _a.focus();
    };
    SplitviewComponent.prototype.movePanel = function (from, to) {
        this.splitview.moveView(from, to);
    };
    SplitviewComponent.prototype.setVisible = function (panel, visible) {
        var index = this.panels.indexOf(panel);
        this.splitview.setViewVisible(index, visible);
    };
    SplitviewComponent.prototype.setActive = function (panel, skipFocus) {
        this._activePanel = panel;
        this.panels
            .filter(function (v) { return v !== panel; })
            .forEach(function (v) {
            v.api._onDidActiveChange.fire({ isActive: false });
            if (!skipFocus) {
                v.focus();
            }
        });
        panel.api._onDidActiveChange.fire({ isActive: true });
        if (!skipFocus) {
            panel.focus();
        }
    };
    SplitviewComponent.prototype.removePanel = function (panel, sizing) {
        var item = this._panels.get(panel.id);
        if (!item) {
            throw new Error("unknown splitview panel ".concat(panel.id));
        }
        item.dispose();
        this._panels.delete(panel.id);
        var index = this.panels.findIndex(function (_) { return _ === panel; });
        var removedView = this.splitview.removeView(index, sizing);
        removedView.dispose();
        var panels = this.panels;
        if (panels.length > 0) {
            this.setActive(panels[panels.length - 1]);
        }
    };
    SplitviewComponent.prototype.getPanel = function (id) {
        return this.panels.find(function (view) { return view.id === id; });
    };
    SplitviewComponent.prototype.addPanel = function (options) {
        var _a;
        if (this._panels.has(options.id)) {
            throw new Error("panel ".concat(options.id, " already exists"));
        }
        var view = this.options.createComponent({
            id: options.id,
            name: options.component,
        });
        view.orientation = this.splitview.orientation;
        view.init({
            params: (_a = options.params) !== null && _a !== void 0 ? _a : {},
            minimumSize: options.minimumSize,
            maximumSize: options.maximumSize,
            snap: options.snap,
            priority: options.priority,
            accessor: this,
        });
        var size = typeof options.size === 'number' ? options.size : splitview_1.Sizing.Distribute;
        var index = typeof options.index === 'number' ? options.index : undefined;
        this.splitview.addView(view, size, index);
        this.doAddView(view);
        this.setActive(view);
        return view;
    };
    SplitviewComponent.prototype.layout = function (width, height) {
        var _a = __read(this.splitview.orientation === splitview_1.Orientation.HORIZONTAL
            ? [width, height]
            : [height, width], 2), size = _a[0], orthogonalSize = _a[1];
        this.splitview.layout(size, orthogonalSize);
    };
    SplitviewComponent.prototype.doAddView = function (view) {
        var _this = this;
        var disposable = view.api.onDidFocusChange(function (event) {
            if (!event.isFocused) {
                return;
            }
            _this.setActive(view, true);
        });
        this._panels.set(view.id, disposable);
    };
    SplitviewComponent.prototype.toJSON = function () {
        var _this = this;
        var _a;
        var views = this.splitview
            .getViews()
            .map(function (view, i) {
            var size = _this.splitview.getViewSize(i);
            return {
                size: size,
                data: view.toJSON(),
                snap: !!view.snap,
                priority: view.priority,
            };
        });
        return {
            views: views,
            activeView: (_a = this._activePanel) === null || _a === void 0 ? void 0 : _a.id,
            size: this.splitview.size,
            orientation: this.splitview.orientation,
        };
    };
    SplitviewComponent.prototype.fromJSON = function (serializedSplitview) {
        var _this = this;
        this.clear();
        var views = serializedSplitview.views, orientation = serializedSplitview.orientation, size = serializedSplitview.size, activeView = serializedSplitview.activeView;
        var queue = [];
        // take note of the existing dimensions
        var width = this.width;
        var height = this.height;
        this.splitview = new splitview_1.Splitview(this.element, {
            orientation: orientation,
            proportionalLayout: this.options.proportionalLayout,
            descriptor: {
                size: size,
                views: views.map(function (view) {
                    var data = view.data;
                    if (_this._panels.has(data.id)) {
                        throw new Error("panel ".concat(data.id, " already exists"));
                    }
                    var panel = _this.options.createComponent({
                        id: data.id,
                        name: data.component,
                    });
                    queue.push(function () {
                        var _a;
                        panel.init({
                            params: (_a = data.params) !== null && _a !== void 0 ? _a : {},
                            minimumSize: data.minimumSize,
                            maximumSize: data.maximumSize,
                            snap: view.snap,
                            priority: view.priority,
                            accessor: _this,
                        });
                    });
                    panel.orientation = orientation;
                    _this.doAddView(panel);
                    setTimeout(function () {
                        // the original onDidAddView events are missed since they are fired before we can subcribe to them
                        _this._onDidAddView.fire(panel);
                    }, 0);
                    return { size: view.size, view: panel };
                }),
            },
        });
        this.layout(width, height);
        queue.forEach(function (f) { return f(); });
        if (typeof activeView === 'string') {
            var panel = this.getPanel(activeView);
            if (panel) {
                this.setActive(panel);
            }
        }
        this._onDidLayoutfromJSON.fire();
    };
    SplitviewComponent.prototype.clear = function () {
        var e_1, _a;
        try {
            for (var _b = __values(this._panels.values()), _c = _b.next(); !_c.done; _c = _b.next()) {
                var disposable = _c.value;
                disposable.dispose();
            }
        }
        catch (e_1_1) { e_1 = { error: e_1_1 }; }
        finally {
            try {
                if (_c && !_c.done && (_a = _b.return)) _a.call(_b);
            }
            finally { if (e_1) throw e_1.error; }
        }
        this._panels.clear();
        while (this.splitview.length > 0) {
            var view = this.splitview.removeView(0, splitview_1.Sizing.Distribute, true);
            view.dispose();
        }
    };
    SplitviewComponent.prototype.dispose = function () {
        var e_2, _a, e_3, _b;
        try {
            for (var _c = __values(this._panels.values()), _d = _c.next(); !_d.done; _d = _c.next()) {
                var disposable = _d.value;
                disposable.dispose();
            }
        }
        catch (e_2_1) { e_2 = { error: e_2_1 }; }
        finally {
            try {
                if (_d && !_d.done && (_a = _c.return)) _a.call(_c);
            }
            finally { if (e_2) throw e_2.error; }
        }
        this._panels.clear();
        var views = this.splitview.getViews();
        this._splitviewChangeDisposable.dispose();
        this.splitview.dispose();
        try {
            for (var views_1 = __values(views), views_1_1 = views_1.next(); !views_1_1.done; views_1_1 = views_1.next()) {
                var view = views_1_1.value;
                view.dispose();
            }
        }
        catch (e_3_1) { e_3 = { error: e_3_1 }; }
        finally {
            try {
                if (views_1_1 && !views_1_1.done && (_b = views_1.return)) _b.call(views_1);
            }
            finally { if (e_3) throw e_3.error; }
        }
        this.element.remove();
        _super.prototype.dispose.call(this);
    };
    return SplitviewComponent;
}(resizable_1.Resizable));
exports.SplitviewComponent = SplitviewComponent;
