import { Droptarget } from '../dnd/droptarget';
import { CompositeDisposable } from '../lifecycle';
import { IDockviewPanel } from '../dockview/dockviewPanel';
import { DockviewComponent } from '../dockview/dockviewComponent';
export type DockviewPanelRenderer = 'onlyWhenVisible' | 'always';
export interface IRenderable {
    readonly element: HTMLElement;
    readonly dropTarget: Droptarget;
}
export declare class OverlayRenderContainer extends CompositeDisposable {
    readonly element: HTMLElement;
    readonly accessor: DockviewComponent;
    private readonly map;
    private _disposed;
    private readonly positionCache;
    private readonly pendingUpdates;
    constructor(element: HTMLElement, accessor: DockviewComponent);
    updateAllPositions(): void;
    detatch(panel: IDockviewPanel): boolean;
    attach(options: {
        panel: IDockviewPanel;
        referenceContainer: IRenderable;
    }): HTMLElement;
}
