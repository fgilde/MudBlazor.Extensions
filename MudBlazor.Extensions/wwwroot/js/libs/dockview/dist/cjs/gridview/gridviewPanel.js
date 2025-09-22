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
Object.defineProperty(exports, "__esModule", { value: true });
exports.GridviewPanel = void 0;
var basePanelView_1 = require("./basePanelView");
var gridviewPanelApi_1 = require("../api/gridviewPanelApi");
var events_1 = require("../events");
var GridviewPanel = /** @class */ (function (_super) {
    __extends(GridviewPanel, _super);
    function GridviewPanel(id, component, options, api) {
        var _this = _super.call(this, id, component, api !== null && api !== void 0 ? api : new gridviewPanelApi_1.GridviewPanelApiImpl(id, component)) || this;
        _this._evaluatedMinimumWidth = 0;
        _this._evaluatedMaximumWidth = Number.MAX_SAFE_INTEGER;
        _this._evaluatedMinimumHeight = 0;
        _this._evaluatedMaximumHeight = Number.MAX_SAFE_INTEGER;
        _this._minimumWidth = 0;
        _this._minimumHeight = 0;
        _this._maximumWidth = Number.MAX_SAFE_INTEGER;
        _this._maximumHeight = Number.MAX_SAFE_INTEGER;
        _this._snap = false;
        _this._onDidChange = new events_1.Emitter();
        _this.onDidChange = _this._onDidChange.event;
        if (typeof (options === null || options === void 0 ? void 0 : options.minimumWidth) === 'number') {
            _this._minimumWidth = options.minimumWidth;
        }
        if (typeof (options === null || options === void 0 ? void 0 : options.maximumWidth) === 'number') {
            _this._maximumWidth = options.maximumWidth;
        }
        if (typeof (options === null || options === void 0 ? void 0 : options.minimumHeight) === 'number') {
            _this._minimumHeight = options.minimumHeight;
        }
        if (typeof (options === null || options === void 0 ? void 0 : options.maximumHeight) === 'number') {
            _this._maximumHeight = options.maximumHeight;
        }
        _this.api.initialize(_this); // TODO: required to by-pass 'super before this' requirement
        _this.addDisposables(_this.api.onWillVisibilityChange(function (event) {
            var isVisible = event.isVisible;
            var accessor = _this._params.accessor;
            accessor.setVisible(_this, isVisible);
        }), _this.api.onActiveChange(function () {
            var accessor = _this._params.accessor;
            accessor.doSetGroupActive(_this);
        }), _this.api.onDidConstraintsChangeInternal(function (event) {
            if (typeof event.minimumWidth === 'number' ||
                typeof event.minimumWidth === 'function') {
                _this._minimumWidth = event.minimumWidth;
            }
            if (typeof event.minimumHeight === 'number' ||
                typeof event.minimumHeight === 'function') {
                _this._minimumHeight = event.minimumHeight;
            }
            if (typeof event.maximumWidth === 'number' ||
                typeof event.maximumWidth === 'function') {
                _this._maximumWidth = event.maximumWidth;
            }
            if (typeof event.maximumHeight === 'number' ||
                typeof event.maximumHeight === 'function') {
                _this._maximumHeight = event.maximumHeight;
            }
        }), _this.api.onDidSizeChange(function (event) {
            _this._onDidChange.fire({
                height: event.height,
                width: event.width,
            });
        }), _this._onDidChange);
        return _this;
    }
    Object.defineProperty(GridviewPanel.prototype, "priority", {
        get: function () {
            return this._priority;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(GridviewPanel.prototype, "snap", {
        get: function () {
            return this._snap;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(GridviewPanel.prototype, "minimumWidth", {
        get: function () {
            /**
             * defer to protected function to allow subclasses to override easily.
             * see https://github.com/microsoft/TypeScript/issues/338
             */
            return this.__minimumWidth();
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(GridviewPanel.prototype, "minimumHeight", {
        get: function () {
            /**
             * defer to protected function to allow subclasses to override easily.
             * see https://github.com/microsoft/TypeScript/issues/338
             */
            return this.__minimumHeight();
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(GridviewPanel.prototype, "maximumHeight", {
        get: function () {
            /**
             * defer to protected function to allow subclasses to override easily.
             * see https://github.com/microsoft/TypeScript/issues/338
             */
            return this.__maximumHeight();
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(GridviewPanel.prototype, "maximumWidth", {
        get: function () {
            /**
             * defer to protected function to allow subclasses to override easily.
             * see https://github.com/microsoft/TypeScript/issues/338
             */
            return this.__maximumWidth();
        },
        enumerable: false,
        configurable: true
    });
    GridviewPanel.prototype.__minimumWidth = function () {
        var width = typeof this._minimumWidth === 'function'
            ? this._minimumWidth()
            : this._minimumWidth;
        if (width !== this._evaluatedMinimumWidth) {
            this._evaluatedMinimumWidth = width;
            this.updateConstraints();
        }
        return width;
    };
    GridviewPanel.prototype.__maximumWidth = function () {
        var width = typeof this._maximumWidth === 'function'
            ? this._maximumWidth()
            : this._maximumWidth;
        if (width !== this._evaluatedMaximumWidth) {
            this._evaluatedMaximumWidth = width;
            this.updateConstraints();
        }
        return width;
    };
    GridviewPanel.prototype.__minimumHeight = function () {
        var height = typeof this._minimumHeight === 'function'
            ? this._minimumHeight()
            : this._minimumHeight;
        if (height !== this._evaluatedMinimumHeight) {
            this._evaluatedMinimumHeight = height;
            this.updateConstraints();
        }
        return height;
    };
    GridviewPanel.prototype.__maximumHeight = function () {
        var height = typeof this._maximumHeight === 'function'
            ? this._maximumHeight()
            : this._maximumHeight;
        if (height !== this._evaluatedMaximumHeight) {
            this._evaluatedMaximumHeight = height;
            this.updateConstraints();
        }
        return height;
    };
    Object.defineProperty(GridviewPanel.prototype, "isActive", {
        get: function () {
            return this.api.isActive;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(GridviewPanel.prototype, "isVisible", {
        get: function () {
            return this.api.isVisible;
        },
        enumerable: false,
        configurable: true
    });
    GridviewPanel.prototype.setVisible = function (isVisible) {
        this.api._onDidVisibilityChange.fire({ isVisible: isVisible });
    };
    GridviewPanel.prototype.setActive = function (isActive) {
        this.api._onDidActiveChange.fire({ isActive: isActive });
    };
    GridviewPanel.prototype.init = function (parameters) {
        if (parameters.maximumHeight) {
            this._maximumHeight = parameters.maximumHeight;
        }
        if (parameters.minimumHeight) {
            this._minimumHeight = parameters.minimumHeight;
        }
        if (parameters.maximumWidth) {
            this._maximumWidth = parameters.maximumWidth;
        }
        if (parameters.minimumWidth) {
            this._minimumWidth = parameters.minimumWidth;
        }
        this._priority = parameters.priority;
        this._snap = !!parameters.snap;
        _super.prototype.init.call(this, parameters);
        if (typeof parameters.isVisible === 'boolean') {
            this.setVisible(parameters.isVisible);
        }
    };
    GridviewPanel.prototype.updateConstraints = function () {
        this.api._onDidConstraintsChange.fire({
            minimumWidth: this._evaluatedMinimumWidth,
            maximumWidth: this._evaluatedMaximumWidth,
            minimumHeight: this._evaluatedMinimumHeight,
            maximumHeight: this._evaluatedMaximumHeight,
        });
    };
    GridviewPanel.prototype.toJSON = function () {
        var state = _super.prototype.toJSON.call(this);
        var maximum = function (value) {
            return value === Number.MAX_SAFE_INTEGER ? undefined : value;
        };
        var minimum = function (value) { return (value <= 0 ? undefined : value); };
        return __assign(__assign({}, state), { minimumHeight: minimum(this.minimumHeight), maximumHeight: maximum(this.maximumHeight), minimumWidth: minimum(this.minimumWidth), maximumWidth: maximum(this.maximumWidth), snap: this.snap, priority: this.priority });
    };
    return GridviewPanel;
}(basePanelView_1.BasePanelView));
exports.GridviewPanel = GridviewPanel;
