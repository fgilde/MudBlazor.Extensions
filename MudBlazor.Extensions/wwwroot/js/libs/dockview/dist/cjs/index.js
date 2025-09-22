"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __exportStar = (this && this.__exportStar) || function(m, exports) {
    for (var p in m) if (p !== "default" && !Object.prototype.hasOwnProperty.call(exports, p)) __createBinding(exports, m, p);
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.createSplitview = exports.createPaneview = exports.createGridview = exports.createDockview = exports.DockviewApi = exports.GridviewApi = exports.PaneviewApi = exports.SplitviewApi = exports.directionToPosition = exports.positionToDirection = exports.SplitviewPanel = exports.PaneviewUnhandledDragOverEvent = exports.PROPERTY_KEYS_PANEVIEW = exports.DefaultDockviewDeserialzier = exports.DefaultTab = exports.DraggablePaneviewPanel = exports.PROPERTY_KEYS_GRIDVIEW = exports.PROPERTY_KEYS_SPLITVIEW = exports.DockviewDisposable = exports.DockviewCompositeDisposable = exports.DockviewMutableDisposable = exports.DockviewEvent = exports.DockviewEmitter = exports.PanelTransfer = exports.PaneTransfer = exports.getPanelData = exports.getPaneData = void 0;
var dataTransfer_1 = require("./dnd/dataTransfer");
Object.defineProperty(exports, "getPaneData", { enumerable: true, get: function () { return dataTransfer_1.getPaneData; } });
Object.defineProperty(exports, "getPanelData", { enumerable: true, get: function () { return dataTransfer_1.getPanelData; } });
Object.defineProperty(exports, "PaneTransfer", { enumerable: true, get: function () { return dataTransfer_1.PaneTransfer; } });
Object.defineProperty(exports, "PanelTransfer", { enumerable: true, get: function () { return dataTransfer_1.PanelTransfer; } });
/**
 * Events, Emitters and Disposables are very common concepts that many codebases will contain, however we need
 * to export them for dockview framework packages to use.
 * To be a good citizen these are exported with a `Dockview` prefix to prevent accidental use by others.
 */
var events_1 = require("./events");
Object.defineProperty(exports, "DockviewEmitter", { enumerable: true, get: function () { return events_1.Emitter; } });
Object.defineProperty(exports, "DockviewEvent", { enumerable: true, get: function () { return events_1.Event; } });
var lifecycle_1 = require("./lifecycle");
Object.defineProperty(exports, "DockviewMutableDisposable", { enumerable: true, get: function () { return lifecycle_1.MutableDisposable; } });
Object.defineProperty(exports, "DockviewCompositeDisposable", { enumerable: true, get: function () { return lifecycle_1.CompositeDisposable; } });
Object.defineProperty(exports, "DockviewDisposable", { enumerable: true, get: function () { return lifecycle_1.Disposable; } });
__exportStar(require("./panel/types"), exports);
__exportStar(require("./splitview/splitview"), exports);
var options_1 = require("./splitview/options");
Object.defineProperty(exports, "PROPERTY_KEYS_SPLITVIEW", { enumerable: true, get: function () { return options_1.PROPERTY_KEYS_SPLITVIEW; } });
__exportStar(require("./paneview/paneview"), exports);
__exportStar(require("./gridview/gridview"), exports);
var options_2 = require("./gridview/options");
Object.defineProperty(exports, "PROPERTY_KEYS_GRIDVIEW", { enumerable: true, get: function () { return options_2.PROPERTY_KEYS_GRIDVIEW; } });
__exportStar(require("./gridview/baseComponentGridview"), exports);
var draggablePaneviewPanel_1 = require("./paneview/draggablePaneviewPanel");
Object.defineProperty(exports, "DraggablePaneviewPanel", { enumerable: true, get: function () { return draggablePaneviewPanel_1.DraggablePaneviewPanel; } });
__exportStar(require("./dockview/components/panel/content"), exports);
__exportStar(require("./dockview/components/tab/tab"), exports);
__exportStar(require("./dockview/dockviewGroupPanelModel"), exports);
__exportStar(require("./dockview/types"), exports);
__exportStar(require("./dockview/dockviewGroupPanel"), exports);
__exportStar(require("./dockview/options"), exports);
__exportStar(require("./dockview/theme"), exports);
__exportStar(require("./dockview/dockviewPanel"), exports);
var defaultTab_1 = require("./dockview/components/tab/defaultTab");
Object.defineProperty(exports, "DefaultTab", { enumerable: true, get: function () { return defaultTab_1.DefaultTab; } });
var deserializer_1 = require("./dockview/deserializer");
Object.defineProperty(exports, "DefaultDockviewDeserialzier", { enumerable: true, get: function () { return deserializer_1.DefaultDockviewDeserialzier; } });
__exportStar(require("./dockview/dockviewComponent"), exports);
__exportStar(require("./gridview/gridviewComponent"), exports);
__exportStar(require("./splitview/splitviewComponent"), exports);
__exportStar(require("./paneview/paneviewComponent"), exports);
var options_3 = require("./paneview/options");
Object.defineProperty(exports, "PROPERTY_KEYS_PANEVIEW", { enumerable: true, get: function () { return options_3.PROPERTY_KEYS_PANEVIEW; } });
Object.defineProperty(exports, "PaneviewUnhandledDragOverEvent", { enumerable: true, get: function () { return options_3.PaneviewUnhandledDragOverEvent; } });
__exportStar(require("./gridview/gridviewPanel"), exports);
var splitviewPanel_1 = require("./splitview/splitviewPanel");
Object.defineProperty(exports, "SplitviewPanel", { enumerable: true, get: function () { return splitviewPanel_1.SplitviewPanel; } });
__exportStar(require("./paneview/paneviewPanel"), exports);
__exportStar(require("./dockview/types"), exports);
var droptarget_1 = require("./dnd/droptarget");
Object.defineProperty(exports, "positionToDirection", { enumerable: true, get: function () { return droptarget_1.positionToDirection; } });
Object.defineProperty(exports, "directionToPosition", { enumerable: true, get: function () { return droptarget_1.directionToPosition; } });
var component_api_1 = require("./api/component.api");
Object.defineProperty(exports, "SplitviewApi", { enumerable: true, get: function () { return component_api_1.SplitviewApi; } });
Object.defineProperty(exports, "PaneviewApi", { enumerable: true, get: function () { return component_api_1.PaneviewApi; } });
Object.defineProperty(exports, "GridviewApi", { enumerable: true, get: function () { return component_api_1.GridviewApi; } });
Object.defineProperty(exports, "DockviewApi", { enumerable: true, get: function () { return component_api_1.DockviewApi; } });
var entryPoints_1 = require("./api/entryPoints");
Object.defineProperty(exports, "createDockview", { enumerable: true, get: function () { return entryPoints_1.createDockview; } });
Object.defineProperty(exports, "createGridview", { enumerable: true, get: function () { return entryPoints_1.createGridview; } });
Object.defineProperty(exports, "createPaneview", { enumerable: true, get: function () { return entryPoints_1.createPaneview; } });
Object.defineProperty(exports, "createSplitview", { enumerable: true, get: function () { return entryPoints_1.createSplitview; } });
