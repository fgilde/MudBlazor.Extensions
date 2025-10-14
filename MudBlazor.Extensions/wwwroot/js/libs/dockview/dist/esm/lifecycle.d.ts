export interface IDisposable {
    dispose(): void;
}
export interface IValueDisposable<T> {
    readonly value: T;
    readonly disposable: IDisposable;
}
export declare namespace Disposable {
    const NONE: IDisposable;
    function from(func: () => void): IDisposable;
}
export declare class CompositeDisposable {
    private _disposables;
    private _isDisposed;
    get isDisposed(): boolean;
    constructor(...args: IDisposable[]);
    addDisposables(...args: IDisposable[]): void;
    dispose(): void;
}
export declare class MutableDisposable implements IDisposable {
    private _disposable;
    set value(disposable: IDisposable);
    dispose(): void;
}
