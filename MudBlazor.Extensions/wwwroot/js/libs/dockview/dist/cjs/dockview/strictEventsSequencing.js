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
exports.StrictEventsSequencing = void 0;
var lifecycle_1 = require("../lifecycle");
var StrictEventsSequencing = /** @class */ (function (_super) {
    __extends(StrictEventsSequencing, _super);
    function StrictEventsSequencing(accessor) {
        var _this = _super.call(this) || this;
        _this.accessor = accessor;
        _this.init();
        return _this;
    }
    StrictEventsSequencing.prototype.init = function () {
        var panels = new Set();
        var groups = new Set();
        this.addDisposables(this.accessor.onDidAddPanel(function (panel) {
            if (panels.has(panel.api.id)) {
                throw new Error("dockview: Invalid event sequence. [onDidAddPanel] called for panel ".concat(panel.api.id, " but panel already exists"));
            }
            else {
                panels.add(panel.api.id);
            }
        }), this.accessor.onDidRemovePanel(function (panel) {
            if (!panels.has(panel.api.id)) {
                throw new Error("dockview: Invalid event sequence. [onDidRemovePanel] called for panel ".concat(panel.api.id, " but panel does not exists"));
            }
            else {
                panels.delete(panel.api.id);
            }
        }), this.accessor.onDidAddGroup(function (group) {
            if (groups.has(group.api.id)) {
                throw new Error("dockview: Invalid event sequence. [onDidAddGroup] called for group ".concat(group.api.id, " but group already exists"));
            }
            else {
                groups.add(group.api.id);
            }
        }), this.accessor.onDidRemoveGroup(function (group) {
            if (!groups.has(group.api.id)) {
                throw new Error("dockview: Invalid event sequence. [onDidRemoveGroup] called for group ".concat(group.api.id, " but group does not exists"));
            }
            else {
                groups.delete(group.api.id);
            }
        }));
    };
    return StrictEventsSequencing;
}(lifecycle_1.CompositeDisposable));
exports.StrictEventsSequencing = StrictEventsSequencing;
