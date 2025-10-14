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
exports.DockviewFloatingGroupPanel = void 0;
var lifecycle_1 = require("../lifecycle");
var DockviewFloatingGroupPanel = /** @class */ (function (_super) {
    __extends(DockviewFloatingGroupPanel, _super);
    function DockviewFloatingGroupPanel(group, overlay) {
        var _this = _super.call(this) || this;
        _this.group = group;
        _this.overlay = overlay;
        _this.addDisposables(overlay);
        return _this;
    }
    DockviewFloatingGroupPanel.prototype.position = function (bounds) {
        this.overlay.setBounds(bounds);
    };
    return DockviewFloatingGroupPanel;
}(lifecycle_1.CompositeDisposable));
exports.DockviewFloatingGroupPanel = DockviewFloatingGroupPanel;
