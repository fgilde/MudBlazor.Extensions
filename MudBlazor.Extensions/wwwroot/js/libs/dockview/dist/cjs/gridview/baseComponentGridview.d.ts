import { Event, AsapEvent } from '../events';
import { Gridview, IGridView } from './gridview';
import { Position } from '../dnd/droptarget';
import { IDisposable, IValueDisposable } from '../lifecycle';
import { ISplitviewStyles, Orientation } from '../splitview/splitview';
import { IPanel } from '../panel/types';
import { MovementOptions2 } from '../dockview/options';
import { Resizable } from '../resizable';
/**
 * A direction in which a panel can be moved or placed relative to another panel.
 */
export type Direction = 'left' | 'right' | 'above' | 'below' | 'within';
export declare function toTarget(direction: Direction): Position;
export interface MaximizedChanged<T extends IGridPanelView> {
    panel: T;
    isMaximized: boolean;
}
export interface BaseGridOptions {
    readonly proportionalLayout: boolean;
    readonly orientation: Orientation;
    readonly styles?: ISplitviewStyles;
    readonly disableAutoResizing?: boolean;
    readonly locked?: boolean;
    readonly margin?: number;
    readonly className?: string;
}
export interface IGridPanelView extends IGridView, IPanel {
    setActive(isActive: boolean): void;
    readonly isActive: boolean;
}
export interface IBaseGrid<T extends IGridPanelView> extends IDisposable {
    readonly element: HTMLElement;
    readonly id: string;
    readonly width: number;
    readonly height: number;
    readonly minimumHeight: number;
    readonly maximumHeight: number;
    readonly minimumWidth: number;
    readonly maximumWidth: number;
    readonly activeGroup: T | undefined;
    readonly size: number;
    readonly groups: T[];
    readonly onDidMaximizedChange: Event<MaximizedChanged<T>>;
    readonly onDidLayoutChange: Event<void>;
    getPanel(id: string): T | undefined;
    toJSON(): object;
    fromJSON(data: any): void;
    clear(): void;
    layout(width: number, height: number, force?: boolean): void;
    setVisible(panel: T, visible: boolean): void;
    isVisible(panel: T): boolean;
    maximizeGroup(panel: T): void;
    isMaximizedGroup(panel: T): boolean;
    exitMaximizedGroup(): void;
    hasMaximizedGroup(): boolean;
}
export declare abstract class BaseGrid<T extends IGridPanelView> extends Resizable implements IBaseGrid<T> {
    private readonly _id;
    protected readonly _groups: Map<string, IValueDisposable<T>>;
    protected readonly gridview: Gridview;
    protected _activeGroup: T | undefined;
    private readonly _onDidRemove;
    readonly onDidRemove: Event<T>;
    private readonly _onDidAdd;
    readonly onDidAdd: Event<T>;
    private readonly _onDidMaximizedChange;
    readonly onDidMaximizedChange: Event<MaximizedChanged<T>>;
    private readonly _onDidActiveChange;
    readonly onDidActiveChange: Event<T | undefined>;
    protected readonly _bufferOnDidLayoutChange: AsapEvent;
    readonly onDidLayoutChange: Event<void>;
    private readonly _onDidViewVisibilityChangeMicroTaskQueue;
    readonly onDidViewVisibilityChangeMicroTaskQueue: Event<void>;
    private readonly _classNames;
    get id(): string;
    get size(): number;
    get groups(): T[];
    get width(): number;
    get height(): number;
    get minimumHeight(): number;
    get maximumHeight(): number;
    get minimumWidth(): number;
    get maximumWidth(): number;
    get activeGroup(): T | undefined;
    get locked(): boolean;
    set locked(value: boolean);
    constructor(container: HTMLElement, options: BaseGridOptions);
    abstract toJSON(): object;
    abstract fromJSON(data: any): void;
    abstract clear(): void;
    setVisible(panel: T, visible: boolean): void;
    isVisible(panel: T): boolean;
    updateOptions(options: Partial<BaseGridOptions>): void;
    maximizeGroup(panel: T): void;
    isMaximizedGroup(panel: T): boolean;
    exitMaximizedGroup(): void;
    hasMaximizedGroup(): boolean;
    protected doAddGroup(group: T, location?: number[], size?: number): void;
    protected doRemoveGroup(group: T, options?: {
        skipActive?: boolean;
        skipDispose?: boolean;
    }): T;
    getPanel(id: string): T | undefined;
    doSetGroupActive(group: T | undefined): void;
    removeGroup(group: T): void;
    moveToNext(options?: MovementOptions2): void;
    moveToPrevious(options?: MovementOptions2): void;
    layout(width: number, height: number, forceResize?: boolean): void;
    dispose(): void;
}
