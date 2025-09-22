import { DockviewMaximizedGroupChanged, FloatingGroupOptions, IDockviewComponent, MovePanelEvent, PopoutGroupChangePositionEvent, PopoutGroupChangeSizeEvent, SerializedDockview } from '../dockview/dockviewComponent';
import { AddGroupOptions, AddPanelOptions, DockviewComponentOptions, DockviewDndOverlayEvent, MovementOptions } from '../dockview/options';
import { Parameters } from '../panel/types';
import { Direction } from '../gridview/baseComponentGridview';
import { AddComponentOptions, IGridviewComponent, SerializedGridviewComponent } from '../gridview/gridviewComponent';
import { IGridviewPanel } from '../gridview/gridviewPanel';
import { AddPaneviewComponentOptions, SerializedPaneview, IPaneviewComponent } from '../paneview/paneviewComponent';
import { IPaneviewPanel } from '../paneview/paneviewPanel';
import { AddSplitviewComponentOptions, ISplitviewComponent, SerializedSplitview } from '../splitview/splitviewComponent';
import { IView, Orientation, Sizing } from '../splitview/splitview';
import { ISplitviewPanel } from '../splitview/splitviewPanel';
import { DockviewGroupPanel, IDockviewGroupPanel } from '../dockview/dockviewGroupPanel';
import { Event } from '../events';
import { IDockviewPanel } from '../dockview/dockviewPanel';
import { PaneviewDidDropEvent } from '../paneview/draggablePaneviewPanel';
import { GroupDragEvent, TabDragEvent } from '../dockview/components/titlebar/tabsContainer';
import { Box } from '../types';
import { DockviewDidDropEvent, DockviewWillDropEvent } from '../dockview/dockviewGroupPanelModel';
import { WillShowOverlayLocationEvent } from '../dockview/events';
import { PaneviewComponentOptions, PaneviewDndOverlayEvent } from '../paneview/options';
import { SplitviewComponentOptions } from '../splitview/options';
import { GridviewComponentOptions } from '../gridview/options';
export interface CommonApi<T = any> {
    readonly height: number;
    readonly width: number;
    readonly onDidLayoutChange: Event<void>;
    readonly onDidLayoutFromJSON: Event<void>;
    focus(): void;
    layout(width: number, height: number): void;
    fromJSON(data: T): void;
    toJSON(): T;
    clear(): void;
    dispose(): void;
}
export declare class SplitviewApi implements CommonApi<SerializedSplitview> {
    private readonly component;
    /**
     * The minimum size  the component can reach where size is measured in the direction of orientation provided.
     */
    get minimumSize(): number;
    /**
     * The maximum size the component can reach where size is measured in the direction of orientation provided.
     */
    get maximumSize(): number;
    /**
     * Width of the component.
     */
    get width(): number;
    /**
     * Height of the component.
     */
    get height(): number;
    /**
     * The current number of panels.
     */
    get length(): number;
    /**
     * The current orientation of the component.
     */
    get orientation(): Orientation;
    /**
     * The list of current panels.
     */
    get panels(): ISplitviewPanel[];
    /**
     * Invoked after a layout is loaded through the `fromJSON` method.
     */
    get onDidLayoutFromJSON(): Event<void>;
    /**
     * Invoked whenever any aspect of the layout changes.
     * If listening to this event it may be worth debouncing ouputs.
     */
    get onDidLayoutChange(): Event<void>;
    /**
     * Invoked when a view is added.
     */
    get onDidAddView(): Event<IView>;
    /**
     * Invoked when a view is removed.
     */
    get onDidRemoveView(): Event<IView>;
    constructor(component: ISplitviewComponent);
    /**
     * Removes an existing panel and optionally provide a `Sizing` method
     * for the subsequent resize.
     */
    removePanel(panel: ISplitviewPanel, sizing?: Sizing): void;
    /**
     * Focus the component.
     */
    focus(): void;
    /**
     * Get the reference to a panel given it's `string` id.
     */
    getPanel(id: string): ISplitviewPanel | undefined;
    /**
     * Layout the panel with a width and height.
     */
    layout(width: number, height: number): void;
    /**
     * Add a new panel and return the created instance.
     */
    addPanel<T extends object = Parameters>(options: AddSplitviewComponentOptions<T>): ISplitviewPanel;
    /**
     * Move a panel given it's current and desired index.
     */
    movePanel(from: number, to: number): void;
    /**
     * Deserialize a layout to built a splitivew.
     */
    fromJSON(data: SerializedSplitview): void;
    /** Serialize a layout */
    toJSON(): SerializedSplitview;
    /**
     * Remove all panels and clear the component.
     */
    clear(): void;
    /**
     * Update configuratable options.
     */
    updateOptions(options: Partial<SplitviewComponentOptions>): void;
    /**
     * Release resources and teardown component. Do not call when using framework versions of dockview.
     */
    dispose(): void;
}
export declare class PaneviewApi implements CommonApi<SerializedPaneview> {
    private readonly component;
    /**
     * The minimum size  the component can reach where size is measured in the direction of orientation provided.
     */
    get minimumSize(): number;
    /**
     * The maximum size the component can reach where size is measured in the direction of orientation provided.
     */
    get maximumSize(): number;
    /**
     * Width of the component.
     */
    get width(): number;
    /**
     * Height of the component.
     */
    get height(): number;
    /**
     * All panel objects.
     */
    get panels(): IPaneviewPanel[];
    /**
     * Invoked when any layout change occures, an aggregation of many events.
     */
    get onDidLayoutChange(): Event<void>;
    /**
     * Invoked after a layout is deserialzied using the `fromJSON` method.
     */
    get onDidLayoutFromJSON(): Event<void>;
    /**
     * Invoked when a panel is added. May be called multiple times when moving panels.
     */
    get onDidAddView(): Event<IPaneviewPanel>;
    /**
     * Invoked when a panel is removed. May be called multiple times when moving panels.
     */
    get onDidRemoveView(): Event<IPaneviewPanel>;
    /**
     * Invoked when a Drag'n'Drop event occurs that the component was unable to handle. Exposed for custom Drag'n'Drop functionality.
     */
    get onDidDrop(): Event<PaneviewDidDropEvent>;
    get onUnhandledDragOverEvent(): Event<PaneviewDndOverlayEvent>;
    constructor(component: IPaneviewComponent);
    /**
     * Remove a panel given the panel object.
     */
    removePanel(panel: IPaneviewPanel): void;
    /**
     * Get a panel object given a `string` id. May return `undefined`.
     */
    getPanel(id: string): IPaneviewPanel | undefined;
    /**
     * Move a panel given it's current and desired index.
     */
    movePanel(from: number, to: number): void;
    /**
     *  Focus the component. Will try to focus an active panel if one exists.
     */
    focus(): void;
    /**
     * Force resize the component to an exact width and height. Read about auto-resizing before using.
     */
    layout(width: number, height: number): void;
    /**
     * Add a panel and return the created object.
     */
    addPanel<T extends object = Parameters>(options: AddPaneviewComponentOptions<T>): IPaneviewPanel;
    /**
     * Create a component from a serialized object.
     */
    fromJSON(data: SerializedPaneview): void;
    /**
     * Create a serialized object of the current component.
     */
    toJSON(): SerializedPaneview;
    /**
     * Reset the component back to an empty and default state.
     */
    clear(): void;
    /**
     * Update configuratable options.
     */
    updateOptions(options: Partial<PaneviewComponentOptions>): void;
    /**
     * Release resources and teardown component. Do not call when using framework versions of dockview.
     */
    dispose(): void;
}
export declare class GridviewApi implements CommonApi<SerializedGridviewComponent> {
    private readonly component;
    /**
     * Width of the component.
     */
    get width(): number;
    /**
     * Height of the component.
     */
    get height(): number;
    /**
     * Minimum height of the component.
     */
    get minimumHeight(): number;
    /**
     * Maximum height of the component.
     */
    get maximumHeight(): number;
    /**
     * Minimum width of the component.
     */
    get minimumWidth(): number;
    /**
     * Maximum width of the component.
     */
    get maximumWidth(): number;
    /**
     * Invoked when any layout change occures, an aggregation of many events.
     */
    get onDidLayoutChange(): Event<void>;
    /**
     * Invoked when a panel is added. May be called multiple times when moving panels.
     */
    get onDidAddPanel(): Event<IGridviewPanel>;
    /**
     * Invoked when a panel is removed. May be called multiple times when moving panels.
     */
    get onDidRemovePanel(): Event<IGridviewPanel>;
    /**
     * Invoked when the active panel changes. May be undefined if no panel is active.
     */
    get onDidActivePanelChange(): Event<IGridviewPanel | undefined>;
    /**
     * Invoked after a layout is deserialzied using the `fromJSON` method.
     */
    get onDidLayoutFromJSON(): Event<void>;
    /**
     * All panel objects.
     */
    get panels(): IGridviewPanel[];
    /**
     * Current orientation. Can be changed after initialization.
     */
    get orientation(): Orientation;
    set orientation(value: Orientation);
    constructor(component: IGridviewComponent);
    /**
     *  Focus the component. Will try to focus an active panel if one exists.
     */
    focus(): void;
    /**
     * Force resize the component to an exact width and height. Read about auto-resizing before using.
     */
    layout(width: number, height: number, force?: boolean): void;
    /**
     * Add a panel and return the created object.
     */
    addPanel<T extends object = Parameters>(options: AddComponentOptions<T>): IGridviewPanel;
    /**
     * Remove a panel given the panel object.
     */
    removePanel(panel: IGridviewPanel, sizing?: Sizing): void;
    /**
     * Move a panel in a particular direction relative to another panel.
     */
    movePanel(panel: IGridviewPanel, options: {
        direction: Direction;
        reference: string;
        size?: number;
    }): void;
    /**
     * Get a panel object given a `string` id. May return `undefined`.
     */
    getPanel(id: string): IGridviewPanel | undefined;
    /**
     * Create a component from a serialized object.
     */
    fromJSON(data: SerializedGridviewComponent): void;
    /**
     * Create a serialized object of the current component.
     */
    toJSON(): SerializedGridviewComponent;
    /**
     * Reset the component back to an empty and default state.
     */
    clear(): void;
    updateOptions(options: Partial<GridviewComponentOptions>): void;
    /**
     * Release resources and teardown component. Do not call when using framework versions of dockview.
     */
    dispose(): void;
}
export declare class DockviewApi implements CommonApi<SerializedDockview> {
    private readonly component;
    /**
     * The unique identifier for this instance. Used to manage scope of Drag'n'Drop events.
     */
    get id(): string;
    /**
     * Width of the component.
     */
    get width(): number;
    /**
     * Height of the component.
     */
    get height(): number;
    /**
     * Minimum height of the component.
     */
    get minimumHeight(): number;
    /**
     * Maximum height of the component.
     */
    get maximumHeight(): number;
    /**
     * Minimum width of the component.
     */
    get minimumWidth(): number;
    /**
     * Maximum width of the component.
     */
    get maximumWidth(): number;
    /**
     * Total number of groups.
     */
    get size(): number;
    /**
     * Total number of panels.
     */
    get totalPanels(): number;
    /**
     * Invoked when the active group changes. May be undefined if no group is active.
     */
    get onDidActiveGroupChange(): Event<DockviewGroupPanel | undefined>;
    /**
     * Invoked when a group is added. May be called multiple times when moving groups.
     */
    get onDidAddGroup(): Event<DockviewGroupPanel>;
    /**
     * Invoked when a group is removed. May be called multiple times when moving groups.
     */
    get onDidRemoveGroup(): Event<DockviewGroupPanel>;
    /**
     * Invoked when the active panel changes. May be undefined if no panel is active.
     */
    get onDidActivePanelChange(): Event<IDockviewPanel | undefined>;
    /**
     * Invoked when a panel is added. May be called multiple times when moving panels.
     */
    get onDidAddPanel(): Event<IDockviewPanel>;
    /**
     * Invoked when a panel is removed. May be called multiple times when moving panels.
     */
    get onDidRemovePanel(): Event<IDockviewPanel>;
    get onDidMovePanel(): Event<MovePanelEvent>;
    /**
     * Invoked after a layout is deserialzied using the `fromJSON` method.
     */
    get onDidLayoutFromJSON(): Event<void>;
    /**
     * Invoked when any layout change occures, an aggregation of many events.
     */
    get onDidLayoutChange(): Event<void>;
    /**
     * Invoked when a Drag'n'Drop event occurs that the component was unable to handle. Exposed for custom Drag'n'Drop functionality.
     */
    get onDidDrop(): Event<DockviewDidDropEvent>;
    /**
     * Invoked when a Drag'n'Drop event occurs but before dockview handles it giving the user an opportunity to intecept and
     * prevent the event from occuring using the standard `preventDefault()` syntax.
     *
     * Preventing certain events may causes unexpected behaviours, use carefully.
     */
    get onWillDrop(): Event<DockviewWillDropEvent>;
    /**
     * Invoked before an overlay is shown indicating a drop target.
     *
     * Calling `event.preventDefault()` will prevent the overlay being shown and prevent
     * the any subsequent drop event.
     */
    get onWillShowOverlay(): Event<WillShowOverlayLocationEvent>;
    /**
     * Invoked before a group is dragged.
     *
     * Calling `event.nativeEvent.preventDefault()` will prevent the group drag starting.
     *
     */
    get onWillDragGroup(): Event<GroupDragEvent>;
    /**
     * Invoked before a panel is dragged.
     *
     * Calling `event.nativeEvent.preventDefault()` will prevent the panel drag starting.
     */
    get onWillDragPanel(): Event<TabDragEvent>;
    get onUnhandledDragOverEvent(): Event<DockviewDndOverlayEvent>;
    get onDidPopoutGroupSizeChange(): Event<PopoutGroupChangeSizeEvent>;
    get onDidPopoutGroupPositionChange(): Event<PopoutGroupChangePositionEvent>;
    get onDidOpenPopoutWindowFail(): Event<void>;
    /**
     * All panel objects.
     */
    get panels(): IDockviewPanel[];
    /**
     * All group objects.
     */
    get groups(): DockviewGroupPanel[];
    /**
     *  Active panel object.
     */
    get activePanel(): IDockviewPanel | undefined;
    /**
     * Active group object.
     */
    get activeGroup(): DockviewGroupPanel | undefined;
    constructor(component: IDockviewComponent);
    /**
     *  Focus the component. Will try to focus an active panel if one exists.
     */
    focus(): void;
    /**
     * Get a panel object given a `string` id. May return `undefined`.
     */
    getPanel(id: string): IDockviewPanel | undefined;
    /**
     * Force resize the component to an exact width and height. Read about auto-resizing before using.
     */
    layout(width: number, height: number, force?: boolean): void;
    /**
     * Add a panel and return the created object.
     */
    addPanel<T extends object = Parameters>(options: AddPanelOptions<T>): IDockviewPanel;
    /**
     * Remove a panel given the panel object.
     */
    removePanel(panel: IDockviewPanel): void;
    /**
     * Add a group and return the created object.
     */
    addGroup(options?: AddGroupOptions): DockviewGroupPanel;
    /**
     * Close all groups and panels.
     */
    closeAllGroups(): void;
    /**
     * Remove a group and any panels within the group.
     */
    removeGroup(group: IDockviewGroupPanel): void;
    /**
     * Get a group object given a `string` id. May return undefined.
     */
    getGroup(id: string): IDockviewGroupPanel | undefined;
    /**
     * Add a floating group
     */
    addFloatingGroup(item: IDockviewPanel | DockviewGroupPanel, options?: FloatingGroupOptions): void;
    /**
     * Create a component from a serialized object.
     */
    fromJSON(data: SerializedDockview): void;
    /**
     * Create a serialized object of the current component.
     */
    toJSON(): SerializedDockview;
    /**
     * Reset the component back to an empty and default state.
     */
    clear(): void;
    /**
     * Move the focus progmatically to the next panel or group.
     */
    moveToNext(options?: MovementOptions): void;
    /**
     * Move the focus progmatically to the previous panel or group.
     */
    moveToPrevious(options?: MovementOptions): void;
    maximizeGroup(panel: IDockviewPanel): void;
    hasMaximizedGroup(): boolean;
    exitMaximizedGroup(): void;
    get onDidMaximizedGroupChange(): Event<DockviewMaximizedGroupChanged>;
    /**
     * Add a popout group in a new Window
     */
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
    updateOptions(options: Partial<DockviewComponentOptions>): void;
    /**
     * Release resources and teardown component. Do not call when using framework versions of dockview.
     */
    dispose(): void;
}
