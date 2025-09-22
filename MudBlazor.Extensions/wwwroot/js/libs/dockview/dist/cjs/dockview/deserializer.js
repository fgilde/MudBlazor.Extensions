"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.DefaultDockviewDeserialzier = void 0;
var dockviewPanel_1 = require("./dockviewPanel");
var dockviewPanelModel_1 = require("./dockviewPanelModel");
var component_api_1 = require("../api/component.api");
var DefaultDockviewDeserialzier = /** @class */ (function () {
    function DefaultDockviewDeserialzier(accessor) {
        this.accessor = accessor;
    }
    DefaultDockviewDeserialzier.prototype.fromJSON = function (panelData, group) {
        var _a, _b;
        var panelId = panelData.id;
        var params = panelData.params;
        var title = panelData.title;
        var viewData = panelData.view;
        var contentComponent = viewData
            ? viewData.content.id
            : (_a = panelData.contentComponent) !== null && _a !== void 0 ? _a : 'unknown';
        var tabComponent = viewData
            ? (_b = viewData.tab) === null || _b === void 0 ? void 0 : _b.id
            : panelData.tabComponent;
        var view = new dockviewPanelModel_1.DockviewPanelModel(this.accessor, panelId, contentComponent, tabComponent);
        var panel = new dockviewPanel_1.DockviewPanel(panelId, contentComponent, tabComponent, this.accessor, new component_api_1.DockviewApi(this.accessor), group, view, {
            renderer: panelData.renderer,
            minimumWidth: panelData.minimumWidth,
            minimumHeight: panelData.minimumHeight,
            maximumWidth: panelData.maximumWidth,
            maximumHeight: panelData.maximumHeight,
        });
        panel.init({
            title: title !== null && title !== void 0 ? title : panelId,
            params: params !== null && params !== void 0 ? params : {},
        });
        return panel;
    };
    return DefaultDockviewDeserialzier;
}());
exports.DefaultDockviewDeserialzier = DefaultDockviewDeserialzier;
