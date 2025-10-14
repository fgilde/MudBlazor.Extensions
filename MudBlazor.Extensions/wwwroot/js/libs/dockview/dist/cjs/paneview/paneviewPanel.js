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
Object.defineProperty(exports, "__esModule", { value: true });
exports.PaneviewPanel = void 0;
var paneviewPanelApi_1 = require("../api/paneviewPanelApi");
var dom_1 = require("../dom");
var events_1 = require("../events");
var basePanelView_1 = require("../gridview/basePanelView");
var splitview_1 = require("../splitview/splitview");
var PaneviewPanel = /** @class */ (function (_super) {
    __extends(PaneviewPanel, _super);
    function PaneviewPanel(options) {
        var _this = _super.call(this, options.id, options.component, new paneviewPanelApi_1.PaneviewPanelApiImpl(options.id, options.component)) || this;
        _this._onDidChangeExpansionState = new events_1.Emitter({ replay: true });
        _this.onDidChangeExpansionState = _this._onDidChangeExpansionState.event;
        _this._onDidChange = new events_1.Emitter();
        _this.onDidChange = _this._onDidChange.event;
        _this._orthogonalSize = 0;
        _this._size = 0;
        _this._isExpanded = false;
        _this.api.pane = _this; // TODO cannot use 'this' before 'super'
        _this.api.initialize(_this);
        _this.headerSize = options.headerSize;
        _this.headerComponent = options.headerComponent;
        _this._minimumBodySize = options.minimumBodySize;
        _this._maximumBodySize = options.maximumBodySize;
        _this._isExpanded = options.isExpanded;
        _this._headerVisible = options.isHeaderVisible;
        _this._onDidChangeExpansionState.fire(_this.isExpanded()); // initialize value
        _this._orientation = options.orientation;
        _this.element.classList.add('dv-pane');
        _this.addDisposables(_this.api.onWillVisibilityChange(function (event) {
            var isVisible = event.isVisible;
            var accessor = _this._params.accessor;
            accessor.setVisible(_this, isVisible);
        }), _this.api.onDidSizeChange(function (event) {
            _this._onDidChange.fire({ size: event.size });
        }), (0, events_1.addDisposableListener)(_this.element, 'mouseenter', function (ev) {
            _this.api._onMouseEnter.fire(ev);
        }), (0, events_1.addDisposableListener)(_this.element, 'mouseleave', function (ev) {
            _this.api._onMouseLeave.fire(ev);
        }));
        _this.addDisposables(_this._onDidChangeExpansionState, _this.onDidChangeExpansionState(function (isPanelExpanded) {
            _this.api._onDidExpansionChange.fire({
                isExpanded: isPanelExpanded,
            });
        }), _this.api.onDidFocusChange(function (e) {
            if (!_this.header) {
                return;
            }
            if (e.isFocused) {
                (0, dom_1.addClasses)(_this.header, 'focused');
            }
            else {
                (0, dom_1.removeClasses)(_this.header, 'focused');
            }
        }));
        _this.renderOnce();
        return _this;
    }
    Object.defineProperty(PaneviewPanel.prototype, "orientation", {
        get: function () {
            return this._orientation;
        },
        set: function (value) {
            this._orientation = value;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(PaneviewPanel.prototype, "minimumSize", {
        get: function () {
            var headerSize = this.headerSize;
            var expanded = this.isExpanded();
            var minimumBodySize = expanded ? this._minimumBodySize : 0;
            return headerSize + minimumBodySize;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(PaneviewPanel.prototype, "maximumSize", {
        get: function () {
            var headerSize = this.headerSize;
            var expanded = this.isExpanded();
            var maximumBodySize = expanded ? this._maximumBodySize : 0;
            return headerSize + maximumBodySize;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(PaneviewPanel.prototype, "size", {
        get: function () {
            return this._size;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(PaneviewPanel.prototype, "orthogonalSize", {
        get: function () {
            return this._orthogonalSize;
        },
        set: function (size) {
            this._orthogonalSize = size;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(PaneviewPanel.prototype, "minimumBodySize", {
        get: function () {
            return this._minimumBodySize;
        },
        set: function (value) {
            this._minimumBodySize = typeof value === 'number' ? value : 0;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(PaneviewPanel.prototype, "maximumBodySize", {
        get: function () {
            return this._maximumBodySize;
        },
        set: function (value) {
            this._maximumBodySize =
                typeof value === 'number' ? value : Number.POSITIVE_INFINITY;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(PaneviewPanel.prototype, "headerVisible", {
        get: function () {
            return this._headerVisible;
        },
        set: function (value) {
            this._headerVisible = value;
            this.header.style.display = value ? '' : 'none';
        },
        enumerable: false,
        configurable: true
    });
    PaneviewPanel.prototype.setVisible = function (isVisible) {
        this.api._onDidVisibilityChange.fire({ isVisible: isVisible });
    };
    PaneviewPanel.prototype.setActive = function (isActive) {
        this.api._onDidActiveChange.fire({ isActive: isActive });
    };
    PaneviewPanel.prototype.isExpanded = function () {
        return this._isExpanded;
    };
    PaneviewPanel.prototype.setExpanded = function (expanded) {
        var _this = this;
        if (this._isExpanded === expanded) {
            return;
        }
        this._isExpanded = expanded;
        if (expanded) {
            if (this.animationTimer) {
                clearTimeout(this.animationTimer);
            }
            if (this.body) {
                this.element.appendChild(this.body);
            }
        }
        else {
            this.animationTimer = setTimeout(function () {
                var _a;
                (_a = _this.body) === null || _a === void 0 ? void 0 : _a.remove();
            }, 200);
        }
        this._onDidChange.fire(expanded ? { size: this.width } : {});
        this._onDidChangeExpansionState.fire(expanded);
    };
    PaneviewPanel.prototype.layout = function (size, orthogonalSize) {
        this._size = size;
        this._orthogonalSize = orthogonalSize;
        var _a = __read(this.orientation === splitview_1.Orientation.HORIZONTAL
            ? [size, orthogonalSize]
            : [orthogonalSize, size], 2), width = _a[0], height = _a[1];
        _super.prototype.layout.call(this, width, height);
    };
    PaneviewPanel.prototype.init = function (parameters) {
        var _a, _b;
        _super.prototype.init.call(this, parameters);
        if (typeof parameters.minimumBodySize === 'number') {
            this.minimumBodySize = parameters.minimumBodySize;
        }
        if (typeof parameters.maximumBodySize === 'number') {
            this.maximumBodySize = parameters.maximumBodySize;
        }
        this.bodyPart = this.getBodyComponent();
        this.headerPart = this.getHeaderComponent();
        this.bodyPart.init(__assign(__assign({}, parameters), { api: this.api }));
        this.headerPart.init(__assign(__assign({}, parameters), { api: this.api }));
        (_a = this.body) === null || _a === void 0 ? void 0 : _a.append(this.bodyPart.element);
        (_b = this.header) === null || _b === void 0 ? void 0 : _b.append(this.headerPart.element);
        if (typeof parameters.isExpanded === 'boolean') {
            this.setExpanded(parameters.isExpanded);
        }
    };
    PaneviewPanel.prototype.toJSON = function () {
        var params = this._params;
        return __assign(__assign({}, _super.prototype.toJSON.call(this)), { headerComponent: this.headerComponent, title: params.title });
    };
    PaneviewPanel.prototype.renderOnce = function () {
        this.header = document.createElement('div');
        this.header.tabIndex = 0;
        this.header.className = 'dv-pane-header';
        this.header.style.height = "".concat(this.headerSize, "px");
        this.header.style.lineHeight = "".concat(this.headerSize, "px");
        this.header.style.minHeight = "".concat(this.headerSize, "px");
        this.header.style.maxHeight = "".concat(this.headerSize, "px");
        this.element.appendChild(this.header);
        this.body = document.createElement('div');
        this.body.className = 'dv-pane-body';
        this.element.appendChild(this.body);
    };
    // TODO slightly hacky by-pass of the component to create a body and header component
    PaneviewPanel.prototype.getComponent = function () {
        var _this = this;
        return {
            update: function (params) {
                var _a, _b;
                (_a = _this.bodyPart) === null || _a === void 0 ? void 0 : _a.update({ params: params });
                (_b = _this.headerPart) === null || _b === void 0 ? void 0 : _b.update({ params: params });
            },
            dispose: function () {
                var _a, _b;
                (_a = _this.bodyPart) === null || _a === void 0 ? void 0 : _a.dispose();
                (_b = _this.headerPart) === null || _b === void 0 ? void 0 : _b.dispose();
            },
        };
    };
    return PaneviewPanel;
}(basePanelView_1.BasePanelView));
exports.PaneviewPanel = PaneviewPanel;
