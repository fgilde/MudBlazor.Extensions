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
exports.Tab = void 0;
var events_1 = require("../../../events");
var lifecycle_1 = require("../../../lifecycle");
var dataTransfer_1 = require("../../../dnd/dataTransfer");
var dom_1 = require("../../../dom");
var droptarget_1 = require("../../../dnd/droptarget");
var abstractDragHandler_1 = require("../../../dnd/abstractDragHandler");
var ghost_1 = require("../../../dnd/ghost");
var TabDragHandler = /** @class */ (function (_super) {
    __extends(TabDragHandler, _super);
    function TabDragHandler(element, accessor, group, panel, disabled) {
        var _this = _super.call(this, element, disabled) || this;
        _this.accessor = accessor;
        _this.group = group;
        _this.panel = panel;
        _this.panelTransfer = dataTransfer_1.LocalSelectionTransfer.getInstance();
        return _this;
    }
    TabDragHandler.prototype.getData = function (event) {
        var _this = this;
        this.panelTransfer.setData([new dataTransfer_1.PanelTransfer(this.accessor.id, this.group.id, this.panel.id)], dataTransfer_1.PanelTransfer.prototype);
        return {
            dispose: function () {
                _this.panelTransfer.clearData(dataTransfer_1.PanelTransfer.prototype);
            },
        };
    };
    return TabDragHandler;
}(abstractDragHandler_1.DragHandler));
var Tab = /** @class */ (function (_super) {
    __extends(Tab, _super);
    function Tab(panel, accessor, group) {
        var _this = _super.call(this) || this;
        _this.panel = panel;
        _this.accessor = accessor;
        _this.group = group;
        _this.content = undefined;
        _this._onPointDown = new events_1.Emitter();
        _this.onPointerDown = _this._onPointDown.event;
        _this._onDropped = new events_1.Emitter();
        _this.onDrop = _this._onDropped.event;
        _this._onDragStart = new events_1.Emitter();
        _this.onDragStart = _this._onDragStart.event;
        _this._element = document.createElement('div');
        _this._element.className = 'dv-tab';
        _this._element.tabIndex = 0;
        _this._element.draggable = !_this.accessor.options.disableDnd;
        (0, dom_1.toggleClass)(_this.element, 'dv-inactive-tab', true);
        _this.dragHandler = new TabDragHandler(_this._element, _this.accessor, _this.group, _this.panel, !!_this.accessor.options.disableDnd);
        _this.dropTarget = new droptarget_1.Droptarget(_this._element, {
            acceptedTargetZones: ['left', 'right'],
            overlayModel: { activationSize: { value: 50, type: 'percentage' } },
            canDisplayOverlay: function (event, position) {
                if (_this.group.locked) {
                    return false;
                }
                var data = (0, dataTransfer_1.getPanelData)();
                if (data && _this.accessor.id === data.viewId) {
                    return true;
                }
                return _this.group.model.canDisplayOverlay(event, position, 'tab');
            },
            getOverrideTarget: function () { var _a; return (_a = group.model.dropTargetContainer) === null || _a === void 0 ? void 0 : _a.model; },
        });
        _this.onWillShowOverlay = _this.dropTarget.onWillShowOverlay;
        _this.addDisposables(_this._onPointDown, _this._onDropped, _this._onDragStart, _this.dragHandler.onDragStart(function (event) {
            if (event.dataTransfer) {
                var style_1 = getComputedStyle(_this.element);
                var newNode_1 = _this.element.cloneNode(true);
                Array.from(style_1).forEach(function (key) {
                    return newNode_1.style.setProperty(key, style_1.getPropertyValue(key), style_1.getPropertyPriority(key));
                });
                newNode_1.style.position = 'absolute';
                (0, ghost_1.addGhostImage)(event.dataTransfer, newNode_1, {
                    y: -10,
                    x: 30,
                });
            }
            _this._onDragStart.fire(event);
        }), _this.dragHandler, (0, events_1.addDisposableListener)(_this._element, 'pointerdown', function (event) {
            _this._onPointDown.fire(event);
        }), _this.dropTarget.onDrop(function (event) {
            _this._onDropped.fire(event);
        }), _this.dropTarget);
        return _this;
    }
    Object.defineProperty(Tab.prototype, "element", {
        get: function () {
            return this._element;
        },
        enumerable: false,
        configurable: true
    });
    Tab.prototype.setActive = function (isActive) {
        (0, dom_1.toggleClass)(this.element, 'dv-active-tab', isActive);
        (0, dom_1.toggleClass)(this.element, 'dv-inactive-tab', !isActive);
    };
    Tab.prototype.setContent = function (part) {
        if (this.content) {
            this._element.removeChild(this.content.element);
        }
        this.content = part;
        this._element.appendChild(this.content.element);
    };
    Tab.prototype.updateDragAndDropState = function () {
        this._element.draggable = !this.accessor.options.disableDnd;
        this.dragHandler.setDisabled(!!this.accessor.options.disableDnd);
    };
    Tab.prototype.dispose = function () {
        _super.prototype.dispose.call(this);
    };
    return Tab;
}(lifecycle_1.CompositeDisposable));
exports.Tab = Tab;
