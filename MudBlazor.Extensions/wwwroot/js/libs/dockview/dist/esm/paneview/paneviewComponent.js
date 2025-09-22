import { PaneviewApi } from '../api/component.api';
import { Emitter } from '../events';
import { CompositeDisposable, MutableDisposable, } from '../lifecycle';
import { Orientation, Sizing } from '../splitview/splitview';
import { Paneview } from './paneview';
import { DraggablePaneviewPanel, } from './draggablePaneviewPanel';
import { DefaultHeader } from './defaultPaneviewHeader';
import { sequentialNumberGenerator } from '../math';
import { Resizable } from '../resizable';
import { Classnames } from '../dom';
const nextLayoutId = sequentialNumberGenerator();
const HEADER_SIZE = 22;
const MINIMUM_BODY_SIZE = 0;
const MAXIMUM_BODY_SIZE = Number.MAX_SAFE_INTEGER;
export class PaneFramework extends DraggablePaneviewPanel {
    constructor(options) {
        super({
            accessor: options.accessor,
            id: options.id,
            component: options.component,
            headerComponent: options.headerComponent,
            orientation: options.orientation,
            isExpanded: options.isExpanded,
            disableDnd: options.disableDnd,
            headerSize: options.headerSize,
            minimumBodySize: options.minimumBodySize,
            maximumBodySize: options.maximumBodySize,
        });
        this.options = options;
    }
    getBodyComponent() {
        return this.options.body;
    }
    getHeaderComponent() {
        return this.options.header;
    }
}
export class PaneviewComponent extends Resizable {
    get id() {
        return this._id;
    }
    get panels() {
        return this.paneview.getPanes();
    }
    set paneview(value) {
        this._paneview = value;
        this._disposable.value = new CompositeDisposable(this._paneview.onDidChange(() => {
            this._onDidLayoutChange.fire(undefined);
        }), this._paneview.onDidAddView((e) => this._onDidAddView.fire(e)), this._paneview.onDidRemoveView((e) => this._onDidRemoveView.fire(e)));
    }
    get paneview() {
        return this._paneview;
    }
    get minimumSize() {
        return this.paneview.minimumSize;
    }
    get maximumSize() {
        return this.paneview.maximumSize;
    }
    get height() {
        return this.paneview.orientation === Orientation.HORIZONTAL
            ? this.paneview.orthogonalSize
            : this.paneview.size;
    }
    get width() {
        return this.paneview.orientation === Orientation.HORIZONTAL
            ? this.paneview.size
            : this.paneview.orthogonalSize;
    }
    get options() {
        return this._options;
    }
    constructor(container, options) {
        var _a;
        super(document.createElement('div'), options.disableAutoResizing);
        this._id = nextLayoutId.next();
        this._disposable = new MutableDisposable();
        this._viewDisposables = new Map();
        this._onDidLayoutfromJSON = new Emitter();
        this.onDidLayoutFromJSON = this._onDidLayoutfromJSON.event;
        this._onDidLayoutChange = new Emitter();
        this.onDidLayoutChange = this._onDidLayoutChange.event;
        this._onDidDrop = new Emitter();
        this.onDidDrop = this._onDidDrop.event;
        this._onDidAddView = new Emitter();
        this.onDidAddView = this._onDidAddView.event;
        this._onDidRemoveView = new Emitter();
        this.onDidRemoveView = this._onDidRemoveView.event;
        this._onUnhandledDragOverEvent = new Emitter();
        this.onUnhandledDragOverEvent = this._onUnhandledDragOverEvent.event;
        this.element.style.height = '100%';
        this.element.style.width = '100%';
        this.addDisposables(this._onDidLayoutChange, this._onDidLayoutfromJSON, this._onDidDrop, this._onDidAddView, this._onDidRemoveView, this._onUnhandledDragOverEvent);
        this._classNames = new Classnames(this.element);
        this._classNames.setClassNames((_a = options.className) !== null && _a !== void 0 ? _a : '');
        // the container is owned by the third-party, do not modify/delete it
        container.appendChild(this.element);
        this._options = options;
        this.paneview = new Paneview(this.element, {
            // only allow paneview in the vertical orientation for now
            orientation: Orientation.VERTICAL,
        });
        this.addDisposables(this._disposable);
    }
    setVisible(panel, visible) {
        const index = this.panels.indexOf(panel);
        this.paneview.setViewVisible(index, visible);
    }
    focus() {
        //noop
    }
    updateOptions(options) {
        var _a, _b;
        if ('className' in options) {
            this._classNames.setClassNames((_a = options.className) !== null && _a !== void 0 ? _a : '');
        }
        if ('disableResizing' in options) {
            this.disableResizing = (_b = options.disableAutoResizing) !== null && _b !== void 0 ? _b : false;
        }
        this._options = Object.assign(Object.assign({}, this.options), options);
    }
    addPanel(options) {
        var _a, _b;
        const body = this.options.createComponent({
            id: options.id,
            name: options.component,
        });
        let header;
        if (options.headerComponent && this.options.createHeaderComponent) {
            header = this.options.createHeaderComponent({
                id: options.id,
                name: options.headerComponent,
            });
        }
        if (!header) {
            header = new DefaultHeader();
        }
        const view = new PaneFramework({
            id: options.id,
            component: options.component,
            headerComponent: options.headerComponent,
            header,
            body,
            orientation: Orientation.VERTICAL,
            isExpanded: !!options.isExpanded,
            disableDnd: !!this.options.disableDnd,
            accessor: this,
            headerSize: (_a = options.headerSize) !== null && _a !== void 0 ? _a : HEADER_SIZE,
            minimumBodySize: MINIMUM_BODY_SIZE,
            maximumBodySize: MAXIMUM_BODY_SIZE,
        });
        this.doAddPanel(view);
        const size = typeof options.size === 'number' ? options.size : Sizing.Distribute;
        const index = typeof options.index === 'number' ? options.index : undefined;
        view.init({
            params: (_b = options.params) !== null && _b !== void 0 ? _b : {},
            minimumBodySize: options.minimumBodySize,
            maximumBodySize: options.maximumBodySize,
            isExpanded: options.isExpanded,
            title: options.title,
            containerApi: new PaneviewApi(this),
            accessor: this,
        });
        this.paneview.addPane(view, size, index);
        view.orientation = this.paneview.orientation;
        return view;
    }
    removePanel(panel) {
        const views = this.panels;
        const index = views.findIndex((_) => _ === panel);
        this.paneview.removePane(index);
        this.doRemovePanel(panel);
    }
    movePanel(from, to) {
        this.paneview.moveView(from, to);
    }
    getPanel(id) {
        return this.panels.find((view) => view.id === id);
    }
    layout(width, height) {
        const [size, orthogonalSize] = this.paneview.orientation === Orientation.HORIZONTAL
            ? [width, height]
            : [height, width];
        this.paneview.layout(size, orthogonalSize);
    }
    toJSON() {
        const maximum = (value) => value === Number.MAX_SAFE_INTEGER ||
            value === Number.POSITIVE_INFINITY
            ? undefined
            : value;
        const minimum = (value) => (value <= 0 ? undefined : value);
        const views = this.paneview
            .getPanes()
            .map((view, i) => {
            const size = this.paneview.getViewSize(i);
            return {
                size,
                data: view.toJSON(),
                minimumSize: minimum(view.minimumBodySize),
                maximumSize: maximum(view.maximumBodySize),
                headerSize: view.headerSize,
                expanded: view.isExpanded(),
            };
        });
        return {
            views,
            size: this.paneview.size,
        };
    }
    fromJSON(serializedPaneview) {
        this.clear();
        const { views, size } = serializedPaneview;
        const queue = [];
        // take note of the existing dimensions
        const width = this.width;
        const height = this.height;
        this.paneview = new Paneview(this.element, {
            orientation: Orientation.VERTICAL,
            descriptor: {
                size,
                views: views.map((view) => {
                    var _a, _b, _c;
                    const data = view.data;
                    const body = this.options.createComponent({
                        id: data.id,
                        name: data.component,
                    });
                    let header;
                    if (data.headerComponent &&
                        this.options.createHeaderComponent) {
                        header = this.options.createHeaderComponent({
                            id: data.id,
                            name: data.headerComponent,
                        });
                    }
                    if (!header) {
                        header = new DefaultHeader();
                    }
                    const panel = new PaneFramework({
                        id: data.id,
                        component: data.component,
                        headerComponent: data.headerComponent,
                        header,
                        body,
                        orientation: Orientation.VERTICAL,
                        isExpanded: !!view.expanded,
                        disableDnd: !!this.options.disableDnd,
                        accessor: this,
                        headerSize: (_a = view.headerSize) !== null && _a !== void 0 ? _a : HEADER_SIZE,
                        minimumBodySize: (_b = view.minimumSize) !== null && _b !== void 0 ? _b : MINIMUM_BODY_SIZE,
                        maximumBodySize: (_c = view.maximumSize) !== null && _c !== void 0 ? _c : MAXIMUM_BODY_SIZE,
                    });
                    this.doAddPanel(panel);
                    queue.push(() => {
                        var _a;
                        panel.init({
                            params: (_a = data.params) !== null && _a !== void 0 ? _a : {},
                            minimumBodySize: view.minimumSize,
                            maximumBodySize: view.maximumSize,
                            title: data.title,
                            isExpanded: !!view.expanded,
                            containerApi: new PaneviewApi(this),
                            accessor: this,
                        });
                        panel.orientation = this.paneview.orientation;
                    });
                    setTimeout(() => {
                        // the original onDidAddView events are missed since they are fired before we can subcribe to them
                        this._onDidAddView.fire(panel);
                    }, 0);
                    return { size: view.size, view: panel };
                }),
            },
        });
        this.layout(width, height);
        queue.forEach((f) => f());
        this._onDidLayoutfromJSON.fire();
    }
    clear() {
        for (const [_, value] of this._viewDisposables.entries()) {
            value.dispose();
        }
        this._viewDisposables.clear();
        this.paneview.dispose();
    }
    doAddPanel(panel) {
        const disposable = new CompositeDisposable(panel.onDidDrop((event) => {
            this._onDidDrop.fire(event);
        }), panel.onUnhandledDragOverEvent((event) => {
            this._onUnhandledDragOverEvent.fire(event);
        }));
        this._viewDisposables.set(panel.id, disposable);
    }
    doRemovePanel(panel) {
        const disposable = this._viewDisposables.get(panel.id);
        if (disposable) {
            disposable.dispose();
            this._viewDisposables.delete(panel.id);
        }
    }
    dispose() {
        super.dispose();
        for (const [_, value] of this._viewDisposables.entries()) {
            value.dispose();
        }
        this._viewDisposables.clear();
        this.element.remove();
        this.paneview.dispose();
    }
}
