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
exports.DockviewGroupPanelApiImpl = void 0;
var droptarget_1 = require("../dnd/droptarget");
var events_1 = require("../events");
var gridviewPanelApi_1 = require("./gridviewPanelApi");
var NOT_INITIALIZED_MESSAGE = 'dockview: DockviewGroupPanelApiImpl not initialized';
var DockviewGroupPanelApiImpl = /** @class */ (function (_super) {
    __extends(DockviewGroupPanelApiImpl, _super);
    function DockviewGroupPanelApiImpl(id, accessor) {
        var _this = _super.call(this, id, '__dockviewgroup__') || this;
        _this.accessor = accessor;
        _this._onDidLocationChange = new events_1.Emitter();
        _this.onDidLocationChange = _this._onDidLocationChange.event;
        _this._onDidActivePanelChange = new events_1.Emitter();
        _this.onDidActivePanelChange = _this._onDidActivePanelChange.event;
        _this.addDisposables(_this._onDidLocationChange, _this._onDidActivePanelChange);
        return _this;
    }
    Object.defineProperty(DockviewGroupPanelApiImpl.prototype, "location", {
        get: function () {
            if (!this._group) {
                throw new Error(NOT_INITIALIZED_MESSAGE);
            }
            return this._group.model.location;
        },
        enumerable: false,
        configurable: true
    });
    DockviewGroupPanelApiImpl.prototype.close = function () {
        if (!this._group) {
            return;
        }
        return this.accessor.removeGroup(this._group);
    };
    DockviewGroupPanelApiImpl.prototype.getWindow = function () {
        return this.location.type === 'popout'
            ? this.location.getWindow()
            : window;
    };
    DockviewGroupPanelApiImpl.prototype.moveTo = function (options) {
        var _a, _b, _c, _d;
        if (!this._group) {
            throw new Error(NOT_INITIALIZED_MESSAGE);
        }
        var group = (_a = options.group) !== null && _a !== void 0 ? _a : this.accessor.addGroup({
            direction: (0, droptarget_1.positionToDirection)((_b = options.position) !== null && _b !== void 0 ? _b : 'right'),
            skipSetActive: (_c = options.skipSetActive) !== null && _c !== void 0 ? _c : false,
        });
        this.accessor.moveGroupOrPanel({
            from: { groupId: this._group.id },
            to: {
                group: group,
                position: options.group
                    ? (_d = options.position) !== null && _d !== void 0 ? _d : 'center'
                    : 'center',
                index: options.index,
            },
            skipSetActive: options.skipSetActive,
        });
    };
    DockviewGroupPanelApiImpl.prototype.maximize = function () {
        if (!this._group) {
            throw new Error(NOT_INITIALIZED_MESSAGE);
        }
        if (this.location.type !== 'grid') {
            // only grid groups can be maximized
            return;
        }
        this.accessor.maximizeGroup(this._group);
    };
    DockviewGroupPanelApiImpl.prototype.isMaximized = function () {
        if (!this._group) {
            throw new Error(NOT_INITIALIZED_MESSAGE);
        }
        return this.accessor.isMaximizedGroup(this._group);
    };
    DockviewGroupPanelApiImpl.prototype.exitMaximized = function () {
        if (!this._group) {
            throw new Error(NOT_INITIALIZED_MESSAGE);
        }
        if (this.isMaximized()) {
            this.accessor.exitMaximizedGroup();
        }
    };
    DockviewGroupPanelApiImpl.prototype.initialize = function (group) {
        this._group = group;
    };
    return DockviewGroupPanelApiImpl;
}(gridviewPanelApi_1.GridviewPanelApiImpl));
exports.DockviewGroupPanelApiImpl = DockviewGroupPanelApiImpl;
