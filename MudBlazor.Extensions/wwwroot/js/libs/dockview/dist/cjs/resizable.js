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
exports.Resizable = void 0;
var dom_1 = require("./dom");
var lifecycle_1 = require("./lifecycle");
var Resizable = /** @class */ (function (_super) {
    __extends(Resizable, _super);
    function Resizable(parentElement, disableResizing) {
        if (disableResizing === void 0) { disableResizing = false; }
        var _this = _super.call(this) || this;
        _this._disableResizing = disableResizing;
        _this._element = parentElement;
        _this.addDisposables((0, dom_1.watchElementResize)(_this._element, function (entry) {
            if (_this.isDisposed) {
                /**
                 * resize is delayed through requestAnimationFrame so there is a small chance
                 * the component has already been disposed of
                 */
                return;
            }
            if (_this.disableResizing) {
                return;
            }
            if (!_this._element.offsetParent) {
                /**
                 * offsetParent === null is equivalent to display: none being set on the element or one
                 * of it's parents. In the display: none case the size will become (0, 0) which we do
                 * not want to propagate.
                 *
                 * @see https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/offsetParent
                 *
                 * You could use checkVisibility() but at the time of writing it's not supported across
                 * all Browsers
                 *
                 * @see https://developer.mozilla.org/en-US/docs/Web/API/Element/checkVisibility
                 */
                return;
            }
            if (!(0, dom_1.isInDocument)(_this._element)) {
                /**
                 * since the event is dispatched through requestAnimationFrame there is a small chance
                 * the component is no longer attached to the DOM, if that is the case the dimensions
                 * are mostly likely all zero and meaningless. we should skip this case.
                 */
                return;
            }
            var _a = entry.contentRect, width = _a.width, height = _a.height;
            _this.layout(width, height);
        }));
        return _this;
    }
    Object.defineProperty(Resizable.prototype, "element", {
        get: function () {
            return this._element;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(Resizable.prototype, "disableResizing", {
        get: function () {
            return this._disableResizing;
        },
        set: function (value) {
            this._disableResizing = value;
        },
        enumerable: false,
        configurable: true
    });
    return Resizable;
}(lifecycle_1.CompositeDisposable));
exports.Resizable = Resizable;
