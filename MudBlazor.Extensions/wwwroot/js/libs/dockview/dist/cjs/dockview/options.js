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
exports.isGroupOptionsWithGroup = exports.isGroupOptionsWithPanel = exports.isPanelOptionsWithGroup = exports.isPanelOptionsWithPanel = exports.PROPERTY_KEYS_DOCKVIEW = exports.DockviewUnhandledDragOverEvent = void 0;
var events_1 = require("../events");
var DockviewUnhandledDragOverEvent = /** @class */ (function (_super) {
    __extends(DockviewUnhandledDragOverEvent, _super);
    function DockviewUnhandledDragOverEvent(nativeEvent, target, position, getData, group) {
        var _this = _super.call(this) || this;
        _this.nativeEvent = nativeEvent;
        _this.target = target;
        _this.position = position;
        _this.getData = getData;
        _this.group = group;
        return _this;
    }
    return DockviewUnhandledDragOverEvent;
}(events_1.AcceptableEvent));
exports.DockviewUnhandledDragOverEvent = DockviewUnhandledDragOverEvent;
exports.PROPERTY_KEYS_DOCKVIEW = (function () {
    /**
     * by readong the keys from an empty value object TypeScript will error
     * when we add or remove new properties to `DockviewOptions`
     */
    var properties = {
        disableAutoResizing: undefined,
        hideBorders: undefined,
        singleTabMode: undefined,
        disableFloatingGroups: undefined,
        floatingGroupBounds: undefined,
        popoutUrl: undefined,
        defaultRenderer: undefined,
        debug: undefined,
        rootOverlayModel: undefined,
        locked: undefined,
        disableDnd: undefined,
        className: undefined,
        noPanelsOverlay: undefined,
        dndEdges: undefined,
        theme: undefined,
        disableTabsOverflowList: undefined,
        scrollbars: undefined,
    };
    return Object.keys(properties);
})();
function isPanelOptionsWithPanel(data) {
    if (data.referencePanel) {
        return true;
    }
    return false;
}
exports.isPanelOptionsWithPanel = isPanelOptionsWithPanel;
function isPanelOptionsWithGroup(data) {
    if (data.referenceGroup) {
        return true;
    }
    return false;
}
exports.isPanelOptionsWithGroup = isPanelOptionsWithGroup;
function isGroupOptionsWithPanel(data) {
    if (data.referencePanel) {
        return true;
    }
    return false;
}
exports.isGroupOptionsWithPanel = isGroupOptionsWithPanel;
function isGroupOptionsWithGroup(data) {
    if (data.referenceGroup) {
        return true;
    }
    return false;
}
exports.isGroupOptionsWithGroup = isGroupOptionsWithGroup;
