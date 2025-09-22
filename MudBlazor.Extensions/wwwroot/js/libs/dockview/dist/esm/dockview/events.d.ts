import { Position, WillShowOverlayEvent } from '../dnd/droptarget';
import { PanelTransfer } from '../dnd/dataTransfer';
import { DockviewApi } from '../api/component.api';
import { IDockviewPanel } from './dockviewPanel';
import { DockviewGroupPanel } from './dockviewGroupPanel';
import { IDockviewEvent } from '../events';
export type DockviewGroupDropLocation = 'tab' | 'header_space' | 'content' | 'edge';
export interface WillShowOverlayLocationEventOptions {
    readonly kind: DockviewGroupDropLocation;
    readonly panel: IDockviewPanel | undefined;
    readonly api: DockviewApi;
    readonly group: DockviewGroupPanel | undefined;
    getData: () => PanelTransfer | undefined;
}
export declare class WillShowOverlayLocationEvent implements IDockviewEvent {
    private readonly event;
    readonly options: WillShowOverlayLocationEventOptions;
    get kind(): DockviewGroupDropLocation;
    get nativeEvent(): DragEvent;
    get position(): Position;
    get defaultPrevented(): boolean;
    get panel(): IDockviewPanel | undefined;
    get api(): DockviewApi;
    get group(): DockviewGroupPanel | undefined;
    preventDefault(): void;
    getData(): PanelTransfer | undefined;
    constructor(event: WillShowOverlayEvent, options: WillShowOverlayLocationEventOptions);
}
