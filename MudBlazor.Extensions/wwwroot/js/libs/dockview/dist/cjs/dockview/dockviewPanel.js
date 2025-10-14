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
Object.defineProperty(exports, "__esModule", { value: true });
exports.DockviewPanel = void 0;
var dockviewPanelApi_1 = require("../api/dockviewPanelApi");
var lifecycle_1 = require("../lifecycle");
var panelApi_1 = require("../api/panelApi");
var DockviewPanel = /** @class */ (function (_super) {
    __extends(DockviewPanel, _super);
    function DockviewPanel(id, component, tabComponent, accessor, containerApi, group, view, options) {
        var _this = _super.call(this) || this;
        _this.id = id;
        _this.accessor = accessor;
        _this.containerApi = containerApi;
        _this.view = view;
        _this._renderer = options.renderer;
        _this._group = group;
        _this._minimumWidth = options.minimumWidth;
        _this._minimumHeight = options.minimumHeight;
        _this._maximumWidth = options.maximumWidth;
        _this._maximumHeight = options.maximumHeight;
        _this.api = new dockviewPanelApi_1.DockviewPanelApiImpl(_this, _this._group, accessor, component, tabComponent);
        _this.addDisposables(_this.api.onActiveChange(function () {
            accessor.setActivePanel(_this);
        }), _this.api.onDidSizeChange(function (event) {
            // forward the resize event to the group since if you want to resize a panel
            // you are actually just resizing the panels parent which is the group
            _this.group.api.setSize(event);
        }), _this.api.onDidRendererChange(function () {
            _this.group.model.rerender(_this);
        }));
        return _this;
    }
    Object.defineProperty(DockviewPanel.prototype, "params", {
        get: function () {
            return this._params;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewPanel.prototype, "title", {
        get: function () {
            return this._title;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewPanel.prototype, "group", {
        get: function () {
            return this._group;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewPanel.prototype, "renderer", {
        get: function () {
            var _a;
            return (_a = this._renderer) !== null && _a !== void 0 ? _a : this.accessor.renderer;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewPanel.prototype, "minimumWidth", {
        get: function () {
            return this._minimumWidth;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewPanel.prototype, "minimumHeight", {
        get: function () {
            return this._minimumHeight;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewPanel.prototype, "maximumWidth", {
        get: function () {
            return this._maximumWidth;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewPanel.prototype, "maximumHeight", {
        get: function () {
            return this._maximumHeight;
        },
        enumerable: false,
        configurable: true
    });
    DockviewPanel.prototype.init = function (params) {
        this._params = params.params;
        this.view.init(__assign(__assign({}, params), { api: this.api, containerApi: this.containerApi }));
        this.setTitle(params.title);
    };
    DockviewPanel.prototype.focus = function () {
        var event = new panelApi_1.WillFocusEvent();
        this.api._onWillFocus.fire(event);
        if (event.defaultPrevented) {
            return;
        }
        if (!this.api.isActive) {
            this.api.setActive();
        }
    };
    DockviewPanel.prototype.toJSON = function () {
        return {
            id: this.id,
            contentComponent: this.view.contentComponent,
            tabComponent: this.view.tabComponent,
            params: Object.keys(this._params || {}).length > 0
                ? this._params
                : undefined,
            title: this.title,
            renderer: this._renderer,
            minimumHeight: this._minimumHeight,
            maximumHeight: this._maximumHeight,
            minimumWidth: this._minimumWidth,
            maximumWidth: this._maximumWidth,
        };
    };
    DockviewPanel.prototype.setTitle = function (title) {
        var didTitleChange = title !== this.title;
        if (didTitleChange) {
            this._title = title;
            this.api._onDidTitleChange.fire({ title: title });
        }
    };
    DockviewPanel.prototype.setRenderer = function (renderer) {
        var didChange = renderer !== this.renderer;
        if (didChange) {
            this._renderer = renderer;
            this.api._onDidRendererChange.fire({
                renderer: renderer,
            });
        }
    };
    DockviewPanel.prototype.update = function (event) {
        var e_1, _a;
        var _b;
        // merge the new parameters with the existing parameters
        this._params = __assign(__assign({}, ((_b = this._params) !== null && _b !== void 0 ? _b : {})), event.params);
        try {
            /**
             * delete new keys that have a value of undefined,
             * allow values of null
             */
            for (var _c = __values(Object.keys(event.params)), _d = _c.next(); !_d.done; _d = _c.next()) {
                var key = _d.value;
                if (event.params[key] === undefined) {
                    delete this._params[key];
                }
            }
        }
        catch (e_1_1) { e_1 = { error: e_1_1 }; }
        finally {
            try {
                if (_d && !_d.done && (_a = _c.return)) _a.call(_c);
            }
            finally { if (e_1) throw e_1.error; }
        }
        // update the view with the updated props
        this.view.update({
            params: this._params,
        });
    };
    DockviewPanel.prototype.updateParentGroup = function (group, options) {
        this._group = group;
        this.api.group = this._group;
        var isPanelVisible = this._group.model.isPanelActive(this);
        var isActive = this.group.api.isActive && isPanelVisible;
        if (!(options === null || options === void 0 ? void 0 : options.skipSetActive)) {
            if (this.api.isActive !== isActive) {
                this.api._onDidActiveChange.fire({
                    isActive: this.group.api.isActive && isPanelVisible,
                });
            }
        }
        if (this.api.isVisible !== isPanelVisible) {
            this.api._onDidVisibilityChange.fire({
                isVisible: isPanelVisible,
            });
        }
    };
    DockviewPanel.prototype.runEvents = function () {
        var isPanelVisible = this._group.model.isPanelActive(this);
        var isActive = this.group.api.isActive && isPanelVisible;
        if (this.api.isActive !== isActive) {
            this.api._onDidActiveChange.fire({
                isActive: this.group.api.isActive && isPanelVisible,
            });
        }
        if (this.api.isVisible !== isPanelVisible) {
            this.api._onDidVisibilityChange.fire({
                isVisible: isPanelVisible,
            });
        }
    };
    DockviewPanel.prototype.layout = function (width, height) {
        // TODO: Can we somehow do height without header height or indicate what the header height is?
        this.api._onDidDimensionChange.fire({
            width: width,
            height: height,
        });
        this.view.layout(width, height);
    };
    DockviewPanel.prototype.dispose = function () {
        this.api.dispose();
        this.view.dispose();
    };
    return DockviewPanel;
}(lifecycle_1.CompositeDisposable));
exports.DockviewPanel = DockviewPanel;
