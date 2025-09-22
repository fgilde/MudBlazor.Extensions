import { CompositeDisposable } from '../lifecycle';
import { DropTargetTargetModel } from './droptarget';
export declare class DropTargetAnchorContainer extends CompositeDisposable {
    readonly element: HTMLElement;
    private _model;
    private _outline;
    private _disabled;
    get disabled(): boolean;
    set disabled(value: boolean);
    get model(): DropTargetTargetModel | undefined;
    constructor(element: HTMLElement, options: {
        disabled: boolean;
    });
    private createContainer;
    private createAnchor;
}
