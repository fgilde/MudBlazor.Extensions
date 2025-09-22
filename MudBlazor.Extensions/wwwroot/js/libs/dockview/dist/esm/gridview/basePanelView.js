import { trackFocus } from '../dom';
import { CompositeDisposable } from '../lifecycle';
import { WillFocusEvent } from '../api/panelApi';
export class BasePanelView extends CompositeDisposable {
    get element() {
        return this._element;
    }
    get width() {
        return this._width;
    }
    get height() {
        return this._height;
    }
    get params() {
        var _a;
        return (_a = this._params) === null || _a === void 0 ? void 0 : _a.params;
    }
    constructor(id, component, api) {
        super();
        this.id = id;
        this.component = component;
        this.api = api;
        this._height = 0;
        this._width = 0;
        this._element = document.createElement('div');
        this._element.tabIndex = -1;
        this._element.style.outline = 'none';
        this._element.style.height = '100%';
        this._element.style.width = '100%';
        this._element.style.overflow = 'hidden';
        const focusTracker = trackFocus(this._element);
        this.addDisposables(this.api, focusTracker.onDidFocus(() => {
            this.api._onDidChangeFocus.fire({ isFocused: true });
        }), focusTracker.onDidBlur(() => {
            this.api._onDidChangeFocus.fire({ isFocused: false });
        }), focusTracker);
    }
    focus() {
        const event = new WillFocusEvent();
        this.api._onWillFocus.fire(event);
        if (event.defaultPrevented) {
            return;
        }
        this._element.focus();
    }
    layout(width, height) {
        this._width = width;
        this._height = height;
        this.api._onDidDimensionChange.fire({ width, height });
        if (this.part) {
            if (this._params) {
                this.part.update(this._params.params);
            }
        }
    }
    init(parameters) {
        this._params = parameters;
        this.part = this.getComponent();
    }
    update(event) {
        var _a, _b;
        // merge the new parameters with the existing parameters
        this._params = Object.assign(Object.assign({}, this._params), { params: Object.assign(Object.assign({}, (_a = this._params) === null || _a === void 0 ? void 0 : _a.params), event.params) });
        /**
         * delete new keys that have a value of undefined,
         * allow values of null
         */
        for (const key of Object.keys(event.params)) {
            if (event.params[key] === undefined) {
                delete this._params.params[key];
            }
        }
        // update the view with the updated props
        (_b = this.part) === null || _b === void 0 ? void 0 : _b.update({ params: this._params.params });
    }
    toJSON() {
        var _a, _b;
        const params = (_b = (_a = this._params) === null || _a === void 0 ? void 0 : _a.params) !== null && _b !== void 0 ? _b : {};
        return {
            id: this.id,
            component: this.component,
            params: Object.keys(params).length > 0 ? params : undefined,
        };
    }
    dispose() {
        var _a;
        this.api.dispose();
        (_a = this.part) === null || _a === void 0 ? void 0 : _a.dispose();
        super.dispose();
    }
}
