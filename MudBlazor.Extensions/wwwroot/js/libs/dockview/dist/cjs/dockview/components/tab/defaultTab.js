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
exports.DefaultTab = void 0;
var lifecycle_1 = require("../../../lifecycle");
var events_1 = require("../../../events");
var svg_1 = require("../../../svg");
var DefaultTab = /** @class */ (function (_super) {
    __extends(DefaultTab, _super);
    function DefaultTab() {
        var _this = _super.call(this) || this;
        _this._element = document.createElement('div');
        _this._element.className = 'dv-default-tab';
        _this._content = document.createElement('div');
        _this._content.className = 'dv-default-tab-content';
        _this.action = document.createElement('div');
        _this.action.className = 'dv-default-tab-action';
        _this.action.appendChild((0, svg_1.createCloseButton)());
        _this._element.appendChild(_this._content);
        _this._element.appendChild(_this.action);
        _this.render();
        return _this;
    }
    Object.defineProperty(DefaultTab.prototype, "element", {
        get: function () {
            return this._element;
        },
        enumerable: false,
        configurable: true
    });
    DefaultTab.prototype.init = function (params) {
        var _this = this;
        this._title = params.title;
        this.addDisposables(params.api.onDidTitleChange(function (event) {
            _this._title = event.title;
            _this.render();
        }), (0, events_1.addDisposableListener)(this.action, 'pointerdown', function (ev) {
            ev.preventDefault();
        }), (0, events_1.addDisposableListener)(this.action, 'click', function (ev) {
            if (ev.defaultPrevented) {
                return;
            }
            ev.preventDefault();
            params.api.close();
        }));
        this.render();
    };
    DefaultTab.prototype.render = function () {
        var _a;
        if (this._content.textContent !== this._title) {
            this._content.textContent = (_a = this._title) !== null && _a !== void 0 ? _a : '';
        }
    };
    return DefaultTab;
}(lifecycle_1.CompositeDisposable));
exports.DefaultTab = DefaultTab;
