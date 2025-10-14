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
exports.getPaneData = exports.getPanelData = exports.LocalSelectionTransfer = exports.PaneTransfer = exports.PanelTransfer = void 0;
var TransferObject = /** @class */ (function () {
    function TransferObject() {
    }
    return TransferObject;
}());
var PanelTransfer = /** @class */ (function (_super) {
    __extends(PanelTransfer, _super);
    function PanelTransfer(viewId, groupId, panelId) {
        var _this = _super.call(this) || this;
        _this.viewId = viewId;
        _this.groupId = groupId;
        _this.panelId = panelId;
        return _this;
    }
    return PanelTransfer;
}(TransferObject));
exports.PanelTransfer = PanelTransfer;
var PaneTransfer = /** @class */ (function (_super) {
    __extends(PaneTransfer, _super);
    function PaneTransfer(viewId, paneId) {
        var _this = _super.call(this) || this;
        _this.viewId = viewId;
        _this.paneId = paneId;
        return _this;
    }
    return PaneTransfer;
}(TransferObject));
exports.PaneTransfer = PaneTransfer;
/**
 * A singleton to store transfer data during drag & drop operations that are only valid within the application.
 */
var LocalSelectionTransfer = /** @class */ (function () {
    function LocalSelectionTransfer() {
        // protect against external instantiation
    }
    LocalSelectionTransfer.getInstance = function () {
        return LocalSelectionTransfer.INSTANCE;
    };
    LocalSelectionTransfer.prototype.hasData = function (proto) {
        return proto && proto === this.proto;
    };
    LocalSelectionTransfer.prototype.clearData = function (proto) {
        if (this.hasData(proto)) {
            this.proto = undefined;
            this.data = undefined;
        }
    };
    LocalSelectionTransfer.prototype.getData = function (proto) {
        if (this.hasData(proto)) {
            return this.data;
        }
        return undefined;
    };
    LocalSelectionTransfer.prototype.setData = function (data, proto) {
        if (proto) {
            this.data = data;
            this.proto = proto;
        }
    };
    LocalSelectionTransfer.INSTANCE = new LocalSelectionTransfer();
    return LocalSelectionTransfer;
}());
exports.LocalSelectionTransfer = LocalSelectionTransfer;
function getPanelData() {
    var panelTransfer = LocalSelectionTransfer.getInstance();
    var isPanelEvent = panelTransfer.hasData(PanelTransfer.prototype);
    if (!isPanelEvent) {
        return undefined;
    }
    return panelTransfer.getData(PanelTransfer.prototype)[0];
}
exports.getPanelData = getPanelData;
function getPaneData() {
    var paneTransfer = LocalSelectionTransfer.getInstance();
    var isPanelEvent = paneTransfer.hasData(PaneTransfer.prototype);
    if (!isPanelEvent) {
        return undefined;
    }
    return paneTransfer.getData(PaneTransfer.prototype)[0];
}
exports.getPaneData = getPaneData;
