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
Object.defineProperty(exports, "__esModule", { value: true });
exports.PanelApiImpl = exports.WillFocusEvent = void 0;
var events_1 = require("../events");
var lifecycle_1 = require("../lifecycle");
var WillFocusEvent = /** @class */ (function (_super) {
    __extends(WillFocusEvent, _super);
    function WillFocusEvent() {
        return _super.call(this) || this;
    }
    return WillFocusEvent;
}(events_1.DockviewEvent));
exports.WillFocusEvent = WillFocusEvent;
/**
 * A core api implementation that should be used across all panel-like objects
 */
var PanelApiImpl = /** @class */ (function (_super) {
    __extends(PanelApiImpl, _super);
    function PanelApiImpl(id, component) {
        var _this = _super.call(this) || this;
        _this.id = id;
        _this.component = component;
        _this._isFocused = false;
        _this._isActive = false;
        _this._isVisible = true;
        _this._width = 0;
        _this._height = 0;
        _this._parameters = {};
        _this.panelUpdatesDisposable = new lifecycle_1.MutableDisposable();
        _this._onDidDimensionChange = new events_1.Emitter();
        _this.onDidDimensionsChange = _this._onDidDimensionChange.event;
        _this._onDidChangeFocus = new events_1.Emitter();
        _this.onDidFocusChange = _this._onDidChangeFocus.event;
        //
        _this._onWillFocus = new events_1.Emitter();
        _this.onWillFocus = _this._onWillFocus.event;
        //
        _this._onDidVisibilityChange = new events_1.Emitter();
        _this.onDidVisibilityChange = _this._onDidVisibilityChange.event;
        _this._onWillVisibilityChange = new events_1.Emitter();
        _this.onWillVisibilityChange = _this._onWillVisibilityChange.event;
        _this._onDidActiveChange = new events_1.Emitter();
        _this.onDidActiveChange = _this._onDidActiveChange.event;
        _this._onActiveChange = new events_1.Emitter();
        _this.onActiveChange = _this._onActiveChange.event;
        _this._onDidParametersChange = new events_1.Emitter();
        _this.onDidParametersChange = _this._onDidParametersChange.event;
        _this.addDisposables(_this.onDidFocusChange(function (event) {
            _this._isFocused = event.isFocused;
        }), _this.onDidActiveChange(function (event) {
            _this._isActive = event.isActive;
        }), _this.onDidVisibilityChange(function (event) {
            _this._isVisible = event.isVisible;
        }), _this.onDidDimensionsChange(function (event) {
            _this._width = event.width;
            _this._height = event.height;
        }), _this.panelUpdatesDisposable, _this._onDidDimensionChange, _this._onDidChangeFocus, _this._onDidVisibilityChange, _this._onDidActiveChange, _this._onWillFocus, _this._onActiveChange, _this._onWillFocus, _this._onWillVisibilityChange, _this._onDidParametersChange);
        return _this;
    }
    Object.defineProperty(PanelApiImpl.prototype, "isFocused", {
        get: function () {
            return this._isFocused;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(PanelApiImpl.prototype, "isActive", {
        get: function () {
            return this._isActive;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(PanelApiImpl.prototype, "isVisible", {
        get: function () {
            return this._isVisible;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(PanelApiImpl.prototype, "width", {
        get: function () {
            return this._width;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(PanelApiImpl.prototype, "height", {
        get: function () {
            return this._height;
        },
        enumerable: false,
        configurable: true
    });
    PanelApiImpl.prototype.getParameters = function () {
        return this._parameters;
    };
    PanelApiImpl.prototype.initialize = function (panel) {
        var _this = this;
        this.panelUpdatesDisposable.value = this._onDidParametersChange.event(function (parameters) {
            _this._parameters = parameters;
            panel.update({
                params: parameters,
            });
        });
    };
    PanelApiImpl.prototype.setVisible = function (isVisible) {
        this._onWillVisibilityChange.fire({ isVisible: isVisible });
    };
    PanelApiImpl.prototype.setActive = function () {
        this._onActiveChange.fire();
    };
    PanelApiImpl.prototype.updateParameters = function (parameters) {
        this._onDidParametersChange.fire(parameters);
    };
    return PanelApiImpl;
}(lifecycle_1.CompositeDisposable));
exports.PanelApiImpl = PanelApiImpl;
