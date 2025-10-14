import { addDisposableListener } from '../events';
import { CompositeDisposable, MutableDisposable } from '../lifecycle';
import { toggleClass } from '../dom';
import { createChevronRightButton, createExpandMoreButton } from '../svg';
export class DefaultHeader extends CompositeDisposable {
    get element() {
        return this._element;
    }
    constructor() {
        super();
        this._expandedIcon = createExpandMoreButton();
        this._collapsedIcon = createChevronRightButton();
        this.disposable = new MutableDisposable();
        this.apiRef = {
            api: null,
        };
        this._element = document.createElement('div');
        this.element.className = 'dv-default-header';
        this._content = document.createElement('span');
        this._expander = document.createElement('div');
        this._expander.className = 'dv-pane-header-icon';
        this.element.appendChild(this._expander);
        this.element.appendChild(this._content);
        this.addDisposables(addDisposableListener(this._element, 'click', () => {
            var _a;
            (_a = this.apiRef.api) === null || _a === void 0 ? void 0 : _a.setExpanded(!this.apiRef.api.isExpanded);
        }));
    }
    init(params) {
        this.apiRef.api = params.api;
        this._content.textContent = params.title;
        this.updateIcon();
        this.disposable.value = params.api.onDidExpansionChange(() => {
            this.updateIcon();
        });
    }
    updateIcon() {
        var _a;
        const isExpanded = !!((_a = this.apiRef.api) === null || _a === void 0 ? void 0 : _a.isExpanded);
        toggleClass(this._expander, 'collapsed', !isExpanded);
        if (isExpanded) {
            if (this._expander.contains(this._collapsedIcon)) {
                this._collapsedIcon.remove();
            }
            if (!this._expander.contains(this._expandedIcon)) {
                this._expander.appendChild(this._expandedIcon);
            }
        }
        else {
            if (this._expander.contains(this._expandedIcon)) {
                this._expandedIcon.remove();
            }
            if (!this._expander.contains(this._collapsedIcon)) {
                this._expander.appendChild(this._collapsedIcon);
            }
        }
    }
    update(_params) {
        //
    }
    dispose() {
        this.disposable.dispose();
        super.dispose();
    }
}
