export var Event;
(function (Event) {
    Event.any = (...children) => {
        return (listener) => {
            const disposables = children.map((child) => child(listener));
            return {
                dispose: () => {
                    disposables.forEach((d) => {
                        d.dispose();
                    });
                },
            };
        };
    };
})(Event || (Event = {}));
export class DockviewEvent {
    constructor() {
        this._defaultPrevented = false;
    }
    get defaultPrevented() {
        return this._defaultPrevented;
    }
    preventDefault() {
        this._defaultPrevented = true;
    }
}
export class AcceptableEvent {
    constructor() {
        this._isAccepted = false;
    }
    get isAccepted() {
        return this._isAccepted;
    }
    accept() {
        this._isAccepted = true;
    }
}
class LeakageMonitor {
    constructor() {
        this.events = new Map();
    }
    get size() {
        return this.events.size;
    }
    add(event, stacktrace) {
        this.events.set(event, stacktrace);
    }
    delete(event) {
        this.events.delete(event);
    }
    clear() {
        this.events.clear();
    }
}
class Stacktrace {
    static create() {
        var _a;
        return new Stacktrace((_a = new Error().stack) !== null && _a !== void 0 ? _a : '');
    }
    constructor(value) {
        this.value = value;
    }
    print() {
        console.warn('dockview: stacktrace', this.value);
    }
}
class Listener {
    constructor(callback, stacktrace) {
        this.callback = callback;
        this.stacktrace = stacktrace;
    }
}
// relatively simple event emitter taken from https://github.com/microsoft/vscode/blob/master/src/vs/base/common/event.ts
export class Emitter {
    static setLeakageMonitorEnabled(isEnabled) {
        if (isEnabled !== Emitter.ENABLE_TRACKING) {
            Emitter.MEMORY_LEAK_WATCHER.clear();
        }
        Emitter.ENABLE_TRACKING = isEnabled;
    }
    get value() {
        return this._last;
    }
    constructor(options) {
        this.options = options;
        this._listeners = [];
        this._disposed = false;
    }
    get event() {
        if (!this._event) {
            this._event = (callback) => {
                var _a;
                if (((_a = this.options) === null || _a === void 0 ? void 0 : _a.replay) && this._last !== undefined) {
                    callback(this._last);
                }
                const listener = new Listener(callback, Emitter.ENABLE_TRACKING ? Stacktrace.create() : undefined);
                this._listeners.push(listener);
                return {
                    dispose: () => {
                        const index = this._listeners.indexOf(listener);
                        if (index > -1) {
                            this._listeners.splice(index, 1);
                        }
                        else if (Emitter.ENABLE_TRACKING) {
                            // console.warn(
                            //     `dockview: listener already disposed`,
                            //     Stacktrace.create().print()
                            // );
                        }
                    },
                };
            };
            if (Emitter.ENABLE_TRACKING) {
                Emitter.MEMORY_LEAK_WATCHER.add(this._event, Stacktrace.create());
            }
        }
        return this._event;
    }
    fire(e) {
        var _a;
        if ((_a = this.options) === null || _a === void 0 ? void 0 : _a.replay) {
            this._last = e;
        }
        for (const listener of this._listeners) {
            listener.callback(e);
        }
    }
    dispose() {
        if (!this._disposed) {
            this._disposed = true;
            if (this._listeners.length > 0) {
                if (Emitter.ENABLE_TRACKING) {
                    queueMicrotask(() => {
                        var _a;
                        // don't check until stack of execution is completed to allow for out-of-order disposals within the same execution block
                        for (const listener of this._listeners) {
                            console.warn('dockview: stacktrace', (_a = listener.stacktrace) === null || _a === void 0 ? void 0 : _a.print());
                        }
                    });
                }
                this._listeners = [];
            }
            if (Emitter.ENABLE_TRACKING && this._event) {
                Emitter.MEMORY_LEAK_WATCHER.delete(this._event);
            }
        }
    }
}
Emitter.ENABLE_TRACKING = false;
Emitter.MEMORY_LEAK_WATCHER = new LeakageMonitor();
export function addDisposableListener(element, type, listener, options) {
    element.addEventListener(type, listener, options);
    return {
        dispose: () => {
            element.removeEventListener(type, listener, options);
        },
    };
}
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
export class AsapEvent {
    constructor() {
        this._onFired = new Emitter();
        this._currentFireCount = 0;
        this._queued = false;
        this.onEvent = (e) => {
            /**
             * when the event is first subscribed to take note of the current fire count
             */
            const fireCountAtTimeOfEventSubscription = this._currentFireCount;
            return this._onFired.event(() => {
                /**
                 * if the current fire count is greater than the fire count at event subscription
                 * then the event has been fired since we subscribed and it's ok to "on_next" the event.
                 *
                 * if the count is not greater then what we are recieving is an event from the microtask
                 * queue that was triggered before we actually subscribed and therfore we should ignore it.
                 */
                if (this._currentFireCount > fireCountAtTimeOfEventSubscription) {
                    e();
                }
            });
        };
    }
    fire() {
        this._currentFireCount++;
        if (this._queued) {
            return;
        }
        this._queued = true;
        queueMicrotask(() => {
            this._queued = false;
            this._onFired.fire();
        });
    }
    dispose() {
        this._onFired.dispose();
    }
}
