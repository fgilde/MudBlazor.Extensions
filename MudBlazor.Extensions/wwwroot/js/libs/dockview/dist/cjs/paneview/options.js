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
exports.PaneviewUnhandledDragOverEvent = exports.PROPERTY_KEYS_PANEVIEW = void 0;
var events_1 = require("../events");
exports.PROPERTY_KEYS_PANEVIEW = (function () {
    /**
     * by readong the keys from an empty value object TypeScript will error
     * when we add or remove new properties to `DockviewOptions`
     */
    var properties = {
        disableAutoResizing: undefined,
        disableDnd: undefined,
        className: undefined,
    };
    return Object.keys(properties);
})();
var PaneviewUnhandledDragOverEvent = /** @class */ (function (_super) {
    __extends(PaneviewUnhandledDragOverEvent, _super);
    function PaneviewUnhandledDragOverEvent(nativeEvent, position, getData, panel) {
        var _this = _super.call(this) || this;
        _this.nativeEvent = nativeEvent;
        _this.position = position;
        _this.getData = getData;
        _this.panel = panel;
        return _this;
    }
    return PaneviewUnhandledDragOverEvent;
}(events_1.AcceptableEvent));
exports.PaneviewUnhandledDragOverEvent = PaneviewUnhandledDragOverEvent;
