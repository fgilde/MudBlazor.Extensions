"use strict";
var __values = (this && this.__values) || function(o) {
    var s = typeof Symbol === "function" && Symbol.iterator, m = s && o[s], i = 0;
    if (m) return m.call(o);
    if (o && typeof o.length === "number") return {
        next: function () {
            if (o && i >= o.length) o = void 0;
            return { value: o && o[i++], done: !o };
        }
    };
    throw new TypeError(s ? "Object is not iterable." : "Symbol.iterator is not defined.");
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.AsapEvent = exports.addDisposableListener = exports.Emitter = exports.AcceptableEvent = exports.DockviewEvent = exports.Event = void 0;
var Event;
(function (Event) {
    Event.any = function () {
        var children = [];
        for (var _i = 0; _i < arguments.length; _i++) {
            children[_i] = arguments[_i];
        }
        return function (listener) {
            var disposables = children.map(function (child) { return child(listener); });
            return {
                dispose: function () {
                    disposables.forEach(function (d) {
                        d.dispose();
                    });
                },
            };
        };
    };
})(Event || (exports.Event = Event = {}));
var DockviewEvent = /** @class */ (function () {
    function DockviewEvent() {
        this._defaultPrevented = false;
    }
    Object.defineProperty(DockviewEvent.prototype, "defaultPrevented", {
        get: function () {
            return this._defaultPrevented;
        },
        enumerable: false,
        configurable: true
    });
    DockviewEvent.prototype.preventDefault = function () {
        this._defaultPrevented = true;
    };
    return DockviewEvent;
}());
exports.DockviewEvent = DockviewEvent;
var AcceptableEvent = /** @class */ (function () {
    function AcceptableEvent() {
        this._isAccepted = false;
    }
    Object.defineProperty(AcceptableEvent.prototype, "isAccepted", {
        get: function () {
            return this._isAccepted;
        },
        enumerable: false,
        configurable: true
    });
    AcceptableEvent.prototype.accept = function () {
        this._isAccepted = true;
    };
    return AcceptableEvent;
}());
exports.AcceptableEvent = AcceptableEvent;
var LeakageMonitor = /** @class */ (function () {
    function LeakageMonitor() {
        this.events = new Map();
    }
    Object.defineProperty(LeakageMonitor.prototype, "size", {
        get: function () {
            return this.events.size;
        },
        enumerable: false,
        configurable: true
    });
    LeakageMonitor.prototype.add = function (event, stacktrace) {
        this.events.set(event, stacktrace);
    };
    LeakageMonitor.prototype.delete = function (event) {
        this.events.delete(event);
    };
    LeakageMonitor.prototype.clear = function () {
        this.events.clear();
    };
    return LeakageMonitor;
}());
var Stacktrace = /** @class */ (function () {
    function Stacktrace(value) {
        this.value = value;
    }
    Stacktrace.create = function () {
        var _a;
        return new Stacktrace((_a = new Error().stack) !== null && _a !== void 0 ? _a : '');
    };
    Stacktrace.prototype.print = function () {
        console.warn('dockview: stacktrace', this.value);
    };
    return Stacktrace;
}());
var Listener = /** @class */ (function () {
    function Listener(callback, stacktrace) {
        this.callback = callback;
        this.stacktrace = stacktrace;
    }
    return Listener;
}());
// relatively simple event emitter taken from https://github.com/microsoft/vscode/blob/master/src/vs/base/common/event.ts
var Emitter = /** @class */ (function () {
    function Emitter(options) {
        this.options = options;
        this._listeners = [];
        this._disposed = false;
    }
    Emitter.setLeakageMonitorEnabled = function (isEnabled) {
        if (isEnabled !== Emitter.ENABLE_TRACKING) {
            Emitter.MEMORY_LEAK_WATCHER.clear();
        }
        Emitter.ENABLE_TRACKING = isEnabled;
    };
    Object.defineProperty(Emitter.prototype, "value", {
        get: function () {
            return this._last;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(Emitter.prototype, "event", {
        get: function () {
            var _this = this;
            if (!this._event) {
                this._event = function (callback) {
                    var _a;
                    if (((_a = _this.options) === null || _a === void 0 ? void 0 : _a.replay) && _this._last !== undefined) {
                        callback(_this._last);
                    }
                    var listener = new Listener(callback, Emitter.ENABLE_TRACKING ? Stacktrace.create() : undefined);
                    _this._listeners.push(listener);
                    return {
                        dispose: function () {
                            var index = _this._listeners.indexOf(listener);
                            if (index > -1) {
                                _this._listeners.splice(index, 1);
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
        },
        enumerable: false,
        configurable: true
    });
    Emitter.prototype.fire = function (e) {
        var e_1, _a;
        var _b;
        if ((_b = this.options) === null || _b === void 0 ? void 0 : _b.replay) {
            this._last = e;
        }
        try {
            for (var _c = __values(this._listeners), _d = _c.next(); !_d.done; _d = _c.next()) {
                var listener = _d.value;
                listener.callback(e);
            }
        }
        catch (e_1_1) { e_1 = { error: e_1_1 }; }
        finally {
            try {
                if (_d && !_d.done && (_a = _c.return)) _a.call(_c);
            }
            finally { if (e_1) throw e_1.error; }
        }
    };
    Emitter.prototype.dispose = function () {
        var _this = this;
        if (!this._disposed) {
            this._disposed = true;
            if (this._listeners.length > 0) {
                if (Emitter.ENABLE_TRACKING) {
                    queueMicrotask(function () {
                        var e_2, _a;
                        var _b;
                        try {
                            // don't check until stack of execution is completed to allow for out-of-order disposals within the same execution block
                            for (var _c = __values(_this._listeners), _d = _c.next(); !_d.done; _d = _c.next()) {
                                var listener = _d.value;
                                console.warn('dockview: stacktrace', (_b = listener.stacktrace) === null || _b === void 0 ? void 0 : _b.print());
                            }
                        }
                        catch (e_2_1) { e_2 = { error: e_2_1 }; }
                        finally {
                            try {
                                if (_d && !_d.done && (_a = _c.return)) _a.call(_c);
                            }
                            finally { if (e_2) throw e_2.error; }
                        }
                    });
                }
                this._listeners = [];
            }
            if (Emitter.ENABLE_TRACKING && this._event) {
                Emitter.MEMORY_LEAK_WATCHER.delete(this._event);
            }
        }
    };
    Emitter.ENABLE_TRACKING = false;
    Emitter.MEMORY_LEAK_WATCHER = new LeakageMonitor();
    return Emitter;
}());
exports.Emitter = Emitter;
function addDisposableListener(element, type, listener, options) {
    element.addEventListener(type, listener, options);
    return {
        dispose: function () {
            element.removeEventListener(type, listener, options);
        },
    };
}
exports.addDisposableListener = addDisposableListener;
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
var AsapEvent = /** @class */ (function () {
    function AsapEvent() {
        var _this = this;
        this._onFired = new Emitter();
        this._currentFireCount = 0;
        this._queued = false;
        this.onEvent = function (e) {
            /**
             * when the event is first subscribed to take note of the current fire count
             */
            var fireCountAtTimeOfEventSubscription = _this._currentFireCount;
            return _this._onFired.event(function () {
                /**
                 * if the current fire count is greater than the fire count at event subscription
                 * then the event has been fired since we subscribed and it's ok to "on_next" the event.
                 *
                 * if the count is not greater then what we are recieving is an event from the microtask
                 * queue that was triggered before we actually subscribed and therfore we should ignore it.
                 */
                if (_this._currentFireCount > fireCountAtTimeOfEventSubscription) {
                    e();
                }
            });
        };
    }
    AsapEvent.prototype.fire = function () {
        var _this = this;
        this._currentFireCount++;
        if (this._queued) {
            return;
        }
        this._queued = true;
        queueMicrotask(function () {
            _this._queued = false;
            _this._onFired.fire();
        });
    };
    AsapEvent.prototype.dispose = function () {
        this._onFired.dispose();
    };
    return AsapEvent;
}());
exports.AsapEvent = AsapEvent;
