import { PaneviewPanelApiImpl } from '../api/paneviewPanelApi';
import { addClasses, removeClasses } from '../dom';
import { addDisposableListener, Emitter } from '../events';
import { BasePanelView, } from '../gridview/basePanelView';
import { Orientation } from '../splitview/splitview';
export class PaneviewPanel extends BasePanelView {
    set orientation(value) {
        this._orientation = value;
    }
    get orientation() {
        return this._orientation;
    }
    get minimumSize() {
        const headerSize = this.headerSize;
        const expanded = this.isExpanded();
        const minimumBodySize = expanded ? this._minimumBodySize : 0;
        return headerSize + minimumBodySize;
    }
    get maximumSize() {
        const headerSize = this.headerSize;
        const expanded = this.isExpanded();
        const maximumBodySize = expanded ? this._maximumBodySize : 0;
        return headerSize + maximumBodySize;
    }
    get size() {
        return this._size;
    }
    get orthogonalSize() {
        return this._orthogonalSize;
    }
    set orthogonalSize(size) {
        this._orthogonalSize = size;
    }
    get minimumBodySize() {
        return this._minimumBodySize;
    }
    set minimumBodySize(value) {
        this._minimumBodySize = typeof value === 'number' ? value : 0;
    }
    get maximumBodySize() {
        return this._maximumBodySize;
    }
    set maximumBodySize(value) {
        this._maximumBodySize =
            typeof value === 'number' ? value : Number.POSITIVE_INFINITY;
    }
    get headerVisible() {
        return this._headerVisible;
    }
    set headerVisible(value) {
        this._headerVisible = value;
        this.header.style.display = value ? '' : 'none';
    }
    constructor(options) {
        super(options.id, options.component, new PaneviewPanelApiImpl(options.id, options.component));
        this._onDidChangeExpansionState = new Emitter({ replay: true });
        this.onDidChangeExpansionState = this._onDidChangeExpansionState.event;
        this._onDidChange = new Emitter();
        this.onDidChange = this._onDidChange.event;
        this._orthogonalSize = 0;
        this._size = 0;
        this._isExpanded = false;
        this.api.pane = this; // TODO cannot use 'this' before 'super'
        this.api.initialize(this);
        this.headerSize = options.headerSize;
        this.headerComponent = options.headerComponent;
        this._minimumBodySize = options.minimumBodySize;
        this._maximumBodySize = options.maximumBodySize;
        this._isExpanded = options.isExpanded;
        this._headerVisible = options.isHeaderVisible;
        this._onDidChangeExpansionState.fire(this.isExpanded()); // initialize value
        this._orientation = options.orientation;
        this.element.classList.add('dv-pane');
        this.addDisposables(this.api.onWillVisibilityChange((event) => {
            const { isVisible } = event;
            const { accessor } = this._params;
            accessor.setVisible(this, isVisible);
        }), this.api.onDidSizeChange((event) => {
            this._onDidChange.fire({ size: event.size });
        }), addDisposableListener(this.element, 'mouseenter', (ev) => {
            this.api._onMouseEnter.fire(ev);
        }), addDisposableListener(this.element, 'mouseleave', (ev) => {
            this.api._onMouseLeave.fire(ev);
        }));
        this.addDisposables(this._onDidChangeExpansionState, this.onDidChangeExpansionState((isPanelExpanded) => {
            this.api._onDidExpansionChange.fire({
                isExpanded: isPanelExpanded,
            });
        }), this.api.onDidFocusChange((e) => {
            if (!this.header) {
                return;
            }
            if (e.isFocused) {
                addClasses(this.header, 'focused');
            }
            else {
                removeClasses(this.header, 'focused');
            }
        }));
        this.renderOnce();
    }
    setVisible(isVisible) {
        this.api._onDidVisibilityChange.fire({ isVisible });
    }
    setActive(isActive) {
        this.api._onDidActiveChange.fire({ isActive });
    }
    isExpanded() {
        return this._isExpanded;
    }
    setExpanded(expanded) {
        if (this._isExpanded === expanded) {
            return;
        }
        this._isExpanded = expanded;
        if (expanded) {
            if (this.animationTimer) {
                clearTimeout(this.animationTimer);
            }
            if (this.body) {
                this.element.appendChild(this.body);
            }
        }
        else {
            this.animationTimer = setTimeout(() => {
                var _a;
                (_a = this.body) === null || _a === void 0 ? void 0 : _a.remove();
            }, 200);
        }
        this._onDidChange.fire(expanded ? { size: this.width } : {});
        this._onDidChangeExpansionState.fire(expanded);
    }
    layout(size, orthogonalSize) {
        this._size = size;
        this._orthogonalSize = orthogonalSize;
        const [width, height] = this.orientation === Orientation.HORIZONTAL
            ? [size, orthogonalSize]
            : [orthogonalSize, size];
        super.layout(width, height);
    }
    init(parameters) {
        var _a, _b;
        super.init(parameters);
        if (typeof parameters.minimumBodySize === 'number') {
            this.minimumBodySize = parameters.minimumBodySize;
        }
        if (typeof parameters.maximumBodySize === 'number') {
            this.maximumBodySize = parameters.maximumBodySize;
        }
        this.bodyPart = this.getBodyComponent();
        this.headerPart = this.getHeaderComponent();
        this.bodyPart.init(Object.assign(Object.assign({}, parameters), { api: this.api }));
        this.headerPart.init(Object.assign(Object.assign({}, parameters), { api: this.api }));
        (_a = this.body) === null || _a === void 0 ? void 0 : _a.append(this.bodyPart.element);
        (_b = this.header) === null || _b === void 0 ? void 0 : _b.append(this.headerPart.element);
        if (typeof parameters.isExpanded === 'boolean') {
            this.setExpanded(parameters.isExpanded);
        }
    }
    toJSON() {
        const params = this._params;
        return Object.assign(Object.assign({}, super.toJSON()), { headerComponent: this.headerComponent, title: params.title });
    }
    renderOnce() {
        this.header = document.createElement('div');
        this.header.tabIndex = 0;
        this.header.className = 'dv-pane-header';
        this.header.style.height = `${this.headerSize}px`;
        this.header.style.lineHeight = `${this.headerSize}px`;
        this.header.style.minHeight = `${this.headerSize}px`;
        this.header.style.maxHeight = `${this.headerSize}px`;
        this.element.appendChild(this.header);
        this.body = document.createElement('div');
        this.body.className = 'dv-pane-body';
        this.element.appendChild(this.body);
    }
    // TODO slightly hacky by-pass of the component to create a body and header component
    getComponent() {
        return {
            update: (params) => {
                var _a, _b;
                (_a = this.bodyPart) === null || _a === void 0 ? void 0 : _a.update({ params });
                (_b = this.headerPart) === null || _b === void 0 ? void 0 : _b.update({ params });
            },
            dispose: () => {
                var _a, _b;
                (_a = this.bodyPart) === null || _a === void 0 ? void 0 : _a.dispose();
                (_b = this.headerPart) === null || _b === void 0 ? void 0 : _b.dispose();
            },
        };
    }
}
