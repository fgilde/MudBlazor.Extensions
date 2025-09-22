import { PaneviewApi } from '../api/component.api';
import { PaneTransfer } from '../dnd/dataTransfer';
import { DroptargetEvent } from '../dnd/droptarget';
import { Event } from '../events';
import { Orientation } from '../splitview/splitview';
import { PaneviewDndOverlayEvent } from './options';
import { IPaneviewComponent } from './paneviewComponent';
import { IPaneviewPanel, PaneviewPanel } from './paneviewPanel';
export interface PaneviewDidDropEvent extends DroptargetEvent {
    panel: IPaneviewPanel;
    getData: () => PaneTransfer | undefined;
    api: PaneviewApi;
}
export declare abstract class DraggablePaneviewPanel extends PaneviewPanel {
    private handler;
    private target;
    private readonly _onDidDrop;
    readonly onDidDrop: Event<PaneviewDidDropEvent>;
    private readonly _onUnhandledDragOverEvent;
    readonly onUnhandledDragOverEvent: Event<PaneviewDndOverlayEvent>;
    readonly accessor: IPaneviewComponent;
    constructor(options: {
        accessor: IPaneviewComponent;
        id: string;
        component: string;
        headerComponent: string | undefined;
        orientation: Orientation;
        isExpanded: boolean;
        disableDnd: boolean;
        headerSize: number;
        minimumBodySize: number;
        maximumBodySize: number;
    });
    private initDragFeatures;
    private onDrop;
}
