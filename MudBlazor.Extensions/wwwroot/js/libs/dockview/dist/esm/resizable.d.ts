import { CompositeDisposable } from './lifecycle';
export declare abstract class Resizable extends CompositeDisposable {
    private readonly _element;
    private _disableResizing;
    get element(): HTMLElement;
    get disableResizing(): boolean;
    set disableResizing(value: boolean);
    constructor(parentElement: HTMLElement, disableResizing?: boolean);
    abstract layout(width: number, height: number): void;
}
