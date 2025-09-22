import { CompositeDisposable, Disposable } from '../lifecycle';
export class DropTargetAnchorContainer extends CompositeDisposable {
    get disabled() {
        return this._disabled;
    }
    set disabled(value) {
        var _a;
        if (this.disabled === value) {
            return;
        }
        this._disabled = value;
        if (value) {
            (_a = this.model) === null || _a === void 0 ? void 0 : _a.clear();
        }
    }
    get model() {
        if (this.disabled) {
            return undefined;
        }
        return {
            clear: () => {
                var _a;
                if (this._model) {
                    (_a = this._model.root.parentElement) === null || _a === void 0 ? void 0 : _a.removeChild(this._model.root);
                }
                this._model = undefined;
            },
            exists: () => {
                return !!this._model;
            },
            getElements: (event, outline) => {
                const changed = this._outline !== outline;
                this._outline = outline;
                if (this._model) {
                    this._model.changed = changed;
                    return this._model;
                }
                const container = this.createContainer();
                const anchor = this.createAnchor();
                this._model = { root: container, overlay: anchor, changed };
                container.appendChild(anchor);
                this.element.appendChild(container);
                if ((event === null || event === void 0 ? void 0 : event.target) instanceof HTMLElement) {
                    const targetBox = event.target.getBoundingClientRect();
                    const box = this.element.getBoundingClientRect();
                    anchor.style.left = `${targetBox.left - box.left}px`;
                    anchor.style.top = `${targetBox.top - box.top}px`;
                }
                return this._model;
            },
        };
    }
    constructor(element, options) {
        super();
        this.element = element;
        this._disabled = false;
        this._disabled = options.disabled;
        this.addDisposables(Disposable.from(() => {
            var _a;
            (_a = this.model) === null || _a === void 0 ? void 0 : _a.clear();
        }));
    }
    createContainer() {
        const el = document.createElement('div');
        el.className = 'dv-drop-target-container';
        return el;
    }
    createAnchor() {
        const el = document.createElement('div');
        el.className = 'dv-drop-target-anchor';
        el.style.visibility = 'hidden';
        return el;
    }
}
