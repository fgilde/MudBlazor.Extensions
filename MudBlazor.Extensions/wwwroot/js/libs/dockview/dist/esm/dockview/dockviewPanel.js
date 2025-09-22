import { DockviewPanelApiImpl, } from '../api/dockviewPanelApi';
import { CompositeDisposable } from '../lifecycle';
import { WillFocusEvent } from '../api/panelApi';
export class DockviewPanel extends CompositeDisposable {
    get params() {
        return this._params;
    }
    get title() {
        return this._title;
    }
    get group() {
        return this._group;
    }
    get renderer() {
        var _a;
        return (_a = this._renderer) !== null && _a !== void 0 ? _a : this.accessor.renderer;
    }
    get minimumWidth() {
        return this._minimumWidth;
    }
    get minimumHeight() {
        return this._minimumHeight;
    }
    get maximumWidth() {
        return this._maximumWidth;
    }
    get maximumHeight() {
        return this._maximumHeight;
    }
    constructor(id, component, tabComponent, accessor, containerApi, group, view, options) {
        super();
        this.id = id;
        this.accessor = accessor;
        this.containerApi = containerApi;
        this.view = view;
        this._renderer = options.renderer;
        this._group = group;
        this._minimumWidth = options.minimumWidth;
        this._minimumHeight = options.minimumHeight;
        this._maximumWidth = options.maximumWidth;
        this._maximumHeight = options.maximumHeight;
        this.api = new DockviewPanelApiImpl(this, this._group, accessor, component, tabComponent);
        this.addDisposables(this.api.onActiveChange(() => {
            accessor.setActivePanel(this);
        }), this.api.onDidSizeChange((event) => {
            // forward the resize event to the group since if you want to resize a panel
            // you are actually just resizing the panels parent which is the group
            this.group.api.setSize(event);
        }), this.api.onDidRendererChange(() => {
            this.group.model.rerender(this);
        }));
    }
    init(params) {
        this._params = params.params;
        this.view.init(Object.assign(Object.assign({}, params), { api: this.api, containerApi: this.containerApi }));
        this.setTitle(params.title);
    }
    focus() {
        const event = new WillFocusEvent();
        this.api._onWillFocus.fire(event);
        if (event.defaultPrevented) {
            return;
        }
        if (!this.api.isActive) {
            this.api.setActive();
        }
    }
    toJSON() {
        return {
            id: this.id,
            contentComponent: this.view.contentComponent,
            tabComponent: this.view.tabComponent,
            params: Object.keys(this._params || {}).length > 0
                ? this._params
                : undefined,
            title: this.title,
            renderer: this._renderer,
            minimumHeight: this._minimumHeight,
            maximumHeight: this._maximumHeight,
            minimumWidth: this._minimumWidth,
            maximumWidth: this._maximumWidth,
        };
    }
    setTitle(title) {
        const didTitleChange = title !== this.title;
        if (didTitleChange) {
            this._title = title;
            this.api._onDidTitleChange.fire({ title });
        }
    }
    setRenderer(renderer) {
        const didChange = renderer !== this.renderer;
        if (didChange) {
            this._renderer = renderer;
            this.api._onDidRendererChange.fire({
                renderer: renderer,
            });
        }
    }
    update(event) {
        var _a;
        // merge the new parameters with the existing parameters
        this._params = Object.assign(Object.assign({}, ((_a = this._params) !== null && _a !== void 0 ? _a : {})), event.params);
        /**
         * delete new keys that have a value of undefined,
         * allow values of null
         */
        for (const key of Object.keys(event.params)) {
            if (event.params[key] === undefined) {
                delete this._params[key];
            }
        }
        // update the view with the updated props
        this.view.update({
            params: this._params,
        });
    }
    updateParentGroup(group, options) {
        this._group = group;
        this.api.group = this._group;
        const isPanelVisible = this._group.model.isPanelActive(this);
        const isActive = this.group.api.isActive && isPanelVisible;
        if (!(options === null || options === void 0 ? void 0 : options.skipSetActive)) {
            if (this.api.isActive !== isActive) {
                this.api._onDidActiveChange.fire({
                    isActive: this.group.api.isActive && isPanelVisible,
                });
            }
        }
        if (this.api.isVisible !== isPanelVisible) {
            this.api._onDidVisibilityChange.fire({
                isVisible: isPanelVisible,
            });
        }
    }
    runEvents() {
        const isPanelVisible = this._group.model.isPanelActive(this);
        const isActive = this.group.api.isActive && isPanelVisible;
        if (this.api.isActive !== isActive) {
            this.api._onDidActiveChange.fire({
                isActive: this.group.api.isActive && isPanelVisible,
            });
        }
        if (this.api.isVisible !== isPanelVisible) {
            this.api._onDidVisibilityChange.fire({
                isVisible: isPanelVisible,
            });
        }
    }
    layout(width, height) {
        // TODO: Can we somehow do height without header height or indicate what the header height is?
        this.api._onDidDimensionChange.fire({
            width,
            height: height,
        });
        this.view.layout(width, height);
    }
    dispose() {
        this.api.dispose();
        this.view.dispose();
    }
}
