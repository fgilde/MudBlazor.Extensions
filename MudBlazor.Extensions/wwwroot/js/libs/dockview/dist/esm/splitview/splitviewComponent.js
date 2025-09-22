import { CompositeDisposable, MutableDisposable, } from '../lifecycle';
import { Orientation, Sizing, Splitview, } from './splitview';
import { Emitter } from '../events';
import { Resizable } from '../resizable';
import { Classnames } from '../dom';
/**
 * A high-level implementation of splitview that works using 'panels'
 */
export class SplitviewComponent extends Resizable {
    get panels() {
        return this.splitview.getViews();
    }
    get options() {
        return this._options;
    }
    get length() {
        return this._panels.size;
    }
    get orientation() {
        return this.splitview.orientation;
    }
    get splitview() {
        return this._splitview;
    }
    set splitview(value) {
        if (this._splitview) {
            this._splitview.dispose();
        }
        this._splitview = value;
        this._splitviewChangeDisposable.value = new CompositeDisposable(this._splitview.onDidSashEnd(() => {
            this._onDidLayoutChange.fire(undefined);
        }), this._splitview.onDidAddView((e) => this._onDidAddView.fire(e)), this._splitview.onDidRemoveView((e) => this._onDidRemoveView.fire(e)));
    }
    get minimumSize() {
        return this.splitview.minimumSize;
    }
    get maximumSize() {
        return this.splitview.maximumSize;
    }
    get height() {
        return this.splitview.orientation === Orientation.HORIZONTAL
            ? this.splitview.orthogonalSize
            : this.splitview.size;
    }
    get width() {
        return this.splitview.orientation === Orientation.HORIZONTAL
            ? this.splitview.size
            : this.splitview.orthogonalSize;
    }
    constructor(container, options) {
        var _a;
        super(document.createElement('div'), options.disableAutoResizing);
        this._splitviewChangeDisposable = new MutableDisposable();
        this._panels = new Map();
        this._onDidLayoutfromJSON = new Emitter();
        this.onDidLayoutFromJSON = this._onDidLayoutfromJSON.event;
        this._onDidAddView = new Emitter();
        this.onDidAddView = this._onDidAddView.event;
        this._onDidRemoveView = new Emitter();
        this.onDidRemoveView = this._onDidRemoveView.event;
        this._onDidLayoutChange = new Emitter();
        this.onDidLayoutChange = this._onDidLayoutChange.event;
        this.element.style.height = '100%';
        this.element.style.width = '100%';
        this._classNames = new Classnames(this.element);
        this._classNames.setClassNames((_a = options.className) !== null && _a !== void 0 ? _a : '');
        // the container is owned by the third-party, do not modify/delete it
        container.appendChild(this.element);
        this._options = options;
        this.splitview = new Splitview(this.element, options);
        this.addDisposables(this._onDidAddView, this._onDidLayoutfromJSON, this._onDidRemoveView, this._onDidLayoutChange);
    }
    updateOptions(options) {
        var _a, _b;
        if ('className' in options) {
            this._classNames.setClassNames((_a = options.className) !== null && _a !== void 0 ? _a : '');
        }
        if ('disableResizing' in options) {
            this.disableResizing = (_b = options.disableAutoResizing) !== null && _b !== void 0 ? _b : false;
        }
        if (typeof options.orientation === 'string') {
            this.splitview.orientation = options.orientation;
        }
        this._options = Object.assign(Object.assign({}, this.options), options);
        this.splitview.layout(this.splitview.size, this.splitview.orthogonalSize);
    }
    focus() {
        var _a;
        (_a = this._activePanel) === null || _a === void 0 ? void 0 : _a.focus();
    }
    movePanel(from, to) {
        this.splitview.moveView(from, to);
    }
    setVisible(panel, visible) {
        const index = this.panels.indexOf(panel);
        this.splitview.setViewVisible(index, visible);
    }
    setActive(panel, skipFocus) {
        this._activePanel = panel;
        this.panels
            .filter((v) => v !== panel)
            .forEach((v) => {
            v.api._onDidActiveChange.fire({ isActive: false });
            if (!skipFocus) {
                v.focus();
            }
        });
        panel.api._onDidActiveChange.fire({ isActive: true });
        if (!skipFocus) {
            panel.focus();
        }
    }
    removePanel(panel, sizing) {
        const item = this._panels.get(panel.id);
        if (!item) {
            throw new Error(`unknown splitview panel ${panel.id}`);
        }
        item.dispose();
        this._panels.delete(panel.id);
        const index = this.panels.findIndex((_) => _ === panel);
        const removedView = this.splitview.removeView(index, sizing);
        removedView.dispose();
        const panels = this.panels;
        if (panels.length > 0) {
            this.setActive(panels[panels.length - 1]);
        }
    }
    getPanel(id) {
        return this.panels.find((view) => view.id === id);
    }
    addPanel(options) {
        var _a;
        if (this._panels.has(options.id)) {
            throw new Error(`panel ${options.id} already exists`);
        }
        const view = this.options.createComponent({
            id: options.id,
            name: options.component,
        });
        view.orientation = this.splitview.orientation;
        view.init({
            params: (_a = options.params) !== null && _a !== void 0 ? _a : {},
            minimumSize: options.minimumSize,
            maximumSize: options.maximumSize,
            snap: options.snap,
            priority: options.priority,
            accessor: this,
        });
        const size = typeof options.size === 'number' ? options.size : Sizing.Distribute;
        const index = typeof options.index === 'number' ? options.index : undefined;
        this.splitview.addView(view, size, index);
        this.doAddView(view);
        this.setActive(view);
        return view;
    }
    layout(width, height) {
        const [size, orthogonalSize] = this.splitview.orientation === Orientation.HORIZONTAL
            ? [width, height]
            : [height, width];
        this.splitview.layout(size, orthogonalSize);
    }
    doAddView(view) {
        const disposable = view.api.onDidFocusChange((event) => {
            if (!event.isFocused) {
                return;
            }
            this.setActive(view, true);
        });
        this._panels.set(view.id, disposable);
    }
    toJSON() {
        var _a;
        const views = this.splitview
            .getViews()
            .map((view, i) => {
            const size = this.splitview.getViewSize(i);
            return {
                size,
                data: view.toJSON(),
                snap: !!view.snap,
                priority: view.priority,
            };
        });
        return {
            views,
            activeView: (_a = this._activePanel) === null || _a === void 0 ? void 0 : _a.id,
            size: this.splitview.size,
            orientation: this.splitview.orientation,
        };
    }
    fromJSON(serializedSplitview) {
        this.clear();
        const { views, orientation, size, activeView } = serializedSplitview;
        const queue = [];
        // take note of the existing dimensions
        const width = this.width;
        const height = this.height;
        this.splitview = new Splitview(this.element, {
            orientation,
            proportionalLayout: this.options.proportionalLayout,
            descriptor: {
                size,
                views: views.map((view) => {
                    const data = view.data;
                    if (this._panels.has(data.id)) {
                        throw new Error(`panel ${data.id} already exists`);
                    }
                    const panel = this.options.createComponent({
                        id: data.id,
                        name: data.component,
                    });
                    queue.push(() => {
                        var _a;
                        panel.init({
                            params: (_a = data.params) !== null && _a !== void 0 ? _a : {},
                            minimumSize: data.minimumSize,
                            maximumSize: data.maximumSize,
                            snap: view.snap,
                            priority: view.priority,
                            accessor: this,
                        });
                    });
                    panel.orientation = orientation;
                    this.doAddView(panel);
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
        if (typeof activeView === 'string') {
            const panel = this.getPanel(activeView);
            if (panel) {
                this.setActive(panel);
            }
        }
        this._onDidLayoutfromJSON.fire();
    }
    clear() {
        for (const disposable of this._panels.values()) {
            disposable.dispose();
        }
        this._panels.clear();
        while (this.splitview.length > 0) {
            const view = this.splitview.removeView(0, Sizing.Distribute, true);
            view.dispose();
        }
    }
    dispose() {
        for (const disposable of this._panels.values()) {
            disposable.dispose();
        }
        this._panels.clear();
        const views = this.splitview.getViews();
        this._splitviewChangeDisposable.dispose();
        this.splitview.dispose();
        for (const view of views) {
            view.dispose();
        }
        this.element.remove();
        super.dispose();
    }
}
