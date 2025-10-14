import { DockviewPanel } from './dockviewPanel';
import { DockviewPanelModel } from './dockviewPanelModel';
import { DockviewApi } from '../api/component.api';
export class DefaultDockviewDeserialzier {
    constructor(accessor) {
        this.accessor = accessor;
    }
    fromJSON(panelData, group) {
        var _a, _b;
        const panelId = panelData.id;
        const params = panelData.params;
        const title = panelData.title;
        const viewData = panelData.view;
        const contentComponent = viewData
            ? viewData.content.id
            : (_a = panelData.contentComponent) !== null && _a !== void 0 ? _a : 'unknown';
        const tabComponent = viewData
            ? (_b = viewData.tab) === null || _b === void 0 ? void 0 : _b.id
            : panelData.tabComponent;
        const view = new DockviewPanelModel(this.accessor, panelId, contentComponent, tabComponent);
        const panel = new DockviewPanel(panelId, contentComponent, tabComponent, this.accessor, new DockviewApi(this.accessor), group, view, {
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
    }
}
