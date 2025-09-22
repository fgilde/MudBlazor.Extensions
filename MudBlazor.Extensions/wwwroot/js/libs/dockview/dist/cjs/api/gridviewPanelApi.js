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
exports.GridviewPanelApiImpl = void 0;
var events_1 = require("../events");
var panelApi_1 = require("./panelApi");
var GridviewPanelApiImpl = /** @class */ (function (_super) {
    __extends(GridviewPanelApiImpl, _super);
    function GridviewPanelApiImpl(id, component, panel) {
        var _this = _super.call(this, id, component) || this;
        _this._onDidConstraintsChangeInternal = new events_1.Emitter();
        _this.onDidConstraintsChangeInternal = _this._onDidConstraintsChangeInternal.event;
        _this._onDidConstraintsChange = new events_1.Emitter();
        _this.onDidConstraintsChange = _this._onDidConstraintsChange.event;
        _this._onDidSizeChange = new events_1.Emitter();
        _this.onDidSizeChange = _this._onDidSizeChange.event;
        _this.addDisposables(_this._onDidConstraintsChangeInternal, _this._onDidConstraintsChange, _this._onDidSizeChange);
        if (panel) {
            _this.initialize(panel);
        }
        return _this;
    }
    GridviewPanelApiImpl.prototype.setConstraints = function (value) {
        this._onDidConstraintsChangeInternal.fire(value);
    };
    GridviewPanelApiImpl.prototype.setSize = function (event) {
        this._onDidSizeChange.fire(event);
    };
    return GridviewPanelApiImpl;
}(panelApi_1.PanelApiImpl));
exports.GridviewPanelApiImpl = GridviewPanelApiImpl;
