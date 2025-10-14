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
var __spreadArray = (this && this.__spreadArray) || function (to, from, pack) {
    if (pack || arguments.length === 2) for (var i = 0, l = from.length, ar; i < l; i++) {
        if (ar || !(i in from)) {
            if (!ar) ar = Array.prototype.slice.call(from, 0, i);
            ar[i] = from[i];
        }
    }
    return to.concat(ar || Array.prototype.slice.call(from));
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.Overlay = void 0;
var dom_1 = require("../dom");
var events_1 = require("../events");
var lifecycle_1 = require("../lifecycle");
var math_1 = require("../math");
var AriaLevelTracker = /** @class */ (function () {
    function AriaLevelTracker() {
        this._orderedList = [];
    }
    AriaLevelTracker.prototype.push = function (element) {
        this._orderedList = __spreadArray(__spreadArray([], __read(this._orderedList.filter(function (item) { return item !== element; })), false), [
            element,
        ], false);
        this.update();
    };
    AriaLevelTracker.prototype.destroy = function (element) {
        this._orderedList = this._orderedList.filter(function (item) { return item !== element; });
        this.update();
    };
    AriaLevelTracker.prototype.update = function () {
        for (var i = 0; i < this._orderedList.length; i++) {
            this._orderedList[i].setAttribute('aria-level', "".concat(i));
            this._orderedList[i].style.zIndex = "calc(var(--dv-overlay-z-index, 999) + ".concat(i * 2, ")");
        }
    };
    return AriaLevelTracker;
}());
var arialLevelTracker = new AriaLevelTracker();
var Overlay = /** @class */ (function (_super) {
    __extends(Overlay, _super);
    function Overlay(options) {
        var _this = _super.call(this) || this;
        _this.options = options;
        _this._element = document.createElement('div');
        _this._onDidChange = new events_1.Emitter();
        _this.onDidChange = _this._onDidChange.event;
        _this._onDidChangeEnd = new events_1.Emitter();
        _this.onDidChangeEnd = _this._onDidChangeEnd.event;
        _this.addDisposables(_this._onDidChange, _this._onDidChangeEnd);
        _this._element.className = 'dv-resize-container';
        _this._isVisible = true;
        _this.setupResize('top');
        _this.setupResize('bottom');
        _this.setupResize('left');
        _this.setupResize('right');
        _this.setupResize('topleft');
        _this.setupResize('topright');
        _this.setupResize('bottomleft');
        _this.setupResize('bottomright');
        _this._element.appendChild(_this.options.content);
        _this.options.container.appendChild(_this._element);
        // if input bad resize within acceptable boundaries
        _this.setBounds(__assign(__assign(__assign(__assign({ height: _this.options.height, width: _this.options.width }, ('top' in _this.options && { top: _this.options.top })), ('bottom' in _this.options && { bottom: _this.options.bottom })), ('left' in _this.options && { left: _this.options.left })), ('right' in _this.options && { right: _this.options.right })));
        arialLevelTracker.push(_this._element);
        return _this;
    }
    Object.defineProperty(Overlay.prototype, "minimumInViewportWidth", {
        set: function (value) {
            this.options.minimumInViewportWidth = value;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(Overlay.prototype, "minimumInViewportHeight", {
        set: function (value) {
            this.options.minimumInViewportHeight = value;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(Overlay.prototype, "element", {
        get: function () {
            return this._element;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(Overlay.prototype, "isVisible", {
        get: function () {
            return this._isVisible;
        },
        enumerable: false,
        configurable: true
    });
    Overlay.prototype.setVisible = function (isVisible) {
        if (isVisible === this.isVisible) {
            return;
        }
        this._isVisible = isVisible;
        (0, dom_1.toggleClass)(this.element, 'dv-hidden', !this.isVisible);
    };
    Overlay.prototype.bringToFront = function () {
        arialLevelTracker.push(this._element);
    };
    Overlay.prototype.setBounds = function (bounds) {
        if (bounds === void 0) { bounds = {}; }
        if (typeof bounds.height === 'number') {
            this._element.style.height = "".concat(bounds.height, "px");
        }
        if (typeof bounds.width === 'number') {
            this._element.style.width = "".concat(bounds.width, "px");
        }
        if ('top' in bounds && typeof bounds.top === 'number') {
            this._element.style.top = "".concat(bounds.top, "px");
            this._element.style.bottom = 'auto';
            this.verticalAlignment = 'top';
        }
        if ('bottom' in bounds && typeof bounds.bottom === 'number') {
            this._element.style.bottom = "".concat(bounds.bottom, "px");
            this._element.style.top = 'auto';
            this.verticalAlignment = 'bottom';
        }
        if ('left' in bounds && typeof bounds.left === 'number') {
            this._element.style.left = "".concat(bounds.left, "px");
            this._element.style.right = 'auto';
            this.horiziontalAlignment = 'left';
        }
        if ('right' in bounds && typeof bounds.right === 'number') {
            this._element.style.right = "".concat(bounds.right, "px");
            this._element.style.left = 'auto';
            this.horiziontalAlignment = 'right';
        }
        var containerRect = this.options.container.getBoundingClientRect();
        var overlayRect = this._element.getBoundingClientRect();
        // region: ensure bounds within allowable limits
        // a minimum width of minimumViewportWidth must be inside the viewport
        var xOffset = Math.max(0, this.getMinimumWidth(overlayRect.width));
        // a minimum height of minimumViewportHeight must be inside the viewport
        var yOffset = Math.max(0, this.getMinimumHeight(overlayRect.height));
        if (this.verticalAlignment === 'top') {
            var top_1 = (0, math_1.clamp)(overlayRect.top - containerRect.top, -yOffset, Math.max(0, containerRect.height - overlayRect.height + yOffset));
            this._element.style.top = "".concat(top_1, "px");
            this._element.style.bottom = 'auto';
        }
        if (this.verticalAlignment === 'bottom') {
            var bottom = (0, math_1.clamp)(containerRect.bottom - overlayRect.bottom, -yOffset, Math.max(0, containerRect.height - overlayRect.height + yOffset));
            this._element.style.bottom = "".concat(bottom, "px");
            this._element.style.top = 'auto';
        }
        if (this.horiziontalAlignment === 'left') {
            var left = (0, math_1.clamp)(overlayRect.left - containerRect.left, -xOffset, Math.max(0, containerRect.width - overlayRect.width + xOffset));
            this._element.style.left = "".concat(left, "px");
            this._element.style.right = 'auto';
        }
        if (this.horiziontalAlignment === 'right') {
            var right = (0, math_1.clamp)(containerRect.right - overlayRect.right, -xOffset, Math.max(0, containerRect.width - overlayRect.width + xOffset));
            this._element.style.right = "".concat(right, "px");
            this._element.style.left = 'auto';
        }
        this._onDidChange.fire();
    };
    Overlay.prototype.toJSON = function () {
        var container = this.options.container.getBoundingClientRect();
        var element = this._element.getBoundingClientRect();
        var result = {};
        if (this.verticalAlignment === 'top') {
            result.top = parseFloat(this._element.style.top);
        }
        else if (this.verticalAlignment === 'bottom') {
            result.bottom = parseFloat(this._element.style.bottom);
        }
        else {
            result.top = element.top - container.top;
        }
        if (this.horiziontalAlignment === 'left') {
            result.left = parseFloat(this._element.style.left);
        }
        else if (this.horiziontalAlignment === 'right') {
            result.right = parseFloat(this._element.style.right);
        }
        else {
            result.left = element.left - container.left;
        }
        result.width = element.width;
        result.height = element.height;
        return result;
    };
    Overlay.prototype.setupDrag = function (dragTarget, options) {
        var _this = this;
        if (options === void 0) { options = { inDragMode: false }; }
        var move = new lifecycle_1.MutableDisposable();
        var track = function () {
            var offset = null;
            var iframes = (0, dom_1.disableIframePointEvents)();
            move.value = new lifecycle_1.CompositeDisposable({
                dispose: function () {
                    iframes.release();
                },
            }, (0, events_1.addDisposableListener)(window, 'pointermove', function (e) {
                var containerRect = _this.options.container.getBoundingClientRect();
                var x = e.clientX - containerRect.left;
                var y = e.clientY - containerRect.top;
                (0, dom_1.toggleClass)(_this._element, 'dv-resize-container-dragging', true);
                var overlayRect = _this._element.getBoundingClientRect();
                if (offset === null) {
                    offset = {
                        x: e.clientX - overlayRect.left,
                        y: e.clientY - overlayRect.top,
                    };
                }
                var xOffset = Math.max(0, _this.getMinimumWidth(overlayRect.width));
                var yOffset = Math.max(0, _this.getMinimumHeight(overlayRect.height));
                var top = (0, math_1.clamp)(y - offset.y, -yOffset, Math.max(0, containerRect.height - overlayRect.height + yOffset));
                var bottom = (0, math_1.clamp)(offset.y -
                    y +
                    containerRect.height -
                    overlayRect.height, -yOffset, Math.max(0, containerRect.height - overlayRect.height + yOffset));
                var left = (0, math_1.clamp)(x - offset.x, -xOffset, Math.max(0, containerRect.width - overlayRect.width + xOffset));
                var right = (0, math_1.clamp)(offset.x - x + containerRect.width - overlayRect.width, -xOffset, Math.max(0, containerRect.width - overlayRect.width + xOffset));
                var bounds = {};
                // Anchor to top or to bottom depending on which one is closer
                if (top <= bottom) {
                    bounds.top = top;
                }
                else {
                    bounds.bottom = bottom;
                }
                // Anchor to left or to right depending on which one is closer
                if (left <= right) {
                    bounds.left = left;
                }
                else {
                    bounds.right = right;
                }
                _this.setBounds(bounds);
            }), (0, events_1.addDisposableListener)(window, 'pointerup', function () {
                (0, dom_1.toggleClass)(_this._element, 'dv-resize-container-dragging', false);
                move.dispose();
                _this._onDidChangeEnd.fire();
            }));
        };
        this.addDisposables(move, (0, events_1.addDisposableListener)(dragTarget, 'pointerdown', function (event) {
            if (event.defaultPrevented) {
                event.preventDefault();
                return;
            }
            // if somebody has marked this event then treat as a defaultPrevented
            // without actually calling event.preventDefault()
            if ((0, dom_1.quasiDefaultPrevented)(event)) {
                return;
            }
            track();
        }), (0, events_1.addDisposableListener)(this.options.content, 'pointerdown', function (event) {
            if (event.defaultPrevented) {
                return;
            }
            // if somebody has marked this event then treat as a defaultPrevented
            // without actually calling event.preventDefault()
            if ((0, dom_1.quasiDefaultPrevented)(event)) {
                return;
            }
            if (event.shiftKey) {
                track();
            }
        }), (0, events_1.addDisposableListener)(this.options.content, 'pointerdown', function () {
            arialLevelTracker.push(_this._element);
        }, true));
        if (options.inDragMode) {
            track();
        }
    };
    Overlay.prototype.setupResize = function (direction) {
        var _this = this;
        var resizeHandleElement = document.createElement('div');
        resizeHandleElement.className = "dv-resize-handle-".concat(direction);
        this._element.appendChild(resizeHandleElement);
        var move = new lifecycle_1.MutableDisposable();
        this.addDisposables(move, (0, events_1.addDisposableListener)(resizeHandleElement, 'pointerdown', function (e) {
            e.preventDefault();
            var startPosition = null;
            var iframes = (0, dom_1.disableIframePointEvents)();
            move.value = new lifecycle_1.CompositeDisposable((0, events_1.addDisposableListener)(window, 'pointermove', function (e) {
                var containerRect = _this.options.container.getBoundingClientRect();
                var overlayRect = _this._element.getBoundingClientRect();
                var y = e.clientY - containerRect.top;
                var x = e.clientX - containerRect.left;
                if (startPosition === null) {
                    // record the initial dimensions since as all subsequence moves are relative to this
                    startPosition = {
                        originalY: y,
                        originalHeight: overlayRect.height,
                        originalX: x,
                        originalWidth: overlayRect.width,
                    };
                }
                var top = undefined;
                var bottom = undefined;
                var height = undefined;
                var left = undefined;
                var right = undefined;
                var width = undefined;
                var moveTop = function () {
                    top = (0, math_1.clamp)(y, -Number.MAX_VALUE, startPosition.originalY +
                        startPosition.originalHeight >
                        containerRect.height
                        ? _this.getMinimumHeight(containerRect.height)
                        : Math.max(0, startPosition.originalY +
                            startPosition.originalHeight -
                            Overlay.MINIMUM_HEIGHT));
                    height =
                        startPosition.originalY +
                            startPosition.originalHeight -
                            top;
                    bottom = containerRect.height - top - height;
                };
                var moveBottom = function () {
                    top =
                        startPosition.originalY -
                            startPosition.originalHeight;
                    height = (0, math_1.clamp)(y - top, top < 0 &&
                        typeof _this.options
                            .minimumInViewportHeight === 'number'
                        ? -top +
                            _this.options.minimumInViewportHeight
                        : Overlay.MINIMUM_HEIGHT, Number.MAX_VALUE);
                    bottom = containerRect.height - top - height;
                };
                var moveLeft = function () {
                    left = (0, math_1.clamp)(x, -Number.MAX_VALUE, startPosition.originalX +
                        startPosition.originalWidth >
                        containerRect.width
                        ? _this.getMinimumWidth(containerRect.width)
                        : Math.max(0, startPosition.originalX +
                            startPosition.originalWidth -
                            Overlay.MINIMUM_WIDTH));
                    width =
                        startPosition.originalX +
                            startPosition.originalWidth -
                            left;
                    right = containerRect.width - left - width;
                };
                var moveRight = function () {
                    left =
                        startPosition.originalX -
                            startPosition.originalWidth;
                    width = (0, math_1.clamp)(x - left, left < 0 &&
                        typeof _this.options
                            .minimumInViewportWidth === 'number'
                        ? -left +
                            _this.options.minimumInViewportWidth
                        : Overlay.MINIMUM_WIDTH, Number.MAX_VALUE);
                    right = containerRect.width - left - width;
                };
                switch (direction) {
                    case 'top':
                        moveTop();
                        break;
                    case 'bottom':
                        moveBottom();
                        break;
                    case 'left':
                        moveLeft();
                        break;
                    case 'right':
                        moveRight();
                        break;
                    case 'topleft':
                        moveTop();
                        moveLeft();
                        break;
                    case 'topright':
                        moveTop();
                        moveRight();
                        break;
                    case 'bottomleft':
                        moveBottom();
                        moveLeft();
                        break;
                    case 'bottomright':
                        moveBottom();
                        moveRight();
                        break;
                }
                var bounds = {};
                // Anchor to top or to bottom depending on which one is closer
                if (top <= bottom) {
                    bounds.top = top;
                }
                else {
                    bounds.bottom = bottom;
                }
                // Anchor to left or to right depending on which one is closer
                if (left <= right) {
                    bounds.left = left;
                }
                else {
                    bounds.right = right;
                }
                bounds.height = height;
                bounds.width = width;
                _this.setBounds(bounds);
            }), {
                dispose: function () {
                    iframes.release();
                },
            }, (0, events_1.addDisposableListener)(window, 'pointerup', function () {
                move.dispose();
                _this._onDidChangeEnd.fire();
            }));
        }));
    };
    Overlay.prototype.getMinimumWidth = function (width) {
        if (typeof this.options.minimumInViewportWidth === 'number') {
            return width - this.options.minimumInViewportWidth;
        }
        return 0;
    };
    Overlay.prototype.getMinimumHeight = function (height) {
        if (typeof this.options.minimumInViewportHeight === 'number') {
            return height - this.options.minimumInViewportHeight;
        }
        return 0;
    };
    Overlay.prototype.dispose = function () {
        arialLevelTracker.destroy(this._element);
        this._element.remove();
        _super.prototype.dispose.call(this);
    };
    Overlay.MINIMUM_HEIGHT = 20;
    Overlay.MINIMUM_WIDTH = 20;
    return Overlay;
}(lifecycle_1.CompositeDisposable));
exports.Overlay = Overlay;
