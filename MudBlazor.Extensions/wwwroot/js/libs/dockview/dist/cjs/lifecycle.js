"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.MutableDisposable = exports.CompositeDisposable = exports.Disposable = void 0;
var Disposable;
(function (Disposable) {
    Disposable.NONE = {
        dispose: function () {
            // noop
        },
    };
    function from(func) {
        return {
            dispose: function () {
                func();
            },
        };
    }
    Disposable.from = from;
})(Disposable || (exports.Disposable = Disposable = {}));
var CompositeDisposable = /** @class */ (function () {
    function CompositeDisposable() {
        var args = [];
        for (var _i = 0; _i < arguments.length; _i++) {
            args[_i] = arguments[_i];
        }
        this._isDisposed = false;
        this._disposables = args;
    }
    Object.defineProperty(CompositeDisposable.prototype, "isDisposed", {
        get: function () {
            return this._isDisposed;
        },
        enumerable: false,
        configurable: true
    });
    CompositeDisposable.prototype.addDisposables = function () {
        var _this = this;
        var args = [];
        for (var _i = 0; _i < arguments.length; _i++) {
            args[_i] = arguments[_i];
        }
        args.forEach(function (arg) { return _this._disposables.push(arg); });
    };
    CompositeDisposable.prototype.dispose = function () {
        if (this._isDisposed) {
            return;
        }
        this._isDisposed = true;
        this._disposables.forEach(function (arg) { return arg.dispose(); });
        this._disposables = [];
    };
    return CompositeDisposable;
}());
exports.CompositeDisposable = CompositeDisposable;
var MutableDisposable = /** @class */ (function () {
    function MutableDisposable() {
        this._disposable = Disposable.NONE;
    }
    Object.defineProperty(MutableDisposable.prototype, "value", {
        set: function (disposable) {
            if (this._disposable) {
                this._disposable.dispose();
            }
            this._disposable = disposable;
        },
        enumerable: false,
        configurable: true
    });
    MutableDisposable.prototype.dispose = function () {
        if (this._disposable) {
            this._disposable.dispose();
            this._disposable = Disposable.NONE;
        }
    };
    return MutableDisposable;
}());
exports.MutableDisposable = MutableDisposable;
