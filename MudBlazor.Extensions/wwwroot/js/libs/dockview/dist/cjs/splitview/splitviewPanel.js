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
exports.SplitviewPanel = void 0;
var basePanelView_1 = require("../gridview/basePanelView");
var splitviewPanelApi_1 = require("../api/splitviewPanelApi");
var splitview_1 = require("./splitview");
var events_1 = require("../events");
var SplitviewPanel = /** @class */ (function (_super) {
    __extends(SplitviewPanel, _super);
    function SplitviewPanel(id, componentName) {
        var _this = _super.call(this, id, componentName, new splitviewPanelApi_1.SplitviewPanelApiImpl(id, componentName)) || this;
        _this._evaluatedMinimumSize = 0;
        _this._evaluatedMaximumSize = Number.POSITIVE_INFINITY;
        _this._minimumSize = 0;
        _this._maximumSize = Number.POSITIVE_INFINITY;
        _this._snap = false;
        _this._onDidChange = new events_1.Emitter();
        _this.onDidChange = _this._onDidChange.event;
        _this.api.initialize(_this);
        _this.addDisposables(_this._onDidChange, _this.api.onWillVisibilityChange(function (event) {
            var isVisible = event.isVisible;
            var accessor = _this._params.accessor;
            accessor.setVisible(_this, isVisible);
        }), _this.api.onActiveChange(function () {
            var accessor = _this._params.accessor;
            accessor.setActive(_this);
        }), _this.api.onDidConstraintsChangeInternal(function (event) {
            if (typeof event.minimumSize === 'number' ||
                typeof event.minimumSize === 'function') {
                _this._minimumSize = event.minimumSize;
            }
            if (typeof event.maximumSize === 'number' ||
                typeof event.maximumSize === 'function') {
                _this._maximumSize = event.maximumSize;
            }
            _this.updateConstraints();
        }), _this.api.onDidSizeChange(function (event) {
            _this._onDidChange.fire({ size: event.size });
        }));
        return _this;
    }
    Object.defineProperty(SplitviewPanel.prototype, "priority", {
        get: function () {
            return this._priority;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(SplitviewPanel.prototype, "orientation", {
        get: function () {
            return this._orientation;
        },
        set: function (value) {
            this._orientation = value;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(SplitviewPanel.prototype, "minimumSize", {
        get: function () {
            var size = typeof this._minimumSize === 'function'
                ? this._minimumSize()
                : this._minimumSize;
            if (size !== this._evaluatedMinimumSize) {
                this._evaluatedMinimumSize = size;
                this.updateConstraints();
            }
            return size;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(SplitviewPanel.prototype, "maximumSize", {
        get: function () {
            var size = typeof this._maximumSize === 'function'
                ? this._maximumSize()
                : this._maximumSize;
            if (size !== this._evaluatedMaximumSize) {
                this._evaluatedMaximumSize = size;
                this.updateConstraints();
            }
            return size;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(SplitviewPanel.prototype, "snap", {
        get: function () {
            return this._snap;
        },
        enumerable: false,
        configurable: true
    });
    SplitviewPanel.prototype.setVisible = function (isVisible) {
        this.api._onDidVisibilityChange.fire({ isVisible: isVisible });
    };
    SplitviewPanel.prototype.setActive = function (isActive) {
        this.api._onDidActiveChange.fire({ isActive: isActive });
    };
    SplitviewPanel.prototype.layout = function (size, orthogonalSize) {
        var _a = __read(this.orientation === splitview_1.Orientation.HORIZONTAL
            ? [size, orthogonalSize]
            : [orthogonalSize, size], 2), width = _a[0], height = _a[1];
        _super.prototype.layout.call(this, width, height);
    };
    SplitviewPanel.prototype.init = function (parameters) {
        _super.prototype.init.call(this, parameters);
        this._priority = parameters.priority;
        if (parameters.minimumSize) {
            this._minimumSize = parameters.minimumSize;
        }
        if (parameters.maximumSize) {
            this._maximumSize = parameters.maximumSize;
        }
        if (parameters.snap) {
            this._snap = parameters.snap;
        }
    };
    SplitviewPanel.prototype.toJSON = function () {
        var maximum = function (value) {
            return value === Number.MAX_SAFE_INTEGER ||
                value === Number.POSITIVE_INFINITY
                ? undefined
                : value;
        };
        var minimum = function (value) { return (value <= 0 ? undefined : value); };
        return __assign(__assign({}, _super.prototype.toJSON.call(this)), { minimumSize: minimum(this.minimumSize), maximumSize: maximum(this.maximumSize) });
    };
    SplitviewPanel.prototype.updateConstraints = function () {
        this.api._onDidConstraintsChange.fire({
            maximumSize: this._evaluatedMaximumSize,
            minimumSize: this._evaluatedMinimumSize,
        });
    };
    return SplitviewPanel;
}(basePanelView_1.BasePanelView));
exports.SplitviewPanel = SplitviewPanel;
