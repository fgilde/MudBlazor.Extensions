import { DefaultTab } from './components/tab/defaultTab';
export class DockviewPanelModel {
    get content() {
        return this._content;
    }
    get tab() {
        return this._tab;
    }
    constructor(accessor, id, contentComponent, tabComponent) {
        this.accessor = accessor;
        this.id = id;
        this.contentComponent = contentComponent;
        this.tabComponent = tabComponent;
        this._content = this.createContentComponent(this.id, contentComponent);
        this._tab = this.createTabComponent(this.id, tabComponent);
    }
    createTabRenderer(tabLocation) {
        var _a;
        const cmp = this.createTabComponent(this.id, this.tabComponent);
        if (this._params) {
            cmp.init(Object.assign(Object.assign({}, this._params), { tabLocation }));
        }
        if (this._updateEvent) {
            (_a = cmp.update) === null || _a === void 0 ? void 0 : _a.call(cmp, this._updateEvent);
        }
        return cmp;
    }
    init(params) {
        this._params = params;
        this.content.init(params);
        this.tab.init(Object.assign(Object.assign({}, params), { tabLocation: 'header' }));
    }
    layout(width, height) {
        var _a, _b;
        (_b = (_a = this.content).layout) === null || _b === void 0 ? void 0 : _b.call(_a, width, height);
    }
    update(event) {
        var _a, _b, _c, _d;
        this._updateEvent = event;
        (_b = (_a = this.content).update) === null || _b === void 0 ? void 0 : _b.call(_a, event);
        (_d = (_c = this.tab).update) === null || _d === void 0 ? void 0 : _d.call(_c, event);
    }
    dispose() {
        var _a, _b, _c, _d;
        (_b = (_a = this.content).dispose) === null || _b === void 0 ? void 0 : _b.call(_a);
        (_d = (_c = this.tab).dispose) === null || _d === void 0 ? void 0 : _d.call(_c);
    }
    createContentComponent(id, componentName) {
        return this.accessor.options.createComponent({
            id,
            name: componentName,
        });
    }
    createTabComponent(id, componentName) {
        const name = componentName !== null && componentName !== void 0 ? componentName : this.accessor.options.defaultTabComponent;
        if (name) {
            if (this.accessor.options.createTabComponent) {
                const component = this.accessor.options.createTabComponent({
                    id,
                    name,
                });
                if (component) {
                    return component;
                }
                else {
                    return new DefaultTab();
                }
            }
            console.warn(`dockview: tabComponent '${componentName}' was not found. falling back to the default tab.`);
        }
        return new DefaultTab();
    }
}
