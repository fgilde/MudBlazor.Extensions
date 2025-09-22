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
exports.DropTargetAnchorContainer = void 0;
var lifecycle_1 = require("../lifecycle");
var DropTargetAnchorContainer = /** @class */ (function (_super) {
    __extends(DropTargetAnchorContainer, _super);
    function DropTargetAnchorContainer(element, options) {
        var _this = _super.call(this) || this;
        _this.element = element;
        _this._disabled = false;
        _this._disabled = options.disabled;
        _this.addDisposables(lifecycle_1.Disposable.from(function () {
            var _a;
            (_a = _this.model) === null || _a === void 0 ? void 0 : _a.clear();
        }));
        return _this;
    }
    Object.defineProperty(DropTargetAnchorContainer.prototype, "disabled", {
        get: function () {
            return this._disabled;
        },
        set: function (value) {
            var _a;
            if (this.disabled === value) {
                return;
            }
            this._disabled = value;
            if (value) {
                (_a = this.model) === null || _a === void 0 ? void 0 : _a.clear();
            }
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DropTargetAnchorContainer.prototype, "model", {
        get: function () {
            var _this = this;
            if (this.disabled) {
                return undefined;
            }
            return {
                clear: function () {
                    var _a;
                    if (_this._model) {
                        (_a = _this._model.root.parentElement) === null || _a === void 0 ? void 0 : _a.removeChild(_this._model.root);
                    }
                    _this._model = undefined;
                },
                exists: function () {
                    return !!_this._model;
                },
                getElements: function (event, outline) {
                    var changed = _this._outline !== outline;
                    _this._outline = outline;
                    if (_this._model) {
                        _this._model.changed = changed;
                        return _this._model;
                    }
                    var container = _this.createContainer();
                    var anchor = _this.createAnchor();
                    _this._model = { root: container, overlay: anchor, changed: changed };
                    container.appendChild(anchor);
                    _this.element.appendChild(container);
                    if ((event === null || event === void 0 ? void 0 : event.target) instanceof HTMLElement) {
                        var targetBox = event.target.getBoundingClientRect();
                        var box = _this.element.getBoundingClientRect();
                        anchor.style.left = "".concat(targetBox.left - box.left, "px");
                        anchor.style.top = "".concat(targetBox.top - box.top, "px");
                    }
                    return _this._model;
                },
            };
        },
        enumerable: false,
        configurable: true
    });
    DropTargetAnchorContainer.prototype.createContainer = function () {
        var el = document.createElement('div');
        el.className = 'dv-drop-target-container';
        return el;
    };
    DropTargetAnchorContainer.prototype.createAnchor = function () {
        var el = document.createElement('div');
        el.className = 'dv-drop-target-anchor';
        el.style.visibility = 'hidden';
        return el;
    };
    return DropTargetAnchorContainer;
}(lifecycle_1.CompositeDisposable));
exports.DropTargetAnchorContainer = DropTargetAnchorContainer;
