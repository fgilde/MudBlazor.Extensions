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
var __assign = (this && this.__assign) || function () {
    __assign = Object.assign || function(t) {
        for (var s, i = 1, n = arguments.length; i < n; i++) {
            s = arguments[i];
            for (var p in s) if (Object.prototype.hasOwnProperty.call(s, p))
                t[p] = s[p];
        }
        return t;
    };
    return __assign.apply(this, arguments);
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.DraggablePaneviewPanel = void 0;
var component_api_1 = require("../api/component.api");
var abstractDragHandler_1 = require("../dnd/abstractDragHandler");
var dataTransfer_1 = require("../dnd/dataTransfer");
var droptarget_1 = require("../dnd/droptarget");
var events_1 = require("../events");
var options_1 = require("./options");
var paneviewPanel_1 = require("./paneviewPanel");
var DraggablePaneviewPanel = /** @class */ (function (_super) {
    __extends(DraggablePaneviewPanel, _super);
    function DraggablePaneviewPanel(options) {
        var _this = _super.call(this, {
            id: options.id,
            component: options.component,
            headerComponent: options.headerComponent,
            orientation: options.orientation,
            isExpanded: options.isExpanded,
            isHeaderVisible: true,
            headerSize: options.headerSize,
            minimumBodySize: options.minimumBodySize,
            maximumBodySize: options.maximumBodySize,
        }) || this;
        _this._onDidDrop = new events_1.Emitter();
        _this.onDidDrop = _this._onDidDrop.event;
        _this._onUnhandledDragOverEvent = new events_1.Emitter();
        _this.onUnhandledDragOverEvent = _this._onUnhandledDragOverEvent.event;
        _this.accessor = options.accessor;
        _this.addDisposables(_this._onDidDrop, _this._onUnhandledDragOverEvent);
        if (!options.disableDnd) {
            _this.initDragFeatures();
        }
        return _this;
    }
    DraggablePaneviewPanel.prototype.initDragFeatures = function () {
        var _this = this;
        if (!this.header) {
            return;
        }
        var id = this.id;
        var accessorId = this.accessor.id;
        this.header.draggable = true;
        this.handler = new (/** @class */ (function (_super) {
            __extends(PaneDragHandler, _super);
            function PaneDragHandler() {
                return _super !== null && _super.apply(this, arguments) || this;
            }
            PaneDragHandler.prototype.getData = function () {
                dataTransfer_1.LocalSelectionTransfer.getInstance().setData([new dataTransfer_1.PaneTransfer(accessorId, id)], dataTransfer_1.PaneTransfer.prototype);
                return {
                    dispose: function () {
                        dataTransfer_1.LocalSelectionTransfer.getInstance().clearData(dataTransfer_1.PaneTransfer.prototype);
                    },
                };
            };
            return PaneDragHandler;
        }(abstractDragHandler_1.DragHandler)))(this.header);
        this.target = new droptarget_1.Droptarget(this.element, {
            acceptedTargetZones: ['top', 'bottom'],
            overlayModel: {
                activationSize: { type: 'percentage', value: 50 },
            },
            canDisplayOverlay: function (event, position) {
                var data = (0, dataTransfer_1.getPaneData)();
                if (data) {
                    if (data.paneId !== _this.id &&
                        data.viewId === _this.accessor.id) {
                        return true;
                    }
                }
                var firedEvent = new options_1.PaneviewUnhandledDragOverEvent(event, position, dataTransfer_1.getPaneData, _this);
                _this._onUnhandledDragOverEvent.fire(firedEvent);
                return firedEvent.isAccepted;
            },
        });
        this.addDisposables(this._onDidDrop, this.handler, this.target, this.target.onDrop(function (event) {
            _this.onDrop(event);
        }));
    };
    DraggablePaneviewPanel.prototype.onDrop = function (event) {
        var data = (0, dataTransfer_1.getPaneData)();
        if (!data || data.viewId !== this.accessor.id) {
            // if there is no local drag event for this panel
            // or if the drag event was creating by another Paneview instance
            this._onDidDrop.fire(__assign(__assign({}, event), { panel: this, api: new component_api_1.PaneviewApi(this.accessor), getData: dataTransfer_1.getPaneData }));
            return;
        }
        var containerApi = this._params
            .containerApi;
        var panelId = data.paneId;
        var existingPanel = containerApi.getPanel(panelId);
        if (!existingPanel) {
            // if the panel doesn't exist
            this._onDidDrop.fire(__assign(__assign({}, event), { panel: this, getData: dataTransfer_1.getPaneData, api: new component_api_1.PaneviewApi(this.accessor) }));
            return;
        }
        var allPanels = containerApi.panels;
        var fromIndex = allPanels.indexOf(existingPanel);
        var toIndex = containerApi.panels.indexOf(this);
        if (event.position === 'left' || event.position === 'top') {
            toIndex = Math.max(0, toIndex - 1);
        }
        if (event.position === 'right' || event.position === 'bottom') {
            if (fromIndex > toIndex) {
                toIndex++;
            }
            toIndex = Math.min(allPanels.length - 1, toIndex);
        }
        containerApi.movePanel(fromIndex, toIndex);
    };
    return DraggablePaneviewPanel;
}(paneviewPanel_1.PaneviewPanel));
exports.DraggablePaneviewPanel = DraggablePaneviewPanel;
