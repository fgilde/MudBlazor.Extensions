import { CompositeDisposable } from '../../lifecycle';
export declare class PopupService extends CompositeDisposable {
    private readonly root;
    private readonly _element;
    private _active;
    private readonly _activeDisposable;
    constructor(root: HTMLElement);
    openPopover(element: HTMLElement, position: {
        x: number;
        y: number;
        zIndex?: string;
    }): void;
    close(): void;
}
