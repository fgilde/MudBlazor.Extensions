import { shiftAbsoluteElementIntoView } from '../../dom';
import { addDisposableListener } from '../../events';
import { CompositeDisposable, Disposable, MutableDisposable, } from '../../lifecycle';
export class PopupService extends CompositeDisposable {
    constructor(root) {
        super();
        this.root = root;
        this._active = null;
        this._activeDisposable = new MutableDisposable();
        this._element = document.createElement('div');
        this._element.className = 'dv-popover-anchor';
        this._element.style.position = 'relative';
        this.root.prepend(this._element);
        this.addDisposables(Disposable.from(() => {
            this.close();
        }), this._activeDisposable);
    }
    openPopover(element, position) {
        var _a;
        this.close();
        const wrapper = document.createElement('div');
        wrapper.style.position = 'absolute';
        wrapper.style.zIndex = (_a = position.zIndex) !== null && _a !== void 0 ? _a : 'var(--dv-overlay-z-index)';
        wrapper.appendChild(element);
        const anchorBox = this._element.getBoundingClientRect();
        const offsetX = anchorBox.left;
        const offsetY = anchorBox.top;
        wrapper.style.top = `${position.y - offsetY}px`;
        wrapper.style.left = `${position.x - offsetX}px`;
        this._element.appendChild(wrapper);
        this._active = wrapper;
        this._activeDisposable.value = new CompositeDisposable(addDisposableListener(window, 'pointerdown', (event) => {
            var _a;
            const target = event.target;
            if (!(target instanceof HTMLElement)) {
                return;
            }
            let el = target;
            while (el && el !== wrapper) {
                el = (_a = el === null || el === void 0 ? void 0 : el.parentElement) !== null && _a !== void 0 ? _a : null;
            }
            if (el) {
                return; // clicked within popover
            }
            this.close();
        }));
        requestAnimationFrame(() => {
            shiftAbsoluteElementIntoView(wrapper, this.root);
        });
    }
    close() {
        if (this._active) {
            this._active.remove();
            this._activeDisposable.dispose();
            this._active = null;
        }
    }
}
