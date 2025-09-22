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
exports.DockviewPanelApiImpl = void 0;
var events_1 = require("../events");
var gridviewPanelApi_1 = require("./gridviewPanelApi");
var lifecycle_1 = require("../lifecycle");
var DockviewPanelApiImpl = /** @class */ (function (_super) {
    __extends(DockviewPanelApiImpl, _super);
    function DockviewPanelApiImpl(panel, group, accessor, component, tabComponent) {
        var _this = _super.call(this, panel.id, component) || this;
        _this.panel = panel;
        _this.accessor = accessor;
        _this._onDidTitleChange = new events_1.Emitter();
        _this.onDidTitleChange = _this._onDidTitleChange.event;
        _this._onDidActiveGroupChange = new events_1.Emitter();
        _this.onDidActiveGroupChange = _this._onDidActiveGroupChange.event;
        _this._onDidGroupChange = new events_1.Emitter();
        _this.onDidGroupChange = _this._onDidGroupChange.event;
        _this._onDidRendererChange = new events_1.Emitter();
        _this.onDidRendererChange = _this._onDidRendererChange.event;
        _this._onDidLocationChange = new events_1.Emitter();
        _this.onDidLocationChange = _this._onDidLocationChange.event;
        _this.groupEventsDisposable = new lifecycle_1.MutableDisposable();
        _this._tabComponent = tabComponent;
        _this.initialize(panel);
        _this._group = group;
        _this.setupGroupEventListeners();
        _this.addDisposables(_this.groupEventsDisposable, _this._onDidRendererChange, _this._onDidTitleChange, _this._onDidGroupChange, _this._onDidActiveGroupChange, _this._onDidLocationChange);
        return _this;
    }
    Object.defineProperty(DockviewPanelApiImpl.prototype, "location", {
        get: function () {
            return this.group.api.location;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewPanelApiImpl.prototype, "title", {
        get: function () {
            return this.panel.title;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewPanelApiImpl.prototype, "isGroupActive", {
        get: function () {
            return this.group.isActive;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewPanelApiImpl.prototype, "renderer", {
        get: function () {
            return this.panel.renderer;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewPanelApiImpl.prototype, "group", {
        get: function () {
            return this._group;
        },
        set: function (value) {
            var oldGroup = this._group;
            if (this._group !== value) {
                this._group = value;
                this._onDidGroupChange.fire({});
                this.setupGroupEventListeners(oldGroup);
                this._onDidLocationChange.fire({
                    location: this.group.api.location,
                });
            }
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewPanelApiImpl.prototype, "tabComponent", {
        get: function () {
            return this._tabComponent;
        },
        enumerable: false,
        configurable: true
    });
    DockviewPanelApiImpl.prototype.getWindow = function () {
        return this.group.api.getWindow();
    };
    DockviewPanelApiImpl.prototype.moveTo = function (options) {
        var _a, _b;
        this.accessor.moveGroupOrPanel({
            from: { groupId: this._group.id, panelId: this.panel.id },
            to: {
                group: (_a = options.group) !== null && _a !== void 0 ? _a : this._group,
                position: options.group
                    ? (_b = options.position) !== null && _b !== void 0 ? _b : 'center'
                    : 'center',
                index: options.index,
            },
            skipSetActive: options.skipSetActive,
        });
    };
    DockviewPanelApiImpl.prototype.setTitle = function (title) {
        this.panel.setTitle(title);
    };
    DockviewPanelApiImpl.prototype.setRenderer = function (renderer) {
        this.panel.setRenderer(renderer);
    };
    DockviewPanelApiImpl.prototype.close = function () {
        this.group.model.closePanel(this.panel);
    };
    DockviewPanelApiImpl.prototype.maximize = function () {
        this.group.api.maximize();
    };
    DockviewPanelApiImpl.prototype.isMaximized = function () {
        return this.group.api.isMaximized();
    };
    DockviewPanelApiImpl.prototype.exitMaximized = function () {
        this.group.api.exitMaximized();
    };
    DockviewPanelApiImpl.prototype.setupGroupEventListeners = function (previousGroup) {
        var _this = this;
        var _a;
        var _trackGroupActive = (_a = previousGroup === null || previousGroup === void 0 ? void 0 : previousGroup.isActive) !== null && _a !== void 0 ? _a : false; // prevent duplicate events with same state
        this.groupEventsDisposable.value = new lifecycle_1.CompositeDisposable(this.group.api.onDidVisibilityChange(function (event) {
            var hasBecomeHidden = !event.isVisible && _this.isVisible;
            var hasBecomeVisible = event.isVisible && !_this.isVisible;
            var isActivePanel = _this.group.model.isPanelActive(_this.panel);
            if (hasBecomeHidden || (hasBecomeVisible && isActivePanel)) {
                _this._onDidVisibilityChange.fire(event);
            }
        }), this.group.api.onDidLocationChange(function (event) {
            if (_this.group !== _this.panel.group) {
                return;
            }
            _this._onDidLocationChange.fire(event);
        }), this.group.api.onDidActiveChange(function () {
            if (_this.group !== _this.panel.group) {
                return;
            }
            if (_trackGroupActive !== _this.isGroupActive) {
                _trackGroupActive = _this.isGroupActive;
                _this._onDidActiveGroupChange.fire({
                    isActive: _this.isGroupActive,
                });
            }
        }));
    };
    return DockviewPanelApiImpl;
}(gridviewPanelApi_1.GridviewPanelApiImpl));
exports.DockviewPanelApiImpl = DockviewPanelApiImpl;
