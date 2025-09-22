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
exports.Scrollbar = void 0;
var dom_1 = require("./dom");
var events_1 = require("./events");
var lifecycle_1 = require("./lifecycle");
var math_1 = require("./math");
var Scrollbar = /** @class */ (function (_super) {
    __extends(Scrollbar, _super);
    function Scrollbar(scrollableElement) {
        var _this = _super.call(this) || this;
        _this.scrollableElement = scrollableElement;
        _this._scrollLeft = 0;
        _this._element = document.createElement('div');
        _this._element.className = 'dv-scrollable';
        _this._horizontalScrollbar = document.createElement('div');
        _this._horizontalScrollbar.className = 'dv-scrollbar-horizontal';
        _this.element.appendChild(scrollableElement);
        _this.element.appendChild(_this._horizontalScrollbar);
        _this.addDisposables((0, events_1.addDisposableListener)(_this.element, 'wheel', function (event) {
            _this._scrollLeft += event.deltaY * Scrollbar.MouseWheelSpeed;
            _this.calculateScrollbarStyles();
        }), (0, events_1.addDisposableListener)(_this._horizontalScrollbar, 'pointerdown', function (event) {
            event.preventDefault();
            (0, dom_1.toggleClass)(_this.element, 'dv-scrollable-scrolling', true);
            var originalClientX = event.clientX;
            var originalScrollLeft = _this._scrollLeft;
            var onPointerMove = function (event) {
                var deltaX = event.clientX - originalClientX;
                var clientWidth = _this.element.clientWidth;
                var scrollWidth = _this.scrollableElement.scrollWidth;
                var p = clientWidth / scrollWidth;
                _this._scrollLeft = originalScrollLeft + deltaX / p;
                _this.calculateScrollbarStyles();
            };
            var onEnd = function () {
                (0, dom_1.toggleClass)(_this.element, 'dv-scrollable-scrolling', false);
                document.removeEventListener('pointermove', onPointerMove);
                document.removeEventListener('pointerup', onEnd);
                document.removeEventListener('pointercancel', onEnd);
            };
            document.addEventListener('pointermove', onPointerMove);
            document.addEventListener('pointerup', onEnd);
            document.addEventListener('pointercancel', onEnd);
        }), (0, events_1.addDisposableListener)(_this.element, 'scroll', function () {
            _this.calculateScrollbarStyles();
        }), (0, events_1.addDisposableListener)(_this.scrollableElement, 'scroll', function () {
            _this._scrollLeft = _this.scrollableElement.scrollLeft;
            _this.calculateScrollbarStyles();
        }), (0, dom_1.watchElementResize)(_this.element, function () {
            (0, dom_1.toggleClass)(_this.element, 'dv-scrollable-resizing', true);
            if (_this._animationTimer) {
                clearTimeout(_this._animationTimer);
            }
            _this._animationTimer = setTimeout(function () {
                clearTimeout(_this._animationTimer);
                (0, dom_1.toggleClass)(_this.element, 'dv-scrollable-resizing', false);
            }, 500);
            _this.calculateScrollbarStyles();
        }));
        return _this;
    }
    Object.defineProperty(Scrollbar.prototype, "element", {
        get: function () {
            return this._element;
        },
        enumerable: false,
        configurable: true
    });
    Scrollbar.prototype.calculateScrollbarStyles = function () {
        var clientWidth = this.element.clientWidth;
        var scrollWidth = this.scrollableElement.scrollWidth;
        var hasScrollbar = scrollWidth > clientWidth;
        if (hasScrollbar) {
            var px = clientWidth * (clientWidth / scrollWidth);
            this._horizontalScrollbar.style.width = "".concat(px, "px");
            this._scrollLeft = (0, math_1.clamp)(this._scrollLeft, 0, this.scrollableElement.scrollWidth - clientWidth);
            this.scrollableElement.scrollLeft = this._scrollLeft;
            var percentageComplete = this._scrollLeft / (scrollWidth - clientWidth);
            this._horizontalScrollbar.style.left = "".concat((clientWidth - px) * percentageComplete, "px");
        }
        else {
            this._horizontalScrollbar.style.width = "0px";
            this._horizontalScrollbar.style.left = "0px";
            this._scrollLeft = 0;
        }
    };
    Scrollbar.MouseWheelSpeed = 1;
    return Scrollbar;
}(lifecycle_1.CompositeDisposable));
exports.Scrollbar = Scrollbar;
