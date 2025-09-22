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
exports.PaneviewPanelApiImpl = void 0;
var events_1 = require("../events");
var splitviewPanelApi_1 = require("./splitviewPanelApi");
var PaneviewPanelApiImpl = /** @class */ (function (_super) {
    __extends(PaneviewPanelApiImpl, _super);
    function PaneviewPanelApiImpl(id, component) {
        var _this = _super.call(this, id, component) || this;
        _this._onDidExpansionChange = new events_1.Emitter({
            replay: true,
        });
        _this.onDidExpansionChange = _this._onDidExpansionChange.event;
        _this._onMouseEnter = new events_1.Emitter({});
        _this.onMouseEnter = _this._onMouseEnter.event;
        _this._onMouseLeave = new events_1.Emitter({});
        _this.onMouseLeave = _this._onMouseLeave.event;
        _this.addDisposables(_this._onDidExpansionChange, _this._onMouseEnter, _this._onMouseLeave);
        return _this;
    }
    Object.defineProperty(PaneviewPanelApiImpl.prototype, "pane", {
        set: function (pane) {
            this._pane = pane;
        },
        enumerable: false,
        configurable: true
    });
    PaneviewPanelApiImpl.prototype.setExpanded = function (isExpanded) {
        var _a;
        (_a = this._pane) === null || _a === void 0 ? void 0 : _a.setExpanded(isExpanded);
    };
    Object.defineProperty(PaneviewPanelApiImpl.prototype, "isExpanded", {
        get: function () {
            var _a;
            return !!((_a = this._pane) === null || _a === void 0 ? void 0 : _a.isExpanded());
        },
        enumerable: false,
        configurable: true
    });
    return PaneviewPanelApiImpl;
}(splitviewPanelApi_1.SplitviewPanelApiImpl));
exports.PaneviewPanelApiImpl = PaneviewPanelApiImpl;
