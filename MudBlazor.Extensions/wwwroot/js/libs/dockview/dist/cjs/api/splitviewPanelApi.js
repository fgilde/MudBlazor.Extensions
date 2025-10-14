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
exports.SplitviewPanelApiImpl = void 0;
var events_1 = require("../events");
var panelApi_1 = require("./panelApi");
var SplitviewPanelApiImpl = /** @class */ (function (_super) {
    __extends(SplitviewPanelApiImpl, _super);
    //
    function SplitviewPanelApiImpl(id, component) {
        var _this = _super.call(this, id, component) || this;
        _this._onDidConstraintsChangeInternal = new events_1.Emitter();
        _this.onDidConstraintsChangeInternal = _this._onDidConstraintsChangeInternal.event;
        //
        _this._onDidConstraintsChange = new events_1.Emitter({
            replay: true,
        });
        _this.onDidConstraintsChange = _this._onDidConstraintsChange.event;
        //
        _this._onDidSizeChange = new events_1.Emitter();
        _this.onDidSizeChange = _this._onDidSizeChange.event;
        _this.addDisposables(_this._onDidConstraintsChangeInternal, _this._onDidConstraintsChange, _this._onDidSizeChange);
        return _this;
    }
    SplitviewPanelApiImpl.prototype.setConstraints = function (value) {
        this._onDidConstraintsChangeInternal.fire(value);
    };
    SplitviewPanelApiImpl.prototype.setSize = function (event) {
        this._onDidSizeChange.fire(event);
    };
    return SplitviewPanelApiImpl;
}(panelApi_1.PanelApiImpl));
exports.SplitviewPanelApiImpl = SplitviewPanelApiImpl;
