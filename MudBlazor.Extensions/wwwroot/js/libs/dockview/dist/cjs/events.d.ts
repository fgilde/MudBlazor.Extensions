import { IDisposable } from './lifecycle';
export interface Event<T> {
    (listener: (e: T) => any): IDisposable;
}
export interface EmitterOptions {
    readonly replay?: boolean;
}
export declare namespace Event {
    const any: <T>(...children: Event<T>[]) => Event<T>;
}
export interface IDockviewEvent {
    readonly defaultPrevented: boolean;
    preventDefault(): void;
}
export declare class DockviewEvent implements IDockviewEvent {
    private _defaultPrevented;
    get defaultPrevented(): boolean;
    preventDefault(): void;
}
export interface IAcceptableEvent {
    readonly isAccepted: boolean;
    accept(): void;
}
export declare class AcceptableEvent implements IAcceptableEvent {
    private _isAccepted;
    get isAccepted(): boolean;
    accept(): void;
}
declare class LeakageMonitor {
    readonly events: Map<Event<any>, Stacktrace>;
    get size(): number;
    add<T>(event: Event<T>, stacktrace: Stacktrace): void;
    delete<T>(event: Event<T>): void;
    clear(): void;
}
declare class Stacktrace {
    readonly value: string;
    static create(): Stacktrace;
    private constructor();
    print(): void;
}
export declare class Emitter<T> implements IDisposable {
    private readonly options?;
    private _event?;
    private _last?;
    private _listeners;
    private _disposed;
    static ENABLE_TRACKING: boolean;
    static readonly MEMORY_LEAK_WATCHER: LeakageMonitor;
    static setLeakageMonitorEnabled(isEnabled: boolean): void;
    get value(): T | undefined;
    constructor(options?: EmitterOptions | undefined);
    get event(): Event<T>;
    fire(e: T): void;
    dispose(): void;
}
export declare function addDisposableListener<K extends keyof WindowEventMap>(element: Window, type: K, listener: (this: Window, ev: WindowEventMap[K]) => any, options?: boolean | AddEventListenerOptions): IDisposable;
export declare function addDisposableListener<K extends keyof HTMLElementEventMap>(element: HTMLElement, type: K, listener: (this: HTMLElement, ev: HTMLElementEventMap[K]) => any, options?: boolean | AddEventListenerOptions): IDisposable;
/**
 *
 * Event Emitter that fires events from a Microtask callback, only one event will fire per event-loop cycle.
 *
 * It's kind of like using an `asapScheduler` in RxJs with additional logic to only fire once per event-loop cycle.
 * This implementation exists to avoid external dependencies.
 *
 * @see https://developer.mozilla.org/en-US/docs/Web/API/queueMicrotask
 * @see https://rxjs.dev/api/index/const/asapScheduler
 */
export declare class AsapEvent implements IDisposable {
    private readonly _onFired;
    private _currentFireCount;
    private _queued;
    readonly onEvent: Event<void>;
    fire(): void;
    dispose(): void;
}
export {};
