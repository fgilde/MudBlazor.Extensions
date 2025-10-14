import { DockviewApi } from '../api/component.api';
import { Direction } from '../gridview/baseComponentGridview';
import { IGridView } from '../gridview/gridview';
import { IContentRenderer, ITabRenderer, IWatermarkRenderer } from './types';
import { Parameters } from '../panel/types';
import { DockviewGroupPanel } from './dockviewGroupPanel';
import { PanelTransfer } from '../dnd/dataTransfer';
import { IDisposable } from '../lifecycle';
import { DroptargetOverlayModel, Position } from '../dnd/droptarget';
import { GroupOptions } from './dockviewGroupPanelModel';
import { DockviewGroupDropLocation } from './events';
import { IDockviewPanel } from './dockviewPanel';
import { DockviewPanelRenderer } from '../overlay/overlayRenderContainer';
import { IGroupHeaderProps } from './framework';
import { FloatingGroupOptions } from './dockviewComponent';
import { Contraints } from '../gridview/gridviewPanel';
import { AcceptableEvent, IAcceptableEvent } from '../events';
import { DockviewTheme } from './theme';
export interface IHeaderActionsRenderer extends IDisposable {
    readonly element: HTMLElement;
    init(params: IGroupHeaderProps): void;
}
export interface TabContextMenuEvent {
    event: MouseEvent;
    api: DockviewApi;
    panel: IDockviewPanel;
}
export interface ViewFactoryData {
    content: string;
    tab?: string;
}
export interface DockviewOptions {
    /**
     * Disable the auto-resizing which is controlled through a `ResizeObserver`.
     * Call `.layout(width, height)` to manually resize the container.
     */
    disableAutoResizing?: boolean;
    hideBorders?: boolean;
    singleTabMode?: 'fullwidth' | 'default';
    disableFloatingGroups?: boolean;
    floatingGroupBounds?: 'boundedWithinViewport' | {
        minimumHeightWithinViewport?: number;
        minimumWidthWithinViewport?: number;
    };
    popoutUrl?: string;
    defaultRenderer?: DockviewPanelRenderer;
    debug?: boolean;
    dndEdges?: false | DroptargetOverlayModel;
    /**
     * @deprecated use `dndEdges` instead. To be removed in a future version.
     * */
    rootOverlayModel?: DroptargetOverlayModel;
    disableDnd?: boolean;
    locked?: boolean;
    className?: string;
    /**
     * Define the behaviour of the dock when there are no panels to display. Defaults to `watermark`.
     */
    noPanelsOverlay?: 'emptyGroup' | 'watermark';
    theme?: DockviewTheme;
    disableTabsOverflowList?: boolean;
    /**
     * Select `native` to use built-in scrollbar behaviours and `custom` to use an internal implementation
     * that allows for improved scrollbar overlay UX.
     *
     * This is only applied to the tab header section. Defaults to `custom`.
     */
    scrollbars?: 'native' | 'custom';
}
export interface DockviewDndOverlayEvent extends IAcceptableEvent {
    nativeEvent: DragEvent;
    target: DockviewGroupDropLocation;
    position: Position;
    group?: DockviewGroupPanel;
    getData: () => PanelTransfer | undefined;
}
export declare class DockviewUnhandledDragOverEvent extends AcceptableEvent implements DockviewDndOverlayEvent {
    readonly nativeEvent: DragEvent;
    readonly target: DockviewGroupDropLocation;
    readonly position: Position;
    readonly getData: () => PanelTransfer | undefined;
    readonly group?: DockviewGroupPanel | undefined;
    constructor(nativeEvent: DragEvent, target: DockviewGroupDropLocation, position: Position, getData: () => PanelTransfer | undefined, group?: DockviewGroupPanel | undefined);
}
export declare const PROPERTY_KEYS_DOCKVIEW: (keyof DockviewOptions)[];
export interface CreateComponentOptions {
    /**
     * The unqiue identifer of the component
     */
    id: string;
    /**
     * The component name, this should determine what is rendered.
     */
    name: string;
}
export interface DockviewFrameworkOptions {
    defaultTabComponent?: string;
    createRightHeaderActionComponent?: (group: DockviewGroupPanel) => IHeaderActionsRenderer;
    createLeftHeaderActionComponent?: (group: DockviewGroupPanel) => IHeaderActionsRenderer;
    createPrefixHeaderActionComponent?: (group: DockviewGroupPanel) => IHeaderActionsRenderer;
    createTabComponent?: (options: CreateComponentOptions) => ITabRenderer | undefined;
    createComponent: (options: CreateComponentOptions) => IContentRenderer;
    createWatermarkComponent?: () => IWatermarkRenderer;
}
export type DockviewComponentOptions = DockviewOptions & DockviewFrameworkOptions;
export interface PanelOptions<P extends object = Parameters> {
    component: string;
    tabComponent?: string;
    params?: P;
    id: string;
    title?: string;
}
type RelativePanel = {
    direction?: Direction;
    referencePanel: string | IDockviewPanel;
    /**
     * The index to place the panel within a group, only applicable if the placement is within an existing group
     */
    index?: number;
};
type RelativeGroup = {
    direction?: Direction;
    referenceGroup: string | DockviewGroupPanel;
    /**
     * The index to place the panel within a group, only applicable if the placement is within an existing group
     */
    index?: number;
};
type AbsolutePosition = {
    direction: Omit<Direction, 'within'>;
};
export type AddPanelPositionOptions = RelativePanel | RelativeGroup | AbsolutePosition;
export declare function isPanelOptionsWithPanel(data: AddPanelPositionOptions): data is RelativePanel;
export declare function isPanelOptionsWithGroup(data: AddPanelPositionOptions): data is RelativeGroup;
type AddPanelFloatingGroupUnion = {
    floating: Partial<FloatingGroupOptions> | true;
    position: never;
};
type AddPanelPositionUnion = {
    floating: false;
    position: AddPanelPositionOptions;
};
type AddPanelOptionsUnion = AddPanelFloatingGroupUnion | AddPanelPositionUnion;
export type AddPanelOptions<P extends object = Parameters> = {
    params?: P;
    /**
     * The unique id for the panel
     */
    id: string;
    /**
     * The title for the panel which can be accessed within both the tab and component.
     *
     * If using the default tab renderer this title will be displayed in the tab.
     */
    title?: string;
    /**
     * The id of the component renderer
     */
    component: string;
    /**
     * The id of the tab componnet renderer
     */
    tabComponent?: string;
    /**
     * The rendering mode of the panel.
     *
     * This dictates what happens to the HTML of the panel when it is hidden.
     */
    renderer?: DockviewPanelRenderer;
    /**
     * If true then add the panel without setting it as the active panel.
     *
     * Defaults to `false` which forces newly added panels to become active.
     */
    inactive?: boolean;
    initialWidth?: number;
    initialHeight?: number;
} & Partial<AddPanelOptionsUnion> & Partial<Contraints>;
type AddGroupOptionsWithPanel = {
    referencePanel: string | IDockviewPanel;
    direction?: Omit<Direction, 'within'>;
};
type AddGroupOptionsWithGroup = {
    referenceGroup: string | DockviewGroupPanel;
    direction?: Omit<Direction, 'within'>;
};
export type AddGroupOptions = (AddGroupOptionsWithGroup | AddGroupOptionsWithPanel | AbsolutePosition) & GroupOptions;
export declare function isGroupOptionsWithPanel(data: AddGroupOptions): data is AddGroupOptionsWithPanel;
export declare function isGroupOptionsWithGroup(data: AddGroupOptions): data is AddGroupOptionsWithGroup;
export interface MovementOptions2 {
    group?: IGridView;
}
export interface MovementOptions extends MovementOptions2 {
    includePanel?: boolean;
    group?: DockviewGroupPanel;
}
export {};
