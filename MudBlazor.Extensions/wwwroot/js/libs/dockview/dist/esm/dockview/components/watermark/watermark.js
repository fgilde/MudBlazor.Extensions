import { CompositeDisposable } from '../../../lifecycle';
export class Watermark extends CompositeDisposable {
    get element() {
        return this._element;
    }
    constructor() {
        super();
        this._element = document.createElement('div');
        this._element.className = 'dv-watermark';
    }
    init(_params) {
        // noop
    }
}
