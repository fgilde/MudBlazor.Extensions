import { PanelInitParameters } from '../panel/types';
import { IGridPanelComponentView } from './gridviewComponent';
import { BasePanelView, BasePanelViewExported, BasePanelViewState } from './basePanelView';
import { GridviewPanelApi, GridviewPanelApiImpl } from '../api/gridviewPanelApi';
import { LayoutPriority } from '../splitview/splitview';
import { Event } from '../events';
import { IViewSize } from './gridview';
import { BaseGrid, IGridPanelView } from './baseComponentGridview';
export interface Contraints {
    minimumWidth?: number;
    maximumWidth?: number;
    minimumHeight?: number;
    maximumHeight?: number;
}
export interface GridviewInitParameters extends PanelInitParameters {
    minimumWidth?: number;
    maximumWidth?: number;
    minimumHeight?: number;
    maximumHeight?: number;
    priority?: LayoutPriority;
    snap?: boolean;
    accessor: BaseGrid<IGridPanelView>;
    isVisible?: boolean;
}
export interface IGridviewPanel<T extends GridviewPanelApi = GridviewPanelApi> extends BasePanelViewExported<T> {
    readonly minimumWidth: number;
    readonly maximumWidth: number;
    readonly minimumHeight: number;
    readonly maximumHeight: number;
    readonly priority: LayoutPriority | undefined;
    readonly snap: boolean;
}
export declare abstract class GridviewPanel<T extends GridviewPanelApiImpl = GridviewPanelApiImpl> extends BasePanelView<T> implements IGridPanelComponentView, IGridviewPanel {
    private _evaluatedMinimumWidth;
    private _evaluatedMaximumWidth;
    private _evaluatedMinimumHeight;
    private _evaluatedMaximumHeight;
    private _minimumWidth;
    private _minimumHeight;
    private _maximumWidth;
    private _maximumHeight;
    private _priority?;
    private _snap;
    private readonly _onDidChange;
    readonly onDidChange: Event<IViewSize | undefined>;
    get priority(): LayoutPriority | undefined;
    get snap(): boolean;
    get minimumWidth(): number;
    get minimumHeight(): number;
    get maximumHeight(): number;
    get maximumWidth(): number;
    protected __minimumWidth(): number;
    protected __maximumWidth(): number;
    protected __minimumHeight(): number;
    protected __maximumHeight(): number;
    get isActive(): boolean;
    get isVisible(): boolean;
    constructor(id: string, component: string, options?: {
        minimumWidth?: number;
        maximumWidth?: number;
        minimumHeight?: number;
        maximumHeight?: number;
    }, api?: T);
    setVisible(isVisible: boolean): void;
    setActive(isActive: boolean): void;
    init(parameters: GridviewInitParameters): void;
    private updateConstraints;
    toJSON(): GridPanelViewState;
}
export interface GridPanelViewState extends BasePanelViewState {
    minimumHeight?: number;
    maximumHeight?: number;
    minimumWidth?: number;
    maximumWidth?: number;
    snap?: boolean;
    priority?: LayoutPriority;
}
