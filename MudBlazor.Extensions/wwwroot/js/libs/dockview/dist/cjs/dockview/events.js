"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.WillShowOverlayLocationEvent = void 0;
var WillShowOverlayLocationEvent = /** @class */ (function () {
    function WillShowOverlayLocationEvent(event, options) {
        this.event = event;
        this.options = options;
    }
    Object.defineProperty(WillShowOverlayLocationEvent.prototype, "kind", {
        get: function () {
            return this.options.kind;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(WillShowOverlayLocationEvent.prototype, "nativeEvent", {
        get: function () {
            return this.event.nativeEvent;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(WillShowOverlayLocationEvent.prototype, "position", {
        get: function () {
            return this.event.position;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(WillShowOverlayLocationEvent.prototype, "defaultPrevented", {
        get: function () {
            return this.event.defaultPrevented;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(WillShowOverlayLocationEvent.prototype, "panel", {
        get: function () {
            return this.options.panel;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(WillShowOverlayLocationEvent.prototype, "api", {
        get: function () {
            return this.options.api;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(WillShowOverlayLocationEvent.prototype, "group", {
        get: function () {
            return this.options.group;
        },
        enumerable: false,
        configurable: true
    });
    WillShowOverlayLocationEvent.prototype.preventDefault = function () {
        this.event.preventDefault();
    };
    WillShowOverlayLocationEvent.prototype.getData = function () {
        return this.options.getData();
    };
    return WillShowOverlayLocationEvent;
}());
exports.WillShowOverlayLocationEvent = WillShowOverlayLocationEvent;
