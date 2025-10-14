import { BasePanelView, } from './basePanelView';
import { GridviewPanelApiImpl, } from '../api/gridviewPanelApi';
import { Emitter } from '../events';
export class GridviewPanel extends BasePanelView {
    get priority() {
        return this._priority;
    }
    get snap() {
        return this._snap;
    }
    get minimumWidth() {
        /**
         * defer to protected function to allow subclasses to override easily.
         * see https://github.com/microsoft/TypeScript/issues/338
         */
        return this.__minimumWidth();
    }
    get minimumHeight() {
        /**
         * defer to protected function to allow subclasses to override easily.
         * see https://github.com/microsoft/TypeScript/issues/338
         */
        return this.__minimumHeight();
    }
    get maximumHeight() {
        /**
         * defer to protected function to allow subclasses to override easily.
         * see https://github.com/microsoft/TypeScript/issues/338
         */
        return this.__maximumHeight();
    }
    get maximumWidth() {
        /**
         * defer to protected function to allow subclasses to override easily.
         * see https://github.com/microsoft/TypeScript/issues/338
         */
        return this.__maximumWidth();
    }
    __minimumWidth() {
        const width = typeof this._minimumWidth === 'function'
            ? this._minimumWidth()
            : this._minimumWidth;
        if (width !== this._evaluatedMinimumWidth) {
            this._evaluatedMinimumWidth = width;
            this.updateConstraints();
        }
        return width;
    }
    __maximumWidth() {
        const width = typeof this._maximumWidth === 'function'
            ? this._maximumWidth()
            : this._maximumWidth;
        if (width !== this._evaluatedMaximumWidth) {
            this._evaluatedMaximumWidth = width;
            this.updateConstraints();
        }
        return width;
    }
    __minimumHeight() {
        const height = typeof this._minimumHeight === 'function'
            ? this._minimumHeight()
            : this._minimumHeight;
        if (height !== this._evaluatedMinimumHeight) {
            this._evaluatedMinimumHeight = height;
            this.updateConstraints();
        }
        return height;
    }
    __maximumHeight() {
        const height = typeof this._maximumHeight === 'function'
            ? this._maximumHeight()
            : this._maximumHeight;
        if (height !== this._evaluatedMaximumHeight) {
            this._evaluatedMaximumHeight = height;
            this.updateConstraints();
        }
        return height;
    }
    get isActive() {
        return this.api.isActive;
    }
    get isVisible() {
        return this.api.isVisible;
    }
    constructor(id, component, options, api) {
        super(id, component, api !== null && api !== void 0 ? api : new GridviewPanelApiImpl(id, component));
        this._evaluatedMinimumWidth = 0;
        this._evaluatedMaximumWidth = Number.MAX_SAFE_INTEGER;
        this._evaluatedMinimumHeight = 0;
        this._evaluatedMaximumHeight = Number.MAX_SAFE_INTEGER;
        this._minimumWidth = 0;
        this._minimumHeight = 0;
        this._maximumWidth = Number.MAX_SAFE_INTEGER;
        this._maximumHeight = Number.MAX_SAFE_INTEGER;
        this._snap = false;
        this._onDidChange = new Emitter();
        this.onDidChange = this._onDidChange.event;
        if (typeof (options === null || options === void 0 ? void 0 : options.minimumWidth) === 'number') {
            this._minimumWidth = options.minimumWidth;
        }
        if (typeof (options === null || options === void 0 ? void 0 : options.maximumWidth) === 'number') {
            this._maximumWidth = options.maximumWidth;
        }
        if (typeof (options === null || options === void 0 ? void 0 : options.minimumHeight) === 'number') {
            this._minimumHeight = options.minimumHeight;
        }
        if (typeof (options === null || options === void 0 ? void 0 : options.maximumHeight) === 'number') {
            this._maximumHeight = options.maximumHeight;
        }
        this.api.initialize(this); // TODO: required to by-pass 'super before this' requirement
        this.addDisposables(this.api.onWillVisibilityChange((event) => {
            const { isVisible } = event;
            const { accessor } = this._params;
            accessor.setVisible(this, isVisible);
        }), this.api.onActiveChange(() => {
            const { accessor } = this._params;
            accessor.doSetGroupActive(this);
        }), this.api.onDidConstraintsChangeInternal((event) => {
            if (typeof event.minimumWidth === 'number' ||
                typeof event.minimumWidth === 'function') {
                this._minimumWidth = event.minimumWidth;
            }
            if (typeof event.minimumHeight === 'number' ||
                typeof event.minimumHeight === 'function') {
                this._minimumHeight = event.minimumHeight;
            }
            if (typeof event.maximumWidth === 'number' ||
                typeof event.maximumWidth === 'function') {
                this._maximumWidth = event.maximumWidth;
            }
            if (typeof event.maximumHeight === 'number' ||
                typeof event.maximumHeight === 'function') {
                this._maximumHeight = event.maximumHeight;
            }
        }), this.api.onDidSizeChange((event) => {
            this._onDidChange.fire({
                height: event.height,
                width: event.width,
            });
        }), this._onDidChange);
    }
    setVisible(isVisible) {
        this.api._onDidVisibilityChange.fire({ isVisible });
    }
    setActive(isActive) {
        this.api._onDidActiveChange.fire({ isActive });
    }
    init(parameters) {
        if (parameters.maximumHeight) {
            this._maximumHeight = parameters.maximumHeight;
        }
        if (parameters.minimumHeight) {
            this._minimumHeight = parameters.minimumHeight;
        }
        if (parameters.maximumWidth) {
            this._maximumWidth = parameters.maximumWidth;
        }
        if (parameters.minimumWidth) {
            this._minimumWidth = parameters.minimumWidth;
        }
        this._priority = parameters.priority;
        this._snap = !!parameters.snap;
        super.init(parameters);
        if (typeof parameters.isVisible === 'boolean') {
            this.setVisible(parameters.isVisible);
        }
    }
    updateConstraints() {
        this.api._onDidConstraintsChange.fire({
            minimumWidth: this._evaluatedMinimumWidth,
            maximumWidth: this._evaluatedMaximumWidth,
            minimumHeight: this._evaluatedMinimumHeight,
            maximumHeight: this._evaluatedMaximumHeight,
        });
    }
    toJSON() {
        const state = super.toJSON();
        const maximum = (value) => value === Number.MAX_SAFE_INTEGER ? undefined : value;
        const minimum = (value) => (value <= 0 ? undefined : value);
        return Object.assign(Object.assign({}, state), { minimumHeight: minimum(this.minimumHeight), maximumHeight: maximum(this.maximumHeight), minimumWidth: minimum(this.minimumWidth), maximumWidth: maximum(this.maximumWidth), snap: this.snap, priority: this.priority });
    }
}
