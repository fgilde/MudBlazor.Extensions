import { Emitter, Event } from '../events';
import { GridviewPanelApiImpl, GridviewPanelApi } from './gridviewPanelApi';
import { DockviewGroupPanel } from '../dockview/dockviewGroupPanel';
import { DockviewPanel } from '../dockview/dockviewPanel';
import { DockviewComponent } from '../dockview/dockviewComponent';
import { DockviewPanelRenderer } from '../overlay/overlayRenderContainer';
import { DockviewGroupMoveParams, DockviewGroupPanelFloatingChangeEvent } from './dockviewGroupPanelApi';
import { DockviewGroupLocation } from '../dockview/dockviewGroupPanelModel';
export interface TitleEvent {
    readonly title: string;
}
export interface RendererChangedEvent {
    readonly renderer: DockviewPanelRenderer;
}
export interface ActiveGroupEvent {
    readonly isActive: boolean;
}
export interface GroupChangedEvent {
}
export type DockviewPanelMoveParams = DockviewGroupMoveParams;
export interface DockviewPanelApi extends Omit<GridviewPanelApi, 'setVisible' | 'onDidConstraintsChange' | 'setConstraints'> {
    /**
     * The id of the tab component renderer
     *
     * Undefined if no custom tab renderer is provided
     */
    readonly tabComponent: string | undefined;
    readonly group: DockviewGroupPanel;
    readonly isGroupActive: boolean;
    readonly renderer: DockviewPanelRenderer;
    readonly title: string | undefined;
    readonly onDidActiveGroupChange: Event<ActiveGroupEvent>;
    readonly onDidGroupChange: Event<GroupChangedEvent>;
    readonly onDidTitleChange: Event<TitleEvent>;
    readonly onDidRendererChange: Event<RendererChangedEvent>;
    readonly location: DockviewGroupLocation;
    readonly onDidLocationChange: Event<DockviewGroupPanelFloatingChangeEvent>;
    close(): void;
    setTitle(title: string): void;
    setRenderer(renderer: DockviewPanelRenderer): void;
    moveTo(options: DockviewPanelMoveParams): void;
    maximize(): void;
    isMaximized(): boolean;
    exitMaximized(): void;
    /**
     * If you require the Window object
     */
    getWindow(): Window;
}
export declare class DockviewPanelApiImpl extends GridviewPanelApiImpl implements DockviewPanelApi {
    private readonly panel;
    private readonly accessor;
    private _group;
    private readonly _tabComponent;
    readonly _onDidTitleChange: Emitter<TitleEvent>;
    readonly onDidTitleChange: Event<TitleEvent>;
    private readonly _onDidActiveGroupChange;
    readonly onDidActiveGroupChange: Event<ActiveGroupEvent>;
    private readonly _onDidGroupChange;
    readonly onDidGroupChange: Event<GroupChangedEvent>;
    readonly _onDidRendererChange: Emitter<RendererChangedEvent>;
    readonly onDidRendererChange: Event<RendererChangedEvent>;
    private readonly _onDidLocationChange;
    readonly onDidLocationChange: Event<DockviewGroupPanelFloatingChangeEvent>;
    private readonly groupEventsDisposable;
    get location(): DockviewGroupLocation;
    get title(): string | undefined;
    get isGroupActive(): boolean;
    get renderer(): DockviewPanelRenderer;
    set group(value: DockviewGroupPanel);
    get group(): DockviewGroupPanel;
    get tabComponent(): string | undefined;
    constructor(panel: DockviewPanel, group: DockviewGroupPanel, accessor: DockviewComponent, component: string, tabComponent?: string);
    getWindow(): Window;
    moveTo(options: DockviewPanelMoveParams): void;
    setTitle(title: string): void;
    setRenderer(renderer: DockviewPanelRenderer): void;
    close(): void;
    maximize(): void;
    isMaximized(): boolean;
    exitMaximized(): void;
    private setupGroupEventListeners;
}
