import { Event } from '../events';
import { IDisposable } from '../lifecycle';
export declare enum Orientation {
    HORIZONTAL = "HORIZONTAL",
    VERTICAL = "VERTICAL"
}
export declare enum SashState {
    MAXIMUM = 0,
    MINIMUM = 1,
    DISABLED = 2,
    ENABLED = 3
}
export interface ISplitviewStyles {
    separatorBorder: string;
}
export interface SplitViewOptions {
    orientation?: Orientation;
    descriptor?: ISplitViewDescriptor;
    proportionalLayout?: boolean;
    styles?: ISplitviewStyles;
    margin?: number;
}
export declare enum LayoutPriority {
    Low = "low",// view is offered space last
    High = "high",// view is offered space first
    Normal = "normal"
}
export interface IBaseView extends IDisposable {
    minimumSize: number;
    maximumSize: number;
    snap?: boolean;
    priority?: LayoutPriority;
}
export interface IView extends IBaseView {
    readonly element: HTMLElement | DocumentFragment;
    readonly onDidChange: Event<{
        size?: number;
        orthogonalSize?: number;
    }>;
    layout(size: number, orthogonalSize: number): void;
    setVisible(visible: boolean): void;
}
export type DistributeSizing = {
    type: 'distribute';
};
export type SplitSizing = {
    type: 'split';
    index: number;
};
export type InvisibleSizing = {
    type: 'invisible';
    cachedVisibleSize: number;
};
export type Sizing = DistributeSizing | SplitSizing | InvisibleSizing;
export declare namespace Sizing {
    const Distribute: DistributeSizing;
    function Split(index: number): SplitSizing;
    function Invisible(cachedVisibleSize: number): InvisibleSizing;
}
export interface ISplitViewDescriptor {
    size: number;
    views: {
        visible?: boolean;
        size: number;
        view: IView;
    }[];
}
export declare class Splitview {
    private readonly container;
    private readonly element;
    private readonly viewContainer;
    private readonly sashContainer;
    private readonly viewItems;
    private readonly sashes;
    private _orientation;
    private _size;
    private _orthogonalSize;
    private _contentSize;
    private _proportions;
    private readonly proportionalLayout;
    private _startSnappingEnabled;
    private _endSnappingEnabled;
    private _disabled;
    private _margin;
    private readonly _onDidSashEnd;
    readonly onDidSashEnd: Event<void>;
    private readonly _onDidAddView;
    readonly onDidAddView: Event<IView>;
    private readonly _onDidRemoveView;
    readonly onDidRemoveView: Event<IView>;
    get contentSize(): number;
    get size(): number;
    set size(value: number);
    get orthogonalSize(): number;
    set orthogonalSize(value: number);
    get length(): number;
    get proportions(): (number | undefined)[] | undefined;
    get orientation(): Orientation;
    set orientation(value: Orientation);
    get minimumSize(): number;
    get maximumSize(): number;
    get startSnappingEnabled(): boolean;
    set startSnappingEnabled(startSnappingEnabled: boolean);
    get endSnappingEnabled(): boolean;
    set endSnappingEnabled(endSnappingEnabled: boolean);
    get disabled(): boolean;
    set disabled(value: boolean);
    get margin(): number;
    set margin(value: number);
    constructor(container: HTMLElement, options: SplitViewOptions);
    style(styles?: ISplitviewStyles): void;
    isViewVisible(index: number): boolean;
    setViewVisible(index: number, visible: boolean): void;
    getViewSize(index: number): number;
    resizeView(index: number, size: number): void;
    getViews<T extends IView>(): T[];
    private onDidChange;
    addView(view: IView, size?: number | Sizing, index?: number, skipLayout?: boolean): void;
    distributeViewSizes(): void;
    removeView(index: number, sizing?: Sizing, skipLayout?: boolean): IView;
    getViewCachedVisibleSize(index: number): number | undefined;
    moveView(from: number, to: number): void;
    layout(size: number, orthogonalSize: number): void;
    private relayout;
    private distributeEmptySpace;
    private saveProportions;
    /**
     * Margin explain:
     *
     * For `n` views in a splitview there will be `n-1` margins `m`.
     *
     * To fit the margins each view must reduce in size by `(m * (n - 1)) / n`.
     *
     * For each view `i` the offet must be adjusted by `m * i/(n - 1)`.
     */
    private layoutViews;
    private findFirstSnapIndex;
    private updateSashEnablement;
    private updateSash;
    private resize;
    private createViewContainer;
    private createSashContainer;
    private createContainer;
    dispose(): void;
}
