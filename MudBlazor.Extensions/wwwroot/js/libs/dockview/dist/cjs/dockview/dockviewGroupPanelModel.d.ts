import { DockviewApi } from '../api/component.api';
import { PanelTransfer } from '../dnd/dataTransfer';
import { Position } from '../dnd/droptarget';
import { DockviewComponent } from './dockviewComponent';
import { DockviewEvent, Event } from '../events';
import { DockviewGroupDropLocation, WillShowOverlayLocationEvent } from './events';
import { IViewSize } from '../gridview/gridview';
import { CompositeDisposable } from '../lifecycle';
import { IPanel, PanelInitParameters, PanelUpdateEvent, Parameters } from '../panel/types';
import { GroupDragEvent, TabDragEvent } from './components/titlebar/tabsContainer';
import { DockviewGroupPanel } from './dockviewGroupPanel';
import { IDockviewPanel } from './dockviewPanel';
import { DockviewDndOverlayEvent } from './options';
import { OverlayRenderContainer } from '../overlay/overlayRenderContainer';
import { TitleEvent } from '../api/dockviewPanelApi';
import { Contraints } from '../gridview/gridviewPanel';
import { DropTargetAnchorContainer } from '../dnd/dropTargetAnchorContainer';
interface GroupMoveEvent {
    groupId: string;
    itemId?: string;
    target: Position;
    index?: number;
}
interface CoreGroupOptions {
    locked?: DockviewGroupPanelLocked;
    hideHeader?: boolean;
    skipSetActive?: boolean;
    constraints?: Partial<Contraints>;
    initialWidth?: number;
    initialHeight?: number;
}
export interface GroupOptions extends CoreGroupOptions {
    readonly panels?: IDockviewPanel[];
    readonly activePanel?: IDockviewPanel;
    readonly id?: string;
}
export interface GroupPanelViewState extends CoreGroupOptions {
    views: string[];
    activeView?: string;
    id: string;
}
export interface DockviewGroupChangeEvent {
    readonly panel: IDockviewPanel;
}
export declare class DockviewDidDropEvent extends DockviewEvent {
    private readonly options;
    get nativeEvent(): DragEvent;
    get position(): Position;
    get panel(): IDockviewPanel | undefined;
    get group(): DockviewGroupPanel | undefined;
    get api(): DockviewApi;
    constructor(options: {
        readonly nativeEvent: DragEvent;
        readonly position: Position;
        readonly panel?: IDockviewPanel;
        getData(): PanelTransfer | undefined;
        group?: DockviewGroupPanel;
        api: DockviewApi;
    });
    getData(): PanelTransfer | undefined;
}
export declare class DockviewWillDropEvent extends DockviewDidDropEvent {
    private readonly _kind;
    get kind(): DockviewGroupDropLocation;
    constructor(options: {
        readonly nativeEvent: DragEvent;
        readonly position: Position;
        readonly panel?: IDockviewPanel;
        getData(): PanelTransfer | undefined;
        kind: DockviewGroupDropLocation;
        group?: DockviewGroupPanel;
        api: DockviewApi;
    });
}
export interface IHeader {
    hidden: boolean;
}
export type DockviewGroupPanelLocked = boolean | 'no-drop-target';
export interface IDockviewGroupPanelModel extends IPanel {
    readonly isActive: boolean;
    readonly size: number;
    readonly panels: IDockviewPanel[];
    readonly activePanel: IDockviewPanel | undefined;
    readonly header: IHeader;
    readonly isContentFocused: boolean;
    readonly onDidDrop: Event<DockviewDidDropEvent>;
    readonly onWillDrop: Event<DockviewWillDropEvent>;
    readonly onDidAddPanel: Event<DockviewGroupChangeEvent>;
    readonly onDidRemovePanel: Event<DockviewGroupChangeEvent>;
    readonly onDidActivePanelChange: Event<DockviewGroupChangeEvent>;
    readonly onMove: Event<GroupMoveEvent>;
    locked: DockviewGroupPanelLocked;
    setActive(isActive: boolean): void;
    initialize(): void;
    isPanelActive: (panel: IDockviewPanel) => boolean;
    indexOf(panel: IDockviewPanel): number;
    openPanel(panel: IDockviewPanel, options?: {
        index?: number;
        skipFocus?: boolean;
        skipSetPanelActive?: boolean;
        skipSetGroupActive?: boolean;
    }): void;
    closePanel(panel: IDockviewPanel): void;
    closeAllPanels(): void;
    containsPanel(panel: IDockviewPanel): boolean;
    removePanel: (panelOrId: IDockviewPanel | string) => IDockviewPanel;
    moveToNext(options?: {
        panel?: IDockviewPanel;
        suppressRoll?: boolean;
    }): void;
    moveToPrevious(options?: {
        panel?: IDockviewPanel;
        suppressRoll?: boolean;
    }): void;
    canDisplayOverlay(event: DragEvent, position: Position, target: DockviewGroupDropLocation): boolean;
}
export type DockviewGroupLocation = {
    type: 'grid';
} | {
    type: 'floating';
} | {
    type: 'popout';
    getWindow: () => Window;
    popoutUrl?: string;
};
export declare class DockviewGroupPanelModel extends CompositeDisposable implements IDockviewGroupPanelModel {
    private readonly container;
    private readonly accessor;
    id: string;
    private readonly options;
    private readonly groupPanel;
    private readonly tabsContainer;
    private readonly contentContainer;
    private _activePanel;
    private watermark?;
    private _isGroupActive;
    private _locked;
    private _rightHeaderActions;
    private _leftHeaderActions;
    private _prefixHeaderActions;
    private _location;
    private mostRecentlyUsed;
    private _overwriteRenderContainer;
    private _overwriteDropTargetContainer;
    private readonly _onDidChange;
    readonly onDidChange: Event<IViewSize | undefined>;
    private _width;
    private _height;
    private readonly _panels;
    private readonly _panelDisposables;
    private readonly _onMove;
    readonly onMove: Event<GroupMoveEvent>;
    private readonly _onDidDrop;
    readonly onDidDrop: Event<DockviewDidDropEvent>;
    private readonly _onWillDrop;
    readonly onWillDrop: Event<DockviewWillDropEvent>;
    private readonly _onWillShowOverlay;
    readonly onWillShowOverlay: Event<WillShowOverlayLocationEvent>;
    private readonly _onTabDragStart;
    readonly onTabDragStart: Event<TabDragEvent>;
    private readonly _onGroupDragStart;
    readonly onGroupDragStart: Event<GroupDragEvent>;
    private readonly _onDidAddPanel;
    readonly onDidAddPanel: Event<DockviewGroupChangeEvent>;
    private readonly _onDidPanelTitleChange;
    readonly onDidPanelTitleChange: Event<TitleEvent>;
    private readonly _onDidPanelParametersChange;
    readonly onDidPanelParametersChange: Event<Parameters>;
    private readonly _onDidRemovePanel;
    readonly onDidRemovePanel: Event<DockviewGroupChangeEvent>;
    private readonly _onDidActivePanelChange;
    readonly onDidActivePanelChange: Event<DockviewGroupChangeEvent>;
    private readonly _onUnhandledDragOverEvent;
    readonly onUnhandledDragOverEvent: Event<DockviewDndOverlayEvent>;
    private readonly _api;
    get element(): HTMLElement;
    get activePanel(): IDockviewPanel | undefined;
    get locked(): DockviewGroupPanelLocked;
    set locked(value: DockviewGroupPanelLocked);
    get isActive(): boolean;
    get panels(): IDockviewPanel[];
    get size(): number;
    get isEmpty(): boolean;
    get hasWatermark(): boolean;
    get header(): IHeader;
    get isContentFocused(): boolean;
    get location(): DockviewGroupLocation;
    set location(value: DockviewGroupLocation);
    constructor(container: HTMLElement, accessor: DockviewComponent, id: string, options: GroupOptions, groupPanel: DockviewGroupPanel);
    focusContent(): void;
    set renderContainer(value: OverlayRenderContainer | null);
    get renderContainer(): OverlayRenderContainer;
    set dropTargetContainer(value: DropTargetAnchorContainer | null);
    get dropTargetContainer(): DropTargetAnchorContainer | null;
    initialize(): void;
    rerender(panel: IDockviewPanel): void;
    indexOf(panel: IDockviewPanel): number;
    toJSON(): GroupPanelViewState;
    moveToNext(options?: {
        panel?: IDockviewPanel;
        suppressRoll?: boolean;
    }): void;
    moveToPrevious(options?: {
        panel?: IDockviewPanel;
        suppressRoll?: boolean;
    }): void;
    containsPanel(panel: IDockviewPanel): boolean;
    init(_params: PanelInitParameters): void;
    update(_params: PanelUpdateEvent): void;
    focus(): void;
    openPanel(panel: IDockviewPanel, options?: {
        index?: number;
        skipSetActive?: boolean;
        skipSetGroupActive?: boolean;
    }): void;
    removePanel(groupItemOrId: IDockviewPanel | string, options?: {
        skipSetActive?: boolean;
        skipSetActiveGroup?: boolean;
    }): IDockviewPanel;
    closeAllPanels(): void;
    closePanel(panel: IDockviewPanel): void;
    private doClose;
    isPanelActive(panel: IDockviewPanel): boolean;
    updateActions(element: HTMLElement | undefined): void;
    setActive(isGroupActive: boolean, force?: boolean): void;
    layout(width: number, height: number): void;
    private _removePanel;
    private doRemovePanel;
    private doAddPanel;
    private doSetActivePanel;
    private updateMru;
    private updateContainer;
    canDisplayOverlay(event: DragEvent, position: Position, target: DockviewGroupDropLocation): boolean;
    private handleDropEvent;
    updateDragAndDropState(): void;
    dispose(): void;
}
export {};
