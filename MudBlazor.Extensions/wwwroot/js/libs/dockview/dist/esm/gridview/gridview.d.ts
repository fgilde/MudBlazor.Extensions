import { ISplitviewStyles, LayoutPriority, Orientation, Sizing } from '../splitview/splitview';
import { LeafNode } from './leafNode';
import { Node } from './types';
import { Event } from '../events';
import { IDisposable } from '../lifecycle';
import { Position } from '../dnd/droptarget';
export declare function indexInParent(element: HTMLElement): number;
/**
 * Find the grid location of a specific DOM element by traversing the parent
 * chain and finding each child index on the way.
 *
 * This will break as soon as DOM structures of the Splitview or Gridview change.
 */
export declare function getGridLocation(element: HTMLElement): number[];
export declare function getRelativeLocation(rootOrientation: Orientation, location: number[], direction: Position): number[];
export declare function getDirectionOrientation(direction: Position): Orientation;
export declare function getLocationOrientation(rootOrientation: Orientation, location: number[]): Orientation;
export interface IViewSize {
    width?: number;
    height?: number;
}
export interface IGridView {
    readonly onDidChange: Event<IViewSize | undefined>;
    readonly element: HTMLElement;
    readonly minimumWidth: number;
    readonly maximumWidth: number;
    readonly minimumHeight: number;
    readonly maximumHeight: number;
    readonly isVisible: boolean;
    priority?: LayoutPriority;
    layout(width: number, height: number): void;
    toJSON(): object;
    fromJSON?(json: object): void;
    snap?: boolean;
    setVisible?(visible: boolean): void;
}
export declare const orthogonal: (orientation: Orientation) => Orientation;
export interface GridLeafNode<T extends IGridView> {
    readonly view: T;
    readonly cachedVisibleSize: number | undefined;
    readonly box: {
        width: number;
        height: number;
    };
}
export interface GridBranchNode<T extends IGridView> {
    readonly children: GridNode<T>[];
    readonly box: {
        width: number;
        height: number;
    };
}
export type GridNode<T extends IGridView> = GridLeafNode<T> | GridBranchNode<T>;
export declare function isGridBranchNode<T extends IGridView>(node: GridNode<T>): node is GridBranchNode<T>;
export interface SerializedGridObject<T> {
    type: 'leaf' | 'branch';
    data: T | SerializedGridObject<T>[];
    size?: number;
    visible?: boolean;
}
export interface ISerializedLeafNode<T = any> {
    type: 'leaf';
    data: T;
    size: number;
    visible?: boolean;
}
export interface ISerializedBranchNode {
    type: 'branch';
    data: ISerializedNode[];
    size: number;
}
export type ISerializedNode = ISerializedLeafNode | ISerializedBranchNode;
export interface INodeDescriptor {
    node: Node;
    visible?: boolean;
}
export interface IViewDeserializer {
    fromJSON: (data: ISerializedLeafNode) => IGridView;
}
export interface SerializedNodeDescriptor {
    location: number[];
}
export interface SerializedGridview<T> {
    root: SerializedGridObject<T>;
    width: number;
    height: number;
    orientation: Orientation;
    maximizedNode?: SerializedNodeDescriptor;
}
export interface MaximizedViewChanged {
    view: IGridView;
    isMaximized: boolean;
}
export declare class Gridview implements IDisposable {
    readonly proportionalLayout: boolean;
    readonly styles: ISplitviewStyles | undefined;
    readonly element: HTMLElement;
    private _root;
    private _locked;
    private _margin;
    private _maximizedNode;
    private readonly disposable;
    private readonly _onDidChange;
    readonly onDidChange: Event<{
        size?: number;
        orthogonalSize?: number;
    }>;
    private readonly _onDidViewVisibilityChange;
    readonly onDidViewVisibilityChange: Event<void>;
    private readonly _onDidMaximizedNodeChange;
    readonly onDidMaximizedNodeChange: Event<MaximizedViewChanged>;
    get length(): number;
    get orientation(): Orientation;
    set orientation(orientation: Orientation);
    get width(): number;
    get height(): number;
    get minimumWidth(): number;
    get minimumHeight(): number;
    get maximumWidth(): number;
    get maximumHeight(): number;
    get locked(): boolean;
    set locked(value: boolean);
    get margin(): number;
    set margin(value: number);
    maximizedView(): IGridView | undefined;
    hasMaximizedView(): boolean;
    maximizeView(view: IGridView): void;
    exitMaximizedView(): void;
    serialize(): SerializedGridview<any>;
    dispose(): void;
    clear(): void;
    deserialize<T>(json: SerializedGridview<T>, deserializer: IViewDeserializer): void;
    private _deserialize;
    private _deserializeNode;
    private get root();
    private set root(value);
    normalize(): void;
    /**
     * If the root is orientated as a VERTICAL node then nest the existing root within a new HORIZIONTAL root node
     * If the root is orientated as a HORIZONTAL node then nest the existing root within a new VERITCAL root node
     */
    insertOrthogonalSplitviewAtRoot(): void;
    next(location: number[]): LeafNode;
    previous(location: number[]): LeafNode;
    getView(): GridBranchNode<IGridView>;
    getView(location?: number[]): GridNode<IGridView>;
    private _getViews;
    private progmaticSelect;
    constructor(proportionalLayout: boolean, styles: ISplitviewStyles | undefined, orientation: Orientation, locked?: boolean, margin?: number);
    isViewVisible(location: number[]): boolean;
    setViewVisible(location: number[], visible: boolean): void;
    moveView(parentLocation: number[], from: number, to: number): void;
    addView(view: IGridView, size: number | Sizing, location: number[]): void;
    remove(view: IGridView, sizing?: Sizing): IGridView;
    removeView(location: number[], sizing?: Sizing): IGridView;
    layout(width: number, height: number): void;
    private getNode;
}
