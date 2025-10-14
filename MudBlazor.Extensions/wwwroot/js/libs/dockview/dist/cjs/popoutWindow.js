"use strict";
var __extends = (this && this.__extends) || (function () {
    var extendStatics = function (d, b) {
        extendStatics = Object.setPrototypeOf ||
            ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
            function (d, b) { for (var p in b) if (Object.prototype.hasOwnProperty.call(b, p)) d[p] = b[p]; };
        return extendStatics(d, b);
    };
    return function (d, b) {
        if (typeof b !== "function" && b !== null)
            throw new TypeError("Class extends value " + String(b) + " is not a constructor or null");
        extendStatics(d, b);
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };
})();
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
var __generator = (this && this.__generator) || function (thisArg, body) {
    var _ = { label: 0, sent: function() { if (t[0] & 1) throw t[1]; return t[1]; }, trys: [], ops: [] }, f, y, t, g;
    return g = { next: verb(0), "throw": verb(1), "return": verb(2) }, typeof Symbol === "function" && (g[Symbol.iterator] = function() { return this; }), g;
    function verb(n) { return function (v) { return step([n, v]); }; }
    function step(op) {
        if (f) throw new TypeError("Generator is already executing.");
        while (g && (g = 0, op[0] && (_ = 0)), _) try {
            if (f = 1, y && (t = op[0] & 2 ? y["return"] : op[0] ? y["throw"] || ((t = y["return"]) && t.call(y), 0) : y.next) && !(t = t.call(y, op[1])).done) return t;
            if (y = 0, t) op = [op[0] & 2, t.value];
            switch (op[0]) {
                case 0: case 1: t = op; break;
                case 4: _.label++; return { value: op[1], done: false };
                case 5: _.label++; y = op[1]; op = [0]; continue;
                case 7: op = _.ops.pop(); _.trys.pop(); continue;
                default:
                    if (!(t = _.trys, t = t.length > 0 && t[t.length - 1]) && (op[0] === 6 || op[0] === 2)) { _ = 0; continue; }
                    if (op[0] === 3 && (!t || (op[1] > t[0] && op[1] < t[3]))) { _.label = op[1]; break; }
                    if (op[0] === 6 && _.label < t[1]) { _.label = t[1]; t = op; break; }
                    if (t && _.label < t[2]) { _.label = t[2]; _.ops.push(op); break; }
                    if (t[2]) _.ops.pop();
                    _.trys.pop(); continue;
            }
            op = body.call(thisArg, _);
        } catch (e) { op = [6, e]; y = 0; } finally { f = t = 0; }
        if (op[0] & 5) throw op[1]; return { value: op[0] ? op[1] : void 0, done: true };
    }
};
var __read = (this && this.__read) || function (o, n) {
    var m = typeof Symbol === "function" && o[Symbol.iterator];
    if (!m) return o;
    var i = m.call(o), r, ar = [], e;
    try {
        while ((n === void 0 || n-- > 0) && !(r = i.next()).done) ar.push(r.value);
    }
    catch (error) { e = { error: error }; }
    finally {
        try {
            if (r && !r.done && (m = i["return"])) m.call(i);
        }
        finally { if (e) throw e.error; }
    }
    return ar;
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.PopoutWindow = void 0;
var dom_1 = require("./dom");
var events_1 = require("./events");
var lifecycle_1 = require("./lifecycle");
var PopoutWindow = /** @class */ (function (_super) {
    __extends(PopoutWindow, _super);
    function PopoutWindow(target, className, options) {
        var _this = _super.call(this) || this;
        _this.target = target;
        _this.className = className;
        _this.options = options;
        _this._onWillClose = new events_1.Emitter();
        _this.onWillClose = _this._onWillClose.event;
        _this._onDidClose = new events_1.Emitter();
        _this.onDidClose = _this._onDidClose.event;
        _this._window = null;
        _this.addDisposables(_this._onWillClose, _this._onDidClose, {
            dispose: function () {
                _this.close();
            },
        });
        return _this;
    }
    Object.defineProperty(PopoutWindow.prototype, "window", {
        get: function () {
            var _a, _b;
            return (_b = (_a = this._window) === null || _a === void 0 ? void 0 : _a.value) !== null && _b !== void 0 ? _b : null;
        },
        enumerable: false,
        configurable: true
    });
    PopoutWindow.prototype.dimensions = function () {
        if (!this._window) {
            return null;
        }
        var left = this._window.value.screenX;
        var top = this._window.value.screenY;
        var width = this._window.value.innerWidth;
        var height = this._window.value.innerHeight;
        return { top: top, left: left, width: width, height: height };
    };
    PopoutWindow.prototype.close = function () {
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
    };
    PopoutWindow.prototype.open = function () {
        var _a, _b;
        return __awaiter(this, void 0, void 0, function () {
            var url, features, externalWindow, disposable, container;
            var _this = this;
            return __generator(this, function (_c) {
                if (this._window) {
                    throw new Error('instance of popout window is already open');
                }
                url = "".concat(this.options.url);
                features = Object.entries({
                    top: this.options.top,
                    left: this.options.left,
                    width: this.options.width,
                    height: this.options.height,
                })
                    .map(function (_a) {
                    var _b = __read(_a, 2), key = _b[0], value = _b[1];
                    return "".concat(key, "=").concat(value);
                })
                    .join(',');
                externalWindow = window.open(url, this.target, features);
                if (!externalWindow) {
                    /**
                     * Popup blocked
                     */
                    return [2 /*return*/, null];
                }
                disposable = new lifecycle_1.CompositeDisposable();
                this._window = { value: externalWindow, disposable: disposable };
                disposable.addDisposables(lifecycle_1.Disposable.from(function () {
                    externalWindow.close();
                }), (0, events_1.addDisposableListener)(window, 'beforeunload', function () {
                    /**
                     * before the main window closes we should close this popup too
                     * to be good citizens
                     *
                     * @see https://developer.mozilla.org/en-US/docs/Web/API/Window/beforeunload_event
                     */
                    _this.close();
                }));
                container = this.createPopoutWindowContainer();
                if (this.className) {
                    container.classList.add(this.className);
                }
                (_b = (_a = this.options).onDidOpen) === null || _b === void 0 ? void 0 : _b.call(_a, {
                    id: this.target,
                    window: externalWindow,
                });
                return [2 /*return*/, new Promise(function (resolve, reject) {
                        externalWindow.addEventListener('unload', function (e) {
                            // if page fails to load before unloading
                            // this.close();
                        });
                        externalWindow.addEventListener('load', function () {
                            /**
                             * @see https://developer.mozilla.org/en-US/docs/Web/API/Window/load_event
                             */
                            try {
                                var externalDocument = externalWindow.document;
                                externalDocument.title = document.title;
                                externalDocument.body.appendChild(container);
                                (0, dom_1.addStyles)(externalDocument, window.document.styleSheets);
                                /**
                                 * beforeunload must be registered after load for reasons I could not determine
                                 * otherwise the beforeunload event will not fire when the window is closed
                                 */
                                (0, events_1.addDisposableListener)(externalWindow, 'beforeunload', function () {
                                    /**
                                     * @see https://developer.mozilla.org/en-US/docs/Web/API/Window/beforeunload_event
                                     */
                                    _this.close();
                                });
                                resolve(container);
                            }
                            catch (err) {
                                // only except this is the DOM isn't setup. e.g. in a in correctly configured test
                                reject(err);
                            }
                        });
                    })];
            });
        });
    };
    PopoutWindow.prototype.createPopoutWindowContainer = function () {
        var el = document.createElement('div');
        el.classList.add('dv-popout-window');
        el.id = 'dv-popout-window';
        el.style.position = 'absolute';
        el.style.width = '100%';
        el.style.height = '100%';
        el.style.top = '0px';
        el.style.left = '0px';
        return el;
    };
    return PopoutWindow;
}(lifecycle_1.CompositeDisposable));
exports.PopoutWindow = PopoutWindow;
