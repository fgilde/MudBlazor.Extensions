import { BasePanelView, } from '../gridview/basePanelView';
import { SplitviewPanelApiImpl } from '../api/splitviewPanelApi';
import { Orientation } from './splitview';
import { Emitter } from '../events';
export class SplitviewPanel extends BasePanelView {
    get priority() {
        return this._priority;
    }
    set orientation(value) {
        this._orientation = value;
    }
    get orientation() {
        return this._orientation;
    }
    get minimumSize() {
        const size = typeof this._minimumSize === 'function'
            ? this._minimumSize()
            : this._minimumSize;
        if (size !== this._evaluatedMinimumSize) {
            this._evaluatedMinimumSize = size;
            this.updateConstraints();
        }
        return size;
    }
    get maximumSize() {
        const size = typeof this._maximumSize === 'function'
            ? this._maximumSize()
            : this._maximumSize;
        if (size !== this._evaluatedMaximumSize) {
            this._evaluatedMaximumSize = size;
            this.updateConstraints();
        }
        return size;
    }
    get snap() {
        return this._snap;
    }
    constructor(id, componentName) {
        super(id, componentName, new SplitviewPanelApiImpl(id, componentName));
        this._evaluatedMinimumSize = 0;
        this._evaluatedMaximumSize = Number.POSITIVE_INFINITY;
        this._minimumSize = 0;
        this._maximumSize = Number.POSITIVE_INFINITY;
        this._snap = false;
        this._onDidChange = new Emitter();
        this.onDidChange = this._onDidChange.event;
        this.api.initialize(this);
        this.addDisposables(this._onDidChange, this.api.onWillVisibilityChange((event) => {
            const { isVisible } = event;
            const { accessor } = this._params;
            accessor.setVisible(this, isVisible);
        }), this.api.onActiveChange(() => {
            const { accessor } = this._params;
            accessor.setActive(this);
        }), this.api.onDidConstraintsChangeInternal((event) => {
            if (typeof event.minimumSize === 'number' ||
                typeof event.minimumSize === 'function') {
                this._minimumSize = event.minimumSize;
            }
            if (typeof event.maximumSize === 'number' ||
                typeof event.maximumSize === 'function') {
                this._maximumSize = event.maximumSize;
            }
            this.updateConstraints();
        }), this.api.onDidSizeChange((event) => {
            this._onDidChange.fire({ size: event.size });
        }));
    }
    setVisible(isVisible) {
        this.api._onDidVisibilityChange.fire({ isVisible });
    }
    setActive(isActive) {
        this.api._onDidActiveChange.fire({ isActive });
    }
    layout(size, orthogonalSize) {
        const [width, height] = this.orientation === Orientation.HORIZONTAL
            ? [size, orthogonalSize]
            : [orthogonalSize, size];
        super.layout(width, height);
    }
    init(parameters) {
        super.init(parameters);
        this._priority = parameters.priority;
        if (parameters.minimumSize) {
            this._minimumSize = parameters.minimumSize;
        }
        if (parameters.maximumSize) {
            this._maximumSize = parameters.maximumSize;
        }
        if (parameters.snap) {
            this._snap = parameters.snap;
        }
    }
    toJSON() {
        const maximum = (value) => value === Number.MAX_SAFE_INTEGER ||
            value === Number.POSITIVE_INFINITY
            ? undefined
            : value;
        const minimum = (value) => (value <= 0 ? undefined : value);
        return Object.assign(Object.assign({}, super.toJSON()), { minimumSize: minimum(this.minimumSize), maximumSize: maximum(this.maximumSize) });
    }
    updateConstraints() {
        this.api._onDidConstraintsChange.fire({
            maximumSize: this._evaluatedMaximumSize,
            minimumSize: this._evaluatedMinimumSize,
        });
    }
}
