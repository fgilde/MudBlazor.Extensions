"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.createPaneview = exports.createGridview = exports.createSplitview = exports.createDockview = void 0;
var component_api_1 = require("../api/component.api");
var dockviewComponent_1 = require("../dockview/dockviewComponent");
var gridviewComponent_1 = require("../gridview/gridviewComponent");
var paneviewComponent_1 = require("../paneview/paneviewComponent");
var splitviewComponent_1 = require("../splitview/splitviewComponent");
function createDockview(element, options) {
    var component = new dockviewComponent_1.DockviewComponent(element, options);
    return component.api;
}
exports.createDockview = createDockview;
function createSplitview(element, options) {
    var component = new splitviewComponent_1.SplitviewComponent(element, options);
    return new component_api_1.SplitviewApi(component);
}
exports.createSplitview = createSplitview;
function createGridview(element, options) {
    var component = new gridviewComponent_1.GridviewComponent(element, options);
    return new component_api_1.GridviewApi(component);
}
exports.createGridview = createGridview;
function createPaneview(element, options) {
    var component = new paneviewComponent_1.PaneviewComponent(element, options);
    return new component_api_1.PaneviewApi(component);
}
exports.createPaneview = createPaneview;
