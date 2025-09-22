"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.ViewItem = void 0;
var math_1 = require("../math");
var ViewItem = /** @class */ (function () {
    function ViewItem(container, view, size, disposable) {
        this.container = container;
        this.view = view;
        this.disposable = disposable;
        this._cachedVisibleSize = undefined;
        if (typeof size === 'number') {
            this._size = size;
            this._cachedVisibleSize = undefined;
            container.classList.add('visible');
        }
        else {
            this._size = 0;
            this._cachedVisibleSize = size.cachedVisibleSize;
        }
    }
    Object.defineProperty(ViewItem.prototype, "size", {
        get: function () {
            return this._size;
        },
        set: function (size) {
            this._size = size;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(ViewItem.prototype, "cachedVisibleSize", {
        get: function () {
            return this._cachedVisibleSize;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(ViewItem.prototype, "visible", {
        get: function () {
            return typeof this._cachedVisibleSize === 'undefined';
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(ViewItem.prototype, "minimumSize", {
        get: function () {
            return this.visible ? this.view.minimumSize : 0;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(ViewItem.prototype, "viewMinimumSize", {
        get: function () {
            return this.view.minimumSize;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(ViewItem.prototype, "maximumSize", {
        get: function () {
            return this.visible ? this.view.maximumSize : 0;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(ViewItem.prototype, "viewMaximumSize", {
        get: function () {
            return this.view.maximumSize;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(ViewItem.prototype, "priority", {
        get: function () {
            return this.view.priority;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(ViewItem.prototype, "snap", {
        get: function () {
            return !!this.view.snap;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(ViewItem.prototype, "enabled", {
        set: function (enabled) {
            this.container.style.pointerEvents = enabled ? '' : 'none';
        },
        enumerable: false,
        configurable: true
    });
    ViewItem.prototype.setVisible = function (visible, size) {
        var _a;
        if (visible === this.visible) {
            return;
        }
        if (visible) {
            this.size = (0, math_1.clamp)((_a = this._cachedVisibleSize) !== null && _a !== void 0 ? _a : 0, this.viewMinimumSize, this.viewMaximumSize);
            this._cachedVisibleSize = undefined;
        }
        else {
            this._cachedVisibleSize =
                typeof size === 'number' ? size : this.size;
            this.size = 0;
        }
        this.container.classList.toggle('visible', visible);
        if (this.view.setVisible) {
            this.view.setVisible(visible);
        }
    };
    ViewItem.prototype.dispose = function () {
        this.disposable.dispose();
        return this.view;
    };
    return ViewItem;
}());
exports.ViewItem = ViewItem;
