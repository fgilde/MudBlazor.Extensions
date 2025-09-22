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
exports.VoidContainer = void 0;
var dataTransfer_1 = require("../../../dnd/dataTransfer");
var droptarget_1 = require("../../../dnd/droptarget");
var groupDragHandler_1 = require("../../../dnd/groupDragHandler");
var events_1 = require("../../../events");
var lifecycle_1 = require("../../../lifecycle");
var dom_1 = require("../../../dom");
var VoidContainer = /** @class */ (function (_super) {
    __extends(VoidContainer, _super);
    function VoidContainer(accessor, group) {
        var _this = _super.call(this) || this;
        _this.accessor = accessor;
        _this.group = group;
        _this._onDrop = new events_1.Emitter();
        _this.onDrop = _this._onDrop.event;
        _this._onDragStart = new events_1.Emitter();
        _this.onDragStart = _this._onDragStart.event;
        _this._element = document.createElement('div');
        _this._element.className = 'dv-void-container';
        _this._element.draggable = !_this.accessor.options.disableDnd;
        (0, dom_1.toggleClass)(_this._element, 'dv-draggable', !_this.accessor.options.disableDnd);
        _this.addDisposables(_this._onDrop, _this._onDragStart, (0, events_1.addDisposableListener)(_this._element, 'pointerdown', function () {
            _this.accessor.doSetGroupActive(_this.group);
        }));
        _this.handler = new groupDragHandler_1.GroupDragHandler(_this._element, accessor, group, !!_this.accessor.options.disableDnd);
        _this.dropTarget = new droptarget_1.Droptarget(_this._element, {
            acceptedTargetZones: ['center'],
            canDisplayOverlay: function (event, position) {
                var data = (0, dataTransfer_1.getPanelData)();
                if (data && _this.accessor.id === data.viewId) {
                    return true;
                }
                return group.model.canDisplayOverlay(event, position, 'header_space');
            },
            getOverrideTarget: function () { var _a; return (_a = group.model.dropTargetContainer) === null || _a === void 0 ? void 0 : _a.model; },
        });
        _this.onWillShowOverlay = _this.dropTarget.onWillShowOverlay;
        _this.addDisposables(_this.handler, _this.handler.onDragStart(function (event) {
            _this._onDragStart.fire(event);
        }), _this.dropTarget.onDrop(function (event) {
            _this._onDrop.fire(event);
        }), _this.dropTarget);
        return _this;
    }
    Object.defineProperty(VoidContainer.prototype, "element", {
        get: function () {
            return this._element;
        },
        enumerable: false,
        configurable: true
    });
    VoidContainer.prototype.updateDragAndDropState = function () {
        this._element.draggable = !this.accessor.options.disableDnd;
        (0, dom_1.toggleClass)(this._element, 'dv-draggable', !this.accessor.options.disableDnd);
        this.handler.setDisabled(!!this.accessor.options.disableDnd);
    };
    return VoidContainer;
}(lifecycle_1.CompositeDisposable));
exports.VoidContainer = VoidContainer;
