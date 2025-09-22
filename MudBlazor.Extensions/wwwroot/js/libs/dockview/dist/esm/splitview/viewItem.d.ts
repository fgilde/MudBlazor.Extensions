import { IDisposable } from '../lifecycle';
import { IView, LayoutPriority } from './splitview';
export declare class ViewItem {
    container: HTMLElement;
    view: IView;
    private readonly disposable;
    private _size;
    private _cachedVisibleSize;
    set size(size: number);
    get size(): number;
    get cachedVisibleSize(): number | undefined;
    get visible(): boolean;
    get minimumSize(): number;
    get viewMinimumSize(): number;
    get maximumSize(): number;
    get viewMaximumSize(): number;
    get priority(): LayoutPriority | undefined;
    get snap(): boolean;
    set enabled(enabled: boolean);
    constructor(container: HTMLElement, view: IView, size: number | {
        cachedVisibleSize: number;
    }, disposable: IDisposable);
    setVisible(visible: boolean, size?: number): void;
    dispose(): IView;
}
