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
exports.GroupDragHandler = void 0;
var dom_1 = require("../dom");
var events_1 = require("../events");
var abstractDragHandler_1 = require("./abstractDragHandler");
var dataTransfer_1 = require("./dataTransfer");
var ghost_1 = require("./ghost");
var GroupDragHandler = /** @class */ (function (_super) {
    __extends(GroupDragHandler, _super);
    function GroupDragHandler(element, accessor, group, disabled) {
        var _this = _super.call(this, element, disabled) || this;
        _this.accessor = accessor;
        _this.group = group;
        _this.panelTransfer = dataTransfer_1.LocalSelectionTransfer.getInstance();
        _this.addDisposables((0, events_1.addDisposableListener)(element, 'pointerdown', function (e) {
            if (e.shiftKey) {
                /**
                 * You cannot call e.preventDefault() because that will prevent drag events from firing
                 * but we also need to stop any group overlay drag events from occuring
                 * Use a custom event marker that can be checked by the overlay drag events
                 */
                (0, dom_1.quasiPreventDefault)(e);
            }
        }, true));
        return _this;
    }
    GroupDragHandler.prototype.isCancelled = function (_event) {
        if (this.group.api.location.type === 'floating' && !_event.shiftKey) {
            return true;
        }
        return false;
    };
    GroupDragHandler.prototype.getData = function (dragEvent) {
        var _this = this;
        var dataTransfer = dragEvent.dataTransfer;
        this.panelTransfer.setData([new dataTransfer_1.PanelTransfer(this.accessor.id, this.group.id, null)], dataTransfer_1.PanelTransfer.prototype);
        var style = window.getComputedStyle(this.el);
        var bgColor = style.getPropertyValue('--dv-activegroup-visiblepanel-tab-background-color');
        var color = style.getPropertyValue('--dv-activegroup-visiblepanel-tab-color');
        if (dataTransfer) {
            var ghostElement = document.createElement('div');
            ghostElement.style.backgroundColor = bgColor;
            ghostElement.style.color = color;
            ghostElement.style.padding = '2px 8px';
            ghostElement.style.height = '24px';
            ghostElement.style.fontSize = '11px';
            ghostElement.style.lineHeight = '20px';
            ghostElement.style.borderRadius = '12px';
            ghostElement.style.position = 'absolute';
            ghostElement.style.pointerEvents = 'none';
            ghostElement.style.top = '-9999px';
            ghostElement.textContent = "Multiple Panels (".concat(this.group.size, ")");
            (0, ghost_1.addGhostImage)(dataTransfer, ghostElement, { y: -10, x: 30 });
        }
        return {
            dispose: function () {
                _this.panelTransfer.clearData(dataTransfer_1.PanelTransfer.prototype);
            },
        };
    };
    return GroupDragHandler;
}(abstractDragHandler_1.DragHandler));
exports.GroupDragHandler = GroupDragHandler;
