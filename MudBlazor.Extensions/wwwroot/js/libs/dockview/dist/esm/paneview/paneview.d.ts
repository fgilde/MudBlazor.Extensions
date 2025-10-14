import { Orientation, ISplitViewDescriptor, Sizing } from '../splitview/splitview';
import { CompositeDisposable, IDisposable } from '../lifecycle';
import { Event } from '../events';
import { PaneviewPanel } from './paneviewPanel';
interface PaneItem {
    pane: PaneviewPanel;
    disposable: IDisposable;
}
export declare class Paneview extends CompositeDisposable implements IDisposable {
    private readonly element;
    private readonly splitview;
    private paneItems;
    private readonly _orientation;
    private animationTimer;
    private skipAnimation;
    private readonly _onDidChange;
    readonly onDidChange: Event<void>;
    get onDidAddView(): Event<PaneviewPanel>;
    get onDidRemoveView(): Event<PaneviewPanel>;
    get minimumSize(): number;
    get maximumSize(): number;
    get orientation(): Orientation;
    get size(): number;
    get orthogonalSize(): number;
    constructor(container: HTMLElement, options: {
        orientation: Orientation;
        descriptor?: ISplitViewDescriptor;
    });
    setViewVisible(index: number, visible: boolean): void;
    addPane(pane: PaneviewPanel, size?: number | Sizing, index?: number, skipLayout?: boolean): void;
    getViewSize(index: number): number;
    getPanes(): PaneviewPanel[];
    removePane(index: number, options?: {
        skipDispose: boolean;
    }): PaneItem;
    moveView(from: number, to: number): void;
    layout(size: number, orthogonalSize: number): void;
    private setupAnimation;
    dispose(): void;
}
export {};
