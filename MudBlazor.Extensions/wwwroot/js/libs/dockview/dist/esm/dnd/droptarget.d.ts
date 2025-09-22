import { DockviewEvent, Event } from '../events';
import { CompositeDisposable } from '../lifecycle';
import { DragAndDropObserver } from './dnd';
import { Direction } from '../gridview/baseComponentGridview';
export interface DroptargetEvent {
    readonly position: Position;
    readonly nativeEvent: DragEvent;
}
export declare class WillShowOverlayEvent extends DockviewEvent implements DroptargetEvent {
    private readonly options;
    get nativeEvent(): DragEvent;
    get position(): Position;
    constructor(options: {
        nativeEvent: DragEvent;
        position: Position;
    });
}
export declare function directionToPosition(direction: Direction): Position;
export declare function positionToDirection(position: Position): Direction;
export type Position = 'top' | 'bottom' | 'left' | 'right' | 'center';
export type CanDisplayOverlay = (dragEvent: DragEvent, state: Position) => boolean;
export type MeasuredValue = {
    value: number;
    type: 'pixels' | 'percentage';
};
export type DroptargetOverlayModel = {
    size?: MeasuredValue;
    activationSize?: MeasuredValue;
};
export interface DropTargetTargetModel {
    getElements(event?: DragEvent, outline?: HTMLElement): {
        root: HTMLElement;
        overlay: HTMLElement;
        changed: boolean;
    };
    exists(): boolean;
    clear(): void;
}
export interface DroptargetOptions {
    canDisplayOverlay: CanDisplayOverlay;
    acceptedTargetZones: Position[];
    overlayModel?: DroptargetOverlayModel;
    getOverrideTarget?: () => DropTargetTargetModel | undefined;
    className?: string;
    getOverlayOutline?: () => HTMLElement | null;
}
export declare class Droptarget extends CompositeDisposable {
    private readonly element;
    private readonly options;
    private targetElement;
    private overlayElement;
    private _state;
    private _acceptedTargetZonesSet;
    private readonly _onDrop;
    readonly onDrop: Event<DroptargetEvent>;
    private readonly _onWillShowOverlay;
    readonly onWillShowOverlay: Event<WillShowOverlayEvent>;
    readonly dnd: DragAndDropObserver;
    private static USED_EVENT_ID;
    private static ACTUAL_TARGET;
    private _disabled;
    get disabled(): boolean;
    set disabled(value: boolean);
    get state(): Position | undefined;
    constructor(element: HTMLElement, options: DroptargetOptions);
    setTargetZones(acceptedTargetZones: Position[]): void;
    setOverlayModel(model: DroptargetOverlayModel): void;
    dispose(): void;
    /**
     * Add a property to the event object for other potential listeners to check
     */
    private markAsUsed;
    /**
     * Check is the event has already been used by another instance of DropTarget
     */
    private isAlreadyUsed;
    private toggleClasses;
    private calculateQuadrant;
    private removeDropTarget;
}
export declare function calculateQuadrantAsPercentage(overlayType: Set<Position>, x: number, y: number, width: number, height: number, threshold: number): Position | null;
export declare function calculateQuadrantAsPixels(overlayType: Set<Position>, x: number, y: number, width: number, height: number, threshold: number): Position | null;
