import { Position } from '../dnd/droptarget';
import { DockviewComponent } from '../dockview/dockviewComponent';
import { DockviewGroupPanel } from '../dockview/dockviewGroupPanel';
import { DockviewGroupChangeEvent, DockviewGroupLocation } from '../dockview/dockviewGroupPanelModel';
import { Emitter, Event } from '../events';
import { GridviewPanelApi, GridviewPanelApiImpl } from './gridviewPanelApi';
export interface DockviewGroupMoveParams {
    group?: DockviewGroupPanel;
    position?: Position;
    /**
     * The index to place the panel within a group, only applicable if the placement is within an existing group
     */
    index?: number;
    /**
     * Whether to skip setting the group as active after moving
     */
    skipSetActive?: boolean;
}
export interface DockviewGroupPanelApi extends GridviewPanelApi {
    readonly onDidLocationChange: Event<DockviewGroupPanelFloatingChangeEvent>;
    readonly onDidActivePanelChange: Event<DockviewGroupChangeEvent>;
    readonly location: DockviewGroupLocation;
    /**
     * If you require the Window object
     */
    getWindow(): Window;
    moveTo(options: DockviewGroupMoveParams): void;
    maximize(): void;
    isMaximized(): boolean;
    exitMaximized(): void;
    close(): void;
}
export interface DockviewGroupPanelFloatingChangeEvent {
    readonly location: DockviewGroupLocation;
}
export declare class DockviewGroupPanelApiImpl extends GridviewPanelApiImpl {
    private readonly accessor;
    private _group;
    readonly _onDidLocationChange: Emitter<DockviewGroupPanelFloatingChangeEvent>;
    readonly onDidLocationChange: Event<DockviewGroupPanelFloatingChangeEvent>;
    readonly _onDidActivePanelChange: Emitter<DockviewGroupChangeEvent>;
    readonly onDidActivePanelChange: Event<DockviewGroupChangeEvent>;
    get location(): DockviewGroupLocation;
    constructor(id: string, accessor: DockviewComponent);
    close(): void;
    getWindow(): Window;
    moveTo(options: DockviewGroupMoveParams): void;
    maximize(): void;
    isMaximized(): boolean;
    exitMaximized(): void;
    initialize(group: DockviewGroupPanel): void;
}
