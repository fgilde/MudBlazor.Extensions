import { PaneTransfer } from '../dnd/dataTransfer';
import { Position } from '../dnd/droptarget';
import { CreateComponentOptions } from '../dockview/options';
import { AcceptableEvent, IAcceptableEvent } from '../events';
import { IPanePart, IPaneviewPanel } from './paneviewPanel';
export interface PaneviewOptions {
    disableAutoResizing?: boolean;
    disableDnd?: boolean;
    className?: string;
}
export interface PaneviewFrameworkOptions {
    createComponent: (options: CreateComponentOptions) => IPanePart;
    createHeaderComponent?: (options: CreateComponentOptions) => IPanePart | undefined;
}
export type PaneviewComponentOptions = PaneviewOptions & PaneviewFrameworkOptions;
export declare const PROPERTY_KEYS_PANEVIEW: (keyof PaneviewOptions)[];
export interface PaneviewDndOverlayEvent extends IAcceptableEvent {
    nativeEvent: DragEvent;
    position: Position;
    panel: IPaneviewPanel;
    getData: () => PaneTransfer | undefined;
}
export declare class PaneviewUnhandledDragOverEvent extends AcceptableEvent implements PaneviewDndOverlayEvent {
    readonly nativeEvent: DragEvent;
    readonly position: Position;
    readonly getData: () => PaneTransfer | undefined;
    readonly panel: IPaneviewPanel;
    constructor(nativeEvent: DragEvent, position: Position, getData: () => PaneTransfer | undefined, panel: IPaneviewPanel);
}
