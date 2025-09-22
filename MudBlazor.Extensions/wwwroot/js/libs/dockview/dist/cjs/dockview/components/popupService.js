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
exports.PopupService = void 0;
var dom_1 = require("../../dom");
var events_1 = require("../../events");
var lifecycle_1 = require("../../lifecycle");
var PopupService = /** @class */ (function (_super) {
    __extends(PopupService, _super);
    function PopupService(root) {
        var _this = _super.call(this) || this;
        _this.root = root;
        _this._active = null;
        _this._activeDisposable = new lifecycle_1.MutableDisposable();
        _this._element = document.createElement('div');
        _this._element.className = 'dv-popover-anchor';
        _this._element.style.position = 'relative';
        _this.root.prepend(_this._element);
        _this.addDisposables(lifecycle_1.Disposable.from(function () {
            _this.close();
        }), _this._activeDisposable);
        return _this;
    }
    PopupService.prototype.openPopover = function (element, position) {
        var _this = this;
        var _a;
        this.close();
        var wrapper = document.createElement('div');
        wrapper.style.position = 'absolute';
        wrapper.style.zIndex = (_a = position.zIndex) !== null && _a !== void 0 ? _a : 'var(--dv-overlay-z-index)';
        wrapper.appendChild(element);
        var anchorBox = this._element.getBoundingClientRect();
        var offsetX = anchorBox.left;
        var offsetY = anchorBox.top;
        wrapper.style.top = "".concat(position.y - offsetY, "px");
        wrapper.style.left = "".concat(position.x - offsetX, "px");
        this._element.appendChild(wrapper);
        this._active = wrapper;
        this._activeDisposable.value = new lifecycle_1.CompositeDisposable((0, events_1.addDisposableListener)(window, 'pointerdown', function (event) {
            var _a;
            var target = event.target;
            if (!(target instanceof HTMLElement)) {
                return;
            }
            var el = target;
            while (el && el !== wrapper) {
                el = (_a = el === null || el === void 0 ? void 0 : el.parentElement) !== null && _a !== void 0 ? _a : null;
            }
            if (el) {
                return; // clicked within popover
            }
            _this.close();
        }));
        requestAnimationFrame(function () {
            (0, dom_1.shiftAbsoluteElementIntoView)(wrapper, _this.root);
        });
    };
    PopupService.prototype.close = function () {
        if (this._active) {
            this._active.remove();
            this._activeDisposable.dispose();
            this._active = null;
        }
    };
    return PopupService;
}(lifecycle_1.CompositeDisposable));
exports.PopupService = PopupService;
