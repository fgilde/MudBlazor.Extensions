export var Disposable;
(function (Disposable) {
    Disposable.NONE = {
        dispose: () => {
            // noop
        },
    };
    function from(func) {
        return {
            dispose: () => {
                func();
            },
        };
    }
    Disposable.from = from;
})(Disposable || (Disposable = {}));
export class CompositeDisposable {
    get isDisposed() {
        return this._isDisposed;
    }
    constructor(...args) {
        this._isDisposed = false;
        this._disposables = args;
    }
    addDisposables(...args) {
        args.forEach((arg) => this._disposables.push(arg));
    }
    dispose() {
        if (this._isDisposed) {
            return;
        }
        this._isDisposed = true;
        this._disposables.forEach((arg) => arg.dispose());
        this._disposables = [];
    }
}
export class MutableDisposable {
    constructor() {
        this._disposable = Disposable.NONE;
    }
    set value(disposable) {
        if (this._disposable) {
            this._disposable.dispose();
        }
        this._disposable = disposable;
    }
    dispose() {
        if (this._disposable) {
            this._disposable.dispose();
            this._disposable = Disposable.NONE;
        }
    }
}
