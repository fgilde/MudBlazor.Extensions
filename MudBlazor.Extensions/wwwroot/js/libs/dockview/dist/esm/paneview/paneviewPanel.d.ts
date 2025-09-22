import { PaneviewApi } from '../api/component.api';
import { PaneviewPanelApiImpl } from '../api/paneviewPanelApi';
import { Event } from '../events';
import { BasePanelView, BasePanelViewExported, BasePanelViewState } from '../gridview/basePanelView';
import { IDisposable } from '../lifecycle';
import { IFrameworkPart, PanelInitParameters, PanelUpdateEvent } from '../panel/types';
import { IView, Orientation } from '../splitview/splitview';
import { PaneviewComponent } from './paneviewComponent';
export interface PanePanelViewState extends BasePanelViewState {
    headerComponent?: string;
    title: string;
}
export interface PanePanelInitParameter extends PanelInitParameters {
    minimumBodySize?: number;
    maximumBodySize?: number;
    isExpanded?: boolean;
    title: string;
    containerApi: PaneviewApi;
    accessor: PaneviewComponent;
}
export interface PanePanelComponentInitParameter extends PanePanelInitParameter {
    api: PaneviewPanelApiImpl;
}
export interface IPanePart extends IDisposable {
    readonly element: HTMLElement;
    update(params: PanelUpdateEvent): void;
    init(parameters: PanePanelComponentInitParameter): void;
}
export interface IPaneview extends IView {
    onDidChangeExpansionState: Event<boolean>;
}
export interface IPaneviewPanel extends BasePanelViewExported<PaneviewPanelApiImpl> {
    readonly minimumSize: number;
    readonly maximumSize: number;
    readonly minimumBodySize: number;
    readonly maximumBodySize: number;
    isExpanded(): boolean;
    setExpanded(isExpanded: boolean): void;
    headerVisible: boolean;
}
export declare abstract class PaneviewPanel extends BasePanelView<PaneviewPanelApiImpl> implements IPaneview, IPaneviewPanel {
    private readonly _onDidChangeExpansionState;
    onDidChangeExpansionState: Event<boolean>;
    private readonly _onDidChange;
    readonly onDidChange: Event<{
        size?: number;
        orthogonalSize?: number;
    }>;
    private _orthogonalSize;
    private _size;
    private _minimumBodySize;
    private _maximumBodySize;
    private _isExpanded;
    protected header?: HTMLElement;
    protected body?: HTMLElement;
    private bodyPart?;
    private headerPart?;
    private animationTimer;
    private _orientation;
    private _headerVisible;
    readonly headerSize: number;
    readonly headerComponent: string | undefined;
    set orientation(value: Orientation);
    get orientation(): Orientation;
    get minimumSize(): number;
    get maximumSize(): number;
    get size(): number;
    get orthogonalSize(): number;
    set orthogonalSize(size: number);
    get minimumBodySize(): number;
    set minimumBodySize(value: number);
    get maximumBodySize(): number;
    set maximumBodySize(value: number);
    get headerVisible(): boolean;
    set headerVisible(value: boolean);
    constructor(options: {
        id: string;
        component: string;
        headerComponent: string | undefined;
        orientation: Orientation;
        isExpanded: boolean;
        isHeaderVisible: boolean;
        headerSize: number;
        minimumBodySize: number;
        maximumBodySize: number;
    });
    setVisible(isVisible: boolean): void;
    setActive(isActive: boolean): void;
    isExpanded(): boolean;
    setExpanded(expanded: boolean): void;
    layout(size: number, orthogonalSize: number): void;
    init(parameters: PanePanelInitParameter): void;
    toJSON(): PanePanelViewState;
    private renderOnce;
    getComponent(): IFrameworkPart;
    protected abstract getBodyComponent(): IPanePart;
    protected abstract getHeaderComponent(): IPanePart;
}
