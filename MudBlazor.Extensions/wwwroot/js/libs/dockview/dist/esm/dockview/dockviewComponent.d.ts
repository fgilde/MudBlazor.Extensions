import { SerializedGridObject } from '../gridview/gridview';
import { Position } from '../dnd/droptarget';
import { DockviewPanel, IDockviewPanel } from './dockviewPanel';
import { Event, Emitter } from '../events';
import { IWatermarkRenderer, GroupviewPanelState } from './types';
import { AddGroupOptions, AddPanelOptions, DockviewComponentOptions, DockviewDndOverlayEvent, DockviewOptions, MovementOptions } from './options';
import { BaseGrid, IBaseGrid } from '../gridview/baseComponentGridview';
import { DockviewApi } from '../api/component.api';
import { Orientation } from '../splitview/splitview';
import { GroupOptions, GroupPanelViewState, DockviewDidDropEvent, DockviewWillDropEvent } from './dockviewGroupPanelModel';
import { WillShowOverlayLocationEvent } from './events';
import { DockviewGroupPanel } from './dockviewGroupPanel';
import { Parameters } from '../panel/types';
import { DockviewFloatingGroupPanel } from './dockviewFloatingGroupPanel';
import { GroupDragEvent, TabDragEvent } from './components/titlebar/tabsContainer';
import { AnchoredBox, AnchorPosition, Box } from '../types';
import { DockviewPanelRenderer, OverlayRenderContainer } from '../overlay/overlayRenderContainer';
import { PopupService } from './components/popupService';
import { DropTargetAnchorContainer } from '../dnd/dropTargetAnchorContainer';
export interface DockviewPopoutGroupOptions {
    /**
     * The position of the popout group
     */
    position?: Box;
    /**
     * The same-origin path at which the popout window will be created
     *
     * Defaults to `/popout.html` if not provided
     */
    popoutUrl?: string;
    referenceGroup?: DockviewGroupPanel;
    onDidOpen?: (event: {
        id: string;
        window: Window;
    }) => void;
    onWillClose?: (event: {
        id: string;
        window: Window;
    }) => void;
    overridePopoutGroup?: DockviewGroupPanel;
}
export interface PanelReference {
    update: (event: {
        params: {
            [key: string]: any;
        };
    }) => void;
    remove: () => void;
}
export interface SerializedFloatingGroup {
    data: GroupPanelViewState;
    position: AnchoredBox;
}
export interface SerializedPopoutGroup {
    data: GroupPanelViewState;
    url?: string;
    gridReferenceGroup?: string;
    position: Box | null;
}
export interface SerializedDockview {
    grid: {
        root: SerializedGridObject<GroupPanelViewState>;
        height: number;
        width: number;
        orientation: Orientation;
    };
    panels: Record<string, GroupviewPanelState>;
    activeGroup?: string;
    floatingGroups?: SerializedFloatingGroup[];
    popoutGroups?: SerializedPopoutGroup[];
}
export interface MovePanelEvent {
    panel: IDockviewPanel;
    from: DockviewGroupPanel;
}
type MoveGroupOptions = {
    from: {
        group: DockviewGroupPanel;
    };
    to: {
        group: DockviewGroupPanel;
        position: Position;
    };
    skipSetActive?: boolean;
};
type MoveGroupOrPanelOptions = {
    from: {
        groupId: string;
        panelId?: string;
    };
    to: {
        group: DockviewGroupPanel;
        position: Position;
        index?: number;
    };
    skipSetActive?: boolean;
};
export interface FloatingGroupOptions {
    x?: number;
    y?: number;
    height?: number;
    width?: number;
    position?: AnchorPosition;
}
export interface FloatingGroupOptionsInternal extends FloatingGroupOptions {
    skipRemoveGroup?: boolean;
    inDragMode?: boolean;
    skipActiveGroup?: boolean;
}
export interface DockviewMaximizedGroupChanged {
    group: DockviewGroupPanel;
    isMaximized: boolean;
}
export interface PopoutGroupChangeSizeEvent {
    width: number;
    height: number;
    group: DockviewGroupPanel;
}
export interface PopoutGroupChangePositionEvent {
    screenX: number;
    screenY: number;
    group: DockviewGroupPanel;
}
export interface IDockviewComponent extends IBaseGrid<DockviewGroupPanel> {
    readonly activePanel: IDockviewPanel | undefined;
    readonly totalPanels: number;
    readonly panels: IDockviewPanel[];
    readonly orientation: Orientation;
    readonly onDidDrop: Event<DockviewDidDropEvent>;
    readonly onWillDrop: Event<DockviewWillDropEvent>;
    readonly onWillShowOverlay: Event<WillShowOverlayLocationEvent>;
    readonly onDidRemovePanel: Event<IDockviewPanel>;
    readonly onDidAddPanel: Event<IDockviewPanel>;
    readonly onDidLayoutFromJSON: Event<void>;
    readonly onDidActivePanelChange: Event<IDockviewPanel | undefined>;
    readonly onWillDragPanel: Event<TabDragEvent>;
    readonly onWillDragGroup: Event<GroupDragEvent>;
    readonly onDidRemoveGroup: Event<DockviewGroupPanel>;
    readonly onDidAddGroup: Event<DockviewGroupPanel>;
    readonly onDidActiveGroupChange: Event<DockviewGroupPanel | undefined>;
    readonly onUnhandledDragOverEvent: Event<DockviewDndOverlayEvent>;
    readonly onDidMovePanel: Event<MovePanelEvent>;
    readonly onDidMaximizedGroupChange: Event<DockviewMaximizedGroupChanged>;
    readonly onDidPopoutGroupSizeChange: Event<PopoutGroupChangeSizeEvent>;
    readonly onDidPopoutGroupPositionChange: Event<PopoutGroupChangePositionEvent>;
    readonly onDidOpenPopoutWindowFail: Event<void>;
    readonly options: DockviewComponentOptions;
    updateOptions(options: DockviewOptions): void;
    moveGroupOrPanel(options: MoveGroupOrPanelOptions): void;
    moveGroup(options: MoveGroupOptions): void;
    doSetGroupActive: (group: DockviewGroupPanel, skipFocus?: boolean) => void;
    removeGroup: (group: DockviewGroupPanel) => void;
    addPanel<T extends object = Parameters>(options: AddPanelOptions<T>): IDockviewPanel;
    removePanel(panel: IDockviewPanel): void;
    getGroupPanel: (id: string) => IDockviewPanel | undefined;
    createWatermarkComponent(): IWatermarkRenderer;
    addGroup(options?: AddGroupOptions): DockviewGroupPanel;
    closeAllGroups(): void;
    moveToNext(options?: MovementOptions): void;
    moveToPrevious(options?: MovementOptions): void;
    setActivePanel(panel: IDockviewPanel): void;
    focus(): void;
    toJSON(): SerializedDockview;
    fromJSON(data: SerializedDockview): void;
    addFloatingGroup(item: IDockviewPanel | DockviewGroupPanel, options?: FloatingGroupOptions): void;
    addPopoutGroup(item: IDockviewPanel | DockviewGroupPanel, options?: {
        position?: Box;
        popoutUrl?: string;
        onDidOpen?: (event: {
            id: string;
            window: Window;
        }) => void;
        onWillClose?: (event: {
            id: string;
            window: Window;
        }) => void;
    }): Promise<boolean>;
}
export declare class DockviewComponent extends BaseGrid<DockviewGroupPanel> implements IDockviewComponent {
    private readonly nextGroupId;
    private readonly _deserializer;
    private readonly _api;
    private _options;
    private _watermark;
    private readonly _themeClassnames;
    readonly overlayRenderContainer: OverlayRenderContainer;
    readonly popupService: PopupService;
    readonly rootDropTargetContainer: DropTargetAnchorContainer;
    private readonly _onWillDragPanel;
    readonly onWillDragPanel: Event<TabDragEvent>;
    private readonly _onWillDragGroup;
    readonly onWillDragGroup: Event<GroupDragEvent>;
    private readonly _onDidDrop;
    readonly onDidDrop: Event<DockviewDidDropEvent>;
    private readonly _onWillDrop;
    readonly onWillDrop: Event<DockviewWillDropEvent>;
    private readonly _onWillShowOverlay;
    readonly onWillShowOverlay: Event<WillShowOverlayLocationEvent>;
    private readonly _onUnhandledDragOverEvent;
    readonly onUnhandledDragOverEvent: Event<DockviewDndOverlayEvent>;
    private readonly _onDidRemovePanel;
    readonly onDidRemovePanel: Event<IDockviewPanel>;
    private readonly _onDidAddPanel;
    readonly onDidAddPanel: Event<IDockviewPanel>;
    private readonly _onDidPopoutGroupSizeChange;
    readonly onDidPopoutGroupSizeChange: Event<PopoutGroupChangeSizeEvent>;
    private readonly _onDidPopoutGroupPositionChange;
    readonly onDidPopoutGroupPositionChange: Event<PopoutGroupChangePositionEvent>;
    private readonly _onDidOpenPopoutWindowFail;
    readonly onDidOpenPopoutWindowFail: Event<void>;
    private readonly _onDidLayoutFromJSON;
    readonly onDidLayoutFromJSON: Event<void>;
    private readonly _onDidActivePanelChange;
    readonly onDidActivePanelChange: Event<IDockviewPanel | undefined>;
    private readonly _onDidMovePanel;
    readonly onDidMovePanel: Event<MovePanelEvent>;
    private readonly _onDidMaximizedGroupChange;
    readonly onDidMaximizedGroupChange: Event<DockviewMaximizedGroupChanged>;
    private readonly _floatingGroups;
    private readonly _popoutGroups;
    private readonly _rootDropTarget;
    private _popoutRestorationPromise;
    private readonly _onDidRemoveGroup;
    readonly onDidRemoveGroup: Event<DockviewGroupPanel>;
    protected readonly _onDidAddGroup: Emitter<DockviewGroupPanel>;
    readonly onDidAddGroup: Event<DockviewGroupPanel>;
    private readonly _onDidOptionsChange;
    readonly onDidOptionsChange: Event<void>;
    private readonly _onDidActiveGroupChange;
    readonly onDidActiveGroupChange: Event<DockviewGroupPanel | undefined>;
    get orientation(): Orientation;
    get totalPanels(): number;
    get panels(): IDockviewPanel[];
    get options(): DockviewComponentOptions;
    get activePanel(): IDockviewPanel | undefined;
    get renderer(): DockviewPanelRenderer;
    get api(): DockviewApi;
    get floatingGroups(): DockviewFloatingGroupPanel[];
    /**
     * Promise that resolves when all popout groups from the last fromJSON call are restored.
     * Useful for tests that need to wait for delayed popout creation.
     */
    get popoutRestorationPromise(): Promise<void>;
    constructor(container: HTMLElement, options: DockviewComponentOptions);
    setVisible(panel: DockviewGroupPanel, visible: boolean): void;
    addPopoutGroup(itemToPopout: DockviewPanel | DockviewGroupPanel, options?: DockviewPopoutGroupOptions): Promise<boolean>;
    addFloatingGroup(item: DockviewPanel | DockviewGroupPanel, options?: FloatingGroupOptionsInternal): void;
    private orthogonalize;
    updateOptions(options: Partial<DockviewComponentOptions>): void;
    layout(width: number, height: number, forceResize?: boolean | undefined): void;
    private updateDragAndDropState;
    focus(): void;
    getGroupPanel(id: string): IDockviewPanel | undefined;
    setActivePanel(panel: IDockviewPanel): void;
    moveToNext(options?: MovementOptions): void;
    moveToPrevious(options?: MovementOptions): void;
    /**
     * Serialize the current state of the layout
     *
     * @returns A JSON respresentation of the layout
     */
    toJSON(): SerializedDockview;
    fromJSON(data: SerializedDockview): void;
    clear(): void;
    closeAllGroups(): void;
    addPanel<T extends object = Parameters>(options: AddPanelOptions<T>): DockviewPanel;
    removePanel(panel: IDockviewPanel, options?: {
        removeEmptyGroup: boolean;
        skipDispose?: boolean;
        skipSetActiveGroup?: boolean;
    }): void;
    createWatermarkComponent(): IWatermarkRenderer;
    private updateWatermark;
    addGroup(options?: AddGroupOptions): DockviewGroupPanel;
    private getLocationOrientation;
    removeGroup(group: DockviewGroupPanel, options?: {
        skipActive?: boolean;
        skipDispose?: boolean;
        skipPopoutAssociated?: boolean;
        skipPopoutReturn?: boolean;
    } | undefined): void;
    protected doRemoveGroup(group: DockviewGroupPanel, options?: {
        skipActive?: boolean;
        skipDispose?: boolean;
        skipPopoutAssociated?: boolean;
        skipPopoutReturn?: boolean;
    } | undefined): DockviewGroupPanel;
    private _moving;
    movingLock<T>(func: () => T): T;
    moveGroupOrPanel(options: MoveGroupOrPanelOptions): void;
    moveGroup(options: MoveGroupOptions): void;
    doSetGroupActive(group: DockviewGroupPanel | undefined): void;
    doSetGroupAndPanelActive(group: DockviewGroupPanel | undefined): void;
    private getNextGroupId;
    createGroup(options?: GroupOptions): DockviewGroupPanel;
    private createPanel;
    private createGroupAtLocation;
    private findGroup;
    private orientationAtLocation;
    private updateDropTargetModel;
    private updateTheme;
}
export {};
