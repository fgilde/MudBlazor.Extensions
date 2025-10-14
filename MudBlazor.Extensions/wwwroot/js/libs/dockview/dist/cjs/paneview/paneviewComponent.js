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
exports.PaneviewComponent = exports.PaneFramework = void 0;
var component_api_1 = require("../api/component.api");
var events_1 = require("../events");
var lifecycle_1 = require("../lifecycle");
var splitview_1 = require("../splitview/splitview");
var paneview_1 = require("./paneview");
var draggablePaneviewPanel_1 = require("./draggablePaneviewPanel");
var defaultPaneviewHeader_1 = require("./defaultPaneviewHeader");
var math_1 = require("../math");
var resizable_1 = require("../resizable");
var dom_1 = require("../dom");
var nextLayoutId = (0, math_1.sequentialNumberGenerator)();
var HEADER_SIZE = 22;
var MINIMUM_BODY_SIZE = 0;
var MAXIMUM_BODY_SIZE = Number.MAX_SAFE_INTEGER;
var PaneFramework = /** @class */ (function (_super) {
    __extends(PaneFramework, _super);
    function PaneFramework(options) {
        var _this = _super.call(this, {
            accessor: options.accessor,
            id: options.id,
            component: options.component,
            headerComponent: options.headerComponent,
            orientation: options.orientation,
            isExpanded: options.isExpanded,
            disableDnd: options.disableDnd,
            headerSize: options.headerSize,
            minimumBodySize: options.minimumBodySize,
            maximumBodySize: options.maximumBodySize,
        }) || this;
        _this.options = options;
        return _this;
    }
    PaneFramework.prototype.getBodyComponent = function () {
        return this.options.body;
    };
    PaneFramework.prototype.getHeaderComponent = function () {
        return this.options.header;
    };
    return PaneFramework;
}(draggablePaneviewPanel_1.DraggablePaneviewPanel));
exports.PaneFramework = PaneFramework;
var PaneviewComponent = /** @class */ (function (_super) {
    __extends(PaneviewComponent, _super);
    function PaneviewComponent(container, options) {
        var _a;
        var _this = _super.call(this, document.createElement('div'), options.disableAutoResizing) || this;
        _this._id = nextLayoutId.next();
        _this._disposable = new lifecycle_1.MutableDisposable();
        _this._viewDisposables = new Map();
        _this._onDidLayoutfromJSON = new events_1.Emitter();
        _this.onDidLayoutFromJSON = _this._onDidLayoutfromJSON.event;
        _this._onDidLayoutChange = new events_1.Emitter();
        _this.onDidLayoutChange = _this._onDidLayoutChange.event;
        _this._onDidDrop = new events_1.Emitter();
        _this.onDidDrop = _this._onDidDrop.event;
        _this._onDidAddView = new events_1.Emitter();
        _this.onDidAddView = _this._onDidAddView.event;
        _this._onDidRemoveView = new events_1.Emitter();
        _this.onDidRemoveView = _this._onDidRemoveView.event;
        _this._onUnhandledDragOverEvent = new events_1.Emitter();
        _this.onUnhandledDragOverEvent = _this._onUnhandledDragOverEvent.event;
        _this.element.style.height = '100%';
        _this.element.style.width = '100%';
        _this.addDisposables(_this._onDidLayoutChange, _this._onDidLayoutfromJSON, _this._onDidDrop, _this._onDidAddView, _this._onDidRemoveView, _this._onUnhandledDragOverEvent);
        _this._classNames = new dom_1.Classnames(_this.element);
        _this._classNames.setClassNames((_a = options.className) !== null && _a !== void 0 ? _a : '');
        // the container is owned by the third-party, do not modify/delete it
        container.appendChild(_this.element);
        _this._options = options;
        _this.paneview = new paneview_1.Paneview(_this.element, {
            // only allow paneview in the vertical orientation for now
            orientation: splitview_1.Orientation.VERTICAL,
        });
        _this.addDisposables(_this._disposable);
        return _this;
    }
    Object.defineProperty(PaneviewComponent.prototype, "id", {
        get: function () {
            return this._id;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(PaneviewComponent.prototype, "panels", {
        get: function () {
            return this.paneview.getPanes();
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(PaneviewComponent.prototype, "paneview", {
        get: function () {
            return this._paneview;
        },
        set: function (value) {
            var _this = this;
            this._paneview = value;
            this._disposable.value = new lifecycle_1.CompositeDisposable(this._paneview.onDidChange(function () {
                _this._onDidLayoutChange.fire(undefined);
            }), this._paneview.onDidAddView(function (e) { return _this._onDidAddView.fire(e); }), this._paneview.onDidRemoveView(function (e) { return _this._onDidRemoveView.fire(e); }));
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(PaneviewComponent.prototype, "minimumSize", {
        get: function () {
            return this.paneview.minimumSize;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(PaneviewComponent.prototype, "maximumSize", {
        get: function () {
            return this.paneview.maximumSize;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(PaneviewComponent.prototype, "height", {
        get: function () {
            return this.paneview.orientation === splitview_1.Orientation.HORIZONTAL
                ? this.paneview.orthogonalSize
                : this.paneview.size;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(PaneviewComponent.prototype, "width", {
        get: function () {
            return this.paneview.orientation === splitview_1.Orientation.HORIZONTAL
                ? this.paneview.size
                : this.paneview.orthogonalSize;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(PaneviewComponent.prototype, "options", {
        get: function () {
            return this._options;
        },
        enumerable: false,
        configurable: true
    });
    PaneviewComponent.prototype.setVisible = function (panel, visible) {
        var index = this.panels.indexOf(panel);
        this.paneview.setViewVisible(index, visible);
    };
    PaneviewComponent.prototype.focus = function () {
        //noop
    };
    PaneviewComponent.prototype.updateOptions = function (options) {
        var _a, _b;
        if ('className' in options) {
            this._classNames.setClassNames((_a = options.className) !== null && _a !== void 0 ? _a : '');
        }
        if ('disableResizing' in options) {
            this.disableResizing = (_b = options.disableAutoResizing) !== null && _b !== void 0 ? _b : false;
        }
        this._options = __assign(__assign({}, this.options), options);
    };
    PaneviewComponent.prototype.addPanel = function (options) {
        var _a, _b;
        var body = this.options.createComponent({
            id: options.id,
            name: options.component,
        });
        var header;
        if (options.headerComponent && this.options.createHeaderComponent) {
            header = this.options.createHeaderComponent({
                id: options.id,
                name: options.headerComponent,
            });
        }
        if (!header) {
            header = new defaultPaneviewHeader_1.DefaultHeader();
        }
        var view = new PaneFramework({
            id: options.id,
            component: options.component,
            headerComponent: options.headerComponent,
            header: header,
            body: body,
            orientation: splitview_1.Orientation.VERTICAL,
            isExpanded: !!options.isExpanded,
            disableDnd: !!this.options.disableDnd,
            accessor: this,
            headerSize: (_a = options.headerSize) !== null && _a !== void 0 ? _a : HEADER_SIZE,
            minimumBodySize: MINIMUM_BODY_SIZE,
            maximumBodySize: MAXIMUM_BODY_SIZE,
        });
        this.doAddPanel(view);
        var size = typeof options.size === 'number' ? options.size : splitview_1.Sizing.Distribute;
        var index = typeof options.index === 'number' ? options.index : undefined;
        view.init({
            params: (_b = options.params) !== null && _b !== void 0 ? _b : {},
            minimumBodySize: options.minimumBodySize,
            maximumBodySize: options.maximumBodySize,
            isExpanded: options.isExpanded,
            title: options.title,
            containerApi: new component_api_1.PaneviewApi(this),
            accessor: this,
        });
        this.paneview.addPane(view, size, index);
        view.orientation = this.paneview.orientation;
        return view;
    };
    PaneviewComponent.prototype.removePanel = function (panel) {
        var views = this.panels;
        var index = views.findIndex(function (_) { return _ === panel; });
        this.paneview.removePane(index);
        this.doRemovePanel(panel);
    };
    PaneviewComponent.prototype.movePanel = function (from, to) {
        this.paneview.moveView(from, to);
    };
    PaneviewComponent.prototype.getPanel = function (id) {
        return this.panels.find(function (view) { return view.id === id; });
    };
    PaneviewComponent.prototype.layout = function (width, height) {
        var _a = __read(this.paneview.orientation === splitview_1.Orientation.HORIZONTAL
            ? [width, height]
            : [height, width], 2), size = _a[0], orthogonalSize = _a[1];
        this.paneview.layout(size, orthogonalSize);
    };
    PaneviewComponent.prototype.toJSON = function () {
        var _this = this;
        var maximum = function (value) {
            return value === Number.MAX_SAFE_INTEGER ||
                value === Number.POSITIVE_INFINITY
                ? undefined
                : value;
        };
        var minimum = function (value) { return (value <= 0 ? undefined : value); };
        var views = this.paneview
            .getPanes()
            .map(function (view, i) {
            var size = _this.paneview.getViewSize(i);
            return {
                size: size,
                data: view.toJSON(),
                minimumSize: minimum(view.minimumBodySize),
                maximumSize: maximum(view.maximumBodySize),
                headerSize: view.headerSize,
                expanded: view.isExpanded(),
            };
        });
        return {
            views: views,
            size: this.paneview.size,
        };
    };
    PaneviewComponent.prototype.fromJSON = function (serializedPaneview) {
        var _this = this;
        this.clear();
        var views = serializedPaneview.views, size = serializedPaneview.size;
        var queue = [];
        // take note of the existing dimensions
        var width = this.width;
        var height = this.height;
        this.paneview = new paneview_1.Paneview(this.element, {
            orientation: splitview_1.Orientation.VERTICAL,
            descriptor: {
                size: size,
                views: views.map(function (view) {
                    var _a, _b, _c;
                    var data = view.data;
                    var body = _this.options.createComponent({
                        id: data.id,
                        name: data.component,
                    });
                    var header;
                    if (data.headerComponent &&
                        _this.options.createHeaderComponent) {
                        header = _this.options.createHeaderComponent({
                            id: data.id,
                            name: data.headerComponent,
                        });
                    }
                    if (!header) {
                        header = new defaultPaneviewHeader_1.DefaultHeader();
                    }
                    var panel = new PaneFramework({
                        id: data.id,
                        component: data.component,
                        headerComponent: data.headerComponent,
                        header: header,
                        body: body,
                        orientation: splitview_1.Orientation.VERTICAL,
                        isExpanded: !!view.expanded,
                        disableDnd: !!_this.options.disableDnd,
                        accessor: _this,
                        headerSize: (_a = view.headerSize) !== null && _a !== void 0 ? _a : HEADER_SIZE,
                        minimumBodySize: (_b = view.minimumSize) !== null && _b !== void 0 ? _b : MINIMUM_BODY_SIZE,
                        maximumBodySize: (_c = view.maximumSize) !== null && _c !== void 0 ? _c : MAXIMUM_BODY_SIZE,
                    });
                    _this.doAddPanel(panel);
                    queue.push(function () {
                        var _a;
                        panel.init({
                            params: (_a = data.params) !== null && _a !== void 0 ? _a : {},
                            minimumBodySize: view.minimumSize,
                            maximumBodySize: view.maximumSize,
                            title: data.title,
                            isExpanded: !!view.expanded,
                            containerApi: new component_api_1.PaneviewApi(_this),
                            accessor: _this,
                        });
                        panel.orientation = _this.paneview.orientation;
                    });
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
        this._onDidLayoutfromJSON.fire();
    };
    PaneviewComponent.prototype.clear = function () {
        var e_1, _a;
        try {
            for (var _b = __values(this._viewDisposables.entries()), _c = _b.next(); !_c.done; _c = _b.next()) {
                var _d = __read(_c.value, 2), _ = _d[0], value = _d[1];
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
        this._viewDisposables.clear();
        this.paneview.dispose();
    };
    PaneviewComponent.prototype.doAddPanel = function (panel) {
        var _this = this;
        var disposable = new lifecycle_1.CompositeDisposable(panel.onDidDrop(function (event) {
            _this._onDidDrop.fire(event);
        }), panel.onUnhandledDragOverEvent(function (event) {
            _this._onUnhandledDragOverEvent.fire(event);
        }));
        this._viewDisposables.set(panel.id, disposable);
    };
    PaneviewComponent.prototype.doRemovePanel = function (panel) {
        var disposable = this._viewDisposables.get(panel.id);
        if (disposable) {
            disposable.dispose();
            this._viewDisposables.delete(panel.id);
        }
    };
    PaneviewComponent.prototype.dispose = function () {
        var e_2, _a;
        _super.prototype.dispose.call(this);
        try {
            for (var _b = __values(this._viewDisposables.entries()), _c = _b.next(); !_c.done; _c = _b.next()) {
                var _d = __read(_c.value, 2), _ = _d[0], value = _d[1];
                value.dispose();
            }
        }
        catch (e_2_1) { e_2 = { error: e_2_1 }; }
        finally {
            try {
                if (_c && !_c.done && (_a = _b.return)) _a.call(_b);
            }
            finally { if (e_2) throw e_2.error; }
        }
        this._viewDisposables.clear();
        this.element.remove();
        this.paneview.dispose();
    };
    return PaneviewComponent;
}(resizable_1.Resizable));
exports.PaneviewComponent = PaneviewComponent;
