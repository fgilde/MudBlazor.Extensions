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
exports.DefaultHeader = void 0;
var events_1 = require("../events");
var lifecycle_1 = require("../lifecycle");
var dom_1 = require("../dom");
var svg_1 = require("../svg");
var DefaultHeader = /** @class */ (function (_super) {
    __extends(DefaultHeader, _super);
    function DefaultHeader() {
        var _this = _super.call(this) || this;
        _this._expandedIcon = (0, svg_1.createExpandMoreButton)();
        _this._collapsedIcon = (0, svg_1.createChevronRightButton)();
        _this.disposable = new lifecycle_1.MutableDisposable();
        _this.apiRef = {
            api: null,
        };
        _this._element = document.createElement('div');
        _this.element.className = 'dv-default-header';
        _this._content = document.createElement('span');
        _this._expander = document.createElement('div');
        _this._expander.className = 'dv-pane-header-icon';
        _this.element.appendChild(_this._expander);
        _this.element.appendChild(_this._content);
        _this.addDisposables((0, events_1.addDisposableListener)(_this._element, 'click', function () {
            var _a;
            (_a = _this.apiRef.api) === null || _a === void 0 ? void 0 : _a.setExpanded(!_this.apiRef.api.isExpanded);
        }));
        return _this;
    }
    Object.defineProperty(DefaultHeader.prototype, "element", {
        get: function () {
            return this._element;
        },
        enumerable: false,
        configurable: true
    });
    DefaultHeader.prototype.init = function (params) {
        var _this = this;
        this.apiRef.api = params.api;
        this._content.textContent = params.title;
        this.updateIcon();
        this.disposable.value = params.api.onDidExpansionChange(function () {
            _this.updateIcon();
        });
    };
    DefaultHeader.prototype.updateIcon = function () {
        var _a;
        var isExpanded = !!((_a = this.apiRef.api) === null || _a === void 0 ? void 0 : _a.isExpanded);
        (0, dom_1.toggleClass)(this._expander, 'collapsed', !isExpanded);
        if (isExpanded) {
            if (this._expander.contains(this._collapsedIcon)) {
                this._collapsedIcon.remove();
            }
            if (!this._expander.contains(this._expandedIcon)) {
                this._expander.appendChild(this._expandedIcon);
            }
        }
        else {
            if (this._expander.contains(this._expandedIcon)) {
                this._expandedIcon.remove();
            }
            if (!this._expander.contains(this._collapsedIcon)) {
                this._expander.appendChild(this._collapsedIcon);
            }
        }
    };
    DefaultHeader.prototype.update = function (_params) {
        //
    };
    DefaultHeader.prototype.dispose = function () {
        this.disposable.dispose();
        _super.prototype.dispose.call(this);
    };
    return DefaultHeader;
}(lifecycle_1.CompositeDisposable));
exports.DefaultHeader = DefaultHeader;
