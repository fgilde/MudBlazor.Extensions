import { CompositeDisposable } from '../lifecycle';
export interface IDragAndDropObserverCallbacks {
    onDragEnter: (e: DragEvent) => void;
    onDragLeave: (e: DragEvent) => void;
    onDrop: (e: DragEvent) => void;
    onDragEnd: (e: DragEvent) => void;
    onDragOver?: (e: DragEvent) => void;
}
export declare class DragAndDropObserver extends CompositeDisposable {
    private readonly element;
    private readonly callbacks;
    private target;
    constructor(element: HTMLElement, callbacks: IDragAndDropObserverCallbacks);
    onDragEnter(e: DragEvent): void;
    onDragOver(e: DragEvent): void;
    onDragLeave(e: DragEvent): void;
    onDragEnd(e: DragEvent): void;
    onDrop(e: DragEvent): void;
    private registerListeners;
}
export interface IDraggedCompositeData {
    eventData: DragEvent;
    dragAndDropData: any;
}
export interface ICompositeDragAndDropObserverCallbacks {
    onDragEnter?: (e: IDraggedCompositeData) => void;
    onDragLeave?: (e: IDraggedCompositeData) => void;
    onDrop?: (e: IDraggedCompositeData) => void;
    onDragOver?: (e: IDraggedCompositeData) => void;
    onDragStart?: (e: IDraggedCompositeData) => void;
    onDragEnd?: (e: IDraggedCompositeData) => void;
}
