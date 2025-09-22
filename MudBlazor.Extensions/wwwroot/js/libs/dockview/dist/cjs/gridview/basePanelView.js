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
exports.BasePanelView = void 0;
var dom_1 = require("../dom");
var lifecycle_1 = require("../lifecycle");
var panelApi_1 = require("../api/panelApi");
var BasePanelView = /** @class */ (function (_super) {
    __extends(BasePanelView, _super);
    function BasePanelView(id, component, api) {
        var _this = _super.call(this) || this;
        _this.id = id;
        _this.component = component;
        _this.api = api;
        _this._height = 0;
        _this._width = 0;
        _this._element = document.createElement('div');
        _this._element.tabIndex = -1;
        _this._element.style.outline = 'none';
        _this._element.style.height = '100%';
        _this._element.style.width = '100%';
        _this._element.style.overflow = 'hidden';
        var focusTracker = (0, dom_1.trackFocus)(_this._element);
        _this.addDisposables(_this.api, focusTracker.onDidFocus(function () {
            _this.api._onDidChangeFocus.fire({ isFocused: true });
        }), focusTracker.onDidBlur(function () {
            _this.api._onDidChangeFocus.fire({ isFocused: false });
        }), focusTracker);
        return _this;
    }
    Object.defineProperty(BasePanelView.prototype, "element", {
        get: function () {
            return this._element;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(BasePanelView.prototype, "width", {
        get: function () {
            return this._width;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(BasePanelView.prototype, "height", {
        get: function () {
            return this._height;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(BasePanelView.prototype, "params", {
        get: function () {
            var _a;
            return (_a = this._params) === null || _a === void 0 ? void 0 : _a.params;
        },
        enumerable: false,
        configurable: true
    });
    BasePanelView.prototype.focus = function () {
        var event = new panelApi_1.WillFocusEvent();
        this.api._onWillFocus.fire(event);
        if (event.defaultPrevented) {
            return;
        }
        this._element.focus();
    };
    BasePanelView.prototype.layout = function (width, height) {
        this._width = width;
        this._height = height;
        this.api._onDidDimensionChange.fire({ width: width, height: height });
        if (this.part) {
            if (this._params) {
                this.part.update(this._params.params);
            }
        }
    };
    BasePanelView.prototype.init = function (parameters) {
        this._params = parameters;
        this.part = this.getComponent();
    };
    BasePanelView.prototype.update = function (event) {
        var e_1, _a;
        var _b, _c;
        // merge the new parameters with the existing parameters
        this._params = __assign(__assign({}, this._params), { params: __assign(__assign({}, (_b = this._params) === null || _b === void 0 ? void 0 : _b.params), event.params) });
        try {
            /**
             * delete new keys that have a value of undefined,
             * allow values of null
             */
            for (var _d = __values(Object.keys(event.params)), _e = _d.next(); !_e.done; _e = _d.next()) {
                var key = _e.value;
                if (event.params[key] === undefined) {
                    delete this._params.params[key];
                }
            }
        }
        catch (e_1_1) { e_1 = { error: e_1_1 }; }
        finally {
            try {
                if (_e && !_e.done && (_a = _d.return)) _a.call(_d);
            }
            finally { if (e_1) throw e_1.error; }
        }
        // update the view with the updated props
        (_c = this.part) === null || _c === void 0 ? void 0 : _c.update({ params: this._params.params });
    };
    BasePanelView.prototype.toJSON = function () {
        var _a, _b;
        var params = (_b = (_a = this._params) === null || _a === void 0 ? void 0 : _a.params) !== null && _b !== void 0 ? _b : {};
        return {
            id: this.id,
            component: this.component,
            params: Object.keys(params).length > 0 ? params : undefined,
        };
    };
    BasePanelView.prototype.dispose = function () {
        var _a;
        this.api.dispose();
        (_a = this.part) === null || _a === void 0 ? void 0 : _a.dispose();
        _super.prototype.dispose.call(this);
    };
    return BasePanelView;
}(lifecycle_1.CompositeDisposable));
exports.BasePanelView = BasePanelView;
