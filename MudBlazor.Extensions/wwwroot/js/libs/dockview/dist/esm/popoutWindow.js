var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
import { addStyles } from './dom';
import { Emitter, addDisposableListener } from './events';
import { CompositeDisposable, Disposable } from './lifecycle';
export class PopoutWindow extends CompositeDisposable {
    get window() {
        var _a, _b;
        return (_b = (_a = this._window) === null || _a === void 0 ? void 0 : _a.value) !== null && _b !== void 0 ? _b : null;
    }
    constructor(target, className, options) {
        super();
        this.target = target;
        this.className = className;
        this.options = options;
        this._onWillClose = new Emitter();
        this.onWillClose = this._onWillClose.event;
        this._onDidClose = new Emitter();
        this.onDidClose = this._onDidClose.event;
        this._window = null;
        this.addDisposables(this._onWillClose, this._onDidClose, {
            dispose: () => {
                this.close();
            },
        });
    }
    dimensions() {
        if (!this._window) {
            return null;
        }
        const left = this._window.value.screenX;
        const top = this._window.value.screenY;
        const width = this._window.value.innerWidth;
        const height = this._window.value.innerHeight;
        return { top, left, width, height };
    }
    close() {
        var _a, _b;
        if (this._window) {
            this._onWillClose.fire();
            (_b = (_a = this.options).onWillClose) === null || _b === void 0 ? void 0 : _b.call(_a, {
                id: this.target,
                window: this._window.value,
            });
            this._window.disposable.dispose();
            this._window = null;
            this._onDidClose.fire();
        }
    }
    open() {
        var _a, _b;
        return __awaiter(this, void 0, void 0, function* () {
            if (this._window) {
                throw new Error('instance of popout window is already open');
            }
            const url = `${this.options.url}`;
            const features = Object.entries({
                top: this.options.top,
                left: this.options.left,
                width: this.options.width,
                height: this.options.height,
            })
                .map(([key, value]) => `${key}=${value}`)
                .join(',');
            /**
             * @see https://developer.mozilla.org/en-US/docs/Web/API/Window/open
             */
            const externalWindow = window.open(url, this.target, features);
            if (!externalWindow) {
                /**
                 * Popup blocked
                 */
                return null;
            }
            const disposable = new CompositeDisposable();
            this._window = { value: externalWindow, disposable };
            disposable.addDisposables(Disposable.from(() => {
                externalWindow.close();
            }), addDisposableListener(window, 'beforeunload', () => {
                /**
                 * before the main window closes we should close this popup too
                 * to be good citizens
                 *
                 * @see https://developer.mozilla.org/en-US/docs/Web/API/Window/beforeunload_event
                 */
                this.close();
            }));
            const container = this.createPopoutWindowContainer();
            if (this.className) {
                container.classList.add(this.className);
            }
            (_b = (_a = this.options).onDidOpen) === null || _b === void 0 ? void 0 : _b.call(_a, {
                id: this.target,
                window: externalWindow,
            });
            return new Promise((resolve, reject) => {
                externalWindow.addEventListener('unload', (e) => {
                    // if page fails to load before unloading
                    // this.close();
                });
                externalWindow.addEventListener('load', () => {
                    /**
                     * @see https://developer.mozilla.org/en-US/docs/Web/API/Window/load_event
                     */
                    try {
                        const externalDocument = externalWindow.document;
                        externalDocument.title = document.title;
                        externalDocument.body.appendChild(container);
                        addStyles(externalDocument, window.document.styleSheets);
                        /**
                         * beforeunload must be registered after load for reasons I could not determine
                         * otherwise the beforeunload event will not fire when the window is closed
                         */
                        addDisposableListener(externalWindow, 'beforeunload', () => {
                            /**
                             * @see https://developer.mozilla.org/en-US/docs/Web/API/Window/beforeunload_event
                             */
                            this.close();
                        });
                        resolve(container);
                    }
                    catch (err) {
                        // only except this is the DOM isn't setup. e.g. in a in correctly configured test
                        reject(err);
                    }
                });
            });
        });
    }
    createPopoutWindowContainer() {
        const el = document.createElement('div');
        el.classList.add('dv-popout-window');
        el.id = 'dv-popout-window';
        el.style.position = 'absolute';
        el.style.width = '100%';
        el.style.height = '100%';
        el.style.top = '0px';
        el.style.left = '0px';
        return el;
    }
}
