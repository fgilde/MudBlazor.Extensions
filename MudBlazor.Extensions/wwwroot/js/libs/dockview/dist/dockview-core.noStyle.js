/**
 * dockview-core
 * @version 4.7.1
 * @link https://github.com/mathuo/dockview
 * @license MIT
 */
(function (global, factory) {
    typeof exports === 'object' && typeof module !== 'undefined' ? factory(exports) :
    typeof define === 'function' && define.amd ? define(['exports'], factory) :
    (global = typeof globalThis !== 'undefined' ? globalThis : global || self, factory(global["dockview-core"] = {}));
})(this, (function (exports) { 'use strict';

    class TransferObject {
    }
    class PanelTransfer extends TransferObject {
        constructor(viewId, groupId, panelId) {
            super();
            this.viewId = viewId;
            this.groupId = groupId;
            this.panelId = panelId;
        }
    }
    class PaneTransfer extends TransferObject {
        constructor(viewId, paneId) {
            super();
            this.viewId = viewId;
            this.paneId = paneId;
        }
    }
    /**
     * A singleton to store transfer data during drag & drop operations that are only valid within the application.
     */
    class LocalSelectionTransfer {
        constructor() {
            // protect against external instantiation
        }
        static getInstance() {
            return LocalSelectionTransfer.INSTANCE;
        }
        hasData(proto) {
            return proto && proto === this.proto;
        }
        clearData(proto) {
            if (this.hasData(proto)) {
                this.proto = undefined;
                this.data = undefined;
            }
        }
        getData(proto) {
            if (this.hasData(proto)) {
                return this.data;
            }
            return undefined;
        }
        setData(data, proto) {
            if (proto) {
                this.data = data;
                this.proto = proto;
            }
        }
    }
    LocalSelectionTransfer.INSTANCE = new LocalSelectionTransfer();
    function getPanelData() {
        const panelTransfer = LocalSelectionTransfer.getInstance();
        const isPanelEvent = panelTransfer.hasData(PanelTransfer.prototype);
        if (!isPanelEvent) {
            return undefined;
        }
        return panelTransfer.getData(PanelTransfer.prototype)[0];
    }
    function getPaneData() {
        const paneTransfer = LocalSelectionTransfer.getInstance();
        const isPanelEvent = paneTransfer.hasData(PaneTransfer.prototype);
        if (!isPanelEvent) {
            return undefined;
        }
        return paneTransfer.getData(PaneTransfer.prototype)[0];
    }

    exports.DockviewEvent = void 0;
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
    })(exports.DockviewEvent || (exports.DockviewEvent = {}));
    class DockviewEvent {
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
    class AcceptableEvent {
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
    class Emitter {
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
                            else if (Emitter.ENABLE_TRACKING) ;
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
    function addDisposableListener(element, type, listener, options) {
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
    class AsapEvent {
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

    exports.DockviewDisposable = void 0;
    (function (Disposable) {
        Disposable.NONE = {
            dispose: () => {
                // noop
            },
        };
        function from(func) {
            return {
                dispose: () => {
                    func();
                },
            };
        }
        Disposable.from = from;
    })(exports.DockviewDisposable || (exports.DockviewDisposable = {}));
    class CompositeDisposable {
        get isDisposed() {
            return this._isDisposed;
        }
        constructor(...args) {
            this._isDisposed = false;
            this._disposables = args;
        }
        addDisposables(...args) {
            args.forEach((arg) => this._disposables.push(arg));
        }
        dispose() {
            if (this._isDisposed) {
                return;
            }
            this._isDisposed = true;
            this._disposables.forEach((arg) => arg.dispose());
            this._disposables = [];
        }
    }
    class MutableDisposable {
        constructor() {
            this._disposable = exports.DockviewDisposable.NONE;
        }
        set value(disposable) {
            if (this._disposable) {
                this._disposable.dispose();
            }
            this._disposable = disposable;
        }
        dispose() {
            if (this._disposable) {
                this._disposable.dispose();
                this._disposable = exports.DockviewDisposable.NONE;
            }
        }
    }

    class OverflowObserver extends CompositeDisposable {
        constructor(el) {
            super();
            this._onDidChange = new Emitter();
            this.onDidChange = this._onDidChange.event;
            this._value = null;
            this.addDisposables(this._onDidChange, watchElementResize(el, (entry) => {
                const hasScrollX = entry.target.scrollWidth > entry.target.clientWidth;
                const hasScrollY = entry.target.scrollHeight > entry.target.clientHeight;
                this._value = { hasScrollX, hasScrollY };
                this._onDidChange.fire(this._value);
            }));
        }
    }
    function watchElementResize(element, cb) {
        const observer = new ResizeObserver((entires) => {
            /**
             * Fast browser window resize produces Error: ResizeObserver loop limit exceeded.
             * The error isn't visible in browser console, doesn't affect functionality, but degrades performance.
             * See https://stackoverflow.com/questions/49384120/resizeobserver-loop-limit-exceeded/58701523#58701523
             */
            requestAnimationFrame(() => {
                const firstEntry = entires[0];
                cb(firstEntry);
            });
        });
        observer.observe(element);
        return {
            dispose: () => {
                observer.unobserve(element);
                observer.disconnect();
            },
        };
    }
    const removeClasses = (element, ...classes) => {
        for (const classname of classes) {
            if (element.classList.contains(classname)) {
                element.classList.remove(classname);
            }
        }
    };
    const addClasses = (element, ...classes) => {
        for (const classname of classes) {
            if (!element.classList.contains(classname)) {
                element.classList.add(classname);
            }
        }
    };
    const toggleClass = (element, className, isToggled) => {
        const hasClass = element.classList.contains(className);
        if (isToggled && !hasClass) {
            element.classList.add(className);
        }
        if (!isToggled && hasClass) {
            element.classList.remove(className);
        }
    };
    function isAncestor(testChild, testAncestor) {
        while (testChild) {
            if (testChild === testAncestor) {
                return true;
            }
            testChild = testChild.parentNode;
        }
        return false;
    }
    function trackFocus(element) {
        return new FocusTracker(element);
    }
    /**
     * Track focus on an element. Ensure tabIndex is set when an HTMLElement is not focusable by default
     */
    class FocusTracker extends CompositeDisposable {
        constructor(element) {
            super();
            this._onDidFocus = new Emitter();
            this.onDidFocus = this._onDidFocus.event;
            this._onDidBlur = new Emitter();
            this.onDidBlur = this._onDidBlur.event;
            this.addDisposables(this._onDidFocus, this._onDidBlur);
            let hasFocus = isAncestor(document.activeElement, element);
            let loosingFocus = false;
            const onFocus = () => {
                loosingFocus = false;
                if (!hasFocus) {
                    hasFocus = true;
                    this._onDidFocus.fire();
                }
            };
            const onBlur = () => {
                if (hasFocus) {
                    loosingFocus = true;
                    window.setTimeout(() => {
                        if (loosingFocus) {
                            loosingFocus = false;
                            hasFocus = false;
                            this._onDidBlur.fire();
                        }
                    }, 0);
                }
            };
            this._refreshStateHandler = () => {
                const currentNodeHasFocus = isAncestor(document.activeElement, element);
                if (currentNodeHasFocus !== hasFocus) {
                    if (hasFocus) {
                        onBlur();
                    }
                    else {
                        onFocus();
                    }
                }
            };
            this.addDisposables(addDisposableListener(element, 'focus', onFocus, true));
            this.addDisposables(addDisposableListener(element, 'blur', onBlur, true));
        }
        refreshState() {
            this._refreshStateHandler();
        }
    }
    // quasi: apparently, but not really; seemingly
    const QUASI_PREVENT_DEFAULT_KEY = 'dv-quasiPreventDefault';
    // mark an event directly for other listeners to check
    function quasiPreventDefault(event) {
        event[QUASI_PREVENT_DEFAULT_KEY] = true;
    }
    // check if this event has been marked
    function quasiDefaultPrevented(event) {
        return event[QUASI_PREVENT_DEFAULT_KEY];
    }
    function addStyles(document, styleSheetList) {
        const styleSheets = Array.from(styleSheetList);
        for (const styleSheet of styleSheets) {
            if (styleSheet.href) {
                const link = document.createElement('link');
                link.href = styleSheet.href;
                link.type = styleSheet.type;
                link.rel = 'stylesheet';
                document.head.appendChild(link);
            }
            let cssTexts = [];
            try {
                if (styleSheet.cssRules) {
                    cssTexts = Array.from(styleSheet.cssRules).map((rule) => rule.cssText);
                }
            }
            catch (err) {
                // security errors (lack of permissions), ignore
            }
            for (const rule of cssTexts) {
                const style = document.createElement('style');
                style.appendChild(document.createTextNode(rule));
                document.head.appendChild(style);
            }
        }
    }
    function getDomNodePagePosition(domNode) {
        const { left, top, width, height } = domNode.getBoundingClientRect();
        return {
            left: left + window.scrollX,
            top: top + window.scrollY,
            width: width,
            height: height,
        };
    }
    /**
     * Check whether an element is in the DOM (including the Shadow DOM)
     * @see https://terodox.tech/how-to-tell-if-an-element-is-in-the-dom-including-the-shadow-dom/
     */
    function isInDocument(element) {
        let currentElement = element;
        while (currentElement === null || currentElement === void 0 ? void 0 : currentElement.parentNode) {
            if (currentElement.parentNode === document) {
                return true;
            }
            else if (currentElement.parentNode instanceof DocumentFragment) {
                // handle shadow DOMs
                currentElement = currentElement.parentNode.host;
            }
            else {
                currentElement = currentElement.parentNode;
            }
        }
        return false;
    }
    function addTestId(element, id) {
        element.setAttribute('data-testid', id);
    }
    /**
     * Should be more efficient than element.querySelectorAll("*") since there
     * is no need to store every element in-memory using this approach
     */
    function allTagsNamesInclusiveOfShadowDoms(tagNames) {
        const iframes = [];
        function findIframesInNode(node) {
            if (node.nodeType === Node.ELEMENT_NODE) {
                if (tagNames.includes(node.tagName)) {
                    iframes.push(node);
                }
                if (node.shadowRoot) {
                    findIframesInNode(node.shadowRoot);
                }
                for (const child of node.children) {
                    findIframesInNode(child);
                }
            }
        }
        findIframesInNode(document.documentElement);
        return iframes;
    }
    function disableIframePointEvents(rootNode = document) {
        const iframes = allTagsNamesInclusiveOfShadowDoms(['IFRAME', 'WEBVIEW']);
        const original = new WeakMap(); // don't hold onto HTMLElement references longer than required
        for (const iframe of iframes) {
            original.set(iframe, iframe.style.pointerEvents);
            iframe.style.pointerEvents = 'none';
        }
        return {
            release: () => {
                var _a;
                for (const iframe of iframes) {
                    iframe.style.pointerEvents = (_a = original.get(iframe)) !== null && _a !== void 0 ? _a : 'auto';
                }
                iframes.splice(0, iframes.length); // don't hold onto HTMLElement references longer than required
            },
        };
    }
    function getDockviewTheme(element) {
        function toClassList(element) {
            const list = [];
            for (let i = 0; i < element.classList.length; i++) {
                list.push(element.classList.item(i));
            }
            return list;
        }
        let theme = undefined;
        let parent = element;
        while (parent !== null) {
            theme = toClassList(parent).find((cls) => cls.startsWith('dockview-theme-'));
            if (typeof theme === 'string') {
                break;
            }
            parent = parent.parentElement;
        }
        return theme;
    }
    class Classnames {
        constructor(element) {
            this.element = element;
            this._classNames = [];
        }
        setClassNames(classNames) {
            for (const className of this._classNames) {
                toggleClass(this.element, className, false);
            }
            this._classNames = classNames
                .split(' ')
                .filter((v) => v.trim().length > 0);
            for (const className of this._classNames) {
                toggleClass(this.element, className, true);
            }
        }
    }
    const DEBOUCE_DELAY = 100;
    function isChildEntirelyVisibleWithinParent(child, parent) {
        //
        const childPosition = getDomNodePagePosition(child);
        const parentPosition = getDomNodePagePosition(parent);
        if (childPosition.left < parentPosition.left) {
            return false;
        }
        if (childPosition.left + childPosition.width >
            parentPosition.left + parentPosition.width) {
            return false;
        }
        return true;
    }
    function onDidWindowMoveEnd(window) {
        const emitter = new Emitter();
        let previousScreenX = window.screenX;
        let previousScreenY = window.screenY;
        let timeout;
        const checkMovement = () => {
            if (window.closed) {
                return;
            }
            const currentScreenX = window.screenX;
            const currentScreenY = window.screenY;
            if (currentScreenX !== previousScreenX ||
                currentScreenY !== previousScreenY) {
                clearTimeout(timeout);
                timeout = setTimeout(() => {
                    emitter.fire();
                }, DEBOUCE_DELAY);
                previousScreenX = currentScreenX;
                previousScreenY = currentScreenY;
            }
            requestAnimationFrame(checkMovement);
        };
        checkMovement();
        return emitter;
    }
    function onDidWindowResizeEnd(element, cb) {
        let resizeTimeout;
        const disposable = new CompositeDisposable(addDisposableListener(element, 'resize', () => {
            clearTimeout(resizeTimeout);
            resizeTimeout = setTimeout(() => {
                cb();
            }, DEBOUCE_DELAY);
        }));
        return disposable;
    }
    function shiftAbsoluteElementIntoView(element, root, options = { buffer: 10 }) {
        const buffer = options.buffer;
        const rect = element.getBoundingClientRect();
        const rootRect = root.getBoundingClientRect();
        let translateX = 0;
        let translateY = 0;
        const left = rect.left - rootRect.left;
        const top = rect.top - rootRect.top;
        const bottom = rect.bottom - rootRect.bottom;
        const right = rect.right - rootRect.right;
        // Check horizontal overflow
        if (left < buffer) {
            translateX = buffer - left;
        }
        else if (right > buffer) {
            translateX = -buffer - right;
        }
        // Check vertical overflow
        if (top < buffer) {
            translateY = buffer - top;
        }
        else if (bottom > buffer) {
            translateY = -bottom - buffer;
        }
        // Apply the translation if needed
        if (translateX !== 0 || translateY !== 0) {
            element.style.transform = `translate(${translateX}px, ${translateY}px)`;
        }
    }
    function findRelativeZIndexParent(el) {
        let tmp = el;
        while (tmp && (tmp.style.zIndex === 'auto' || tmp.style.zIndex === '')) {
            tmp = tmp.parentElement;
        }
        return tmp;
    }

    function tail(arr) {
        if (arr.length === 0) {
            throw new Error('Invalid tail call');
        }
        return [arr.slice(0, arr.length - 1), arr[arr.length - 1]];
    }
    function sequenceEquals(arr1, arr2) {
        if (arr1.length !== arr2.length) {
            return false;
        }
        for (let i = 0; i < arr1.length; i++) {
            if (arr1[i] !== arr2[i]) {
                return false;
            }
        }
        return true;
    }
    /**
     * Pushes an element to the start of the array, if found.
     */
    function pushToStart(arr, value) {
        const index = arr.indexOf(value);
        if (index > -1) {
            arr.splice(index, 1);
            arr.unshift(value);
        }
    }
    /**
     * Pushes an element to the end of the array, if found.
     */
    function pushToEnd(arr, value) {
        const index = arr.indexOf(value);
        if (index > -1) {
            arr.splice(index, 1);
            arr.push(value);
        }
    }
    function firstIndex(array, fn) {
        for (let i = 0; i < array.length; i++) {
            const element = array[i];
            if (fn(element)) {
                return i;
            }
        }
        return -1;
    }
    function remove(array, value) {
        const index = array.findIndex((t) => t === value);
        if (index > -1) {
            array.splice(index, 1);
            return true;
        }
        return false;
    }

    const clamp = (value, min, max) => {
        if (min > max) {
            /**
             * caveat: an error should be thrown here if this was a proper `clamp` function but we need to handle
             * cases where `min` > `max` and in those cases return `min`.
             */
            return min;
        }
        return Math.min(max, Math.max(value, min));
    };
    const sequentialNumberGenerator = () => {
        let value = 1;
        return { next: () => (value++).toString() };
    };
    const range = (from, to) => {
        const result = [];
        if (typeof to !== 'number') {
            to = from;
            from = 0;
        }
        if (from <= to) {
            for (let i = from; i < to; i++) {
                result.push(i);
            }
        }
        else {
            for (let i = from; i > to; i--) {
                result.push(i);
            }
        }
        return result;
    };

    class ViewItem {
        set size(size) {
            this._size = size;
        }
        get size() {
            return this._size;
        }
        get cachedVisibleSize() {
            return this._cachedVisibleSize;
        }
        get visible() {
            return typeof this._cachedVisibleSize === 'undefined';
        }
        get minimumSize() {
            return this.visible ? this.view.minimumSize : 0;
        }
        get viewMinimumSize() {
            return this.view.minimumSize;
        }
        get maximumSize() {
            return this.visible ? this.view.maximumSize : 0;
        }
        get viewMaximumSize() {
            return this.view.maximumSize;
        }
        get priority() {
            return this.view.priority;
        }
        get snap() {
            return !!this.view.snap;
        }
        set enabled(enabled) {
            this.container.style.pointerEvents = enabled ? '' : 'none';
        }
        constructor(container, view, size, disposable) {
            this.container = container;
            this.view = view;
            this.disposable = disposable;
            this._cachedVisibleSize = undefined;
            if (typeof size === 'number') {
                this._size = size;
                this._cachedVisibleSize = undefined;
                container.classList.add('visible');
            }
            else {
                this._size = 0;
                this._cachedVisibleSize = size.cachedVisibleSize;
            }
        }
        setVisible(visible, size) {
            var _a;
            if (visible === this.visible) {
                return;
            }
            if (visible) {
                this.size = clamp((_a = this._cachedVisibleSize) !== null && _a !== void 0 ? _a : 0, this.viewMinimumSize, this.viewMaximumSize);
                this._cachedVisibleSize = undefined;
            }
            else {
                this._cachedVisibleSize =
                    typeof size === 'number' ? size : this.size;
                this.size = 0;
            }
            this.container.classList.toggle('visible', visible);
            if (this.view.setVisible) {
                this.view.setVisible(visible);
            }
        }
        dispose() {
            this.disposable.dispose();
            return this.view;
        }
    }

    /*---------------------------------------------------------------------------------------------
     * Accreditation: This file is largly based upon the MIT licenced VSCode sourcecode found at:
     * https://github.com/microsoft/vscode/tree/main/src/vs/base/browser/ui/splitview
     *--------------------------------------------------------------------------------------------*/
    exports.Orientation = void 0;
    (function (Orientation) {
        Orientation["HORIZONTAL"] = "HORIZONTAL";
        Orientation["VERTICAL"] = "VERTICAL";
    })(exports.Orientation || (exports.Orientation = {}));
    exports.SashState = void 0;
    (function (SashState) {
        SashState[SashState["MAXIMUM"] = 0] = "MAXIMUM";
        SashState[SashState["MINIMUM"] = 1] = "MINIMUM";
        SashState[SashState["DISABLED"] = 2] = "DISABLED";
        SashState[SashState["ENABLED"] = 3] = "ENABLED";
    })(exports.SashState || (exports.SashState = {}));
    exports.LayoutPriority = void 0;
    (function (LayoutPriority) {
        LayoutPriority["Low"] = "low";
        LayoutPriority["High"] = "high";
        LayoutPriority["Normal"] = "normal";
    })(exports.LayoutPriority || (exports.LayoutPriority = {}));
    exports.Sizing = void 0;
    (function (Sizing) {
        Sizing.Distribute = { type: 'distribute' };
        function Split(index) {
            return { type: 'split', index };
        }
        Sizing.Split = Split;
        function Invisible(cachedVisibleSize) {
            return { type: 'invisible', cachedVisibleSize };
        }
        Sizing.Invisible = Invisible;
    })(exports.Sizing || (exports.Sizing = {}));
    class Splitview {
        get contentSize() {
            return this._contentSize;
        }
        get size() {
            return this._size;
        }
        set size(value) {
            this._size = value;
        }
        get orthogonalSize() {
            return this._orthogonalSize;
        }
        set orthogonalSize(value) {
            this._orthogonalSize = value;
        }
        get length() {
            return this.viewItems.length;
        }
        get proportions() {
            return this._proportions ? [...this._proportions] : undefined;
        }
        get orientation() {
            return this._orientation;
        }
        set orientation(value) {
            this._orientation = value;
            const tmp = this.size;
            this.size = this.orthogonalSize;
            this.orthogonalSize = tmp;
            removeClasses(this.element, 'dv-horizontal', 'dv-vertical');
            this.element.classList.add(this.orientation == exports.Orientation.HORIZONTAL
                ? 'dv-horizontal'
                : 'dv-vertical');
        }
        get minimumSize() {
            return this.viewItems.reduce((r, item) => r + item.minimumSize, 0);
        }
        get maximumSize() {
            return this.length === 0
                ? Number.POSITIVE_INFINITY
                : this.viewItems.reduce((r, item) => r + item.maximumSize, 0);
        }
        get startSnappingEnabled() {
            return this._startSnappingEnabled;
        }
        set startSnappingEnabled(startSnappingEnabled) {
            if (this._startSnappingEnabled === startSnappingEnabled) {
                return;
            }
            this._startSnappingEnabled = startSnappingEnabled;
            this.updateSashEnablement();
        }
        get endSnappingEnabled() {
            return this._endSnappingEnabled;
        }
        set endSnappingEnabled(endSnappingEnabled) {
            if (this._endSnappingEnabled === endSnappingEnabled) {
                return;
            }
            this._endSnappingEnabled = endSnappingEnabled;
            this.updateSashEnablement();
        }
        get disabled() {
            return this._disabled;
        }
        set disabled(value) {
            this._disabled = value;
            toggleClass(this.element, 'dv-splitview-disabled', value);
        }
        get margin() {
            return this._margin;
        }
        set margin(value) {
            this._margin = value;
            toggleClass(this.element, 'dv-splitview-has-margin', value !== 0);
        }
        constructor(container, options) {
            var _a, _b;
            this.container = container;
            this.viewItems = [];
            this.sashes = [];
            this._size = 0;
            this._orthogonalSize = 0;
            this._contentSize = 0;
            this._proportions = undefined;
            this._startSnappingEnabled = true;
            this._endSnappingEnabled = true;
            this._disabled = false;
            this._margin = 0;
            this._onDidSashEnd = new Emitter();
            this.onDidSashEnd = this._onDidSashEnd.event;
            this._onDidAddView = new Emitter();
            this.onDidAddView = this._onDidAddView.event;
            this._onDidRemoveView = new Emitter();
            this.onDidRemoveView = this._onDidRemoveView.event;
            this.resize = (index, delta, sizes = this.viewItems.map((x) => x.size), lowPriorityIndexes, highPriorityIndexes, overloadMinDelta = Number.NEGATIVE_INFINITY, overloadMaxDelta = Number.POSITIVE_INFINITY, snapBefore, snapAfter) => {
                if (index < 0 || index > this.viewItems.length) {
                    return 0;
                }
                const upIndexes = range(index, -1);
                const downIndexes = range(index + 1, this.viewItems.length);
                //
                if (highPriorityIndexes) {
                    for (const i of highPriorityIndexes) {
                        pushToStart(upIndexes, i);
                        pushToStart(downIndexes, i);
                    }
                }
                if (lowPriorityIndexes) {
                    for (const i of lowPriorityIndexes) {
                        pushToEnd(upIndexes, i);
                        pushToEnd(downIndexes, i);
                    }
                }
                //
                const upItems = upIndexes.map((i) => this.viewItems[i]);
                const upSizes = upIndexes.map((i) => sizes[i]);
                //
                const downItems = downIndexes.map((i) => this.viewItems[i]);
                const downSizes = downIndexes.map((i) => sizes[i]);
                //
                const minDeltaUp = upIndexes.reduce((_, i) => _ + this.viewItems[i].minimumSize - sizes[i], 0);
                const maxDeltaUp = upIndexes.reduce((_, i) => _ + this.viewItems[i].maximumSize - sizes[i], 0);
                //
                const maxDeltaDown = downIndexes.length === 0
                    ? Number.POSITIVE_INFINITY
                    : downIndexes.reduce((_, i) => _ + sizes[i] - this.viewItems[i].minimumSize, 0);
                const minDeltaDown = downIndexes.length === 0
                    ? Number.NEGATIVE_INFINITY
                    : downIndexes.reduce((_, i) => _ + sizes[i] - this.viewItems[i].maximumSize, 0);
                //
                const minDelta = Math.max(minDeltaUp, minDeltaDown);
                const maxDelta = Math.min(maxDeltaDown, maxDeltaUp);
                //
                let snapped = false;
                if (snapBefore) {
                    const snapView = this.viewItems[snapBefore.index];
                    const visible = delta >= snapBefore.limitDelta;
                    snapped = visible !== snapView.visible;
                    snapView.setVisible(visible, snapBefore.size);
                }
                if (!snapped && snapAfter) {
                    const snapView = this.viewItems[snapAfter.index];
                    const visible = delta < snapAfter.limitDelta;
                    snapped = visible !== snapView.visible;
                    snapView.setVisible(visible, snapAfter.size);
                }
                if (snapped) {
                    return this.resize(index, delta, sizes, lowPriorityIndexes, highPriorityIndexes, overloadMinDelta, overloadMaxDelta);
                }
                //
                const tentativeDelta = clamp(delta, minDelta, maxDelta);
                let actualDelta = 0;
                //
                let deltaUp = tentativeDelta;
                for (let i = 0; i < upItems.length; i++) {
                    const item = upItems[i];
                    const size = clamp(upSizes[i] + deltaUp, item.minimumSize, item.maximumSize);
                    const viewDelta = size - upSizes[i];
                    actualDelta += viewDelta;
                    deltaUp -= viewDelta;
                    item.size = size;
                }
                //
                let deltaDown = actualDelta;
                for (let i = 0; i < downItems.length; i++) {
                    const item = downItems[i];
                    const size = clamp(downSizes[i] - deltaDown, item.minimumSize, item.maximumSize);
                    const viewDelta = size - downSizes[i];
                    deltaDown += viewDelta;
                    item.size = size;
                }
                //
                return delta;
            };
            this._orientation = (_a = options.orientation) !== null && _a !== void 0 ? _a : exports.Orientation.VERTICAL;
            this.element = this.createContainer();
            this.margin = (_b = options.margin) !== null && _b !== void 0 ? _b : 0;
            this.proportionalLayout =
                options.proportionalLayout === undefined
                    ? true
                    : !!options.proportionalLayout;
            this.viewContainer = this.createViewContainer();
            this.sashContainer = this.createSashContainer();
            this.element.appendChild(this.sashContainer);
            this.element.appendChild(this.viewContainer);
            this.container.appendChild(this.element);
            this.style(options.styles);
            // We have an existing set of view, add them now
            if (options.descriptor) {
                this._size = options.descriptor.size;
                options.descriptor.views.forEach((viewDescriptor, index) => {
                    const sizing = viewDescriptor.visible === undefined ||
                        viewDescriptor.visible
                        ? viewDescriptor.size
                        : {
                            type: 'invisible',
                            cachedVisibleSize: viewDescriptor.size,
                        };
                    const view = viewDescriptor.view;
                    this.addView(view, sizing, index, true
                    // true skip layout
                    );
                });
                // Initialize content size and proportions for first layout
                this._contentSize = this.viewItems.reduce((r, i) => r + i.size, 0);
                this.saveProportions();
            }
        }
        style(styles) {
            if ((styles === null || styles === void 0 ? void 0 : styles.separatorBorder) === 'transparent') {
                removeClasses(this.element, 'dv-separator-border');
                this.element.style.removeProperty('--dv-separator-border');
            }
            else {
                addClasses(this.element, 'dv-separator-border');
                if (styles === null || styles === void 0 ? void 0 : styles.separatorBorder) {
                    this.element.style.setProperty('--dv-separator-border', styles.separatorBorder);
                }
            }
        }
        isViewVisible(index) {
            if (index < 0 || index >= this.viewItems.length) {
                throw new Error('Index out of bounds');
            }
            const viewItem = this.viewItems[index];
            return viewItem.visible;
        }
        setViewVisible(index, visible) {
            if (index < 0 || index >= this.viewItems.length) {
                throw new Error('Index out of bounds');
            }
            const viewItem = this.viewItems[index];
            viewItem.setVisible(visible, viewItem.size);
            this.distributeEmptySpace(index);
            this.layoutViews();
            this.saveProportions();
        }
        getViewSize(index) {
            if (index < 0 || index >= this.viewItems.length) {
                return -1;
            }
            return this.viewItems[index].size;
        }
        resizeView(index, size) {
            if (index < 0 || index >= this.viewItems.length) {
                return;
            }
            const indexes = range(this.viewItems.length).filter((i) => i !== index);
            const lowPriorityIndexes = [
                ...indexes.filter((i) => this.viewItems[i].priority === exports.LayoutPriority.Low),
                index,
            ];
            const highPriorityIndexes = indexes.filter((i) => this.viewItems[i].priority === exports.LayoutPriority.High);
            const item = this.viewItems[index];
            size = Math.round(size);
            size = clamp(size, item.minimumSize, Math.min(item.maximumSize, this._size));
            item.size = size;
            this.relayout(lowPriorityIndexes, highPriorityIndexes);
        }
        getViews() {
            return this.viewItems.map((x) => x.view);
        }
        onDidChange(item, size) {
            const index = this.viewItems.indexOf(item);
            if (index < 0 || index >= this.viewItems.length) {
                return;
            }
            size = typeof size === 'number' ? size : item.size;
            size = clamp(size, item.minimumSize, item.maximumSize);
            item.size = size;
            const indexes = range(this.viewItems.length).filter((i) => i !== index);
            const lowPriorityIndexes = [
                ...indexes.filter((i) => this.viewItems[i].priority === exports.LayoutPriority.Low),
                index,
            ];
            const highPriorityIndexes = indexes.filter((i) => this.viewItems[i].priority === exports.LayoutPriority.High);
            /**
             * add this view we are changing to the low-index list since we have determined the size
             * here and don't want it changed
             */
            this.relayout([...lowPriorityIndexes, index], highPriorityIndexes);
        }
        addView(view, size = { type: 'distribute' }, index = this.viewItems.length, skipLayout) {
            const container = document.createElement('div');
            container.className = 'dv-view';
            container.appendChild(view.element);
            let viewSize;
            if (typeof size === 'number') {
                viewSize = size;
            }
            else if (size.type === 'split') {
                viewSize = this.getViewSize(size.index) / 2;
            }
            else if (size.type === 'invisible') {
                viewSize = { cachedVisibleSize: size.cachedVisibleSize };
            }
            else {
                viewSize = view.minimumSize;
            }
            const disposable = view.onDidChange((newSize) => this.onDidChange(viewItem, newSize.size));
            const viewItem = new ViewItem(container, view, viewSize, {
                dispose: () => {
                    disposable.dispose();
                    this.viewContainer.removeChild(container);
                },
            });
            if (index === this.viewItems.length) {
                this.viewContainer.appendChild(container);
            }
            else {
                this.viewContainer.insertBefore(container, this.viewContainer.children.item(index));
            }
            this.viewItems.splice(index, 0, viewItem);
            if (this.viewItems.length > 1) {
                //add sash
                const sash = document.createElement('div');
                sash.className = 'dv-sash';
                const onPointerStart = (event) => {
                    for (const item of this.viewItems) {
                        item.enabled = false;
                    }
                    const iframes = disableIframePointEvents();
                    const start = this._orientation === exports.Orientation.HORIZONTAL
                        ? event.clientX
                        : event.clientY;
                    const sashIndex = firstIndex(this.sashes, (s) => s.container === sash);
                    //
                    const sizes = this.viewItems.map((x) => x.size);
                    //
                    let snapBefore;
                    let snapAfter;
                    const upIndexes = range(sashIndex, -1);
                    const downIndexes = range(sashIndex + 1, this.viewItems.length);
                    const minDeltaUp = upIndexes.reduce((r, i) => r + (this.viewItems[i].minimumSize - sizes[i]), 0);
                    const maxDeltaUp = upIndexes.reduce((r, i) => r + (this.viewItems[i].viewMaximumSize - sizes[i]), 0);
                    const maxDeltaDown = downIndexes.length === 0
                        ? Number.POSITIVE_INFINITY
                        : downIndexes.reduce((r, i) => r +
                            (sizes[i] - this.viewItems[i].minimumSize), 0);
                    const minDeltaDown = downIndexes.length === 0
                        ? Number.NEGATIVE_INFINITY
                        : downIndexes.reduce((r, i) => r +
                            (sizes[i] -
                                this.viewItems[i].viewMaximumSize), 0);
                    const minDelta = Math.max(minDeltaUp, minDeltaDown);
                    const maxDelta = Math.min(maxDeltaDown, maxDeltaUp);
                    const snapBeforeIndex = this.findFirstSnapIndex(upIndexes);
                    const snapAfterIndex = this.findFirstSnapIndex(downIndexes);
                    if (typeof snapBeforeIndex === 'number') {
                        const snappedViewItem = this.viewItems[snapBeforeIndex];
                        const halfSize = Math.floor(snappedViewItem.viewMinimumSize / 2);
                        snapBefore = {
                            index: snapBeforeIndex,
                            limitDelta: snappedViewItem.visible
                                ? minDelta - halfSize
                                : minDelta + halfSize,
                            size: snappedViewItem.size,
                        };
                    }
                    if (typeof snapAfterIndex === 'number') {
                        const snappedViewItem = this.viewItems[snapAfterIndex];
                        const halfSize = Math.floor(snappedViewItem.viewMinimumSize / 2);
                        snapAfter = {
                            index: snapAfterIndex,
                            limitDelta: snappedViewItem.visible
                                ? maxDelta + halfSize
                                : maxDelta - halfSize,
                            size: snappedViewItem.size,
                        };
                    }
                    const onPointerMove = (event) => {
                        const current = this._orientation === exports.Orientation.HORIZONTAL
                            ? event.clientX
                            : event.clientY;
                        const delta = current - start;
                        this.resize(sashIndex, delta, sizes, undefined, undefined, minDelta, maxDelta, snapBefore, snapAfter);
                        this.distributeEmptySpace();
                        this.layoutViews();
                    };
                    const end = () => {
                        for (const item of this.viewItems) {
                            item.enabled = true;
                        }
                        iframes.release();
                        this.saveProportions();
                        document.removeEventListener('pointermove', onPointerMove);
                        document.removeEventListener('pointerup', end);
                        document.removeEventListener('pointercancel', end);
                        this._onDidSashEnd.fire(undefined);
                    };
                    document.addEventListener('pointermove', onPointerMove);
                    document.addEventListener('pointerup', end);
                    document.addEventListener('pointercancel', end);
                };
                sash.addEventListener('pointerdown', onPointerStart);
                const sashItem = {
                    container: sash,
                    disposable: () => {
                        sash.removeEventListener('pointerdown', onPointerStart);
                        this.sashContainer.removeChild(sash);
                    },
                };
                this.sashContainer.appendChild(sash);
                this.sashes.push(sashItem);
            }
            if (!skipLayout) {
                this.relayout([index]);
            }
            if (!skipLayout &&
                typeof size !== 'number' &&
                size.type === 'distribute') {
                this.distributeViewSizes();
            }
            this._onDidAddView.fire(view);
        }
        distributeViewSizes() {
            const flexibleViewItems = [];
            let flexibleSize = 0;
            for (const item of this.viewItems) {
                if (item.maximumSize - item.minimumSize > 0) {
                    flexibleViewItems.push(item);
                    flexibleSize += item.size;
                }
            }
            const size = Math.floor(flexibleSize / flexibleViewItems.length);
            for (const item of flexibleViewItems) {
                item.size = clamp(size, item.minimumSize, item.maximumSize);
            }
            const indexes = range(this.viewItems.length);
            const lowPriorityIndexes = indexes.filter((i) => this.viewItems[i].priority === exports.LayoutPriority.Low);
            const highPriorityIndexes = indexes.filter((i) => this.viewItems[i].priority === exports.LayoutPriority.High);
            this.relayout(lowPriorityIndexes, highPriorityIndexes);
        }
        removeView(index, sizing, skipLayout = false) {
            // Remove view
            const viewItem = this.viewItems.splice(index, 1)[0];
            viewItem.dispose();
            // Remove sash
            if (this.viewItems.length >= 1) {
                const sashIndex = Math.max(index - 1, 0);
                const sashItem = this.sashes.splice(sashIndex, 1)[0];
                sashItem.disposable();
            }
            if (!skipLayout) {
                this.relayout();
            }
            if (sizing && sizing.type === 'distribute') {
                this.distributeViewSizes();
            }
            this._onDidRemoveView.fire(viewItem.view);
            return viewItem.view;
        }
        getViewCachedVisibleSize(index) {
            if (index < 0 || index >= this.viewItems.length) {
                throw new Error('Index out of bounds');
            }
            const viewItem = this.viewItems[index];
            return viewItem.cachedVisibleSize;
        }
        moveView(from, to) {
            const cachedVisibleSize = this.getViewCachedVisibleSize(from);
            const sizing = typeof cachedVisibleSize === 'undefined'
                ? this.getViewSize(from)
                : exports.Sizing.Invisible(cachedVisibleSize);
            const view = this.removeView(from, undefined, true);
            this.addView(view, sizing, to);
        }
        layout(size, orthogonalSize) {
            const previousSize = Math.max(this.size, this._contentSize);
            this.size = size;
            this.orthogonalSize = orthogonalSize;
            if (!this.proportions) {
                const indexes = range(this.viewItems.length);
                const lowPriorityIndexes = indexes.filter((i) => this.viewItems[i].priority === exports.LayoutPriority.Low);
                const highPriorityIndexes = indexes.filter((i) => this.viewItems[i].priority === exports.LayoutPriority.High);
                this.resize(this.viewItems.length - 1, size - previousSize, undefined, lowPriorityIndexes, highPriorityIndexes);
            }
            else {
                let total = 0;
                for (let i = 0; i < this.viewItems.length; i++) {
                    const item = this.viewItems[i];
                    const proportion = this.proportions[i];
                    if (typeof proportion === 'number') {
                        total += proportion;
                    }
                    else {
                        size -= item.size;
                    }
                }
                for (let i = 0; i < this.viewItems.length; i++) {
                    const item = this.viewItems[i];
                    const proportion = this.proportions[i];
                    if (typeof proportion === 'number' && total > 0) {
                        item.size = clamp(Math.round((proportion * size) / total), item.minimumSize, item.maximumSize);
                    }
                }
            }
            this.distributeEmptySpace();
            this.layoutViews();
        }
        relayout(lowPriorityIndexes, highPriorityIndexes) {
            const contentSize = this.viewItems.reduce((r, i) => r + i.size, 0);
            this.resize(this.viewItems.length - 1, this._size - contentSize, undefined, lowPriorityIndexes, highPriorityIndexes);
            this.distributeEmptySpace();
            this.layoutViews();
            this.saveProportions();
        }
        distributeEmptySpace(lowPriorityIndex) {
            const contentSize = this.viewItems.reduce((r, i) => r + i.size, 0);
            let emptyDelta = this.size - contentSize;
            const indexes = range(this.viewItems.length - 1, -1);
            const lowPriorityIndexes = indexes.filter((i) => this.viewItems[i].priority === exports.LayoutPriority.Low);
            const highPriorityIndexes = indexes.filter((i) => this.viewItems[i].priority === exports.LayoutPriority.High);
            for (const index of highPriorityIndexes) {
                pushToStart(indexes, index);
            }
            for (const index of lowPriorityIndexes) {
                pushToEnd(indexes, index);
            }
            if (typeof lowPriorityIndex === 'number') {
                pushToEnd(indexes, lowPriorityIndex);
            }
            for (let i = 0; emptyDelta !== 0 && i < indexes.length; i++) {
                const item = this.viewItems[indexes[i]];
                const size = clamp(item.size + emptyDelta, item.minimumSize, item.maximumSize);
                const viewDelta = size - item.size;
                emptyDelta -= viewDelta;
                item.size = size;
            }
        }
        saveProportions() {
            if (this.proportionalLayout && this._contentSize > 0) {
                this._proportions = this.viewItems.map((i) => i.visible ? i.size / this._contentSize : undefined);
            }
        }
        /**
         * Margin explain:
         *
         * For `n` views in a splitview there will be `n-1` margins `m`.
         *
         * To fit the margins each view must reduce in size by `(m * (n - 1)) / n`.
         *
         * For each view `i` the offet must be adjusted by `m * i/(n - 1)`.
         */
        layoutViews() {
            this._contentSize = this.viewItems.reduce((r, i) => r + i.size, 0);
            this.updateSashEnablement();
            if (this.viewItems.length === 0) {
                return;
            }
            const visibleViewItems = this.viewItems.filter((i) => i.visible);
            const sashCount = Math.max(0, visibleViewItems.length - 1);
            const marginReducedSize = (this.margin * sashCount) / Math.max(1, visibleViewItems.length);
            let totalLeftOffset = 0;
            const viewLeftOffsets = [];
            const sashWidth = 4; // hardcoded in css
            const runningVisiblePanelCount = this.viewItems.reduce((arr, viewItem, i) => {
                const flag = viewItem.visible ? 1 : 0;
                if (i === 0) {
                    arr.push(flag);
                }
                else {
                    arr.push(arr[i - 1] + flag);
                }
                return arr;
            }, []);
            // calculate both view and cash positions
            this.viewItems.forEach((view, i) => {
                totalLeftOffset += this.viewItems[i].size;
                viewLeftOffsets.push(totalLeftOffset);
                const size = view.visible ? view.size - marginReducedSize : 0;
                const visiblePanelsBeforeThisView = Math.max(0, runningVisiblePanelCount[i] - 1);
                const offset = i === 0 || visiblePanelsBeforeThisView === 0
                    ? 0
                    : viewLeftOffsets[i - 1] +
                        (visiblePanelsBeforeThisView / sashCount) *
                            marginReducedSize;
                if (i < this.viewItems.length - 1) {
                    // calculate sash position
                    const newSize = view.visible
                        ? offset + size - sashWidth / 2 + this.margin / 2
                        : offset;
                    if (this._orientation === exports.Orientation.HORIZONTAL) {
                        this.sashes[i].container.style.left = `${newSize}px`;
                        this.sashes[i].container.style.top = `0px`;
                    }
                    if (this._orientation === exports.Orientation.VERTICAL) {
                        this.sashes[i].container.style.left = `0px`;
                        this.sashes[i].container.style.top = `${newSize}px`;
                    }
                }
                // calculate view position
                if (this._orientation === exports.Orientation.HORIZONTAL) {
                    view.container.style.width = `${size}px`;
                    view.container.style.left = `${offset}px`;
                    view.container.style.top = '';
                    view.container.style.height = '';
                }
                if (this._orientation === exports.Orientation.VERTICAL) {
                    view.container.style.height = `${size}px`;
                    view.container.style.top = `${offset}px`;
                    view.container.style.width = '';
                    view.container.style.left = '';
                }
                view.view.layout(view.size - marginReducedSize, this._orthogonalSize);
            });
        }
        findFirstSnapIndex(indexes) {
            // visible views first
            for (const index of indexes) {
                const viewItem = this.viewItems[index];
                if (!viewItem.visible) {
                    continue;
                }
                if (viewItem.snap) {
                    return index;
                }
            }
            // then, hidden views
            for (const index of indexes) {
                const viewItem = this.viewItems[index];
                if (viewItem.visible &&
                    viewItem.maximumSize - viewItem.minimumSize > 0) {
                    return undefined;
                }
                if (!viewItem.visible && viewItem.snap) {
                    return index;
                }
            }
            return undefined;
        }
        updateSashEnablement() {
            let previous = false;
            const collapsesDown = this.viewItems.map((i) => (previous = i.size - i.minimumSize > 0 || previous));
            previous = false;
            const expandsDown = this.viewItems.map((i) => (previous = i.maximumSize - i.size > 0 || previous));
            const reverseViews = [...this.viewItems].reverse();
            previous = false;
            const collapsesUp = reverseViews
                .map((i) => (previous = i.size - i.minimumSize > 0 || previous))
                .reverse();
            previous = false;
            const expandsUp = reverseViews
                .map((i) => (previous = i.maximumSize - i.size > 0 || previous))
                .reverse();
            let position = 0;
            for (let index = 0; index < this.sashes.length; index++) {
                const sash = this.sashes[index];
                const viewItem = this.viewItems[index];
                position += viewItem.size;
                const min = !(collapsesDown[index] && expandsUp[index + 1]);
                const max = !(expandsDown[index] && collapsesUp[index + 1]);
                if (min && max) {
                    const upIndexes = range(index, -1);
                    const downIndexes = range(index + 1, this.viewItems.length);
                    const snapBeforeIndex = this.findFirstSnapIndex(upIndexes);
                    const snapAfterIndex = this.findFirstSnapIndex(downIndexes);
                    const snappedBefore = typeof snapBeforeIndex === 'number' &&
                        !this.viewItems[snapBeforeIndex].visible;
                    const snappedAfter = typeof snapAfterIndex === 'number' &&
                        !this.viewItems[snapAfterIndex].visible;
                    if (snappedBefore &&
                        collapsesUp[index] &&
                        (position > 0 || this.startSnappingEnabled)) {
                        this.updateSash(sash, exports.SashState.MINIMUM);
                    }
                    else if (snappedAfter &&
                        collapsesDown[index] &&
                        (position < this._contentSize || this.endSnappingEnabled)) {
                        this.updateSash(sash, exports.SashState.MAXIMUM);
                    }
                    else {
                        this.updateSash(sash, exports.SashState.DISABLED);
                    }
                }
                else if (min && !max) {
                    this.updateSash(sash, exports.SashState.MINIMUM);
                }
                else if (!min && max) {
                    this.updateSash(sash, exports.SashState.MAXIMUM);
                }
                else {
                    this.updateSash(sash, exports.SashState.ENABLED);
                }
            }
        }
        updateSash(sash, state) {
            toggleClass(sash.container, 'dv-disabled', state === exports.SashState.DISABLED);
            toggleClass(sash.container, 'dv-enabled', state === exports.SashState.ENABLED);
            toggleClass(sash.container, 'dv-maximum', state === exports.SashState.MAXIMUM);
            toggleClass(sash.container, 'dv-minimum', state === exports.SashState.MINIMUM);
        }
        createViewContainer() {
            const element = document.createElement('div');
            element.className = 'dv-view-container';
            return element;
        }
        createSashContainer() {
            const element = document.createElement('div');
            element.className = 'dv-sash-container';
            return element;
        }
        createContainer() {
            const element = document.createElement('div');
            const orientationClassname = this._orientation === exports.Orientation.HORIZONTAL
                ? 'dv-horizontal'
                : 'dv-vertical';
            element.className = `dv-split-view-container ${orientationClassname}`;
            return element;
        }
        dispose() {
            this._onDidSashEnd.dispose();
            this._onDidAddView.dispose();
            this._onDidRemoveView.dispose();
            for (let i = 0; i < this.element.children.length; i++) {
                if (this.element.children.item(i) === this.element) {
                    this.element.removeChild(this.element);
                    break;
                }
            }
            for (const viewItem of this.viewItems) {
                viewItem.dispose();
            }
            this.element.remove();
        }
    }

    const PROPERTY_KEYS_SPLITVIEW = (() => {
        /**
         * by readong the keys from an empty value object TypeScript will error
         * when we add or remove new properties to `DockviewOptions`
         */
        const properties = {
            orientation: undefined,
            descriptor: undefined,
            proportionalLayout: undefined,
            styles: undefined,
            margin: undefined,
            disableAutoResizing: undefined,
            className: undefined,
        };
        return Object.keys(properties);
    })();

    class Paneview extends CompositeDisposable {
        get onDidAddView() {
            return this.splitview.onDidAddView;
        }
        get onDidRemoveView() {
            return this.splitview.onDidRemoveView;
        }
        get minimumSize() {
            return this.splitview.minimumSize;
        }
        get maximumSize() {
            return this.splitview.maximumSize;
        }
        get orientation() {
            return this.splitview.orientation;
        }
        get size() {
            return this.splitview.size;
        }
        get orthogonalSize() {
            return this.splitview.orthogonalSize;
        }
        constructor(container, options) {
            var _a;
            super();
            this.paneItems = [];
            this.skipAnimation = false;
            this._onDidChange = new Emitter();
            this.onDidChange = this._onDidChange.event;
            this._orientation = (_a = options.orientation) !== null && _a !== void 0 ? _a : exports.Orientation.VERTICAL;
            this.element = document.createElement('div');
            this.element.className = 'dv-pane-container';
            container.appendChild(this.element);
            this.splitview = new Splitview(this.element, {
                orientation: this._orientation,
                proportionalLayout: false,
                descriptor: options.descriptor,
            });
            // if we've added views from the descriptor we need to
            // add the panes to our Pane array and setup animation
            this.getPanes().forEach((pane) => {
                const disposable = new CompositeDisposable(pane.onDidChangeExpansionState(() => {
                    this.setupAnimation();
                    this._onDidChange.fire(undefined);
                }));
                const paneItem = {
                    pane,
                    disposable: {
                        dispose: () => {
                            disposable.dispose();
                        },
                    },
                };
                this.paneItems.push(paneItem);
                pane.orthogonalSize = this.splitview.orthogonalSize;
            });
            this.addDisposables(this._onDidChange, this.splitview.onDidSashEnd(() => {
                this._onDidChange.fire(undefined);
            }), this.splitview.onDidAddView(() => {
                this._onDidChange.fire();
            }), this.splitview.onDidRemoveView(() => {
                this._onDidChange.fire();
            }));
        }
        setViewVisible(index, visible) {
            this.splitview.setViewVisible(index, visible);
        }
        addPane(pane, size, index = this.splitview.length, skipLayout = false) {
            const disposable = pane.onDidChangeExpansionState(() => {
                this.setupAnimation();
                this._onDidChange.fire(undefined);
            });
            const paneItem = {
                pane,
                disposable: {
                    dispose: () => {
                        disposable.dispose();
                    },
                },
            };
            this.paneItems.splice(index, 0, paneItem);
            pane.orthogonalSize = this.splitview.orthogonalSize;
            this.splitview.addView(pane, size, index, skipLayout);
        }
        getViewSize(index) {
            return this.splitview.getViewSize(index);
        }
        getPanes() {
            return this.splitview.getViews();
        }
        removePane(index, options = { skipDispose: false }) {
            const paneItem = this.paneItems.splice(index, 1)[0];
            this.splitview.removeView(index);
            if (!options.skipDispose) {
                paneItem.disposable.dispose();
                paneItem.pane.dispose();
            }
            return paneItem;
        }
        moveView(from, to) {
            if (from === to) {
                return;
            }
            const view = this.removePane(from, { skipDispose: true });
            this.skipAnimation = true;
            try {
                this.addPane(view.pane, view.pane.size, to, false);
            }
            finally {
                this.skipAnimation = false;
            }
        }
        layout(size, orthogonalSize) {
            this.splitview.layout(size, orthogonalSize);
        }
        setupAnimation() {
            if (this.skipAnimation) {
                return;
            }
            if (this.animationTimer) {
                clearTimeout(this.animationTimer);
                this.animationTimer = undefined;
            }
            addClasses(this.element, 'dv-animated');
            this.animationTimer = setTimeout(() => {
                this.animationTimer = undefined;
                removeClasses(this.element, 'dv-animated');
            }, 200);
        }
        dispose() {
            super.dispose();
            if (this.animationTimer) {
                clearTimeout(this.animationTimer);
                this.animationTimer = undefined;
            }
            this.paneItems.forEach((paneItem) => {
                paneItem.disposable.dispose();
                paneItem.pane.dispose();
            });
            this.paneItems = [];
            this.splitview.dispose();
            this.element.remove();
        }
    }

    /*---------------------------------------------------------------------------------------------
     * Accreditation: This file is largly based upon the MIT licenced VSCode sourcecode found at:
     * https://github.com/microsoft/vscode/tree/main/src/vs/base/browser/ui/grid
     *--------------------------------------------------------------------------------------------*/
    class LeafNode {
        get minimumWidth() {
            return this.view.minimumWidth;
        }
        get maximumWidth() {
            return this.view.maximumWidth;
        }
        get minimumHeight() {
            return this.view.minimumHeight;
        }
        get maximumHeight() {
            return this.view.maximumHeight;
        }
        get priority() {
            return this.view.priority;
        }
        get snap() {
            return this.view.snap;
        }
        get minimumSize() {
            return this.orientation === exports.Orientation.HORIZONTAL
                ? this.minimumHeight
                : this.minimumWidth;
        }
        get maximumSize() {
            return this.orientation === exports.Orientation.HORIZONTAL
                ? this.maximumHeight
                : this.maximumWidth;
        }
        get minimumOrthogonalSize() {
            return this.orientation === exports.Orientation.HORIZONTAL
                ? this.minimumWidth
                : this.minimumHeight;
        }
        get maximumOrthogonalSize() {
            return this.orientation === exports.Orientation.HORIZONTAL
                ? this.maximumWidth
                : this.maximumHeight;
        }
        get orthogonalSize() {
            return this._orthogonalSize;
        }
        get size() {
            return this._size;
        }
        get element() {
            return this.view.element;
        }
        get width() {
            return this.orientation === exports.Orientation.HORIZONTAL
                ? this.orthogonalSize
                : this.size;
        }
        get height() {
            return this.orientation === exports.Orientation.HORIZONTAL
                ? this.size
                : this.orthogonalSize;
        }
        constructor(view, orientation, orthogonalSize, size = 0) {
            this.view = view;
            this.orientation = orientation;
            this._onDidChange = new Emitter();
            this.onDidChange = this._onDidChange.event;
            this._orthogonalSize = orthogonalSize;
            this._size = size;
            this._disposable = this.view.onDidChange((event) => {
                if (event) {
                    this._onDidChange.fire({
                        size: this.orientation === exports.Orientation.VERTICAL
                            ? event.width
                            : event.height,
                        orthogonalSize: this.orientation === exports.Orientation.VERTICAL
                            ? event.height
                            : event.width,
                    });
                }
                else {
                    this._onDidChange.fire({});
                }
            });
        }
        setVisible(visible) {
            if (this.view.setVisible) {
                this.view.setVisible(visible);
            }
        }
        layout(size, orthogonalSize) {
            this._size = size;
            this._orthogonalSize = orthogonalSize;
            this.view.layout(this.width, this.height);
        }
        dispose() {
            this._onDidChange.dispose();
            this._disposable.dispose();
        }
    }

    /*---------------------------------------------------------------------------------------------
     * Accreditation: This file is largly based upon the MIT licenced VSCode sourcecode found at:
     * https://github.com/microsoft/vscode/tree/main/src/vs/base/browser/ui/grid
     *--------------------------------------------------------------------------------------------*/
    class BranchNode extends CompositeDisposable {
        get width() {
            return this.orientation === exports.Orientation.HORIZONTAL
                ? this.size
                : this.orthogonalSize;
        }
        get height() {
            return this.orientation === exports.Orientation.HORIZONTAL
                ? this.orthogonalSize
                : this.size;
        }
        get minimumSize() {
            return this.children.length === 0
                ? 0
                : Math.max(...this.children.map((c, index) => this.splitview.isViewVisible(index)
                    ? c.minimumOrthogonalSize
                    : 0));
        }
        get maximumSize() {
            return Math.min(...this.children.map((c, index) => this.splitview.isViewVisible(index)
                ? c.maximumOrthogonalSize
                : Number.POSITIVE_INFINITY));
        }
        get minimumOrthogonalSize() {
            return this.splitview.minimumSize;
        }
        get maximumOrthogonalSize() {
            return this.splitview.maximumSize;
        }
        get orthogonalSize() {
            return this._orthogonalSize;
        }
        get size() {
            return this._size;
        }
        get minimumWidth() {
            return this.orientation === exports.Orientation.HORIZONTAL
                ? this.minimumOrthogonalSize
                : this.minimumSize;
        }
        get minimumHeight() {
            return this.orientation === exports.Orientation.HORIZONTAL
                ? this.minimumSize
                : this.minimumOrthogonalSize;
        }
        get maximumWidth() {
            return this.orientation === exports.Orientation.HORIZONTAL
                ? this.maximumOrthogonalSize
                : this.maximumSize;
        }
        get maximumHeight() {
            return this.orientation === exports.Orientation.HORIZONTAL
                ? this.maximumSize
                : this.maximumOrthogonalSize;
        }
        get priority() {
            if (this.children.length === 0) {
                return exports.LayoutPriority.Normal;
            }
            const priorities = this.children.map((c) => typeof c.priority === 'undefined'
                ? exports.LayoutPriority.Normal
                : c.priority);
            if (priorities.some((p) => p === exports.LayoutPriority.High)) {
                return exports.LayoutPriority.High;
            }
            else if (priorities.some((p) => p === exports.LayoutPriority.Low)) {
                return exports.LayoutPriority.Low;
            }
            return exports.LayoutPriority.Normal;
        }
        get disabled() {
            return this.splitview.disabled;
        }
        set disabled(value) {
            this.splitview.disabled = value;
        }
        get margin() {
            return this.splitview.margin;
        }
        set margin(value) {
            this.splitview.margin = value;
            this.children.forEach((child) => {
                if (child instanceof BranchNode) {
                    child.margin = value;
                }
            });
        }
        constructor(orientation, proportionalLayout, styles, size, orthogonalSize, disabled, margin, childDescriptors) {
            super();
            this.orientation = orientation;
            this.proportionalLayout = proportionalLayout;
            this.styles = styles;
            this._childrenDisposable = exports.DockviewDisposable.NONE;
            this.children = [];
            this._onDidChange = new Emitter();
            this.onDidChange = this._onDidChange.event;
            this._onDidVisibilityChange = new Emitter();
            this.onDidVisibilityChange = this._onDidVisibilityChange.event;
            this._orthogonalSize = orthogonalSize;
            this._size = size;
            this.element = document.createElement('div');
            this.element.className = 'dv-branch-node';
            if (!childDescriptors) {
                this.splitview = new Splitview(this.element, {
                    orientation: this.orientation,
                    proportionalLayout,
                    styles,
                    margin,
                });
                this.splitview.layout(this.size, this.orthogonalSize);
            }
            else {
                const descriptor = {
                    views: childDescriptors.map((childDescriptor) => {
                        return {
                            view: childDescriptor.node,
                            size: childDescriptor.node.size,
                            visible: childDescriptor.node instanceof LeafNode &&
                                childDescriptor.visible !== undefined
                                ? childDescriptor.visible
                                : true,
                        };
                    }),
                    size: this.orthogonalSize,
                };
                this.children = childDescriptors.map((c) => c.node);
                this.splitview = new Splitview(this.element, {
                    orientation: this.orientation,
                    descriptor,
                    proportionalLayout,
                    styles,
                    margin,
                });
            }
            this.disabled = disabled;
            this.addDisposables(this._onDidChange, this._onDidVisibilityChange, this.splitview.onDidSashEnd(() => {
                this._onDidChange.fire({});
            }));
            this.setupChildrenEvents();
        }
        setVisible(_visible) {
            // noop
        }
        isChildVisible(index) {
            if (index < 0 || index >= this.children.length) {
                throw new Error('Invalid index');
            }
            return this.splitview.isViewVisible(index);
        }
        setChildVisible(index, visible) {
            if (index < 0 || index >= this.children.length) {
                throw new Error('Invalid index');
            }
            if (this.splitview.isViewVisible(index) === visible) {
                return;
            }
            const wereAllChildrenHidden = this.splitview.contentSize === 0;
            this.splitview.setViewVisible(index, visible);
            // }
            const areAllChildrenHidden = this.splitview.contentSize === 0;
            // If all children are hidden then the parent should hide the entire splitview
            // If the entire splitview is hidden then the parent should show the splitview when a child is shown
            if ((visible && wereAllChildrenHidden) ||
                (!visible && areAllChildrenHidden)) {
                this._onDidVisibilityChange.fire({ visible });
            }
        }
        moveChild(from, to) {
            if (from === to) {
                return;
            }
            if (from < 0 || from >= this.children.length) {
                throw new Error('Invalid from index');
            }
            if (from < to) {
                to--;
            }
            this.splitview.moveView(from, to);
            const child = this._removeChild(from);
            this._addChild(child, to);
        }
        getChildSize(index) {
            if (index < 0 || index >= this.children.length) {
                throw new Error('Invalid index');
            }
            return this.splitview.getViewSize(index);
        }
        resizeChild(index, size) {
            if (index < 0 || index >= this.children.length) {
                throw new Error('Invalid index');
            }
            this.splitview.resizeView(index, size);
        }
        layout(size, orthogonalSize) {
            this._size = orthogonalSize;
            this._orthogonalSize = size;
            this.splitview.layout(orthogonalSize, size);
        }
        addChild(node, size, index, skipLayout) {
            if (index < 0 || index > this.children.length) {
                throw new Error('Invalid index');
            }
            this.splitview.addView(node, size, index, skipLayout);
            this._addChild(node, index);
        }
        getChildCachedVisibleSize(index) {
            if (index < 0 || index >= this.children.length) {
                throw new Error('Invalid index');
            }
            return this.splitview.getViewCachedVisibleSize(index);
        }
        removeChild(index, sizing) {
            if (index < 0 || index >= this.children.length) {
                throw new Error('Invalid index');
            }
            this.splitview.removeView(index, sizing);
            return this._removeChild(index);
        }
        _addChild(node, index) {
            this.children.splice(index, 0, node);
            this.setupChildrenEvents();
        }
        _removeChild(index) {
            const [child] = this.children.splice(index, 1);
            this.setupChildrenEvents();
            return child;
        }
        setupChildrenEvents() {
            this._childrenDisposable.dispose();
            this._childrenDisposable = new CompositeDisposable(exports.DockviewEvent.any(...this.children.map((c) => c.onDidChange))((e) => {
                /**
                 * indicate a change has occured to allows any re-rendering but don't bubble
                 * event because that was specific to this branch
                 */
                this._onDidChange.fire({ size: e.orthogonalSize });
            }), ...this.children.map((c, i) => {
                if (c instanceof BranchNode) {
                    return c.onDidVisibilityChange(({ visible }) => {
                        this.setChildVisible(i, visible);
                    });
                }
                return exports.DockviewDisposable.NONE;
            }));
        }
        dispose() {
            this._childrenDisposable.dispose();
            this.splitview.dispose();
            this.children.forEach((child) => child.dispose());
            super.dispose();
        }
    }

    /*---------------------------------------------------------------------------------------------
     * Accreditation: This file is largly based upon the MIT licenced VSCode sourcecode found at:
     * https://github.com/microsoft/vscode/tree/main/src/vs/base/browser/ui/grid
     *--------------------------------------------------------------------------------------------*/
    function findLeaf(candiateNode, last) {
        if (candiateNode instanceof LeafNode) {
            return candiateNode;
        }
        if (candiateNode instanceof BranchNode) {
            return findLeaf(candiateNode.children[last ? candiateNode.children.length - 1 : 0], last);
        }
        throw new Error('invalid node');
    }
    function cloneNode(node, size, orthogonalSize) {
        if (node instanceof BranchNode) {
            const result = new BranchNode(node.orientation, node.proportionalLayout, node.styles, size, orthogonalSize, node.disabled, node.margin);
            for (let i = node.children.length - 1; i >= 0; i--) {
                const child = node.children[i];
                result.addChild(cloneNode(child, child.size, child.orthogonalSize), child.size, 0, true);
            }
            return result;
        }
        else {
            return new LeafNode(node.view, node.orientation, orthogonalSize);
        }
    }
    function flipNode(node, size, orthogonalSize) {
        if (node instanceof BranchNode) {
            const result = new BranchNode(orthogonal(node.orientation), node.proportionalLayout, node.styles, size, orthogonalSize, node.disabled, node.margin);
            let totalSize = 0;
            for (let i = node.children.length - 1; i >= 0; i--) {
                const child = node.children[i];
                const childSize = child instanceof BranchNode ? child.orthogonalSize : child.size;
                let newSize = node.size === 0
                    ? 0
                    : Math.round((size * childSize) / node.size);
                totalSize += newSize;
                // The last view to add should adjust to rounding errors
                if (i === 0) {
                    newSize += size - totalSize;
                }
                result.addChild(flipNode(child, orthogonalSize, newSize), newSize, 0, true);
            }
            return result;
        }
        else {
            return new LeafNode(node.view, orthogonal(node.orientation), orthogonalSize);
        }
    }
    function indexInParent(element) {
        const parentElement = element.parentElement;
        if (!parentElement) {
            throw new Error('Invalid grid element');
        }
        let el = parentElement.firstElementChild;
        let index = 0;
        while (el !== element && el !== parentElement.lastElementChild && el) {
            el = el.nextElementSibling;
            index++;
        }
        return index;
    }
    /**
     * Find the grid location of a specific DOM element by traversing the parent
     * chain and finding each child index on the way.
     *
     * This will break as soon as DOM structures of the Splitview or Gridview change.
     */
    function getGridLocation(element) {
        const parentElement = element.parentElement;
        if (!parentElement) {
            throw new Error('Invalid grid element');
        }
        if (/\bdv-grid-view\b/.test(parentElement.className)) {
            return [];
        }
        const index = indexInParent(parentElement);
        const ancestor = parentElement.parentElement.parentElement.parentElement;
        return [...getGridLocation(ancestor), index];
    }
    function getRelativeLocation(rootOrientation, location, direction) {
        const orientation = getLocationOrientation(rootOrientation, location);
        const directionOrientation = getDirectionOrientation(direction);
        if (orientation === directionOrientation) {
            const [rest, _index] = tail(location);
            let index = _index;
            if (direction === 'right' || direction === 'bottom') {
                index += 1;
            }
            return [...rest, index];
        }
        else {
            const index = direction === 'right' || direction === 'bottom' ? 1 : 0;
            return [...location, index];
        }
    }
    function getDirectionOrientation(direction) {
        return direction === 'top' || direction === 'bottom'
            ? exports.Orientation.VERTICAL
            : exports.Orientation.HORIZONTAL;
    }
    function getLocationOrientation(rootOrientation, location) {
        return location.length % 2 === 0
            ? orthogonal(rootOrientation)
            : rootOrientation;
    }
    const orthogonal = (orientation) => orientation === exports.Orientation.HORIZONTAL
        ? exports.Orientation.VERTICAL
        : exports.Orientation.HORIZONTAL;
    function isGridBranchNode(node) {
        return !!node.children;
    }
    const serializeBranchNode = (node, orientation) => {
        const size = orientation === exports.Orientation.VERTICAL ? node.box.width : node.box.height;
        if (!isGridBranchNode(node)) {
            if (typeof node.cachedVisibleSize === 'number') {
                return {
                    type: 'leaf',
                    data: node.view.toJSON(),
                    size: node.cachedVisibleSize,
                    visible: false,
                };
            }
            return { type: 'leaf', data: node.view.toJSON(), size };
        }
        return {
            type: 'branch',
            data: node.children.map((c) => serializeBranchNode(c, orthogonal(orientation))),
            size,
        };
    };
    class Gridview {
        get length() {
            return this._root ? this._root.children.length : 0;
        }
        get orientation() {
            return this.root.orientation;
        }
        set orientation(orientation) {
            if (this.root.orientation === orientation) {
                return;
            }
            const { size, orthogonalSize } = this.root;
            this.root = flipNode(this.root, orthogonalSize, size);
            this.root.layout(size, orthogonalSize);
        }
        get width() {
            return this.root.width;
        }
        get height() {
            return this.root.height;
        }
        get minimumWidth() {
            return this.root.minimumWidth;
        }
        get minimumHeight() {
            return this.root.minimumHeight;
        }
        get maximumWidth() {
            return this.root.maximumHeight;
        }
        get maximumHeight() {
            return this.root.maximumHeight;
        }
        get locked() {
            return this._locked;
        }
        set locked(value) {
            this._locked = value;
            const branch = [this.root];
            /**
             * simple depth-first-search to cover all nodes
             *
             * @see https://en.wikipedia.org/wiki/Depth-first_search
             */
            while (branch.length > 0) {
                const node = branch.pop();
                if (node instanceof BranchNode) {
                    node.disabled = value;
                    branch.push(...node.children);
                }
            }
        }
        get margin() {
            return this._margin;
        }
        set margin(value) {
            this._margin = value;
            this.root.margin = value;
        }
        maximizedView() {
            var _a;
            return (_a = this._maximizedNode) === null || _a === void 0 ? void 0 : _a.leaf.view;
        }
        hasMaximizedView() {
            return this._maximizedNode !== undefined;
        }
        maximizeView(view) {
            var _a;
            const location = getGridLocation(view.element);
            const [_, node] = this.getNode(location);
            if (!(node instanceof LeafNode)) {
                return;
            }
            if (((_a = this._maximizedNode) === null || _a === void 0 ? void 0 : _a.leaf) === node) {
                return;
            }
            if (this.hasMaximizedView()) {
                this.exitMaximizedView();
            }
            serializeBranchNode(this.getView(), this.orientation);
            const hiddenOnMaximize = [];
            function hideAllViewsBut(parent, exclude) {
                for (let i = 0; i < parent.children.length; i++) {
                    const child = parent.children[i];
                    if (child instanceof LeafNode) {
                        if (child !== exclude) {
                            if (parent.isChildVisible(i)) {
                                parent.setChildVisible(i, false);
                            }
                            else {
                                hiddenOnMaximize.push(child);
                            }
                        }
                    }
                    else {
                        hideAllViewsBut(child, exclude);
                    }
                }
            }
            hideAllViewsBut(this.root, node);
            this._maximizedNode = { leaf: node, hiddenOnMaximize };
            this._onDidMaximizedNodeChange.fire({
                view: node.view,
                isMaximized: true,
            });
        }
        exitMaximizedView() {
            if (!this._maximizedNode) {
                return;
            }
            const hiddenOnMaximize = this._maximizedNode.hiddenOnMaximize;
            function showViewsInReverseOrder(parent) {
                for (let index = parent.children.length - 1; index >= 0; index--) {
                    const child = parent.children[index];
                    if (child instanceof LeafNode) {
                        if (!hiddenOnMaximize.includes(child)) {
                            parent.setChildVisible(index, true);
                        }
                    }
                    else {
                        showViewsInReverseOrder(child);
                    }
                }
            }
            showViewsInReverseOrder(this.root);
            const tmp = this._maximizedNode.leaf;
            this._maximizedNode = undefined;
            this._onDidMaximizedNodeChange.fire({
                view: tmp.view,
                isMaximized: false,
            });
        }
        serialize() {
            const maximizedView = this.maximizedView();
            let maxmizedViewLocation;
            if (maximizedView) {
                /**
                 * The minimum information we can get away with in order to serialize a maxmized view is it's location within the grid
                 * which is represented as a branch of indices
                 */
                maxmizedViewLocation = getGridLocation(maximizedView.element);
            }
            if (this.hasMaximizedView()) {
                /**
                 * the saved layout cannot be in its maxmized state otherwise all of the underlying
                 * view dimensions will be wrong
                 *
                 * To counteract this we temporaily remove the maximized view to compute the serialized output
                 * of the grid before adding back the maxmized view as to not alter the layout from the users
                 * perspective when `.toJSON()` is called
                 */
                this.exitMaximizedView();
            }
            const root = serializeBranchNode(this.getView(), this.orientation);
            const resullt = {
                root,
                width: this.width,
                height: this.height,
                orientation: this.orientation,
            };
            if (maxmizedViewLocation) {
                resullt.maximizedNode = {
                    location: maxmizedViewLocation,
                };
            }
            if (maximizedView) {
                // replace any maximzied view that was removed for serialization purposes
                this.maximizeView(maximizedView);
            }
            return resullt;
        }
        dispose() {
            this.disposable.dispose();
            this._onDidChange.dispose();
            this._onDidMaximizedNodeChange.dispose();
            this._onDidViewVisibilityChange.dispose();
            this.root.dispose();
            this._maximizedNode = undefined;
            this.element.remove();
        }
        clear() {
            const orientation = this.root.orientation;
            this.root = new BranchNode(orientation, this.proportionalLayout, this.styles, this.root.size, this.root.orthogonalSize, this.locked, this.margin);
        }
        deserialize(json, deserializer) {
            const orientation = json.orientation;
            const height = orientation === exports.Orientation.VERTICAL ? json.height : json.width;
            this._deserialize(json.root, orientation, deserializer, height);
            /**
             * The deserialied layout must be positioned through this.layout(...)
             * before any maximizedNode can be positioned
             */
            this.layout(json.width, json.height);
            if (json.maximizedNode) {
                const location = json.maximizedNode.location;
                const [_, node] = this.getNode(location);
                if (!(node instanceof LeafNode)) {
                    return;
                }
                this.maximizeView(node.view);
            }
        }
        _deserialize(root, orientation, deserializer, orthogonalSize) {
            this.root = this._deserializeNode(root, orientation, deserializer, orthogonalSize);
        }
        _deserializeNode(node, orientation, deserializer, orthogonalSize) {
            var _a;
            let result;
            if (node.type === 'branch') {
                const serializedChildren = node.data;
                const children = serializedChildren.map((serializedChild) => {
                    return {
                        node: this._deserializeNode(serializedChild, orthogonal(orientation), deserializer, node.size),
                        visible: serializedChild.visible,
                    };
                });
                result = new BranchNode(orientation, this.proportionalLayout, this.styles, node.size, // <- orthogonal size - flips at each depth
                orthogonalSize, // <- size - flips at each depth,
                this.locked, this.margin, children);
            }
            else {
                const view = deserializer.fromJSON(node);
                if (typeof node.visible === 'boolean') {
                    (_a = view.setVisible) === null || _a === void 0 ? void 0 : _a.call(view, node.visible);
                }
                result = new LeafNode(view, orientation, orthogonalSize, node.size);
            }
            return result;
        }
        get root() {
            return this._root;
        }
        set root(root) {
            const oldRoot = this._root;
            if (oldRoot) {
                oldRoot.dispose();
                this._maximizedNode = undefined;
                this.element.removeChild(oldRoot.element);
            }
            this._root = root;
            this.element.appendChild(this._root.element);
            this.disposable.value = this._root.onDidChange((e) => {
                this._onDidChange.fire(e);
            });
        }
        normalize() {
            if (!this._root) {
                return;
            }
            if (this._root.children.length !== 1) {
                return;
            }
            const oldRoot = this.root;
            // can remove one level of redundant branching if there is only a single child
            const childReference = oldRoot.children[0];
            if (childReference instanceof LeafNode) {
                return;
            }
            oldRoot.element.remove();
            const child = oldRoot.removeChild(0); // Remove child to prevent double disposal
            oldRoot.dispose(); // Dispose old root (won't dispose removed child)
            child.dispose(); // Dispose the removed child
            this._root = cloneNode(childReference, childReference.size, childReference.orthogonalSize);
            this.element.appendChild(this._root.element);
            this.disposable.value = this._root.onDidChange((e) => {
                this._onDidChange.fire(e);
            });
        }
        /**
         * If the root is orientated as a VERTICAL node then nest the existing root within a new HORIZIONTAL root node
         * If the root is orientated as a HORIZONTAL node then nest the existing root within a new VERITCAL root node
         */
        insertOrthogonalSplitviewAtRoot() {
            if (!this._root) {
                return;
            }
            const oldRoot = this.root;
            oldRoot.element.remove();
            this._root = new BranchNode(orthogonal(oldRoot.orientation), this.proportionalLayout, this.styles, this.root.orthogonalSize, this.root.size, this.locked, this.margin);
            if (oldRoot.children.length === 0) ;
            else if (oldRoot.children.length === 1) {
                // can remove one level of redundant branching if there is only a single child
                const childReference = oldRoot.children[0];
                const child = oldRoot.removeChild(0); // remove to prevent disposal when disposing of unwanted root
                child.dispose();
                oldRoot.dispose();
                this._root.addChild(
                /**
                 * the child node will have the same orientation as the new root since
                 * we are removing the inbetween node.
                 * the entire 'tree' must be flipped recursively to ensure that the orientation
                 * flips at each level
                 */
                flipNode(childReference, childReference.orthogonalSize, childReference.size), exports.Sizing.Distribute, 0);
            }
            else {
                this._root.addChild(oldRoot, exports.Sizing.Distribute, 0);
            }
            this.element.appendChild(this._root.element);
            this.disposable.value = this._root.onDidChange((e) => {
                this._onDidChange.fire(e);
            });
        }
        next(location) {
            return this.progmaticSelect(location);
        }
        previous(location) {
            return this.progmaticSelect(location, true);
        }
        getView(location) {
            const node = location ? this.getNode(location)[1] : this.root;
            return this._getViews(node, this.orientation);
        }
        _getViews(node, orientation, cachedVisibleSize) {
            const box = { height: node.height, width: node.width };
            if (node instanceof LeafNode) {
                return { box, view: node.view, cachedVisibleSize };
            }
            const children = [];
            for (let i = 0; i < node.children.length; i++) {
                const child = node.children[i];
                const nodeCachedVisibleSize = node.getChildCachedVisibleSize(i);
                children.push(this._getViews(child, orthogonal(orientation), nodeCachedVisibleSize));
            }
            return { box, children };
        }
        progmaticSelect(location, reverse = false) {
            const [path, node] = this.getNode(location);
            if (!(node instanceof LeafNode)) {
                throw new Error('invalid location');
            }
            for (let i = path.length - 1; i > -1; i--) {
                const n = path[i];
                const l = location[i] || 0;
                const canProgressInCurrentLevel = reverse
                    ? l - 1 > -1
                    : l + 1 < n.children.length;
                if (canProgressInCurrentLevel) {
                    return findLeaf(n.children[reverse ? l - 1 : l + 1], reverse);
                }
            }
            return findLeaf(this.root, reverse);
        }
        constructor(proportionalLayout, styles, orientation, locked, margin) {
            this.proportionalLayout = proportionalLayout;
            this.styles = styles;
            this._locked = false;
            this._margin = 0;
            this._maximizedNode = undefined;
            this.disposable = new MutableDisposable();
            this._onDidChange = new Emitter();
            this.onDidChange = this._onDidChange.event;
            this._onDidViewVisibilityChange = new Emitter();
            this.onDidViewVisibilityChange = this._onDidViewVisibilityChange.event;
            this._onDidMaximizedNodeChange = new Emitter();
            this.onDidMaximizedNodeChange = this._onDidMaximizedNodeChange.event;
            this.element = document.createElement('div');
            this.element.className = 'dv-grid-view';
            this._locked = locked !== null && locked !== void 0 ? locked : false;
            this._margin = margin !== null && margin !== void 0 ? margin : 0;
            this.root = new BranchNode(orientation, proportionalLayout, styles, 0, 0, this.locked, this.margin);
        }
        isViewVisible(location) {
            const [rest, index] = tail(location);
            const [, parent] = this.getNode(rest);
            if (!(parent instanceof BranchNode)) {
                throw new Error('Invalid from location');
            }
            return parent.isChildVisible(index);
        }
        setViewVisible(location, visible) {
            if (this.hasMaximizedView()) {
                this.exitMaximizedView();
            }
            const [rest, index] = tail(location);
            const [, parent] = this.getNode(rest);
            if (!(parent instanceof BranchNode)) {
                throw new Error('Invalid from location');
            }
            this._onDidViewVisibilityChange.fire();
            parent.setChildVisible(index, visible);
        }
        moveView(parentLocation, from, to) {
            if (this.hasMaximizedView()) {
                this.exitMaximizedView();
            }
            const [, parent] = this.getNode(parentLocation);
            if (!(parent instanceof BranchNode)) {
                throw new Error('Invalid location');
            }
            parent.moveChild(from, to);
        }
        addView(view, size, location) {
            if (this.hasMaximizedView()) {
                this.exitMaximizedView();
            }
            const [rest, index] = tail(location);
            const [pathToParent, parent] = this.getNode(rest);
            if (parent instanceof BranchNode) {
                const node = new LeafNode(view, orthogonal(parent.orientation), parent.orthogonalSize);
                parent.addChild(node, size, index);
            }
            else {
                const [grandParent, ..._] = [...pathToParent].reverse();
                const [parentIndex, ...__] = [...rest].reverse();
                let newSiblingSize = 0;
                const newSiblingCachedVisibleSize = grandParent.getChildCachedVisibleSize(parentIndex);
                if (typeof newSiblingCachedVisibleSize === 'number') {
                    newSiblingSize = exports.Sizing.Invisible(newSiblingCachedVisibleSize);
                }
                const child = grandParent.removeChild(parentIndex);
                child.dispose();
                const newParent = new BranchNode(parent.orientation, this.proportionalLayout, this.styles, parent.size, parent.orthogonalSize, this.locked, this.margin);
                grandParent.addChild(newParent, parent.size, parentIndex);
                const newSibling = new LeafNode(parent.view, grandParent.orientation, parent.size);
                newParent.addChild(newSibling, newSiblingSize, 0);
                if (typeof size !== 'number' && size.type === 'split') {
                    size = { type: 'split', index: 0 };
                }
                const node = new LeafNode(view, grandParent.orientation, parent.size);
                newParent.addChild(node, size, index);
            }
        }
        remove(view, sizing) {
            const location = getGridLocation(view.element);
            return this.removeView(location, sizing);
        }
        removeView(location, sizing) {
            if (this.hasMaximizedView()) {
                this.exitMaximizedView();
            }
            const [rest, index] = tail(location);
            const [pathToParent, parent] = this.getNode(rest);
            if (!(parent instanceof BranchNode)) {
                throw new Error('Invalid location');
            }
            const nodeToRemove = parent.children[index];
            if (!(nodeToRemove instanceof LeafNode)) {
                throw new Error('Invalid location');
            }
            parent.removeChild(index, sizing);
            nodeToRemove.dispose();
            if (parent.children.length !== 1) {
                return nodeToRemove.view;
            }
            // if the parent has only one child and we know the parent is a BranchNode we can make the tree
            // more efficiently spaced by replacing the parent BranchNode with the child.
            // if that child is a LeafNode then we simply replace the BranchNode with the child otherwise if the child
            // is a BranchNode too we should spread it's children into the grandparent.
            // refer to the remaining child as the sibling
            const sibling = parent.children[0];
            if (pathToParent.length === 0) {
                // if the parent is root
                if (sibling instanceof LeafNode) {
                    // if the sibling is a leaf node no action is required
                    return nodeToRemove.view;
                }
                // otherwise the sibling is a branch node. since the parent is the root and the root has only one child
                // which is a branch node we can just set this branch node to be the new root node
                // for good housekeeping we'll removing the sibling from it's existing tree
                parent.removeChild(0, sizing);
                // and set that sibling node to be root
                this.root = sibling;
                return nodeToRemove.view;
            }
            // otherwise the parent is apart of a large sub-tree
            const [grandParent, ..._] = [...pathToParent].reverse();
            const [parentIndex, ...__] = [...rest].reverse();
            const isSiblingVisible = parent.isChildVisible(0);
            // either way we need to remove the sibling from it's existing tree
            parent.removeChild(0, sizing);
            // note the sizes of all of the grandparents children
            const sizes = grandParent.children.map((_size, i) => grandParent.getChildSize(i));
            // remove the parent from the grandparent since we are moving the sibling to take the parents place
            // this parent is no longer used and can be disposed of
            grandParent.removeChild(parentIndex, sizing).dispose();
            if (sibling instanceof BranchNode) {
                // replace the parent with the siblings children
                sizes.splice(parentIndex, 1, ...sibling.children.map((c) => c.size));
                // and add those siblings to the grandparent
                for (let i = 0; i < sibling.children.length; i++) {
                    const child = sibling.children[i];
                    grandParent.addChild(child, child.size, parentIndex + i);
                }
                /**
                 * clean down the branch node since we need to dipose of it and
                 * when .dispose() it called on a branch it will dispose of any
                 * views it is holding onto.
                 */
                while (sibling.children.length > 0) {
                    sibling.removeChild(0);
                }
            }
            else {
                // otherwise create a new leaf node and add that to the grandparent
                const newSibling = new LeafNode(sibling.view, orthogonal(sibling.orientation), sibling.size);
                const siblingSizing = isSiblingVisible
                    ? sibling.orthogonalSize
                    : exports.Sizing.Invisible(sibling.orthogonalSize);
                grandParent.addChild(newSibling, siblingSizing, parentIndex);
            }
            // the containing node of the sibling is no longer required and can be disposed of
            sibling.dispose();
            // resize everything
            for (let i = 0; i < sizes.length; i++) {
                grandParent.resizeChild(i, sizes[i]);
            }
            return nodeToRemove.view;
        }
        layout(width, height) {
            const [size, orthogonalSize] = this.root.orientation === exports.Orientation.HORIZONTAL
                ? [height, width]
                : [width, height];
            this.root.layout(size, orthogonalSize);
        }
        getNode(location, node = this.root, path = []) {
            if (location.length === 0) {
                return [path, node];
            }
            if (!(node instanceof BranchNode)) {
                throw new Error('Invalid location');
            }
            const [index, ...rest] = location;
            if (index < 0 || index >= node.children.length) {
                throw new Error('Invalid location');
            }
            const child = node.children[index];
            path.push(node);
            return this.getNode(rest, child, path);
        }
    }

    const PROPERTY_KEYS_GRIDVIEW = (() => {
        /**
         * by readong the keys from an empty value object TypeScript will error
         * when we add or remove new properties to `DockviewOptions`
         */
        const properties = {
            disableAutoResizing: undefined,
            proportionalLayout: undefined,
            orientation: undefined,
            hideBorders: undefined,
            className: undefined,
        };
        return Object.keys(properties);
    })();

    class Resizable extends CompositeDisposable {
        get element() {
            return this._element;
        }
        get disableResizing() {
            return this._disableResizing;
        }
        set disableResizing(value) {
            this._disableResizing = value;
        }
        constructor(parentElement, disableResizing = false) {
            super();
            this._disableResizing = disableResizing;
            this._element = parentElement;
            this.addDisposables(watchElementResize(this._element, (entry) => {
                if (this.isDisposed) {
                    /**
                     * resize is delayed through requestAnimationFrame so there is a small chance
                     * the component has already been disposed of
                     */
                    return;
                }
                if (this.disableResizing) {
                    return;
                }
                if (!this._element.offsetParent) {
                    /**
                     * offsetParent === null is equivalent to display: none being set on the element or one
                     * of it's parents. In the display: none case the size will become (0, 0) which we do
                     * not want to propagate.
                     *
                     * @see https://developer.mozilla.org/en-US/docs/Web/API/HTMLElement/offsetParent
                     *
                     * You could use checkVisibility() but at the time of writing it's not supported across
                     * all Browsers
                     *
                     * @see https://developer.mozilla.org/en-US/docs/Web/API/Element/checkVisibility
                     */
                    return;
                }
                if (!isInDocument(this._element)) {
                    /**
                     * since the event is dispatched through requestAnimationFrame there is a small chance
                     * the component is no longer attached to the DOM, if that is the case the dimensions
                     * are mostly likely all zero and meaningless. we should skip this case.
                     */
                    return;
                }
                const { width, height } = entry.contentRect;
                this.layout(width, height);
            }));
        }
    }

    const nextLayoutId$1 = sequentialNumberGenerator();
    function toTarget(direction) {
        switch (direction) {
            case 'left':
                return 'left';
            case 'right':
                return 'right';
            case 'above':
                return 'top';
            case 'below':
                return 'bottom';
            case 'within':
            default:
                return 'center';
        }
    }
    class BaseGrid extends Resizable {
        get id() {
            return this._id;
        }
        get size() {
            return this._groups.size;
        }
        get groups() {
            return Array.from(this._groups.values()).map((_) => _.value);
        }
        get width() {
            return this.gridview.width;
        }
        get height() {
            return this.gridview.height;
        }
        get minimumHeight() {
            return this.gridview.minimumHeight;
        }
        get maximumHeight() {
            return this.gridview.maximumHeight;
        }
        get minimumWidth() {
            return this.gridview.minimumWidth;
        }
        get maximumWidth() {
            return this.gridview.maximumWidth;
        }
        get activeGroup() {
            return this._activeGroup;
        }
        get locked() {
            return this.gridview.locked;
        }
        set locked(value) {
            this.gridview.locked = value;
        }
        constructor(container, options) {
            var _a;
            super(document.createElement('div'), options.disableAutoResizing);
            this._id = nextLayoutId$1.next();
            this._groups = new Map();
            this._onDidRemove = new Emitter();
            this.onDidRemove = this._onDidRemove.event;
            this._onDidAdd = new Emitter();
            this.onDidAdd = this._onDidAdd.event;
            this._onDidMaximizedChange = new Emitter();
            this.onDidMaximizedChange = this._onDidMaximizedChange.event;
            this._onDidActiveChange = new Emitter();
            this.onDidActiveChange = this._onDidActiveChange.event;
            this._bufferOnDidLayoutChange = new AsapEvent();
            this.onDidLayoutChange = this._bufferOnDidLayoutChange.onEvent;
            this._onDidViewVisibilityChangeMicroTaskQueue = new AsapEvent();
            this.onDidViewVisibilityChangeMicroTaskQueue = this._onDidViewVisibilityChangeMicroTaskQueue.onEvent;
            this.element.style.height = '100%';
            this.element.style.width = '100%';
            this._classNames = new Classnames(this.element);
            this._classNames.setClassNames((_a = options.className) !== null && _a !== void 0 ? _a : '');
            // the container is owned by the third-party, do not modify/delete it
            container.appendChild(this.element);
            this.gridview = new Gridview(!!options.proportionalLayout, options.styles, options.orientation, options.locked, options.margin);
            this.gridview.locked = !!options.locked;
            this.element.appendChild(this.gridview.element);
            this.layout(0, 0, true); // set some elements height/widths
            this.addDisposables(this.gridview.onDidMaximizedNodeChange((event) => {
                this._onDidMaximizedChange.fire({
                    panel: event.view,
                    isMaximized: event.isMaximized,
                });
            }), this.gridview.onDidViewVisibilityChange(() => this._onDidViewVisibilityChangeMicroTaskQueue.fire()), this.onDidViewVisibilityChangeMicroTaskQueue(() => {
                this.layout(this.width, this.height, true);
            }), exports.DockviewDisposable.from(() => {
                var _a;
                (_a = this.element.parentElement) === null || _a === void 0 ? void 0 : _a.removeChild(this.element);
            }), this.gridview.onDidChange(() => {
                this._bufferOnDidLayoutChange.fire();
            }), exports.DockviewEvent.any(this.onDidAdd, this.onDidRemove, this.onDidActiveChange)(() => {
                this._bufferOnDidLayoutChange.fire();
            }), this._onDidMaximizedChange, this._onDidViewVisibilityChangeMicroTaskQueue, this._bufferOnDidLayoutChange);
        }
        setVisible(panel, visible) {
            this.gridview.setViewVisible(getGridLocation(panel.element), visible);
            this._bufferOnDidLayoutChange.fire();
        }
        isVisible(panel) {
            return this.gridview.isViewVisible(getGridLocation(panel.element));
        }
        updateOptions(options) {
            var _a, _b, _c, _d;
            if (typeof options.proportionalLayout === 'boolean') ;
            if (options.orientation) {
                this.gridview.orientation = options.orientation;
            }
            if ('disableResizing' in options) {
                this.disableResizing = (_a = options.disableAutoResizing) !== null && _a !== void 0 ? _a : false;
            }
            if ('locked' in options) {
                this.locked = (_b = options.locked) !== null && _b !== void 0 ? _b : false;
            }
            if ('margin' in options) {
                this.gridview.margin = (_c = options.margin) !== null && _c !== void 0 ? _c : 0;
            }
            if ('className' in options) {
                this._classNames.setClassNames((_d = options.className) !== null && _d !== void 0 ? _d : '');
            }
        }
        maximizeGroup(panel) {
            this.gridview.maximizeView(panel);
            this.doSetGroupActive(panel);
        }
        isMaximizedGroup(panel) {
            return this.gridview.maximizedView() === panel;
        }
        exitMaximizedGroup() {
            this.gridview.exitMaximizedView();
        }
        hasMaximizedGroup() {
            return this.gridview.hasMaximizedView();
        }
        doAddGroup(group, location = [0], size) {
            this.gridview.addView(group, size !== null && size !== void 0 ? size : exports.Sizing.Distribute, location);
            this._onDidAdd.fire(group);
        }
        doRemoveGroup(group, options) {
            if (!this._groups.has(group.id)) {
                throw new Error('invalid operation');
            }
            const item = this._groups.get(group.id);
            const view = this.gridview.remove(group, exports.Sizing.Distribute);
            if (item && !(options === null || options === void 0 ? void 0 : options.skipDispose)) {
                item.disposable.dispose();
                item.value.dispose();
                this._groups.delete(group.id);
                this._onDidRemove.fire(group);
            }
            if (!(options === null || options === void 0 ? void 0 : options.skipActive) && this._activeGroup === group) {
                const groups = Array.from(this._groups.values());
                this.doSetGroupActive(groups.length > 0 ? groups[0].value : undefined);
            }
            return view;
        }
        getPanel(id) {
            var _a;
            return (_a = this._groups.get(id)) === null || _a === void 0 ? void 0 : _a.value;
        }
        doSetGroupActive(group) {
            if (this._activeGroup === group) {
                return;
            }
            if (this._activeGroup) {
                this._activeGroup.setActive(false);
            }
            if (group) {
                group.setActive(true);
            }
            this._activeGroup = group;
            this._onDidActiveChange.fire(group);
        }
        removeGroup(group) {
            this.doRemoveGroup(group);
        }
        moveToNext(options) {
            var _a;
            if (!options) {
                options = {};
            }
            if (!options.group) {
                if (!this.activeGroup) {
                    return;
                }
                options.group = this.activeGroup;
            }
            const location = getGridLocation(options.group.element);
            const next = (_a = this.gridview.next(location)) === null || _a === void 0 ? void 0 : _a.view;
            this.doSetGroupActive(next);
        }
        moveToPrevious(options) {
            var _a;
            if (!options) {
                options = {};
            }
            if (!options.group) {
                if (!this.activeGroup) {
                    return;
                }
                options.group = this.activeGroup;
            }
            const location = getGridLocation(options.group.element);
            const next = (_a = this.gridview.previous(location)) === null || _a === void 0 ? void 0 : _a.view;
            this.doSetGroupActive(next);
        }
        layout(width, height, forceResize) {
            const different = forceResize || width !== this.width || height !== this.height;
            if (!different) {
                return;
            }
            this.gridview.element.style.height = `${height}px`;
            this.gridview.element.style.width = `${width}px`;
            this.gridview.layout(width, height);
        }
        dispose() {
            this._onDidActiveChange.dispose();
            this._onDidAdd.dispose();
            this._onDidRemove.dispose();
            for (const group of this.groups) {
                group.dispose();
            }
            this.gridview.dispose();
            super.dispose();
        }
    }

    class SplitviewApi {
        /**
         * The minimum size  the component can reach where size is measured in the direction of orientation provided.
         */
        get minimumSize() {
            return this.component.minimumSize;
        }
        /**
         * The maximum size the component can reach where size is measured in the direction of orientation provided.
         */
        get maximumSize() {
            return this.component.maximumSize;
        }
        /**
         * Width of the component.
         */
        get width() {
            return this.component.width;
        }
        /**
         * Height of the component.
         */
        get height() {
            return this.component.height;
        }
        /**
         * The current number of panels.
         */
        get length() {
            return this.component.length;
        }
        /**
         * The current orientation of the component.
         */
        get orientation() {
            return this.component.orientation;
        }
        /**
         * The list of current panels.
         */
        get panels() {
            return this.component.panels;
        }
        /**
         * Invoked after a layout is loaded through the `fromJSON` method.
         */
        get onDidLayoutFromJSON() {
            return this.component.onDidLayoutFromJSON;
        }
        /**
         * Invoked whenever any aspect of the layout changes.
         * If listening to this event it may be worth debouncing ouputs.
         */
        get onDidLayoutChange() {
            return this.component.onDidLayoutChange;
        }
        /**
         * Invoked when a view is added.
         */
        get onDidAddView() {
            return this.component.onDidAddView;
        }
        /**
         * Invoked when a view is removed.
         */
        get onDidRemoveView() {
            return this.component.onDidRemoveView;
        }
        constructor(component) {
            this.component = component;
        }
        /**
         * Removes an existing panel and optionally provide a `Sizing` method
         * for the subsequent resize.
         */
        removePanel(panel, sizing) {
            this.component.removePanel(panel, sizing);
        }
        /**
         * Focus the component.
         */
        focus() {
            this.component.focus();
        }
        /**
         * Get the reference to a panel given it's `string` id.
         */
        getPanel(id) {
            return this.component.getPanel(id);
        }
        /**
         * Layout the panel with a width and height.
         */
        layout(width, height) {
            return this.component.layout(width, height);
        }
        /**
         * Add a new panel and return the created instance.
         */
        addPanel(options) {
            return this.component.addPanel(options);
        }
        /**
         * Move a panel given it's current and desired index.
         */
        movePanel(from, to) {
            this.component.movePanel(from, to);
        }
        /**
         * Deserialize a layout to built a splitivew.
         */
        fromJSON(data) {
            this.component.fromJSON(data);
        }
        /** Serialize a layout */
        toJSON() {
            return this.component.toJSON();
        }
        /**
         * Remove all panels and clear the component.
         */
        clear() {
            this.component.clear();
        }
        /**
         * Update configuratable options.
         */
        updateOptions(options) {
            this.component.updateOptions(options);
        }
        /**
         * Release resources and teardown component. Do not call when using framework versions of dockview.
         */
        dispose() {
            this.component.dispose();
        }
    }
    class PaneviewApi {
        /**
         * The minimum size  the component can reach where size is measured in the direction of orientation provided.
         */
        get minimumSize() {
            return this.component.minimumSize;
        }
        /**
         * The maximum size the component can reach where size is measured in the direction of orientation provided.
         */
        get maximumSize() {
            return this.component.maximumSize;
        }
        /**
         * Width of the component.
         */
        get width() {
            return this.component.width;
        }
        /**
         * Height of the component.
         */
        get height() {
            return this.component.height;
        }
        /**
         * All panel objects.
         */
        get panels() {
            return this.component.panels;
        }
        /**
         * Invoked when any layout change occures, an aggregation of many events.
         */
        get onDidLayoutChange() {
            return this.component.onDidLayoutChange;
        }
        /**
         * Invoked after a layout is deserialzied using the `fromJSON` method.
         */
        get onDidLayoutFromJSON() {
            return this.component.onDidLayoutFromJSON;
        }
        /**
         * Invoked when a panel is added. May be called multiple times when moving panels.
         */
        get onDidAddView() {
            return this.component.onDidAddView;
        }
        /**
         * Invoked when a panel is removed. May be called multiple times when moving panels.
         */
        get onDidRemoveView() {
            return this.component.onDidRemoveView;
        }
        /**
         * Invoked when a Drag'n'Drop event occurs that the component was unable to handle. Exposed for custom Drag'n'Drop functionality.
         */
        get onDidDrop() {
            return this.component.onDidDrop;
        }
        get onUnhandledDragOverEvent() {
            return this.component.onUnhandledDragOverEvent;
        }
        constructor(component) {
            this.component = component;
        }
        /**
         * Remove a panel given the panel object.
         */
        removePanel(panel) {
            this.component.removePanel(panel);
        }
        /**
         * Get a panel object given a `string` id. May return `undefined`.
         */
        getPanel(id) {
            return this.component.getPanel(id);
        }
        /**
         * Move a panel given it's current and desired index.
         */
        movePanel(from, to) {
            this.component.movePanel(from, to);
        }
        /**
         *  Focus the component. Will try to focus an active panel if one exists.
         */
        focus() {
            this.component.focus();
        }
        /**
         * Force resize the component to an exact width and height. Read about auto-resizing before using.
         */
        layout(width, height) {
            this.component.layout(width, height);
        }
        /**
         * Add a panel and return the created object.
         */
        addPanel(options) {
            return this.component.addPanel(options);
        }
        /**
         * Create a component from a serialized object.
         */
        fromJSON(data) {
            this.component.fromJSON(data);
        }
        /**
         * Create a serialized object of the current component.
         */
        toJSON() {
            return this.component.toJSON();
        }
        /**
         * Reset the component back to an empty and default state.
         */
        clear() {
            this.component.clear();
        }
        /**
         * Update configuratable options.
         */
        updateOptions(options) {
            this.component.updateOptions(options);
        }
        /**
         * Release resources and teardown component. Do not call when using framework versions of dockview.
         */
        dispose() {
            this.component.dispose();
        }
    }
    class GridviewApi {
        /**
         * Width of the component.
         */
        get width() {
            return this.component.width;
        }
        /**
         * Height of the component.
         */
        get height() {
            return this.component.height;
        }
        /**
         * Minimum height of the component.
         */
        get minimumHeight() {
            return this.component.minimumHeight;
        }
        /**
         * Maximum height of the component.
         */
        get maximumHeight() {
            return this.component.maximumHeight;
        }
        /**
         * Minimum width of the component.
         */
        get minimumWidth() {
            return this.component.minimumWidth;
        }
        /**
         * Maximum width of the component.
         */
        get maximumWidth() {
            return this.component.maximumWidth;
        }
        /**
         * Invoked when any layout change occures, an aggregation of many events.
         */
        get onDidLayoutChange() {
            return this.component.onDidLayoutChange;
        }
        /**
         * Invoked when a panel is added. May be called multiple times when moving panels.
         */
        get onDidAddPanel() {
            return this.component.onDidAddGroup;
        }
        /**
         * Invoked when a panel is removed. May be called multiple times when moving panels.
         */
        get onDidRemovePanel() {
            return this.component.onDidRemoveGroup;
        }
        /**
         * Invoked when the active panel changes. May be undefined if no panel is active.
         */
        get onDidActivePanelChange() {
            return this.component.onDidActiveGroupChange;
        }
        /**
         * Invoked after a layout is deserialzied using the `fromJSON` method.
         */
        get onDidLayoutFromJSON() {
            return this.component.onDidLayoutFromJSON;
        }
        /**
         * All panel objects.
         */
        get panels() {
            return this.component.groups;
        }
        /**
         * Current orientation. Can be changed after initialization.
         */
        get orientation() {
            return this.component.orientation;
        }
        set orientation(value) {
            this.component.updateOptions({ orientation: value });
        }
        constructor(component) {
            this.component = component;
        }
        /**
         *  Focus the component. Will try to focus an active panel if one exists.
         */
        focus() {
            this.component.focus();
        }
        /**
         * Force resize the component to an exact width and height. Read about auto-resizing before using.
         */
        layout(width, height, force = false) {
            this.component.layout(width, height, force);
        }
        /**
         * Add a panel and return the created object.
         */
        addPanel(options) {
            return this.component.addPanel(options);
        }
        /**
         * Remove a panel given the panel object.
         */
        removePanel(panel, sizing) {
            this.component.removePanel(panel, sizing);
        }
        /**
         * Move a panel in a particular direction relative to another panel.
         */
        movePanel(panel, options) {
            this.component.movePanel(panel, options);
        }
        /**
         * Get a panel object given a `string` id. May return `undefined`.
         */
        getPanel(id) {
            return this.component.getPanel(id);
        }
        /**
         * Create a component from a serialized object.
         */
        fromJSON(data) {
            return this.component.fromJSON(data);
        }
        /**
         * Create a serialized object of the current component.
         */
        toJSON() {
            return this.component.toJSON();
        }
        /**
         * Reset the component back to an empty and default state.
         */
        clear() {
            this.component.clear();
        }
        updateOptions(options) {
            this.component.updateOptions(options);
        }
        /**
         * Release resources and teardown component. Do not call when using framework versions of dockview.
         */
        dispose() {
            this.component.dispose();
        }
    }
    class DockviewApi {
        /**
         * The unique identifier for this instance. Used to manage scope of Drag'n'Drop events.
         */
        get id() {
            return this.component.id;
        }
        /**
         * Width of the component.
         */
        get width() {
            return this.component.width;
        }
        /**
         * Height of the component.
         */
        get height() {
            return this.component.height;
        }
        /**
         * Minimum height of the component.
         */
        get minimumHeight() {
            return this.component.minimumHeight;
        }
        /**
         * Maximum height of the component.
         */
        get maximumHeight() {
            return this.component.maximumHeight;
        }
        /**
         * Minimum width of the component.
         */
        get minimumWidth() {
            return this.component.minimumWidth;
        }
        /**
         * Maximum width of the component.
         */
        get maximumWidth() {
            return this.component.maximumWidth;
        }
        /**
         * Total number of groups.
         */
        get size() {
            return this.component.size;
        }
        /**
         * Total number of panels.
         */
        get totalPanels() {
            return this.component.totalPanels;
        }
        /**
         * Invoked when the active group changes. May be undefined if no group is active.
         */
        get onDidActiveGroupChange() {
            return this.component.onDidActiveGroupChange;
        }
        /**
         * Invoked when a group is added. May be called multiple times when moving groups.
         */
        get onDidAddGroup() {
            return this.component.onDidAddGroup;
        }
        /**
         * Invoked when a group is removed. May be called multiple times when moving groups.
         */
        get onDidRemoveGroup() {
            return this.component.onDidRemoveGroup;
        }
        /**
         * Invoked when the active panel changes. May be undefined if no panel is active.
         */
        get onDidActivePanelChange() {
            return this.component.onDidActivePanelChange;
        }
        /**
         * Invoked when a panel is added. May be called multiple times when moving panels.
         */
        get onDidAddPanel() {
            return this.component.onDidAddPanel;
        }
        /**
         * Invoked when a panel is removed. May be called multiple times when moving panels.
         */
        get onDidRemovePanel() {
            return this.component.onDidRemovePanel;
        }
        get onDidMovePanel() {
            return this.component.onDidMovePanel;
        }
        /**
         * Invoked after a layout is deserialzied using the `fromJSON` method.
         */
        get onDidLayoutFromJSON() {
            return this.component.onDidLayoutFromJSON;
        }
        /**
         * Invoked when any layout change occures, an aggregation of many events.
         */
        get onDidLayoutChange() {
            return this.component.onDidLayoutChange;
        }
        /**
         * Invoked when a Drag'n'Drop event occurs that the component was unable to handle. Exposed for custom Drag'n'Drop functionality.
         */
        get onDidDrop() {
            return this.component.onDidDrop;
        }
        /**
         * Invoked when a Drag'n'Drop event occurs but before dockview handles it giving the user an opportunity to intecept and
         * prevent the event from occuring using the standard `preventDefault()` syntax.
         *
         * Preventing certain events may causes unexpected behaviours, use carefully.
         */
        get onWillDrop() {
            return this.component.onWillDrop;
        }
        /**
         * Invoked before an overlay is shown indicating a drop target.
         *
         * Calling `event.preventDefault()` will prevent the overlay being shown and prevent
         * the any subsequent drop event.
         */
        get onWillShowOverlay() {
            return this.component.onWillShowOverlay;
        }
        /**
         * Invoked before a group is dragged.
         *
         * Calling `event.nativeEvent.preventDefault()` will prevent the group drag starting.
         *
         */
        get onWillDragGroup() {
            return this.component.onWillDragGroup;
        }
        /**
         * Invoked before a panel is dragged.
         *
         * Calling `event.nativeEvent.preventDefault()` will prevent the panel drag starting.
         */
        get onWillDragPanel() {
            return this.component.onWillDragPanel;
        }
        get onUnhandledDragOverEvent() {
            return this.component.onUnhandledDragOverEvent;
        }
        get onDidPopoutGroupSizeChange() {
            return this.component.onDidPopoutGroupSizeChange;
        }
        get onDidPopoutGroupPositionChange() {
            return this.component.onDidPopoutGroupPositionChange;
        }
        get onDidOpenPopoutWindowFail() {
            return this.component.onDidOpenPopoutWindowFail;
        }
        /**
         * All panel objects.
         */
        get panels() {
            return this.component.panels;
        }
        /**
         * All group objects.
         */
        get groups() {
            return this.component.groups;
        }
        /**
         *  Active panel object.
         */
        get activePanel() {
            return this.component.activePanel;
        }
        /**
         * Active group object.
         */
        get activeGroup() {
            return this.component.activeGroup;
        }
        constructor(component) {
            this.component = component;
        }
        /**
         *  Focus the component. Will try to focus an active panel if one exists.
         */
        focus() {
            this.component.focus();
        }
        /**
         * Get a panel object given a `string` id. May return `undefined`.
         */
        getPanel(id) {
            return this.component.getGroupPanel(id);
        }
        /**
         * Force resize the component to an exact width and height. Read about auto-resizing before using.
         */
        layout(width, height, force = false) {
            this.component.layout(width, height, force);
        }
        /**
         * Add a panel and return the created object.
         */
        addPanel(options) {
            return this.component.addPanel(options);
        }
        /**
         * Remove a panel given the panel object.
         */
        removePanel(panel) {
            this.component.removePanel(panel);
        }
        /**
         * Add a group and return the created object.
         */
        addGroup(options) {
            return this.component.addGroup(options);
        }
        /**
         * Close all groups and panels.
         */
        closeAllGroups() {
            return this.component.closeAllGroups();
        }
        /**
         * Remove a group and any panels within the group.
         */
        removeGroup(group) {
            this.component.removeGroup(group);
        }
        /**
         * Get a group object given a `string` id. May return undefined.
         */
        getGroup(id) {
            return this.component.getPanel(id);
        }
        /**
         * Add a floating group
         */
        addFloatingGroup(item, options) {
            return this.component.addFloatingGroup(item, options);
        }
        /**
         * Create a component from a serialized object.
         */
        fromJSON(data) {
            this.component.fromJSON(data);
        }
        /**
         * Create a serialized object of the current component.
         */
        toJSON() {
            return this.component.toJSON();
        }
        /**
         * Reset the component back to an empty and default state.
         */
        clear() {
            this.component.clear();
        }
        /**
         * Move the focus progmatically to the next panel or group.
         */
        moveToNext(options) {
            this.component.moveToNext(options);
        }
        /**
         * Move the focus progmatically to the previous panel or group.
         */
        moveToPrevious(options) {
            this.component.moveToPrevious(options);
        }
        maximizeGroup(panel) {
            this.component.maximizeGroup(panel.group);
        }
        hasMaximizedGroup() {
            return this.component.hasMaximizedGroup();
        }
        exitMaximizedGroup() {
            this.component.exitMaximizedGroup();
        }
        get onDidMaximizedGroupChange() {
            return this.component.onDidMaximizedGroupChange;
        }
        /**
         * Add a popout group in a new Window
         */
        addPopoutGroup(item, options) {
            return this.component.addPopoutGroup(item, options);
        }
        updateOptions(options) {
            this.component.updateOptions(options);
        }
        /**
         * Release resources and teardown component. Do not call when using framework versions of dockview.
         */
        dispose() {
            this.component.dispose();
        }
    }

    class DragHandler extends CompositeDisposable {
        constructor(el, disabled) {
            super();
            this.el = el;
            this.disabled = disabled;
            this.dataDisposable = new MutableDisposable();
            this.pointerEventsDisposable = new MutableDisposable();
            this._onDragStart = new Emitter();
            this.onDragStart = this._onDragStart.event;
            this.addDisposables(this._onDragStart, this.dataDisposable, this.pointerEventsDisposable);
            this.configure();
        }
        setDisabled(disabled) {
            this.disabled = disabled;
        }
        isCancelled(_event) {
            return false;
        }
        configure() {
            this.addDisposables(this._onDragStart, addDisposableListener(this.el, 'dragstart', (event) => {
                if (event.defaultPrevented || this.isCancelled(event) || this.disabled) {
                    event.preventDefault();
                    return;
                }
                const iframes = disableIframePointEvents();
                this.pointerEventsDisposable.value = {
                    dispose: () => {
                        iframes.release();
                    },
                };
                this.el.classList.add('dv-dragged');
                setTimeout(() => this.el.classList.remove('dv-dragged'), 0);
                this.dataDisposable.value = this.getData(event);
                this._onDragStart.fire(event);
                if (event.dataTransfer) {
                    event.dataTransfer.effectAllowed = 'move';
                    const hasData = event.dataTransfer.items.length > 0;
                    if (!hasData) {
                        /**
                         * Although this is not used by dockview many third party dnd libraries will check
                         * dataTransfer.types to determine valid drag events.
                         *
                         * For example: in react-dnd if dataTransfer.types is not set then the dragStart event will be cancelled
                         * through .preventDefault(). Since this is applied globally to all drag events this would break dockviews
                         * dnd logic. You can see the code at
                     P    * https://github.com/react-dnd/react-dnd/blob/main/packages/backend-html5/src/HTML5BackendImpl.ts#L542
                         */
                        event.dataTransfer.setData('text/plain', '');
                    }
                }
            }), addDisposableListener(this.el, 'dragend', () => {
                this.pointerEventsDisposable.dispose();
                setTimeout(() => {
                    this.dataDisposable.dispose(); // allow the data to be read by other handlers before disposing
                }, 0);
            }));
        }
    }

    class DragAndDropObserver extends CompositeDisposable {
        constructor(element, callbacks) {
            super();
            this.element = element;
            this.callbacks = callbacks;
            this.target = null;
            this.registerListeners();
        }
        onDragEnter(e) {
            this.target = e.target;
            this.callbacks.onDragEnter(e);
        }
        onDragOver(e) {
            e.preventDefault(); // needed so that the drop event fires (https://stackoverflow.com/questions/21339924/drop-event-not-firing-in-chrome)
            if (this.callbacks.onDragOver) {
                this.callbacks.onDragOver(e);
            }
        }
        onDragLeave(e) {
            if (this.target === e.target) {
                this.target = null;
                this.callbacks.onDragLeave(e);
            }
        }
        onDragEnd(e) {
            this.target = null;
            this.callbacks.onDragEnd(e);
        }
        onDrop(e) {
            this.callbacks.onDrop(e);
        }
        registerListeners() {
            this.addDisposables(addDisposableListener(this.element, 'dragenter', (e) => {
                this.onDragEnter(e);
            }, true));
            this.addDisposables(addDisposableListener(this.element, 'dragover', (e) => {
                this.onDragOver(e);
            }, true));
            this.addDisposables(addDisposableListener(this.element, 'dragleave', (e) => {
                this.onDragLeave(e);
            }));
            this.addDisposables(addDisposableListener(this.element, 'dragend', (e) => {
                this.onDragEnd(e);
            }));
            this.addDisposables(addDisposableListener(this.element, 'drop', (e) => {
                this.onDrop(e);
            }));
        }
    }

    function setGPUOptimizedBounds(element, bounds) {
        const { top, left, width, height } = bounds;
        const topPx = `${Math.round(top)}px`;
        const leftPx = `${Math.round(left)}px`;
        const widthPx = `${Math.round(width)}px`;
        const heightPx = `${Math.round(height)}px`;
        // Use traditional positioning but maintain GPU layer
        element.style.top = topPx;
        element.style.left = leftPx;
        element.style.width = widthPx;
        element.style.height = heightPx;
        element.style.visibility = 'visible';
        // Ensure GPU layer is maintained
        if (!element.style.transform || element.style.transform === '') {
            element.style.transform = 'translate3d(0, 0, 0)';
        }
    }
    function setGPUOptimizedBoundsFromStrings(element, bounds) {
        const { top, left, width, height } = bounds;
        // Use traditional positioning but maintain GPU layer
        element.style.top = top;
        element.style.left = left;
        element.style.width = width;
        element.style.height = height;
        element.style.visibility = 'visible';
        // Ensure GPU layer is maintained
        if (!element.style.transform || element.style.transform === '') {
            element.style.transform = 'translate3d(0, 0, 0)';
        }
    }
    function checkBoundsChanged(element, bounds) {
        const { top, left, width, height } = bounds;
        const topPx = `${Math.round(top)}px`;
        const leftPx = `${Math.round(left)}px`;
        const widthPx = `${Math.round(width)}px`;
        const heightPx = `${Math.round(height)}px`;
        // Check if position or size changed (back to traditional method)
        return element.style.top !== topPx ||
            element.style.left !== leftPx ||
            element.style.width !== widthPx ||
            element.style.height !== heightPx;
    }
    class WillShowOverlayEvent extends DockviewEvent {
        get nativeEvent() {
            return this.options.nativeEvent;
        }
        get position() {
            return this.options.position;
        }
        constructor(options) {
            super();
            this.options = options;
        }
    }
    function directionToPosition(direction) {
        switch (direction) {
            case 'above':
                return 'top';
            case 'below':
                return 'bottom';
            case 'left':
                return 'left';
            case 'right':
                return 'right';
            case 'within':
                return 'center';
            default:
                throw new Error(`invalid direction '${direction}'`);
        }
    }
    function positionToDirection(position) {
        switch (position) {
            case 'top':
                return 'above';
            case 'bottom':
                return 'below';
            case 'left':
                return 'left';
            case 'right':
                return 'right';
            case 'center':
                return 'within';
            default:
                throw new Error(`invalid position '${position}'`);
        }
    }
    const DEFAULT_ACTIVATION_SIZE = {
        value: 20,
        type: 'percentage',
    };
    const DEFAULT_SIZE = {
        value: 50,
        type: 'percentage',
    };
    const SMALL_WIDTH_BOUNDARY = 100;
    const SMALL_HEIGHT_BOUNDARY = 100;
    class Droptarget extends CompositeDisposable {
        get disabled() {
            return this._disabled;
        }
        set disabled(value) {
            this._disabled = value;
        }
        get state() {
            return this._state;
        }
        constructor(element, options) {
            super();
            this.element = element;
            this.options = options;
            this._onDrop = new Emitter();
            this.onDrop = this._onDrop.event;
            this._onWillShowOverlay = new Emitter();
            this.onWillShowOverlay = this._onWillShowOverlay.event;
            this._disabled = false;
            // use a set to take advantage of #<set>.has
            this._acceptedTargetZonesSet = new Set(this.options.acceptedTargetZones);
            this.dnd = new DragAndDropObserver(this.element, {
                onDragEnter: () => {
                    var _a, _b, _c;
                    (_c = (_b = (_a = this.options).getOverrideTarget) === null || _b === void 0 ? void 0 : _b.call(_a)) === null || _c === void 0 ? void 0 : _c.getElements();
                },
                onDragOver: (e) => {
                    var _a, _b, _c, _d, _e, _f, _g;
                    Droptarget.ACTUAL_TARGET = this;
                    const overrideTarget = (_b = (_a = this.options).getOverrideTarget) === null || _b === void 0 ? void 0 : _b.call(_a);
                    if (this._acceptedTargetZonesSet.size === 0) {
                        if (overrideTarget) {
                            return;
                        }
                        this.removeDropTarget();
                        return;
                    }
                    const target = (_e = (_d = (_c = this.options).getOverlayOutline) === null || _d === void 0 ? void 0 : _d.call(_c)) !== null && _e !== void 0 ? _e : this.element;
                    const width = target.offsetWidth;
                    const height = target.offsetHeight;
                    if (width === 0 || height === 0) {
                        return; // avoid div!0
                    }
                    const rect = e.currentTarget.getBoundingClientRect();
                    const x = ((_f = e.clientX) !== null && _f !== void 0 ? _f : 0) - rect.left;
                    const y = ((_g = e.clientY) !== null && _g !== void 0 ? _g : 0) - rect.top;
                    const quadrant = this.calculateQuadrant(this._acceptedTargetZonesSet, x, y, width, height);
                    /**
                     * If the event has already been used by another DropTarget instance
                     * then don't show a second drop target, only one target should be
                     * active at any one time
                     */
                    if (this.isAlreadyUsed(e) || quadrant === null) {
                        // no drop target should be displayed
                        this.removeDropTarget();
                        return;
                    }
                    if (!this.options.canDisplayOverlay(e, quadrant)) {
                        if (overrideTarget) {
                            return;
                        }
                        this.removeDropTarget();
                        return;
                    }
                    const willShowOverlayEvent = new WillShowOverlayEvent({
                        nativeEvent: e,
                        position: quadrant,
                    });
                    /**
                     * Provide an opportunity to prevent the overlay appearing and in turn
                     * any dnd behaviours
                     */
                    this._onWillShowOverlay.fire(willShowOverlayEvent);
                    if (willShowOverlayEvent.defaultPrevented) {
                        this.removeDropTarget();
                        return;
                    }
                    this.markAsUsed(e);
                    if (overrideTarget) ;
                    else if (!this.targetElement) {
                        this.targetElement = document.createElement('div');
                        this.targetElement.className = 'dv-drop-target-dropzone';
                        this.overlayElement = document.createElement('div');
                        this.overlayElement.className = 'dv-drop-target-selection';
                        this._state = 'center';
                        this.targetElement.appendChild(this.overlayElement);
                        target.classList.add('dv-drop-target');
                        target.append(this.targetElement);
                        // this.overlayElement.style.opacity = '0';
                        // requestAnimationFrame(() => {
                        //     if (this.overlayElement) {
                        //         this.overlayElement.style.opacity = '';
                        //     }
                        // });
                    }
                    this.toggleClasses(quadrant, width, height);
                    this._state = quadrant;
                },
                onDragLeave: () => {
                    var _a, _b;
                    const target = (_b = (_a = this.options).getOverrideTarget) === null || _b === void 0 ? void 0 : _b.call(_a);
                    if (target) {
                        return;
                    }
                    this.removeDropTarget();
                },
                onDragEnd: (e) => {
                    var _a, _b;
                    const target = (_b = (_a = this.options).getOverrideTarget) === null || _b === void 0 ? void 0 : _b.call(_a);
                    if (target && Droptarget.ACTUAL_TARGET === this) {
                        if (this._state) {
                            // only stop the propagation of the event if we are dealing with it
                            // which is only when the target has state
                            e.stopPropagation();
                            this._onDrop.fire({
                                position: this._state,
                                nativeEvent: e,
                            });
                        }
                    }
                    this.removeDropTarget();
                    target === null || target === void 0 ? void 0 : target.clear();
                },
                onDrop: (e) => {
                    var _a, _b, _c;
                    e.preventDefault();
                    const state = this._state;
                    this.removeDropTarget();
                    (_c = (_b = (_a = this.options).getOverrideTarget) === null || _b === void 0 ? void 0 : _b.call(_a)) === null || _c === void 0 ? void 0 : _c.clear();
                    if (state) {
                        // only stop the propagation of the event if we are dealing with it
                        // which is only when the target has state
                        e.stopPropagation();
                        this._onDrop.fire({ position: state, nativeEvent: e });
                    }
                },
            });
            this.addDisposables(this._onDrop, this._onWillShowOverlay, this.dnd);
        }
        setTargetZones(acceptedTargetZones) {
            this._acceptedTargetZonesSet = new Set(acceptedTargetZones);
        }
        setOverlayModel(model) {
            this.options.overlayModel = model;
        }
        dispose() {
            this.removeDropTarget();
            super.dispose();
        }
        /**
         * Add a property to the event object for other potential listeners to check
         */
        markAsUsed(event) {
            event[Droptarget.USED_EVENT_ID] = true;
        }
        /**
         * Check is the event has already been used by another instance of DropTarget
         */
        isAlreadyUsed(event) {
            const value = event[Droptarget.USED_EVENT_ID];
            return typeof value === 'boolean' && value;
        }
        toggleClasses(quadrant, width, height) {
            var _a, _b, _c, _d, _e, _f, _g;
            const target = (_b = (_a = this.options).getOverrideTarget) === null || _b === void 0 ? void 0 : _b.call(_a);
            if (!target && !this.overlayElement) {
                return;
            }
            const isSmallX = width < SMALL_WIDTH_BOUNDARY;
            const isSmallY = height < SMALL_HEIGHT_BOUNDARY;
            const isLeft = quadrant === 'left';
            const isRight = quadrant === 'right';
            const isTop = quadrant === 'top';
            const isBottom = quadrant === 'bottom';
            const rightClass = !isSmallX && isRight;
            const leftClass = !isSmallX && isLeft;
            const topClass = !isSmallY && isTop;
            const bottomClass = !isSmallY && isBottom;
            let size = 1;
            const sizeOptions = (_d = (_c = this.options.overlayModel) === null || _c === void 0 ? void 0 : _c.size) !== null && _d !== void 0 ? _d : DEFAULT_SIZE;
            if (sizeOptions.type === 'percentage') {
                size = clamp(sizeOptions.value, 0, 100) / 100;
            }
            else {
                if (rightClass || leftClass) {
                    size = clamp(0, sizeOptions.value, width) / width;
                }
                if (topClass || bottomClass) {
                    size = clamp(0, sizeOptions.value, height) / height;
                }
            }
            if (target) {
                const outlineEl = (_g = (_f = (_e = this.options).getOverlayOutline) === null || _f === void 0 ? void 0 : _f.call(_e)) !== null && _g !== void 0 ? _g : this.element;
                const elBox = outlineEl.getBoundingClientRect();
                const ta = target.getElements(undefined, outlineEl);
                const el = ta.root;
                const overlay = ta.overlay;
                const bigbox = el.getBoundingClientRect();
                const rootTop = elBox.top - bigbox.top;
                const rootLeft = elBox.left - bigbox.left;
                const box = {
                    top: rootTop,
                    left: rootLeft,
                    width: width,
                    height: height,
                };
                if (rightClass) {
                    box.left = rootLeft + width * (1 - size);
                    box.width = width * size;
                }
                else if (leftClass) {
                    box.width = width * size;
                }
                else if (topClass) {
                    box.height = height * size;
                }
                else if (bottomClass) {
                    box.top = rootTop + height * (1 - size);
                    box.height = height * size;
                }
                if (isSmallX && isLeft) {
                    box.width = 4;
                }
                if (isSmallX && isRight) {
                    box.left = rootLeft + width - 4;
                    box.width = 4;
                }
                // Use GPU-optimized bounds checking and setting
                if (!checkBoundsChanged(overlay, box)) {
                    return;
                }
                setGPUOptimizedBounds(overlay, box);
                overlay.className = `dv-drop-target-anchor${this.options.className ? ` ${this.options.className}` : ''}`;
                toggleClass(overlay, 'dv-drop-target-left', isLeft);
                toggleClass(overlay, 'dv-drop-target-right', isRight);
                toggleClass(overlay, 'dv-drop-target-top', isTop);
                toggleClass(overlay, 'dv-drop-target-bottom', isBottom);
                toggleClass(overlay, 'dv-drop-target-center', quadrant === 'center');
                if (ta.changed) {
                    toggleClass(overlay, 'dv-drop-target-anchor-container-changed', true);
                    setTimeout(() => {
                        toggleClass(overlay, 'dv-drop-target-anchor-container-changed', false);
                    }, 10);
                }
                return;
            }
            if (!this.overlayElement) {
                return;
            }
            const box = { top: '0px', left: '0px', width: '100%', height: '100%' };
            /**
             * You can also achieve the overlay placement using the transform CSS property
             * to translate and scale the element however this has the undesired effect of
             * 'skewing' the element. Comment left here for anybody that ever revisits this.
             *
             * @see https://developer.mozilla.org/en-US/docs/Web/CSS/transform
             *
             * right
             * translateX(${100 * (1 - size) / 2}%) scaleX(${scale})
             *
             * left
             * translateX(-${100 * (1 - size) / 2}%) scaleX(${scale})
             *
             * top
             * translateY(-${100 * (1 - size) / 2}%) scaleY(${scale})
             *
             * bottom
             * translateY(${100 * (1 - size) / 2}%) scaleY(${scale})
             */
            if (rightClass) {
                box.left = `${100 * (1 - size)}%`;
                box.width = `${100 * size}%`;
            }
            else if (leftClass) {
                box.width = `${100 * size}%`;
            }
            else if (topClass) {
                box.height = `${100 * size}%`;
            }
            else if (bottomClass) {
                box.top = `${100 * (1 - size)}%`;
                box.height = `${100 * size}%`;
            }
            setGPUOptimizedBoundsFromStrings(this.overlayElement, box);
            toggleClass(this.overlayElement, 'dv-drop-target-small-vertical', isSmallY);
            toggleClass(this.overlayElement, 'dv-drop-target-small-horizontal', isSmallX);
            toggleClass(this.overlayElement, 'dv-drop-target-left', isLeft);
            toggleClass(this.overlayElement, 'dv-drop-target-right', isRight);
            toggleClass(this.overlayElement, 'dv-drop-target-top', isTop);
            toggleClass(this.overlayElement, 'dv-drop-target-bottom', isBottom);
            toggleClass(this.overlayElement, 'dv-drop-target-center', quadrant === 'center');
        }
        calculateQuadrant(overlayType, x, y, width, height) {
            var _a, _b;
            const activationSizeOptions = (_b = (_a = this.options.overlayModel) === null || _a === void 0 ? void 0 : _a.activationSize) !== null && _b !== void 0 ? _b : DEFAULT_ACTIVATION_SIZE;
            const isPercentage = activationSizeOptions.type === 'percentage';
            if (isPercentage) {
                return calculateQuadrantAsPercentage(overlayType, x, y, width, height, activationSizeOptions.value);
            }
            return calculateQuadrantAsPixels(overlayType, x, y, width, height, activationSizeOptions.value);
        }
        removeDropTarget() {
            var _a;
            if (this.targetElement) {
                this._state = undefined;
                (_a = this.targetElement.parentElement) === null || _a === void 0 ? void 0 : _a.classList.remove('dv-drop-target');
                this.targetElement.remove();
                this.targetElement = undefined;
                this.overlayElement = undefined;
            }
        }
    }
    Droptarget.USED_EVENT_ID = '__dockview_droptarget_event_is_used__';
    function calculateQuadrantAsPercentage(overlayType, x, y, width, height, threshold) {
        const xp = (100 * x) / width;
        const yp = (100 * y) / height;
        if (overlayType.has('left') && xp < threshold) {
            return 'left';
        }
        if (overlayType.has('right') && xp > 100 - threshold) {
            return 'right';
        }
        if (overlayType.has('top') && yp < threshold) {
            return 'top';
        }
        if (overlayType.has('bottom') && yp > 100 - threshold) {
            return 'bottom';
        }
        if (!overlayType.has('center')) {
            return null;
        }
        return 'center';
    }
    function calculateQuadrantAsPixels(overlayType, x, y, width, height, threshold) {
        if (overlayType.has('left') && x < threshold) {
            return 'left';
        }
        if (overlayType.has('right') && x > width - threshold) {
            return 'right';
        }
        if (overlayType.has('top') && y < threshold) {
            return 'top';
        }
        if (overlayType.has('bottom') && y > height - threshold) {
            return 'bottom';
        }
        if (!overlayType.has('center')) {
            return null;
        }
        return 'center';
    }

    const PROPERTY_KEYS_PANEVIEW = (() => {
        /**
         * by readong the keys from an empty value object TypeScript will error
         * when we add or remove new properties to `DockviewOptions`
         */
        const properties = {
            disableAutoResizing: undefined,
            disableDnd: undefined,
            className: undefined,
        };
        return Object.keys(properties);
    })();
    class PaneviewUnhandledDragOverEvent extends AcceptableEvent {
        constructor(nativeEvent, position, getData, panel) {
            super();
            this.nativeEvent = nativeEvent;
            this.position = position;
            this.getData = getData;
            this.panel = panel;
        }
    }

    class WillFocusEvent extends DockviewEvent {
        constructor() {
            super();
        }
    }
    /**
     * A core api implementation that should be used across all panel-like objects
     */
    class PanelApiImpl extends CompositeDisposable {
        get isFocused() {
            return this._isFocused;
        }
        get isActive() {
            return this._isActive;
        }
        get isVisible() {
            return this._isVisible;
        }
        get width() {
            return this._width;
        }
        get height() {
            return this._height;
        }
        constructor(id, component) {
            super();
            this.id = id;
            this.component = component;
            this._isFocused = false;
            this._isActive = false;
            this._isVisible = true;
            this._width = 0;
            this._height = 0;
            this._parameters = {};
            this.panelUpdatesDisposable = new MutableDisposable();
            this._onDidDimensionChange = new Emitter();
            this.onDidDimensionsChange = this._onDidDimensionChange.event;
            this._onDidChangeFocus = new Emitter();
            this.onDidFocusChange = this._onDidChangeFocus.event;
            //
            this._onWillFocus = new Emitter();
            this.onWillFocus = this._onWillFocus.event;
            //
            this._onDidVisibilityChange = new Emitter();
            this.onDidVisibilityChange = this._onDidVisibilityChange.event;
            this._onWillVisibilityChange = new Emitter();
            this.onWillVisibilityChange = this._onWillVisibilityChange.event;
            this._onDidActiveChange = new Emitter();
            this.onDidActiveChange = this._onDidActiveChange.event;
            this._onActiveChange = new Emitter();
            this.onActiveChange = this._onActiveChange.event;
            this._onDidParametersChange = new Emitter();
            this.onDidParametersChange = this._onDidParametersChange.event;
            this.addDisposables(this.onDidFocusChange((event) => {
                this._isFocused = event.isFocused;
            }), this.onDidActiveChange((event) => {
                this._isActive = event.isActive;
            }), this.onDidVisibilityChange((event) => {
                this._isVisible = event.isVisible;
            }), this.onDidDimensionsChange((event) => {
                this._width = event.width;
                this._height = event.height;
            }), this.panelUpdatesDisposable, this._onDidDimensionChange, this._onDidChangeFocus, this._onDidVisibilityChange, this._onDidActiveChange, this._onWillFocus, this._onActiveChange, this._onWillFocus, this._onWillVisibilityChange, this._onDidParametersChange);
        }
        getParameters() {
            return this._parameters;
        }
        initialize(panel) {
            this.panelUpdatesDisposable.value = this._onDidParametersChange.event((parameters) => {
                this._parameters = parameters;
                panel.update({
                    params: parameters,
                });
            });
        }
        setVisible(isVisible) {
            this._onWillVisibilityChange.fire({ isVisible });
        }
        setActive() {
            this._onActiveChange.fire();
        }
        updateParameters(parameters) {
            this._onDidParametersChange.fire(parameters);
        }
    }

    class SplitviewPanelApiImpl extends PanelApiImpl {
        //
        constructor(id, component) {
            super(id, component);
            this._onDidConstraintsChangeInternal = new Emitter();
            this.onDidConstraintsChangeInternal = this._onDidConstraintsChangeInternal.event;
            //
            this._onDidConstraintsChange = new Emitter({
                replay: true,
            });
            this.onDidConstraintsChange = this._onDidConstraintsChange.event;
            //
            this._onDidSizeChange = new Emitter();
            this.onDidSizeChange = this._onDidSizeChange.event;
            this.addDisposables(this._onDidConstraintsChangeInternal, this._onDidConstraintsChange, this._onDidSizeChange);
        }
        setConstraints(value) {
            this._onDidConstraintsChangeInternal.fire(value);
        }
        setSize(event) {
            this._onDidSizeChange.fire(event);
        }
    }

    class PaneviewPanelApiImpl extends SplitviewPanelApiImpl {
        set pane(pane) {
            this._pane = pane;
        }
        constructor(id, component) {
            super(id, component);
            this._onDidExpansionChange = new Emitter({
                replay: true,
            });
            this.onDidExpansionChange = this._onDidExpansionChange.event;
            this._onMouseEnter = new Emitter({});
            this.onMouseEnter = this._onMouseEnter.event;
            this._onMouseLeave = new Emitter({});
            this.onMouseLeave = this._onMouseLeave.event;
            this.addDisposables(this._onDidExpansionChange, this._onMouseEnter, this._onMouseLeave);
        }
        setExpanded(isExpanded) {
            var _a;
            (_a = this._pane) === null || _a === void 0 ? void 0 : _a.setExpanded(isExpanded);
        }
        get isExpanded() {
            var _a;
            return !!((_a = this._pane) === null || _a === void 0 ? void 0 : _a.isExpanded());
        }
    }

    class BasePanelView extends CompositeDisposable {
        get element() {
            return this._element;
        }
        get width() {
            return this._width;
        }
        get height() {
            return this._height;
        }
        get params() {
            var _a;
            return (_a = this._params) === null || _a === void 0 ? void 0 : _a.params;
        }
        constructor(id, component, api) {
            super();
            this.id = id;
            this.component = component;
            this.api = api;
            this._height = 0;
            this._width = 0;
            this._element = document.createElement('div');
            this._element.tabIndex = -1;
            this._element.style.outline = 'none';
            this._element.style.height = '100%';
            this._element.style.width = '100%';
            this._element.style.overflow = 'hidden';
            const focusTracker = trackFocus(this._element);
            this.addDisposables(this.api, focusTracker.onDidFocus(() => {
                this.api._onDidChangeFocus.fire({ isFocused: true });
            }), focusTracker.onDidBlur(() => {
                this.api._onDidChangeFocus.fire({ isFocused: false });
            }), focusTracker);
        }
        focus() {
            const event = new WillFocusEvent();
            this.api._onWillFocus.fire(event);
            if (event.defaultPrevented) {
                return;
            }
            this._element.focus();
        }
        layout(width, height) {
            this._width = width;
            this._height = height;
            this.api._onDidDimensionChange.fire({ width, height });
            if (this.part) {
                if (this._params) {
                    this.part.update(this._params.params);
                }
            }
        }
        init(parameters) {
            this._params = parameters;
            this.part = this.getComponent();
        }
        update(event) {
            var _a, _b;
            // merge the new parameters with the existing parameters
            this._params = Object.assign(Object.assign({}, this._params), { params: Object.assign(Object.assign({}, (_a = this._params) === null || _a === void 0 ? void 0 : _a.params), event.params) });
            /**
             * delete new keys that have a value of undefined,
             * allow values of null
             */
            for (const key of Object.keys(event.params)) {
                if (event.params[key] === undefined) {
                    delete this._params.params[key];
                }
            }
            // update the view with the updated props
            (_b = this.part) === null || _b === void 0 ? void 0 : _b.update({ params: this._params.params });
        }
        toJSON() {
            var _a, _b;
            const params = (_b = (_a = this._params) === null || _a === void 0 ? void 0 : _a.params) !== null && _b !== void 0 ? _b : {};
            return {
                id: this.id,
                component: this.component,
                params: Object.keys(params).length > 0 ? params : undefined,
            };
        }
        dispose() {
            var _a;
            this.api.dispose();
            (_a = this.part) === null || _a === void 0 ? void 0 : _a.dispose();
            super.dispose();
        }
    }

    class PaneviewPanel extends BasePanelView {
        set orientation(value) {
            this._orientation = value;
        }
        get orientation() {
            return this._orientation;
        }
        get minimumSize() {
            const headerSize = this.headerSize;
            const expanded = this.isExpanded();
            const minimumBodySize = expanded ? this._minimumBodySize : 0;
            return headerSize + minimumBodySize;
        }
        get maximumSize() {
            const headerSize = this.headerSize;
            const expanded = this.isExpanded();
            const maximumBodySize = expanded ? this._maximumBodySize : 0;
            return headerSize + maximumBodySize;
        }
        get size() {
            return this._size;
        }
        get orthogonalSize() {
            return this._orthogonalSize;
        }
        set orthogonalSize(size) {
            this._orthogonalSize = size;
        }
        get minimumBodySize() {
            return this._minimumBodySize;
        }
        set minimumBodySize(value) {
            this._minimumBodySize = typeof value === 'number' ? value : 0;
        }
        get maximumBodySize() {
            return this._maximumBodySize;
        }
        set maximumBodySize(value) {
            this._maximumBodySize =
                typeof value === 'number' ? value : Number.POSITIVE_INFINITY;
        }
        get headerVisible() {
            return this._headerVisible;
        }
        set headerVisible(value) {
            this._headerVisible = value;
            this.header.style.display = value ? '' : 'none';
        }
        constructor(options) {
            super(options.id, options.component, new PaneviewPanelApiImpl(options.id, options.component));
            this._onDidChangeExpansionState = new Emitter({ replay: true });
            this.onDidChangeExpansionState = this._onDidChangeExpansionState.event;
            this._onDidChange = new Emitter();
            this.onDidChange = this._onDidChange.event;
            this._orthogonalSize = 0;
            this._size = 0;
            this._isExpanded = false;
            this.api.pane = this; // TODO cannot use 'this' before 'super'
            this.api.initialize(this);
            this.headerSize = options.headerSize;
            this.headerComponent = options.headerComponent;
            this._minimumBodySize = options.minimumBodySize;
            this._maximumBodySize = options.maximumBodySize;
            this._isExpanded = options.isExpanded;
            this._headerVisible = options.isHeaderVisible;
            this._onDidChangeExpansionState.fire(this.isExpanded()); // initialize value
            this._orientation = options.orientation;
            this.element.classList.add('dv-pane');
            this.addDisposables(this.api.onWillVisibilityChange((event) => {
                const { isVisible } = event;
                const { accessor } = this._params;
                accessor.setVisible(this, isVisible);
            }), this.api.onDidSizeChange((event) => {
                this._onDidChange.fire({ size: event.size });
            }), addDisposableListener(this.element, 'mouseenter', (ev) => {
                this.api._onMouseEnter.fire(ev);
            }), addDisposableListener(this.element, 'mouseleave', (ev) => {
                this.api._onMouseLeave.fire(ev);
            }));
            this.addDisposables(this._onDidChangeExpansionState, this.onDidChangeExpansionState((isPanelExpanded) => {
                this.api._onDidExpansionChange.fire({
                    isExpanded: isPanelExpanded,
                });
            }), this.api.onDidFocusChange((e) => {
                if (!this.header) {
                    return;
                }
                if (e.isFocused) {
                    addClasses(this.header, 'focused');
                }
                else {
                    removeClasses(this.header, 'focused');
                }
            }));
            this.renderOnce();
        }
        setVisible(isVisible) {
            this.api._onDidVisibilityChange.fire({ isVisible });
        }
        setActive(isActive) {
            this.api._onDidActiveChange.fire({ isActive });
        }
        isExpanded() {
            return this._isExpanded;
        }
        setExpanded(expanded) {
            if (this._isExpanded === expanded) {
                return;
            }
            this._isExpanded = expanded;
            if (expanded) {
                if (this.animationTimer) {
                    clearTimeout(this.animationTimer);
                }
                if (this.body) {
                    this.element.appendChild(this.body);
                }
            }
            else {
                this.animationTimer = setTimeout(() => {
                    var _a;
                    (_a = this.body) === null || _a === void 0 ? void 0 : _a.remove();
                }, 200);
            }
            this._onDidChange.fire(expanded ? { size: this.width } : {});
            this._onDidChangeExpansionState.fire(expanded);
        }
        layout(size, orthogonalSize) {
            this._size = size;
            this._orthogonalSize = orthogonalSize;
            const [width, height] = this.orientation === exports.Orientation.HORIZONTAL
                ? [size, orthogonalSize]
                : [orthogonalSize, size];
            super.layout(width, height);
        }
        init(parameters) {
            var _a, _b;
            super.init(parameters);
            if (typeof parameters.minimumBodySize === 'number') {
                this.minimumBodySize = parameters.minimumBodySize;
            }
            if (typeof parameters.maximumBodySize === 'number') {
                this.maximumBodySize = parameters.maximumBodySize;
            }
            this.bodyPart = this.getBodyComponent();
            this.headerPart = this.getHeaderComponent();
            this.bodyPart.init(Object.assign(Object.assign({}, parameters), { api: this.api }));
            this.headerPart.init(Object.assign(Object.assign({}, parameters), { api: this.api }));
            (_a = this.body) === null || _a === void 0 ? void 0 : _a.append(this.bodyPart.element);
            (_b = this.header) === null || _b === void 0 ? void 0 : _b.append(this.headerPart.element);
            if (typeof parameters.isExpanded === 'boolean') {
                this.setExpanded(parameters.isExpanded);
            }
        }
        toJSON() {
            const params = this._params;
            return Object.assign(Object.assign({}, super.toJSON()), { headerComponent: this.headerComponent, title: params.title });
        }
        renderOnce() {
            this.header = document.createElement('div');
            this.header.tabIndex = 0;
            this.header.className = 'dv-pane-header';
            this.header.style.height = `${this.headerSize}px`;
            this.header.style.lineHeight = `${this.headerSize}px`;
            this.header.style.minHeight = `${this.headerSize}px`;
            this.header.style.maxHeight = `${this.headerSize}px`;
            this.element.appendChild(this.header);
            this.body = document.createElement('div');
            this.body.className = 'dv-pane-body';
            this.element.appendChild(this.body);
        }
        // TODO slightly hacky by-pass of the component to create a body and header component
        getComponent() {
            return {
                update: (params) => {
                    var _a, _b;
                    (_a = this.bodyPart) === null || _a === void 0 ? void 0 : _a.update({ params });
                    (_b = this.headerPart) === null || _b === void 0 ? void 0 : _b.update({ params });
                },
                dispose: () => {
                    var _a, _b;
                    (_a = this.bodyPart) === null || _a === void 0 ? void 0 : _a.dispose();
                    (_b = this.headerPart) === null || _b === void 0 ? void 0 : _b.dispose();
                },
            };
        }
    }

    class DraggablePaneviewPanel extends PaneviewPanel {
        constructor(options) {
            super({
                id: options.id,
                component: options.component,
                headerComponent: options.headerComponent,
                orientation: options.orientation,
                isExpanded: options.isExpanded,
                isHeaderVisible: true,
                headerSize: options.headerSize,
                minimumBodySize: options.minimumBodySize,
                maximumBodySize: options.maximumBodySize,
            });
            this._onDidDrop = new Emitter();
            this.onDidDrop = this._onDidDrop.event;
            this._onUnhandledDragOverEvent = new Emitter();
            this.onUnhandledDragOverEvent = this._onUnhandledDragOverEvent.event;
            this.accessor = options.accessor;
            this.addDisposables(this._onDidDrop, this._onUnhandledDragOverEvent);
            if (!options.disableDnd) {
                this.initDragFeatures();
            }
        }
        initDragFeatures() {
            if (!this.header) {
                return;
            }
            const id = this.id;
            const accessorId = this.accessor.id;
            this.header.draggable = true;
            this.handler = new (class PaneDragHandler extends DragHandler {
                getData() {
                    LocalSelectionTransfer.getInstance().setData([new PaneTransfer(accessorId, id)], PaneTransfer.prototype);
                    return {
                        dispose: () => {
                            LocalSelectionTransfer.getInstance().clearData(PaneTransfer.prototype);
                        },
                    };
                }
            })(this.header);
            this.target = new Droptarget(this.element, {
                acceptedTargetZones: ['top', 'bottom'],
                overlayModel: {
                    activationSize: { type: 'percentage', value: 50 },
                },
                canDisplayOverlay: (event, position) => {
                    const data = getPaneData();
                    if (data) {
                        if (data.paneId !== this.id &&
                            data.viewId === this.accessor.id) {
                            return true;
                        }
                    }
                    const firedEvent = new PaneviewUnhandledDragOverEvent(event, position, getPaneData, this);
                    this._onUnhandledDragOverEvent.fire(firedEvent);
                    return firedEvent.isAccepted;
                },
            });
            this.addDisposables(this._onDidDrop, this.handler, this.target, this.target.onDrop((event) => {
                this.onDrop(event);
            }));
        }
        onDrop(event) {
            const data = getPaneData();
            if (!data || data.viewId !== this.accessor.id) {
                // if there is no local drag event for this panel
                // or if the drag event was creating by another Paneview instance
                this._onDidDrop.fire(Object.assign(Object.assign({}, event), { panel: this, api: new PaneviewApi(this.accessor), getData: getPaneData }));
                return;
            }
            const containerApi = this._params
                .containerApi;
            const panelId = data.paneId;
            const existingPanel = containerApi.getPanel(panelId);
            if (!existingPanel) {
                // if the panel doesn't exist
                this._onDidDrop.fire(Object.assign(Object.assign({}, event), { panel: this, getData: getPaneData, api: new PaneviewApi(this.accessor) }));
                return;
            }
            const allPanels = containerApi.panels;
            const fromIndex = allPanels.indexOf(existingPanel);
            let toIndex = containerApi.panels.indexOf(this);
            if (event.position === 'left' || event.position === 'top') {
                toIndex = Math.max(0, toIndex - 1);
            }
            if (event.position === 'right' || event.position === 'bottom') {
                if (fromIndex > toIndex) {
                    toIndex++;
                }
                toIndex = Math.min(allPanels.length - 1, toIndex);
            }
            containerApi.movePanel(fromIndex, toIndex);
        }
    }

    class ContentContainer extends CompositeDisposable {
        get element() {
            return this._element;
        }
        constructor(accessor, group) {
            super();
            this.accessor = accessor;
            this.group = group;
            this.disposable = new MutableDisposable();
            this._onDidFocus = new Emitter();
            this.onDidFocus = this._onDidFocus.event;
            this._onDidBlur = new Emitter();
            this.onDidBlur = this._onDidBlur.event;
            this._element = document.createElement('div');
            this._element.className = 'dv-content-container';
            this._element.tabIndex = -1;
            this.addDisposables(this._onDidFocus, this._onDidBlur);
            const target = group.dropTargetContainer;
            this.dropTarget = new Droptarget(this.element, {
                getOverlayOutline: () => {
                    var _a;
                    return ((_a = accessor.options.theme) === null || _a === void 0 ? void 0 : _a.dndPanelOverlay) === 'group'
                        ? this.element.parentElement
                        : null;
                },
                className: 'dv-drop-target-content',
                acceptedTargetZones: ['top', 'bottom', 'left', 'right', 'center'],
                canDisplayOverlay: (event, position) => {
                    if (this.group.locked === 'no-drop-target' ||
                        (this.group.locked && position === 'center')) {
                        return false;
                    }
                    const data = getPanelData();
                    if (!data &&
                        event.shiftKey &&
                        this.group.location.type !== 'floating') {
                        return false;
                    }
                    if (data && data.viewId === this.accessor.id) {
                        return true;
                    }
                    return this.group.canDisplayOverlay(event, position, 'content');
                },
                getOverrideTarget: target ? () => target.model : undefined,
            });
            this.addDisposables(this.dropTarget);
        }
        show() {
            this.element.style.display = '';
        }
        hide() {
            this.element.style.display = 'none';
        }
        renderPanel(panel, options = { asActive: true }) {
            const doRender = options.asActive ||
                (this.panel && this.group.isPanelActive(this.panel));
            if (this.panel &&
                this.panel.view.content.element.parentElement === this._element) {
                /**
                 * If the currently attached panel is mounted directly to the content then remove it
                 */
                this._element.removeChild(this.panel.view.content.element);
            }
            this.panel = panel;
            let container;
            switch (panel.api.renderer) {
                case 'onlyWhenVisible':
                    this.group.renderContainer.detatch(panel);
                    if (this.panel) {
                        if (doRender) {
                            this._element.appendChild(this.panel.view.content.element);
                        }
                    }
                    container = this._element;
                    break;
                case 'always':
                    if (panel.view.content.element.parentElement === this._element) {
                        this._element.removeChild(panel.view.content.element);
                    }
                    container = this.group.renderContainer.attach({
                        panel,
                        referenceContainer: this,
                    });
                    break;
                default:
                    throw new Error(`dockview: invalid renderer type '${panel.api.renderer}'`);
            }
            if (doRender) {
                const focusTracker = trackFocus(container);
                const disposable = new CompositeDisposable();
                disposable.addDisposables(focusTracker, focusTracker.onDidFocus(() => this._onDidFocus.fire()), focusTracker.onDidBlur(() => this._onDidBlur.fire()));
                this.disposable.value = disposable;
            }
        }
        openPanel(panel) {
            if (this.panel === panel) {
                return;
            }
            this.renderPanel(panel);
        }
        layout(_width, _height) {
            // noop
        }
        closePanel() {
            var _a;
            if (this.panel) {
                if (this.panel.api.renderer === 'onlyWhenVisible') {
                    (_a = this.panel.view.content.element.parentElement) === null || _a === void 0 ? void 0 : _a.removeChild(this.panel.view.content.element);
                }
            }
            this.panel = undefined;
        }
        dispose() {
            this.disposable.dispose();
            super.dispose();
        }
    }

    function addGhostImage(dataTransfer, ghostElement, options) {
        var _a, _b;
        // class dockview provides to force ghost image to be drawn on a different layer and prevent weird rendering issues
        addClasses(ghostElement, 'dv-dragged');
        // move the element off-screen initially otherwise it may in some cases be rendered at (0,0) momentarily
        ghostElement.style.top = '-9999px';
        document.body.appendChild(ghostElement);
        dataTransfer.setDragImage(ghostElement, (_a = options === null || options === void 0 ? void 0 : options.x) !== null && _a !== void 0 ? _a : 0, (_b = options === null || options === void 0 ? void 0 : options.y) !== null && _b !== void 0 ? _b : 0);
        setTimeout(() => {
            removeClasses(ghostElement, 'dv-dragged');
            ghostElement.remove();
        }, 0);
    }

    class TabDragHandler extends DragHandler {
        constructor(element, accessor, group, panel, disabled) {
            super(element, disabled);
            this.accessor = accessor;
            this.group = group;
            this.panel = panel;
            this.panelTransfer = LocalSelectionTransfer.getInstance();
        }
        getData(event) {
            this.panelTransfer.setData([new PanelTransfer(this.accessor.id, this.group.id, this.panel.id)], PanelTransfer.prototype);
            return {
                dispose: () => {
                    this.panelTransfer.clearData(PanelTransfer.prototype);
                },
            };
        }
    }
    class Tab extends CompositeDisposable {
        get element() {
            return this._element;
        }
        constructor(panel, accessor, group) {
            super();
            this.panel = panel;
            this.accessor = accessor;
            this.group = group;
            this.content = undefined;
            this._onPointDown = new Emitter();
            this.onPointerDown = this._onPointDown.event;
            this._onDropped = new Emitter();
            this.onDrop = this._onDropped.event;
            this._onDragStart = new Emitter();
            this.onDragStart = this._onDragStart.event;
            this._element = document.createElement('div');
            this._element.className = 'dv-tab';
            this._element.tabIndex = 0;
            this._element.draggable = !this.accessor.options.disableDnd;
            toggleClass(this.element, 'dv-inactive-tab', true);
            this.dragHandler = new TabDragHandler(this._element, this.accessor, this.group, this.panel, !!this.accessor.options.disableDnd);
            this.dropTarget = new Droptarget(this._element, {
                acceptedTargetZones: ['left', 'right'],
                overlayModel: { activationSize: { value: 50, type: 'percentage' } },
                canDisplayOverlay: (event, position) => {
                    if (this.group.locked) {
                        return false;
                    }
                    const data = getPanelData();
                    if (data && this.accessor.id === data.viewId) {
                        return true;
                    }
                    return this.group.model.canDisplayOverlay(event, position, 'tab');
                },
                getOverrideTarget: () => { var _a; return (_a = group.model.dropTargetContainer) === null || _a === void 0 ? void 0 : _a.model; },
            });
            this.onWillShowOverlay = this.dropTarget.onWillShowOverlay;
            this.addDisposables(this._onPointDown, this._onDropped, this._onDragStart, this.dragHandler.onDragStart((event) => {
                if (event.dataTransfer) {
                    const style = getComputedStyle(this.element);
                    const newNode = this.element.cloneNode(true);
                    Array.from(style).forEach((key) => newNode.style.setProperty(key, style.getPropertyValue(key), style.getPropertyPriority(key)));
                    newNode.style.position = 'absolute';
                    addGhostImage(event.dataTransfer, newNode, {
                        y: -10,
                        x: 30,
                    });
                }
                this._onDragStart.fire(event);
            }), this.dragHandler, addDisposableListener(this._element, 'pointerdown', (event) => {
                this._onPointDown.fire(event);
            }), this.dropTarget.onDrop((event) => {
                this._onDropped.fire(event);
            }), this.dropTarget);
        }
        setActive(isActive) {
            toggleClass(this.element, 'dv-active-tab', isActive);
            toggleClass(this.element, 'dv-inactive-tab', !isActive);
        }
        setContent(part) {
            if (this.content) {
                this._element.removeChild(this.content.element);
            }
            this.content = part;
            this._element.appendChild(this.content.element);
        }
        updateDragAndDropState() {
            this._element.draggable = !this.accessor.options.disableDnd;
            this.dragHandler.setDisabled(!!this.accessor.options.disableDnd);
        }
        dispose() {
            super.dispose();
        }
    }

    class WillShowOverlayLocationEvent {
        get kind() {
            return this.options.kind;
        }
        get nativeEvent() {
            return this.event.nativeEvent;
        }
        get position() {
            return this.event.position;
        }
        get defaultPrevented() {
            return this.event.defaultPrevented;
        }
        get panel() {
            return this.options.panel;
        }
        get api() {
            return this.options.api;
        }
        get group() {
            return this.options.group;
        }
        preventDefault() {
            this.event.preventDefault();
        }
        getData() {
            return this.options.getData();
        }
        constructor(event, options) {
            this.event = event;
            this.options = options;
        }
    }

    class GroupDragHandler extends DragHandler {
        constructor(element, accessor, group, disabled) {
            super(element, disabled);
            this.accessor = accessor;
            this.group = group;
            this.panelTransfer = LocalSelectionTransfer.getInstance();
            this.addDisposables(addDisposableListener(element, 'pointerdown', (e) => {
                if (e.shiftKey) {
                    /**
                     * You cannot call e.preventDefault() because that will prevent drag events from firing
                     * but we also need to stop any group overlay drag events from occuring
                     * Use a custom event marker that can be checked by the overlay drag events
                     */
                    quasiPreventDefault(e);
                }
            }, true));
        }
        isCancelled(_event) {
            if (this.group.api.location.type === 'floating' && !_event.shiftKey) {
                return true;
            }
            return false;
        }
        getData(dragEvent) {
            const dataTransfer = dragEvent.dataTransfer;
            this.panelTransfer.setData([new PanelTransfer(this.accessor.id, this.group.id, null)], PanelTransfer.prototype);
            const style = window.getComputedStyle(this.el);
            const bgColor = style.getPropertyValue('--dv-activegroup-visiblepanel-tab-background-color');
            const color = style.getPropertyValue('--dv-activegroup-visiblepanel-tab-color');
            if (dataTransfer) {
                const ghostElement = document.createElement('div');
                ghostElement.style.backgroundColor = bgColor;
                ghostElement.style.color = color;
                ghostElement.style.padding = '2px 8px';
                ghostElement.style.height = '24px';
                ghostElement.style.fontSize = '11px';
                ghostElement.style.lineHeight = '20px';
                ghostElement.style.borderRadius = '12px';
                ghostElement.style.position = 'absolute';
                ghostElement.style.pointerEvents = 'none';
                ghostElement.style.top = '-9999px';
                ghostElement.textContent = `Multiple Panels (${this.group.size})`;
                addGhostImage(dataTransfer, ghostElement, { y: -10, x: 30 });
            }
            return {
                dispose: () => {
                    this.panelTransfer.clearData(PanelTransfer.prototype);
                },
            };
        }
    }

    class VoidContainer extends CompositeDisposable {
        get element() {
            return this._element;
        }
        constructor(accessor, group) {
            super();
            this.accessor = accessor;
            this.group = group;
            this._onDrop = new Emitter();
            this.onDrop = this._onDrop.event;
            this._onDragStart = new Emitter();
            this.onDragStart = this._onDragStart.event;
            this._element = document.createElement('div');
            this._element.className = 'dv-void-container';
            this._element.draggable = !this.accessor.options.disableDnd;
            toggleClass(this._element, 'dv-draggable', !this.accessor.options.disableDnd);
            this.addDisposables(this._onDrop, this._onDragStart, addDisposableListener(this._element, 'pointerdown', () => {
                this.accessor.doSetGroupActive(this.group);
            }));
            this.handler = new GroupDragHandler(this._element, accessor, group, !!this.accessor.options.disableDnd);
            this.dropTarget = new Droptarget(this._element, {
                acceptedTargetZones: ['center'],
                canDisplayOverlay: (event, position) => {
                    const data = getPanelData();
                    if (data && this.accessor.id === data.viewId) {
                        return true;
                    }
                    return group.model.canDisplayOverlay(event, position, 'header_space');
                },
                getOverrideTarget: () => { var _a; return (_a = group.model.dropTargetContainer) === null || _a === void 0 ? void 0 : _a.model; },
            });
            this.onWillShowOverlay = this.dropTarget.onWillShowOverlay;
            this.addDisposables(this.handler, this.handler.onDragStart((event) => {
                this._onDragStart.fire(event);
            }), this.dropTarget.onDrop((event) => {
                this._onDrop.fire(event);
            }), this.dropTarget);
        }
        updateDragAndDropState() {
            this._element.draggable = !this.accessor.options.disableDnd;
            toggleClass(this._element, 'dv-draggable', !this.accessor.options.disableDnd);
            this.handler.setDisabled(!!this.accessor.options.disableDnd);
        }
    }

    class Scrollbar extends CompositeDisposable {
        get element() {
            return this._element;
        }
        constructor(scrollableElement) {
            super();
            this.scrollableElement = scrollableElement;
            this._scrollLeft = 0;
            this._element = document.createElement('div');
            this._element.className = 'dv-scrollable';
            this._horizontalScrollbar = document.createElement('div');
            this._horizontalScrollbar.className = 'dv-scrollbar-horizontal';
            this.element.appendChild(scrollableElement);
            this.element.appendChild(this._horizontalScrollbar);
            this.addDisposables(addDisposableListener(this.element, 'wheel', (event) => {
                this._scrollLeft += event.deltaY * Scrollbar.MouseWheelSpeed;
                this.calculateScrollbarStyles();
            }), addDisposableListener(this._horizontalScrollbar, 'pointerdown', (event) => {
                event.preventDefault();
                toggleClass(this.element, 'dv-scrollable-scrolling', true);
                const originalClientX = event.clientX;
                const originalScrollLeft = this._scrollLeft;
                const onPointerMove = (event) => {
                    const deltaX = event.clientX - originalClientX;
                    const { clientWidth } = this.element;
                    const { scrollWidth } = this.scrollableElement;
                    const p = clientWidth / scrollWidth;
                    this._scrollLeft = originalScrollLeft + deltaX / p;
                    this.calculateScrollbarStyles();
                };
                const onEnd = () => {
                    toggleClass(this.element, 'dv-scrollable-scrolling', false);
                    document.removeEventListener('pointermove', onPointerMove);
                    document.removeEventListener('pointerup', onEnd);
                    document.removeEventListener('pointercancel', onEnd);
                };
                document.addEventListener('pointermove', onPointerMove);
                document.addEventListener('pointerup', onEnd);
                document.addEventListener('pointercancel', onEnd);
            }), addDisposableListener(this.element, 'scroll', () => {
                this.calculateScrollbarStyles();
            }), addDisposableListener(this.scrollableElement, 'scroll', () => {
                this._scrollLeft = this.scrollableElement.scrollLeft;
                this.calculateScrollbarStyles();
            }), watchElementResize(this.element, () => {
                toggleClass(this.element, 'dv-scrollable-resizing', true);
                if (this._animationTimer) {
                    clearTimeout(this._animationTimer);
                }
                this._animationTimer = setTimeout(() => {
                    clearTimeout(this._animationTimer);
                    toggleClass(this.element, 'dv-scrollable-resizing', false);
                }, 500);
                this.calculateScrollbarStyles();
            }));
        }
        calculateScrollbarStyles() {
            const { clientWidth } = this.element;
            const { scrollWidth } = this.scrollableElement;
            const hasScrollbar = scrollWidth > clientWidth;
            if (hasScrollbar) {
                const px = clientWidth * (clientWidth / scrollWidth);
                this._horizontalScrollbar.style.width = `${px}px`;
                this._scrollLeft = clamp(this._scrollLeft, 0, this.scrollableElement.scrollWidth - clientWidth);
                this.scrollableElement.scrollLeft = this._scrollLeft;
                const percentageComplete = this._scrollLeft / (scrollWidth - clientWidth);
                this._horizontalScrollbar.style.left = `${(clientWidth - px) * percentageComplete}px`;
            }
            else {
                this._horizontalScrollbar.style.width = `0px`;
                this._horizontalScrollbar.style.left = `0px`;
                this._scrollLeft = 0;
            }
        }
    }
    Scrollbar.MouseWheelSpeed = 1;

    class Tabs extends CompositeDisposable {
        get showTabsOverflowControl() {
            return this._showTabsOverflowControl;
        }
        set showTabsOverflowControl(value) {
            if (this._showTabsOverflowControl == value) {
                return;
            }
            this._showTabsOverflowControl = value;
            if (value) {
                const observer = new OverflowObserver(this._tabsList);
                this._observerDisposable.value = new CompositeDisposable(observer, observer.onDidChange((event) => {
                    const hasOverflow = event.hasScrollX || event.hasScrollY;
                    this.toggleDropdown({ reset: !hasOverflow });
                }), addDisposableListener(this._tabsList, 'scroll', () => {
                    this.toggleDropdown({ reset: false });
                }));
            }
        }
        get element() {
            return this._element;
        }
        get panels() {
            return this._tabs.map((_) => _.value.panel.id);
        }
        get size() {
            return this._tabs.length;
        }
        get tabs() {
            return this._tabs.map((_) => _.value);
        }
        constructor(group, accessor, options) {
            super();
            this.group = group;
            this.accessor = accessor;
            this._observerDisposable = new MutableDisposable();
            this._tabs = [];
            this.selectedIndex = -1;
            this._showTabsOverflowControl = false;
            this._onTabDragStart = new Emitter();
            this.onTabDragStart = this._onTabDragStart.event;
            this._onDrop = new Emitter();
            this.onDrop = this._onDrop.event;
            this._onWillShowOverlay = new Emitter();
            this.onWillShowOverlay = this._onWillShowOverlay.event;
            this._onOverflowTabsChange = new Emitter();
            this.onOverflowTabsChange = this._onOverflowTabsChange.event;
            this._tabsList = document.createElement('div');
            this._tabsList.className = 'dv-tabs-container dv-horizontal';
            this.showTabsOverflowControl = options.showTabsOverflowControl;
            if (accessor.options.scrollbars === 'native') {
                this._element = this._tabsList;
            }
            else {
                const scrollbar = new Scrollbar(this._tabsList);
                this._element = scrollbar.element;
                this.addDisposables(scrollbar);
            }
            this.addDisposables(this._onOverflowTabsChange, this._observerDisposable, this._onWillShowOverlay, this._onDrop, this._onTabDragStart, addDisposableListener(this.element, 'pointerdown', (event) => {
                if (event.defaultPrevented) {
                    return;
                }
                const isLeftClick = event.button === 0;
                if (isLeftClick) {
                    this.accessor.doSetGroupActive(this.group);
                }
            }), exports.DockviewDisposable.from(() => {
                for (const { value, disposable } of this._tabs) {
                    disposable.dispose();
                    value.dispose();
                }
                this._tabs = [];
            }));
        }
        indexOf(id) {
            return this._tabs.findIndex((tab) => tab.value.panel.id === id);
        }
        isActive(tab) {
            return (this.selectedIndex > -1 &&
                this._tabs[this.selectedIndex].value === tab);
        }
        setActivePanel(panel) {
            let runningWidth = 0;
            for (const tab of this._tabs) {
                const isActivePanel = panel.id === tab.value.panel.id;
                tab.value.setActive(isActivePanel);
                if (isActivePanel) {
                    const element = tab.value.element;
                    const parentElement = element.parentElement;
                    if (runningWidth < parentElement.scrollLeft ||
                        runningWidth + element.clientWidth >
                            parentElement.scrollLeft + parentElement.clientWidth) {
                        parentElement.scrollLeft = runningWidth;
                    }
                }
                runningWidth += tab.value.element.clientWidth;
            }
        }
        openPanel(panel, index = this._tabs.length) {
            if (this._tabs.find((tab) => tab.value.panel.id === panel.id)) {
                return;
            }
            const tab = new Tab(panel, this.accessor, this.group);
            tab.setContent(panel.view.tab);
            const disposable = new CompositeDisposable(tab.onDragStart((event) => {
                this._onTabDragStart.fire({ nativeEvent: event, panel });
            }), tab.onPointerDown((event) => {
                if (event.defaultPrevented) {
                    return;
                }
                const isFloatingGroupsEnabled = !this.accessor.options.disableFloatingGroups;
                const isFloatingWithOnePanel = this.group.api.location.type === 'floating' &&
                    this.size === 1;
                if (isFloatingGroupsEnabled &&
                    !isFloatingWithOnePanel &&
                    event.shiftKey) {
                    event.preventDefault();
                    const panel = this.accessor.getGroupPanel(tab.panel.id);
                    const { top, left } = tab.element.getBoundingClientRect();
                    const { top: rootTop, left: rootLeft } = this.accessor.element.getBoundingClientRect();
                    this.accessor.addFloatingGroup(panel, {
                        x: left - rootLeft,
                        y: top - rootTop,
                        inDragMode: true,
                    });
                    return;
                }
                switch (event.button) {
                    case 0: // left click or touch
                        if (this.group.activePanel !== panel) {
                            this.group.model.openPanel(panel);
                        }
                        break;
                }
            }), tab.onDrop((event) => {
                this._onDrop.fire({
                    event: event.nativeEvent,
                    index: this._tabs.findIndex((x) => x.value === tab),
                });
            }), tab.onWillShowOverlay((event) => {
                this._onWillShowOverlay.fire(new WillShowOverlayLocationEvent(event, {
                    kind: 'tab',
                    panel: this.group.activePanel,
                    api: this.accessor.api,
                    group: this.group,
                    getData: getPanelData,
                }));
            }));
            const value = { value: tab, disposable };
            this.addTab(value, index);
        }
        delete(id) {
            const index = this.indexOf(id);
            const tabToRemove = this._tabs.splice(index, 1)[0];
            const { value, disposable } = tabToRemove;
            disposable.dispose();
            value.dispose();
            value.element.remove();
        }
        addTab(tab, index = this._tabs.length) {
            if (index < 0 || index > this._tabs.length) {
                throw new Error('invalid location');
            }
            this._tabsList.insertBefore(tab.value.element, this._tabsList.children[index]);
            this._tabs = [
                ...this._tabs.slice(0, index),
                tab,
                ...this._tabs.slice(index),
            ];
            if (this.selectedIndex < 0) {
                this.selectedIndex = index;
            }
        }
        toggleDropdown(options) {
            const tabs = options.reset
                ? []
                : this._tabs
                    .filter((tab) => !isChildEntirelyVisibleWithinParent(tab.value.element, this._tabsList))
                    .map((x) => x.value.panel.id);
            this._onOverflowTabsChange.fire({ tabs, reset: options.reset });
        }
        updateDragAndDropState() {
            for (const tab of this._tabs) {
                tab.value.updateDragAndDropState();
            }
        }
    }

    const createSvgElementFromPath = (params) => {
        const svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
        svg.setAttributeNS(null, 'height', params.height);
        svg.setAttributeNS(null, 'width', params.width);
        svg.setAttributeNS(null, 'viewBox', params.viewbox);
        svg.setAttributeNS(null, 'aria-hidden', 'false');
        svg.setAttributeNS(null, 'focusable', 'false');
        svg.classList.add('dv-svg');
        const path = document.createElementNS('http://www.w3.org/2000/svg', 'path');
        path.setAttributeNS(null, 'd', params.path);
        svg.appendChild(path);
        return svg;
    };
    const createCloseButton = () => createSvgElementFromPath({
        width: '11',
        height: '11',
        viewbox: '0 0 28 28',
        path: 'M2.1 27.3L0 25.2L11.55 13.65L0 2.1L2.1 0L13.65 11.55L25.2 0L27.3 2.1L15.75 13.65L27.3 25.2L25.2 27.3L13.65 15.75L2.1 27.3Z',
    });
    const createExpandMoreButton = () => createSvgElementFromPath({
        width: '11',
        height: '11',
        viewbox: '0 0 24 15',
        path: 'M12 14.15L0 2.15L2.15 0L12 9.9L21.85 0.0499992L24 2.2L12 14.15Z',
    });
    const createChevronRightButton = () => createSvgElementFromPath({
        width: '11',
        height: '11',
        viewbox: '0 0 15 25',
        path: 'M2.15 24.1L0 21.95L9.9 12.05L0 2.15L2.15 0L14.2 12.05L2.15 24.1Z',
    });

    function createDropdownElementHandle() {
        const el = document.createElement('div');
        el.className = 'dv-tabs-overflow-dropdown-default';
        const text = document.createElement('span');
        text.textContent = ``;
        const icon = createChevronRightButton();
        el.appendChild(icon);
        el.appendChild(text);
        return {
            element: el,
            update: (params) => {
                text.textContent = `${params.tabs}`;
            },
        };
    }

    class TabsContainer extends CompositeDisposable {
        get onTabDragStart() {
            return this.tabs.onTabDragStart;
        }
        get panels() {
            return this.tabs.panels;
        }
        get size() {
            return this.tabs.size;
        }
        get hidden() {
            return this._hidden;
        }
        set hidden(value) {
            this._hidden = value;
            this.element.style.display = value ? 'none' : '';
        }
        get element() {
            return this._element;
        }
        constructor(accessor, group) {
            super();
            this.accessor = accessor;
            this.group = group;
            this._hidden = false;
            this.dropdownPart = null;
            this._overflowTabs = [];
            this._dropdownDisposable = new MutableDisposable();
            this._onDrop = new Emitter();
            this.onDrop = this._onDrop.event;
            this._onGroupDragStart = new Emitter();
            this.onGroupDragStart = this._onGroupDragStart.event;
            this._onWillShowOverlay = new Emitter();
            this.onWillShowOverlay = this._onWillShowOverlay.event;
            this._element = document.createElement('div');
            this._element.className = 'dv-tabs-and-actions-container';
            toggleClass(this._element, 'dv-full-width-single-tab', this.accessor.options.singleTabMode === 'fullwidth');
            this.rightActionsContainer = document.createElement('div');
            this.rightActionsContainer.className = 'dv-right-actions-container';
            this.leftActionsContainer = document.createElement('div');
            this.leftActionsContainer.className = 'dv-left-actions-container';
            this.preActionsContainer = document.createElement('div');
            this.preActionsContainer.className = 'dv-pre-actions-container';
            this.tabs = new Tabs(group, accessor, {
                showTabsOverflowControl: !accessor.options.disableTabsOverflowList,
            });
            this.voidContainer = new VoidContainer(this.accessor, this.group);
            this._element.appendChild(this.preActionsContainer);
            this._element.appendChild(this.tabs.element);
            this._element.appendChild(this.leftActionsContainer);
            this._element.appendChild(this.voidContainer.element);
            this._element.appendChild(this.rightActionsContainer);
            this.addDisposables(this.tabs.onDrop((e) => this._onDrop.fire(e)), this.tabs.onWillShowOverlay((e) => this._onWillShowOverlay.fire(e)), accessor.onDidOptionsChange(() => {
                this.tabs.showTabsOverflowControl =
                    !accessor.options.disableTabsOverflowList;
            }), this.tabs.onOverflowTabsChange((event) => {
                this.toggleDropdown(event);
            }), this.tabs, this._onWillShowOverlay, this._onDrop, this._onGroupDragStart, this.voidContainer, this.voidContainer.onDragStart((event) => {
                this._onGroupDragStart.fire({
                    nativeEvent: event,
                    group: this.group,
                });
            }), this.voidContainer.onDrop((event) => {
                this._onDrop.fire({
                    event: event.nativeEvent,
                    index: this.tabs.size,
                });
            }), this.voidContainer.onWillShowOverlay((event) => {
                this._onWillShowOverlay.fire(new WillShowOverlayLocationEvent(event, {
                    kind: 'header_space',
                    panel: this.group.activePanel,
                    api: this.accessor.api,
                    group: this.group,
                    getData: getPanelData,
                }));
            }), addDisposableListener(this.voidContainer.element, 'pointerdown', (event) => {
                if (event.defaultPrevented) {
                    return;
                }
                const isFloatingGroupsEnabled = !this.accessor.options.disableFloatingGroups;
                if (isFloatingGroupsEnabled &&
                    event.shiftKey &&
                    this.group.api.location.type !== 'floating') {
                    event.preventDefault();
                    const { top, left } = this.element.getBoundingClientRect();
                    const { top: rootTop, left: rootLeft } = this.accessor.element.getBoundingClientRect();
                    this.accessor.addFloatingGroup(this.group, {
                        x: left - rootLeft + 20,
                        y: top - rootTop + 20,
                        inDragMode: true,
                    });
                }
            }));
        }
        show() {
            if (!this.hidden) {
                this.element.style.display = '';
            }
        }
        hide() {
            this._element.style.display = 'none';
        }
        setRightActionsElement(element) {
            if (this.rightActions === element) {
                return;
            }
            if (this.rightActions) {
                this.rightActions.remove();
                this.rightActions = undefined;
            }
            if (element) {
                this.rightActionsContainer.appendChild(element);
                this.rightActions = element;
            }
        }
        setLeftActionsElement(element) {
            if (this.leftActions === element) {
                return;
            }
            if (this.leftActions) {
                this.leftActions.remove();
                this.leftActions = undefined;
            }
            if (element) {
                this.leftActionsContainer.appendChild(element);
                this.leftActions = element;
            }
        }
        setPrefixActionsElement(element) {
            if (this.preActions === element) {
                return;
            }
            if (this.preActions) {
                this.preActions.remove();
                this.preActions = undefined;
            }
            if (element) {
                this.preActionsContainer.appendChild(element);
                this.preActions = element;
            }
        }
        isActive(tab) {
            return this.tabs.isActive(tab);
        }
        indexOf(id) {
            return this.tabs.indexOf(id);
        }
        setActive(_isGroupActive) {
            // noop
        }
        delete(id) {
            this.tabs.delete(id);
            this.updateClassnames();
        }
        setActivePanel(panel) {
            this.tabs.setActivePanel(panel);
        }
        openPanel(panel, index = this.tabs.size) {
            this.tabs.openPanel(panel, index);
            this.updateClassnames();
        }
        closePanel(panel) {
            this.delete(panel.id);
        }
        updateClassnames() {
            toggleClass(this._element, 'dv-single-tab', this.size === 1);
        }
        toggleDropdown(options) {
            const tabs = options.reset ? [] : options.tabs;
            this._overflowTabs = tabs;
            if (this._overflowTabs.length > 0 && this.dropdownPart) {
                this.dropdownPart.update({ tabs: tabs.length });
                return;
            }
            if (this._overflowTabs.length === 0) {
                this._dropdownDisposable.dispose();
                return;
            }
            const root = document.createElement('div');
            root.className = 'dv-tabs-overflow-dropdown-root';
            const part = createDropdownElementHandle();
            part.update({ tabs: tabs.length });
            this.dropdownPart = part;
            root.appendChild(part.element);
            this.rightActionsContainer.prepend(root);
            this._dropdownDisposable.value = new CompositeDisposable(exports.DockviewDisposable.from(() => {
                var _a, _b;
                root.remove();
                (_b = (_a = this.dropdownPart) === null || _a === void 0 ? void 0 : _a.dispose) === null || _b === void 0 ? void 0 : _b.call(_a);
                this.dropdownPart = null;
            }), addDisposableListener(root, 'pointerdown', (event) => {
                event.preventDefault();
            }, { capture: true }), addDisposableListener(root, 'click', (event) => {
                const el = document.createElement('div');
                el.style.overflow = 'auto';
                el.className = 'dv-tabs-overflow-container';
                for (const tab of this.tabs.tabs.filter((tab) => this._overflowTabs.includes(tab.panel.id))) {
                    const panelObject = this.group.panels.find((panel) => panel === tab.panel);
                    const tabComponent = panelObject.view.createTabRenderer('headerOverflow');
                    const child = tabComponent.element;
                    const wrapper = document.createElement('div');
                    toggleClass(wrapper, 'dv-tab', true);
                    toggleClass(wrapper, 'dv-active-tab', panelObject.api.isActive);
                    toggleClass(wrapper, 'dv-inactive-tab', !panelObject.api.isActive);
                    wrapper.addEventListener('click', (event) => {
                        this.accessor.popupService.close();
                        if (event.defaultPrevented) {
                            return;
                        }
                        tab.element.scrollIntoView();
                        tab.panel.api.setActive();
                    });
                    wrapper.appendChild(child);
                    el.appendChild(wrapper);
                }
                const relativeParent = findRelativeZIndexParent(root);
                this.accessor.popupService.openPopover(el, {
                    x: event.clientX,
                    y: event.clientY,
                    zIndex: (relativeParent === null || relativeParent === void 0 ? void 0 : relativeParent.style.zIndex)
                        ? `calc(${relativeParent.style.zIndex} * 2)`
                        : undefined,
                });
            }));
        }
        updateDragAndDropState() {
            this.tabs.updateDragAndDropState();
            this.voidContainer.updateDragAndDropState();
        }
    }

    class DockviewUnhandledDragOverEvent extends AcceptableEvent {
        constructor(nativeEvent, target, position, getData, group) {
            super();
            this.nativeEvent = nativeEvent;
            this.target = target;
            this.position = position;
            this.getData = getData;
            this.group = group;
        }
    }
    const PROPERTY_KEYS_DOCKVIEW = (() => {
        /**
         * by readong the keys from an empty value object TypeScript will error
         * when we add or remove new properties to `DockviewOptions`
         */
        const properties = {
            disableAutoResizing: undefined,
            hideBorders: undefined,
            singleTabMode: undefined,
            disableFloatingGroups: undefined,
            floatingGroupBounds: undefined,
            popoutUrl: undefined,
            defaultRenderer: undefined,
            debug: undefined,
            rootOverlayModel: undefined,
            locked: undefined,
            disableDnd: undefined,
            className: undefined,
            noPanelsOverlay: undefined,
            dndEdges: undefined,
            theme: undefined,
            disableTabsOverflowList: undefined,
            scrollbars: undefined,
        };
        return Object.keys(properties);
    })();
    function isPanelOptionsWithPanel(data) {
        if (data.referencePanel) {
            return true;
        }
        return false;
    }
    function isPanelOptionsWithGroup(data) {
        if (data.referenceGroup) {
            return true;
        }
        return false;
    }
    function isGroupOptionsWithPanel(data) {
        if (data.referencePanel) {
            return true;
        }
        return false;
    }
    function isGroupOptionsWithGroup(data) {
        if (data.referenceGroup) {
            return true;
        }
        return false;
    }

    class DockviewDidDropEvent extends DockviewEvent {
        get nativeEvent() {
            return this.options.nativeEvent;
        }
        get position() {
            return this.options.position;
        }
        get panel() {
            return this.options.panel;
        }
        get group() {
            return this.options.group;
        }
        get api() {
            return this.options.api;
        }
        constructor(options) {
            super();
            this.options = options;
        }
        getData() {
            return this.options.getData();
        }
    }
    class DockviewWillDropEvent extends DockviewDidDropEvent {
        get kind() {
            return this._kind;
        }
        constructor(options) {
            super(options);
            this._kind = options.kind;
        }
    }
    class DockviewGroupPanelModel extends CompositeDisposable {
        get element() {
            throw new Error('dockview: not supported');
        }
        get activePanel() {
            return this._activePanel;
        }
        get locked() {
            return this._locked;
        }
        set locked(value) {
            this._locked = value;
            toggleClass(this.container, 'dv-locked-groupview', value === 'no-drop-target' || value);
        }
        get isActive() {
            return this._isGroupActive;
        }
        get panels() {
            return this._panels;
        }
        get size() {
            return this._panels.length;
        }
        get isEmpty() {
            return this._panels.length === 0;
        }
        get hasWatermark() {
            return !!(this.watermark && this.container.contains(this.watermark.element));
        }
        get header() {
            return this.tabsContainer;
        }
        get isContentFocused() {
            if (!document.activeElement) {
                return false;
            }
            return isAncestor(document.activeElement, this.contentContainer.element);
        }
        get location() {
            return this._location;
        }
        set location(value) {
            this._location = value;
            toggleClass(this.container, 'dv-groupview-floating', false);
            toggleClass(this.container, 'dv-groupview-popout', false);
            switch (value.type) {
                case 'grid':
                    this.contentContainer.dropTarget.setTargetZones([
                        'top',
                        'bottom',
                        'left',
                        'right',
                        'center',
                    ]);
                    break;
                case 'floating':
                    this.contentContainer.dropTarget.setTargetZones(['center']);
                    this.contentContainer.dropTarget.setTargetZones(value
                        ? ['center']
                        : ['top', 'bottom', 'left', 'right', 'center']);
                    toggleClass(this.container, 'dv-groupview-floating', true);
                    break;
                case 'popout':
                    this.contentContainer.dropTarget.setTargetZones(['center']);
                    toggleClass(this.container, 'dv-groupview-popout', true);
                    break;
            }
            this.groupPanel.api._onDidLocationChange.fire({
                location: this.location,
            });
        }
        constructor(container, accessor, id, options, groupPanel) {
            var _a;
            super();
            this.container = container;
            this.accessor = accessor;
            this.id = id;
            this.options = options;
            this.groupPanel = groupPanel;
            this._isGroupActive = false;
            this._locked = false;
            this._location = { type: 'grid' };
            this.mostRecentlyUsed = [];
            this._overwriteRenderContainer = null;
            this._overwriteDropTargetContainer = null;
            this._onDidChange = new Emitter();
            this.onDidChange = this._onDidChange.event;
            this._width = 0;
            this._height = 0;
            this._panels = [];
            this._panelDisposables = new Map();
            this._onMove = new Emitter();
            this.onMove = this._onMove.event;
            this._onDidDrop = new Emitter();
            this.onDidDrop = this._onDidDrop.event;
            this._onWillDrop = new Emitter();
            this.onWillDrop = this._onWillDrop.event;
            this._onWillShowOverlay = new Emitter();
            this.onWillShowOverlay = this._onWillShowOverlay.event;
            this._onTabDragStart = new Emitter();
            this.onTabDragStart = this._onTabDragStart.event;
            this._onGroupDragStart = new Emitter();
            this.onGroupDragStart = this._onGroupDragStart.event;
            this._onDidAddPanel = new Emitter();
            this.onDidAddPanel = this._onDidAddPanel.event;
            this._onDidPanelTitleChange = new Emitter();
            this.onDidPanelTitleChange = this._onDidPanelTitleChange.event;
            this._onDidPanelParametersChange = new Emitter();
            this.onDidPanelParametersChange = this._onDidPanelParametersChange.event;
            this._onDidRemovePanel = new Emitter();
            this.onDidRemovePanel = this._onDidRemovePanel.event;
            this._onDidActivePanelChange = new Emitter();
            this.onDidActivePanelChange = this._onDidActivePanelChange.event;
            this._onUnhandledDragOverEvent = new Emitter();
            this.onUnhandledDragOverEvent = this._onUnhandledDragOverEvent.event;
            toggleClass(this.container, 'dv-groupview', true);
            this._api = new DockviewApi(this.accessor);
            this.tabsContainer = new TabsContainer(this.accessor, this.groupPanel);
            this.contentContainer = new ContentContainer(this.accessor, this);
            container.append(this.tabsContainer.element, this.contentContainer.element);
            this.header.hidden = !!options.hideHeader;
            this.locked = (_a = options.locked) !== null && _a !== void 0 ? _a : false;
            this.addDisposables(this._onTabDragStart, this._onGroupDragStart, this._onWillShowOverlay, this.tabsContainer.onTabDragStart((event) => {
                this._onTabDragStart.fire(event);
            }), this.tabsContainer.onGroupDragStart((event) => {
                this._onGroupDragStart.fire(event);
            }), this.tabsContainer.onDrop((event) => {
                this.handleDropEvent('header', event.event, 'center', event.index);
            }), this.contentContainer.onDidFocus(() => {
                this.accessor.doSetGroupActive(this.groupPanel);
            }), this.contentContainer.onDidBlur(() => {
                // noop
            }), this.contentContainer.dropTarget.onDrop((event) => {
                this.handleDropEvent('content', event.nativeEvent, event.position);
            }), this.tabsContainer.onWillShowOverlay((event) => {
                this._onWillShowOverlay.fire(event);
            }), this.contentContainer.dropTarget.onWillShowOverlay((event) => {
                this._onWillShowOverlay.fire(new WillShowOverlayLocationEvent(event, {
                    kind: 'content',
                    panel: this.activePanel,
                    api: this._api,
                    group: this.groupPanel,
                    getData: getPanelData,
                }));
            }), this._onMove, this._onDidChange, this._onDidDrop, this._onWillDrop, this._onDidAddPanel, this._onDidRemovePanel, this._onDidActivePanelChange, this._onUnhandledDragOverEvent, this._onDidPanelTitleChange, this._onDidPanelParametersChange);
        }
        focusContent() {
            this.contentContainer.element.focus();
        }
        set renderContainer(value) {
            this.panels.forEach((panel) => {
                this.renderContainer.detatch(panel);
            });
            this._overwriteRenderContainer = value;
            this.panels.forEach((panel) => {
                this.rerender(panel);
            });
        }
        get renderContainer() {
            var _a;
            return ((_a = this._overwriteRenderContainer) !== null && _a !== void 0 ? _a : this.accessor.overlayRenderContainer);
        }
        set dropTargetContainer(value) {
            this._overwriteDropTargetContainer = value;
        }
        get dropTargetContainer() {
            var _a;
            return ((_a = this._overwriteDropTargetContainer) !== null && _a !== void 0 ? _a : this.accessor.rootDropTargetContainer);
        }
        initialize() {
            if (this.options.panels) {
                this.options.panels.forEach((panel) => {
                    this.doAddPanel(panel);
                });
            }
            if (this.options.activePanel) {
                this.openPanel(this.options.activePanel);
            }
            // must be run after the constructor otherwise this.parent may not be
            // correctly initialized
            this.setActive(this.isActive, true);
            this.updateContainer();
            if (this.accessor.options.createRightHeaderActionComponent) {
                this._rightHeaderActions =
                    this.accessor.options.createRightHeaderActionComponent(this.groupPanel);
                this.addDisposables(this._rightHeaderActions);
                this._rightHeaderActions.init({
                    containerApi: this._api,
                    api: this.groupPanel.api,
                    group: this.groupPanel,
                });
                this.tabsContainer.setRightActionsElement(this._rightHeaderActions.element);
            }
            if (this.accessor.options.createLeftHeaderActionComponent) {
                this._leftHeaderActions =
                    this.accessor.options.createLeftHeaderActionComponent(this.groupPanel);
                this.addDisposables(this._leftHeaderActions);
                this._leftHeaderActions.init({
                    containerApi: this._api,
                    api: this.groupPanel.api,
                    group: this.groupPanel,
                });
                this.tabsContainer.setLeftActionsElement(this._leftHeaderActions.element);
            }
            if (this.accessor.options.createPrefixHeaderActionComponent) {
                this._prefixHeaderActions =
                    this.accessor.options.createPrefixHeaderActionComponent(this.groupPanel);
                this.addDisposables(this._prefixHeaderActions);
                this._prefixHeaderActions.init({
                    containerApi: this._api,
                    api: this.groupPanel.api,
                    group: this.groupPanel,
                });
                this.tabsContainer.setPrefixActionsElement(this._prefixHeaderActions.element);
            }
        }
        rerender(panel) {
            this.contentContainer.renderPanel(panel, { asActive: false });
        }
        indexOf(panel) {
            return this.tabsContainer.indexOf(panel.id);
        }
        toJSON() {
            var _a;
            const result = {
                views: this.tabsContainer.panels,
                activeView: (_a = this._activePanel) === null || _a === void 0 ? void 0 : _a.id,
                id: this.id,
            };
            if (this.locked !== false) {
                result.locked = this.locked;
            }
            if (this.header.hidden) {
                result.hideHeader = true;
            }
            return result;
        }
        moveToNext(options) {
            if (!options) {
                options = {};
            }
            if (!options.panel) {
                options.panel = this.activePanel;
            }
            const index = options.panel ? this.panels.indexOf(options.panel) : -1;
            let normalizedIndex;
            if (index < this.panels.length - 1) {
                normalizedIndex = index + 1;
            }
            else if (!options.suppressRoll) {
                normalizedIndex = 0;
            }
            else {
                return;
            }
            this.openPanel(this.panels[normalizedIndex]);
        }
        moveToPrevious(options) {
            if (!options) {
                options = {};
            }
            if (!options.panel) {
                options.panel = this.activePanel;
            }
            if (!options.panel) {
                return;
            }
            const index = this.panels.indexOf(options.panel);
            let normalizedIndex;
            if (index > 0) {
                normalizedIndex = index - 1;
            }
            else if (!options.suppressRoll) {
                normalizedIndex = this.panels.length - 1;
            }
            else {
                return;
            }
            this.openPanel(this.panels[normalizedIndex]);
        }
        containsPanel(panel) {
            return this.panels.includes(panel);
        }
        init(_params) {
            //noop
        }
        update(_params) {
            //noop
        }
        focus() {
            var _a;
            (_a = this._activePanel) === null || _a === void 0 ? void 0 : _a.focus();
        }
        openPanel(panel, options = {}) {
            /**
             * set the panel group
             * add the panel
             * check if group active
             * check if panel active
             */
            if (typeof options.index !== 'number' ||
                options.index > this.panels.length) {
                options.index = this.panels.length;
            }
            const skipSetActive = !!options.skipSetActive;
            // ensure the group is updated before we fire any events
            panel.updateParentGroup(this.groupPanel, {
                skipSetActive: options.skipSetActive,
            });
            this.doAddPanel(panel, options.index, {
                skipSetActive: skipSetActive,
            });
            if (this._activePanel === panel) {
                this.contentContainer.renderPanel(panel, { asActive: true });
                return;
            }
            if (!skipSetActive) {
                this.doSetActivePanel(panel);
            }
            if (!options.skipSetGroupActive) {
                this.accessor.doSetGroupActive(this.groupPanel);
            }
            if (!options.skipSetActive) {
                this.updateContainer();
            }
        }
        removePanel(groupItemOrId, options = {
            skipSetActive: false,
        }) {
            const id = typeof groupItemOrId === 'string'
                ? groupItemOrId
                : groupItemOrId.id;
            const panelToRemove = this._panels.find((panel) => panel.id === id);
            if (!panelToRemove) {
                throw new Error('invalid operation');
            }
            return this._removePanel(panelToRemove, options);
        }
        closeAllPanels() {
            if (this.panels.length > 0) {
                // take a copy since we will be edting the array as we iterate through
                const arrPanelCpy = [...this.panels];
                for (const panel of arrPanelCpy) {
                    this.doClose(panel);
                }
            }
            else {
                this.accessor.removeGroup(this.groupPanel);
            }
        }
        closePanel(panel) {
            this.doClose(panel);
        }
        doClose(panel) {
            const isLast = this.panels.length === 1 && this.accessor.groups.length === 1;
            this.accessor.removePanel(panel, isLast && this.accessor.options.noPanelsOverlay === 'emptyGroup'
                ? { removeEmptyGroup: false }
                : undefined);
        }
        isPanelActive(panel) {
            return this._activePanel === panel;
        }
        updateActions(element) {
            this.tabsContainer.setRightActionsElement(element);
        }
        setActive(isGroupActive, force = false) {
            if (!force && this.isActive === isGroupActive) {
                return;
            }
            this._isGroupActive = isGroupActive;
            toggleClass(this.container, 'dv-active-group', isGroupActive);
            toggleClass(this.container, 'dv-inactive-group', !isGroupActive);
            this.tabsContainer.setActive(this.isActive);
            if (!this._activePanel && this.panels.length > 0) {
                this.doSetActivePanel(this.panels[0]);
            }
            this.updateContainer();
        }
        layout(width, height) {
            var _a;
            this._width = width;
            this._height = height;
            this.contentContainer.layout(this._width, this._height);
            if ((_a = this._activePanel) === null || _a === void 0 ? void 0 : _a.layout) {
                this._activePanel.layout(this._width, this._height);
            }
        }
        _removePanel(panel, options) {
            const isActivePanel = this._activePanel === panel;
            this.doRemovePanel(panel);
            if (isActivePanel && this.panels.length > 0) {
                const nextPanel = this.mostRecentlyUsed[0];
                this.openPanel(nextPanel, {
                    skipSetActive: options.skipSetActive,
                    skipSetGroupActive: options.skipSetActiveGroup,
                });
            }
            if (this._activePanel && this.panels.length === 0) {
                this.doSetActivePanel(undefined);
            }
            if (!options.skipSetActive) {
                this.updateContainer();
            }
            return panel;
        }
        doRemovePanel(panel) {
            const index = this.panels.indexOf(panel);
            if (this._activePanel === panel) {
                this.contentContainer.closePanel();
            }
            this.tabsContainer.delete(panel.id);
            this._panels.splice(index, 1);
            if (this.mostRecentlyUsed.includes(panel)) {
                const index = this.mostRecentlyUsed.indexOf(panel);
                this.mostRecentlyUsed.splice(index, 1);
            }
            const disposable = this._panelDisposables.get(panel.id);
            if (disposable) {
                disposable.dispose();
                this._panelDisposables.delete(panel.id);
            }
            this._onDidRemovePanel.fire({ panel });
        }
        doAddPanel(panel, index = this.panels.length, options = { skipSetActive: false }) {
            const existingPanel = this._panels.indexOf(panel);
            const hasExistingPanel = existingPanel > -1;
            this.tabsContainer.show();
            this.contentContainer.show();
            this.tabsContainer.openPanel(panel, index);
            if (!options.skipSetActive) {
                this.contentContainer.openPanel(panel);
            }
            if (hasExistingPanel) {
                // TODO - need to ensure ordering hasn't changed and if it has need to re-order this.panels
                return;
            }
            this.updateMru(panel);
            this.panels.splice(index, 0, panel);
            this._panelDisposables.set(panel.id, new CompositeDisposable(panel.api.onDidTitleChange((event) => this._onDidPanelTitleChange.fire(event)), panel.api.onDidParametersChange((event) => this._onDidPanelParametersChange.fire(event))));
            this._onDidAddPanel.fire({ panel });
        }
        doSetActivePanel(panel) {
            if (this._activePanel === panel) {
                return;
            }
            this._activePanel = panel;
            if (panel) {
                this.tabsContainer.setActivePanel(panel);
                panel.layout(this._width, this._height);
                this.updateMru(panel);
                this._onDidActivePanelChange.fire({
                    panel,
                });
            }
        }
        updateMru(panel) {
            if (this.mostRecentlyUsed.includes(panel)) {
                this.mostRecentlyUsed.splice(this.mostRecentlyUsed.indexOf(panel), 1);
            }
            this.mostRecentlyUsed = [panel, ...this.mostRecentlyUsed];
        }
        updateContainer() {
            var _a, _b;
            this.panels.forEach((panel) => panel.runEvents());
            if (this.isEmpty && !this.watermark) {
                const watermark = this.accessor.createWatermarkComponent();
                watermark.init({
                    containerApi: this._api,
                    group: this.groupPanel,
                });
                this.watermark = watermark;
                addDisposableListener(this.watermark.element, 'pointerdown', () => {
                    if (!this.isActive) {
                        this.accessor.doSetGroupActive(this.groupPanel);
                    }
                });
                this.contentContainer.element.appendChild(this.watermark.element);
            }
            if (!this.isEmpty && this.watermark) {
                this.watermark.element.remove();
                (_b = (_a = this.watermark).dispose) === null || _b === void 0 ? void 0 : _b.call(_a);
                this.watermark = undefined;
            }
        }
        canDisplayOverlay(event, position, target) {
            const firedEvent = new DockviewUnhandledDragOverEvent(event, target, position, getPanelData, this.accessor.getPanel(this.id));
            this._onUnhandledDragOverEvent.fire(firedEvent);
            return firedEvent.isAccepted;
        }
        handleDropEvent(type, event, position, index) {
            if (this.locked === 'no-drop-target') {
                return;
            }
            function getKind() {
                switch (type) {
                    case 'header':
                        return typeof index === 'number' ? 'tab' : 'header_space';
                    case 'content':
                        return 'content';
                }
            }
            const panel = typeof index === 'number' ? this.panels[index] : undefined;
            const willDropEvent = new DockviewWillDropEvent({
                nativeEvent: event,
                position,
                panel,
                getData: () => getPanelData(),
                kind: getKind(),
                group: this.groupPanel,
                api: this._api,
            });
            this._onWillDrop.fire(willDropEvent);
            if (willDropEvent.defaultPrevented) {
                return;
            }
            const data = getPanelData();
            if (data && data.viewId === this.accessor.id) {
                if (type === 'content') {
                    if (data.groupId === this.id) {
                        // don't allow to drop on self for center position
                        if (position === 'center') {
                            return;
                        }
                        if (data.panelId === null) {
                            // don't allow group move to drop anywhere on self
                            return;
                        }
                    }
                }
                if (type === 'header') {
                    if (data.groupId === this.id) {
                        if (data.panelId === null) {
                            return;
                        }
                    }
                }
                if (data.panelId === null) {
                    // this is a group move dnd event
                    const { groupId } = data;
                    this._onMove.fire({
                        target: position,
                        groupId: groupId,
                        index,
                    });
                    return;
                }
                const fromSameGroup = this.tabsContainer.indexOf(data.panelId) !== -1;
                if (fromSameGroup && this.tabsContainer.size === 1) {
                    return;
                }
                const { groupId, panelId } = data;
                const isSameGroup = this.id === groupId;
                if (isSameGroup && !position) {
                    const oldIndex = this.tabsContainer.indexOf(panelId);
                    if (oldIndex === index) {
                        return;
                    }
                }
                this._onMove.fire({
                    target: position,
                    groupId: data.groupId,
                    itemId: data.panelId,
                    index,
                });
            }
            else {
                this._onDidDrop.fire(new DockviewDidDropEvent({
                    nativeEvent: event,
                    position,
                    panel,
                    getData: () => getPanelData(),
                    group: this.groupPanel,
                    api: this._api,
                }));
            }
        }
        updateDragAndDropState() {
            this.tabsContainer.updateDragAndDropState();
        }
        dispose() {
            var _a, _b, _c;
            super.dispose();
            (_a = this.watermark) === null || _a === void 0 ? void 0 : _a.element.remove();
            (_c = (_b = this.watermark) === null || _b === void 0 ? void 0 : _b.dispose) === null || _c === void 0 ? void 0 : _c.call(_b);
            this.watermark = undefined;
            for (const panel of this.panels) {
                panel.dispose();
            }
            this.tabsContainer.dispose();
            this.contentContainer.dispose();
        }
    }

    class GridviewPanelApiImpl extends PanelApiImpl {
        constructor(id, component, panel) {
            super(id, component);
            this._onDidConstraintsChangeInternal = new Emitter();
            this.onDidConstraintsChangeInternal = this._onDidConstraintsChangeInternal.event;
            this._onDidConstraintsChange = new Emitter();
            this.onDidConstraintsChange = this._onDidConstraintsChange.event;
            this._onDidSizeChange = new Emitter();
            this.onDidSizeChange = this._onDidSizeChange.event;
            this.addDisposables(this._onDidConstraintsChangeInternal, this._onDidConstraintsChange, this._onDidSizeChange);
            if (panel) {
                this.initialize(panel);
            }
        }
        setConstraints(value) {
            this._onDidConstraintsChangeInternal.fire(value);
        }
        setSize(event) {
            this._onDidSizeChange.fire(event);
        }
    }

    class GridviewPanel extends BasePanelView {
        get priority() {
            return this._priority;
        }
        get snap() {
            return this._snap;
        }
        get minimumWidth() {
            /**
             * defer to protected function to allow subclasses to override easily.
             * see https://github.com/microsoft/TypeScript/issues/338
             */
            return this.__minimumWidth();
        }
        get minimumHeight() {
            /**
             * defer to protected function to allow subclasses to override easily.
             * see https://github.com/microsoft/TypeScript/issues/338
             */
            return this.__minimumHeight();
        }
        get maximumHeight() {
            /**
             * defer to protected function to allow subclasses to override easily.
             * see https://github.com/microsoft/TypeScript/issues/338
             */
            return this.__maximumHeight();
        }
        get maximumWidth() {
            /**
             * defer to protected function to allow subclasses to override easily.
             * see https://github.com/microsoft/TypeScript/issues/338
             */
            return this.__maximumWidth();
        }
        __minimumWidth() {
            const width = typeof this._minimumWidth === 'function'
                ? this._minimumWidth()
                : this._minimumWidth;
            if (width !== this._evaluatedMinimumWidth) {
                this._evaluatedMinimumWidth = width;
                this.updateConstraints();
            }
            return width;
        }
        __maximumWidth() {
            const width = typeof this._maximumWidth === 'function'
                ? this._maximumWidth()
                : this._maximumWidth;
            if (width !== this._evaluatedMaximumWidth) {
                this._evaluatedMaximumWidth = width;
                this.updateConstraints();
            }
            return width;
        }
        __minimumHeight() {
            const height = typeof this._minimumHeight === 'function'
                ? this._minimumHeight()
                : this._minimumHeight;
            if (height !== this._evaluatedMinimumHeight) {
                this._evaluatedMinimumHeight = height;
                this.updateConstraints();
            }
            return height;
        }
        __maximumHeight() {
            const height = typeof this._maximumHeight === 'function'
                ? this._maximumHeight()
                : this._maximumHeight;
            if (height !== this._evaluatedMaximumHeight) {
                this._evaluatedMaximumHeight = height;
                this.updateConstraints();
            }
            return height;
        }
        get isActive() {
            return this.api.isActive;
        }
        get isVisible() {
            return this.api.isVisible;
        }
        constructor(id, component, options, api) {
            super(id, component, api !== null && api !== void 0 ? api : new GridviewPanelApiImpl(id, component));
            this._evaluatedMinimumWidth = 0;
            this._evaluatedMaximumWidth = Number.MAX_SAFE_INTEGER;
            this._evaluatedMinimumHeight = 0;
            this._evaluatedMaximumHeight = Number.MAX_SAFE_INTEGER;
            this._minimumWidth = 0;
            this._minimumHeight = 0;
            this._maximumWidth = Number.MAX_SAFE_INTEGER;
            this._maximumHeight = Number.MAX_SAFE_INTEGER;
            this._snap = false;
            this._onDidChange = new Emitter();
            this.onDidChange = this._onDidChange.event;
            if (typeof (options === null || options === void 0 ? void 0 : options.minimumWidth) === 'number') {
                this._minimumWidth = options.minimumWidth;
            }
            if (typeof (options === null || options === void 0 ? void 0 : options.maximumWidth) === 'number') {
                this._maximumWidth = options.maximumWidth;
            }
            if (typeof (options === null || options === void 0 ? void 0 : options.minimumHeight) === 'number') {
                this._minimumHeight = options.minimumHeight;
            }
            if (typeof (options === null || options === void 0 ? void 0 : options.maximumHeight) === 'number') {
                this._maximumHeight = options.maximumHeight;
            }
            this.api.initialize(this); // TODO: required to by-pass 'super before this' requirement
            this.addDisposables(this.api.onWillVisibilityChange((event) => {
                const { isVisible } = event;
                const { accessor } = this._params;
                accessor.setVisible(this, isVisible);
            }), this.api.onActiveChange(() => {
                const { accessor } = this._params;
                accessor.doSetGroupActive(this);
            }), this.api.onDidConstraintsChangeInternal((event) => {
                if (typeof event.minimumWidth === 'number' ||
                    typeof event.minimumWidth === 'function') {
                    this._minimumWidth = event.minimumWidth;
                }
                if (typeof event.minimumHeight === 'number' ||
                    typeof event.minimumHeight === 'function') {
                    this._minimumHeight = event.minimumHeight;
                }
                if (typeof event.maximumWidth === 'number' ||
                    typeof event.maximumWidth === 'function') {
                    this._maximumWidth = event.maximumWidth;
                }
                if (typeof event.maximumHeight === 'number' ||
                    typeof event.maximumHeight === 'function') {
                    this._maximumHeight = event.maximumHeight;
                }
            }), this.api.onDidSizeChange((event) => {
                this._onDidChange.fire({
                    height: event.height,
                    width: event.width,
                });
            }), this._onDidChange);
        }
        setVisible(isVisible) {
            this.api._onDidVisibilityChange.fire({ isVisible });
        }
        setActive(isActive) {
            this.api._onDidActiveChange.fire({ isActive });
        }
        init(parameters) {
            if (parameters.maximumHeight) {
                this._maximumHeight = parameters.maximumHeight;
            }
            if (parameters.minimumHeight) {
                this._minimumHeight = parameters.minimumHeight;
            }
            if (parameters.maximumWidth) {
                this._maximumWidth = parameters.maximumWidth;
            }
            if (parameters.minimumWidth) {
                this._minimumWidth = parameters.minimumWidth;
            }
            this._priority = parameters.priority;
            this._snap = !!parameters.snap;
            super.init(parameters);
            if (typeof parameters.isVisible === 'boolean') {
                this.setVisible(parameters.isVisible);
            }
        }
        updateConstraints() {
            this.api._onDidConstraintsChange.fire({
                minimumWidth: this._evaluatedMinimumWidth,
                maximumWidth: this._evaluatedMaximumWidth,
                minimumHeight: this._evaluatedMinimumHeight,
                maximumHeight: this._evaluatedMaximumHeight,
            });
        }
        toJSON() {
            const state = super.toJSON();
            const maximum = (value) => value === Number.MAX_SAFE_INTEGER ? undefined : value;
            const minimum = (value) => (value <= 0 ? undefined : value);
            return Object.assign(Object.assign({}, state), { minimumHeight: minimum(this.minimumHeight), maximumHeight: maximum(this.maximumHeight), minimumWidth: minimum(this.minimumWidth), maximumWidth: maximum(this.maximumWidth), snap: this.snap, priority: this.priority });
        }
    }

    const NOT_INITIALIZED_MESSAGE = 'dockview: DockviewGroupPanelApiImpl not initialized';
    class DockviewGroupPanelApiImpl extends GridviewPanelApiImpl {
        get location() {
            if (!this._group) {
                throw new Error(NOT_INITIALIZED_MESSAGE);
            }
            return this._group.model.location;
        }
        constructor(id, accessor) {
            super(id, '__dockviewgroup__');
            this.accessor = accessor;
            this._onDidLocationChange = new Emitter();
            this.onDidLocationChange = this._onDidLocationChange.event;
            this._onDidActivePanelChange = new Emitter();
            this.onDidActivePanelChange = this._onDidActivePanelChange.event;
            this.addDisposables(this._onDidLocationChange, this._onDidActivePanelChange);
        }
        close() {
            if (!this._group) {
                return;
            }
            return this.accessor.removeGroup(this._group);
        }
        getWindow() {
            return this.location.type === 'popout'
                ? this.location.getWindow()
                : window;
        }
        moveTo(options) {
            var _a, _b, _c, _d;
            if (!this._group) {
                throw new Error(NOT_INITIALIZED_MESSAGE);
            }
            const group = (_a = options.group) !== null && _a !== void 0 ? _a : this.accessor.addGroup({
                direction: positionToDirection((_b = options.position) !== null && _b !== void 0 ? _b : 'right'),
                skipSetActive: (_c = options.skipSetActive) !== null && _c !== void 0 ? _c : false,
            });
            this.accessor.moveGroupOrPanel({
                from: { groupId: this._group.id },
                to: {
                    group,
                    position: options.group
                        ? (_d = options.position) !== null && _d !== void 0 ? _d : 'center'
                        : 'center',
                    index: options.index,
                },
                skipSetActive: options.skipSetActive,
            });
        }
        maximize() {
            if (!this._group) {
                throw new Error(NOT_INITIALIZED_MESSAGE);
            }
            if (this.location.type !== 'grid') {
                // only grid groups can be maximized
                return;
            }
            this.accessor.maximizeGroup(this._group);
        }
        isMaximized() {
            if (!this._group) {
                throw new Error(NOT_INITIALIZED_MESSAGE);
            }
            return this.accessor.isMaximizedGroup(this._group);
        }
        exitMaximized() {
            if (!this._group) {
                throw new Error(NOT_INITIALIZED_MESSAGE);
            }
            if (this.isMaximized()) {
                this.accessor.exitMaximizedGroup();
            }
        }
        initialize(group) {
            this._group = group;
        }
    }

    const MINIMUM_DOCKVIEW_GROUP_PANEL_WIDTH = 100;
    const MINIMUM_DOCKVIEW_GROUP_PANEL_HEIGHT = 100;
    class DockviewGroupPanel extends GridviewPanel {
        get minimumWidth() {
            var _a;
            const activePanelMinimumWidth = (_a = this.activePanel) === null || _a === void 0 ? void 0 : _a.minimumWidth;
            if (typeof activePanelMinimumWidth === 'number') {
                return activePanelMinimumWidth;
            }
            return super.__minimumWidth();
        }
        get minimumHeight() {
            var _a;
            const activePanelMinimumHeight = (_a = this.activePanel) === null || _a === void 0 ? void 0 : _a.minimumHeight;
            if (typeof activePanelMinimumHeight === 'number') {
                return activePanelMinimumHeight;
            }
            return super.__minimumHeight();
        }
        get maximumWidth() {
            var _a;
            const activePanelMaximumWidth = (_a = this.activePanel) === null || _a === void 0 ? void 0 : _a.maximumWidth;
            if (typeof activePanelMaximumWidth === 'number') {
                return activePanelMaximumWidth;
            }
            return super.__maximumWidth();
        }
        get maximumHeight() {
            var _a;
            const activePanelMaximumHeight = (_a = this.activePanel) === null || _a === void 0 ? void 0 : _a.maximumHeight;
            if (typeof activePanelMaximumHeight === 'number') {
                return activePanelMaximumHeight;
            }
            return super.__maximumHeight();
        }
        get panels() {
            return this._model.panels;
        }
        get activePanel() {
            return this._model.activePanel;
        }
        get size() {
            return this._model.size;
        }
        get model() {
            return this._model;
        }
        get locked() {
            return this._model.locked;
        }
        set locked(value) {
            this._model.locked = value;
        }
        get header() {
            return this._model.header;
        }
        constructor(accessor, id, options) {
            var _a, _b, _c, _d, _e, _f;
            super(id, 'groupview_default', {
                minimumHeight: (_b = (_a = options.constraints) === null || _a === void 0 ? void 0 : _a.minimumHeight) !== null && _b !== void 0 ? _b : MINIMUM_DOCKVIEW_GROUP_PANEL_HEIGHT,
                minimumWidth: (_d = (_c = options.constraints) === null || _c === void 0 ? void 0 : _c.maximumHeight) !== null && _d !== void 0 ? _d : MINIMUM_DOCKVIEW_GROUP_PANEL_WIDTH,
                maximumHeight: (_e = options.constraints) === null || _e === void 0 ? void 0 : _e.maximumHeight,
                maximumWidth: (_f = options.constraints) === null || _f === void 0 ? void 0 : _f.maximumWidth,
            }, new DockviewGroupPanelApiImpl(id, accessor));
            this.api.initialize(this); // cannot use 'this' after after 'super' call
            this._model = new DockviewGroupPanelModel(this.element, accessor, id, options, this);
            this.addDisposables(this.model.onDidActivePanelChange((event) => {
                this.api._onDidActivePanelChange.fire(event);
            }));
        }
        focus() {
            if (!this.api.isActive) {
                this.api.setActive();
            }
            super.focus();
        }
        initialize() {
            this._model.initialize();
        }
        setActive(isActive) {
            super.setActive(isActive);
            this.model.setActive(isActive);
        }
        layout(width, height) {
            super.layout(width, height);
            this.model.layout(width, height);
        }
        getComponent() {
            return this._model;
        }
        toJSON() {
            return this.model.toJSON();
        }
    }

    const themeDark = {
        name: 'dark',
        className: 'dockview-theme-dark',
    };
    const themeLight = {
        name: 'light',
        className: 'dockview-theme-light',
    };
    const themeVisualStudio = {
        name: 'visualStudio',
        className: 'dockview-theme-vs',
    };
    const themeAbyss = {
        name: 'abyss',
        className: 'dockview-theme-abyss',
    };
    const themeDracula = {
        name: 'dracula',
        className: 'dockview-theme-dracula',
    };
    const themeReplit = {
        name: 'replit',
        className: 'dockview-theme-replit',
        gap: 10,
    };
    const themeAbyssSpaced = {
        name: 'abyssSpaced',
        className: 'dockview-theme-abyss-spaced',
        gap: 10,
        dndOverlayMounting: 'absolute',
        dndPanelOverlay: 'group',
    };
    const themeLightSpaced = {
        name: 'lightSpaced',
        className: 'dockview-theme-light-spaced',
        gap: 10,
        dndOverlayMounting: 'absolute',
        dndPanelOverlay: 'group',
    };

    class DockviewPanelApiImpl extends GridviewPanelApiImpl {
        get location() {
            return this.group.api.location;
        }
        get title() {
            return this.panel.title;
        }
        get isGroupActive() {
            return this.group.isActive;
        }
        get renderer() {
            return this.panel.renderer;
        }
        set group(value) {
            const oldGroup = this._group;
            if (this._group !== value) {
                this._group = value;
                this._onDidGroupChange.fire({});
                this.setupGroupEventListeners(oldGroup);
                this._onDidLocationChange.fire({
                    location: this.group.api.location,
                });
            }
        }
        get group() {
            return this._group;
        }
        get tabComponent() {
            return this._tabComponent;
        }
        constructor(panel, group, accessor, component, tabComponent) {
            super(panel.id, component);
            this.panel = panel;
            this.accessor = accessor;
            this._onDidTitleChange = new Emitter();
            this.onDidTitleChange = this._onDidTitleChange.event;
            this._onDidActiveGroupChange = new Emitter();
            this.onDidActiveGroupChange = this._onDidActiveGroupChange.event;
            this._onDidGroupChange = new Emitter();
            this.onDidGroupChange = this._onDidGroupChange.event;
            this._onDidRendererChange = new Emitter();
            this.onDidRendererChange = this._onDidRendererChange.event;
            this._onDidLocationChange = new Emitter();
            this.onDidLocationChange = this._onDidLocationChange.event;
            this.groupEventsDisposable = new MutableDisposable();
            this._tabComponent = tabComponent;
            this.initialize(panel);
            this._group = group;
            this.setupGroupEventListeners();
            this.addDisposables(this.groupEventsDisposable, this._onDidRendererChange, this._onDidTitleChange, this._onDidGroupChange, this._onDidActiveGroupChange, this._onDidLocationChange);
        }
        getWindow() {
            return this.group.api.getWindow();
        }
        moveTo(options) {
            var _a, _b;
            this.accessor.moveGroupOrPanel({
                from: { groupId: this._group.id, panelId: this.panel.id },
                to: {
                    group: (_a = options.group) !== null && _a !== void 0 ? _a : this._group,
                    position: options.group
                        ? (_b = options.position) !== null && _b !== void 0 ? _b : 'center'
                        : 'center',
                    index: options.index,
                },
                skipSetActive: options.skipSetActive,
            });
        }
        setTitle(title) {
            this.panel.setTitle(title);
        }
        setRenderer(renderer) {
            this.panel.setRenderer(renderer);
        }
        close() {
            this.group.model.closePanel(this.panel);
        }
        maximize() {
            this.group.api.maximize();
        }
        isMaximized() {
            return this.group.api.isMaximized();
        }
        exitMaximized() {
            this.group.api.exitMaximized();
        }
        setupGroupEventListeners(previousGroup) {
            var _a;
            let _trackGroupActive = (_a = previousGroup === null || previousGroup === void 0 ? void 0 : previousGroup.isActive) !== null && _a !== void 0 ? _a : false; // prevent duplicate events with same state
            this.groupEventsDisposable.value = new CompositeDisposable(this.group.api.onDidVisibilityChange((event) => {
                const hasBecomeHidden = !event.isVisible && this.isVisible;
                const hasBecomeVisible = event.isVisible && !this.isVisible;
                const isActivePanel = this.group.model.isPanelActive(this.panel);
                if (hasBecomeHidden || (hasBecomeVisible && isActivePanel)) {
                    this._onDidVisibilityChange.fire(event);
                }
            }), this.group.api.onDidLocationChange((event) => {
                if (this.group !== this.panel.group) {
                    return;
                }
                this._onDidLocationChange.fire(event);
            }), this.group.api.onDidActiveChange(() => {
                if (this.group !== this.panel.group) {
                    return;
                }
                if (_trackGroupActive !== this.isGroupActive) {
                    _trackGroupActive = this.isGroupActive;
                    this._onDidActiveGroupChange.fire({
                        isActive: this.isGroupActive,
                    });
                }
            }));
        }
    }

    class DockviewPanel extends CompositeDisposable {
        get params() {
            return this._params;
        }
        get title() {
            return this._title;
        }
        get group() {
            return this._group;
        }
        get renderer() {
            var _a;
            return (_a = this._renderer) !== null && _a !== void 0 ? _a : this.accessor.renderer;
        }
        get minimumWidth() {
            return this._minimumWidth;
        }
        get minimumHeight() {
            return this._minimumHeight;
        }
        get maximumWidth() {
            return this._maximumWidth;
        }
        get maximumHeight() {
            return this._maximumHeight;
        }
        constructor(id, component, tabComponent, accessor, containerApi, group, view, options) {
            super();
            this.id = id;
            this.accessor = accessor;
            this.containerApi = containerApi;
            this.view = view;
            this._renderer = options.renderer;
            this._group = group;
            this._minimumWidth = options.minimumWidth;
            this._minimumHeight = options.minimumHeight;
            this._maximumWidth = options.maximumWidth;
            this._maximumHeight = options.maximumHeight;
            this.api = new DockviewPanelApiImpl(this, this._group, accessor, component, tabComponent);
            this.addDisposables(this.api.onActiveChange(() => {
                accessor.setActivePanel(this);
            }), this.api.onDidSizeChange((event) => {
                // forward the resize event to the group since if you want to resize a panel
                // you are actually just resizing the panels parent which is the group
                this.group.api.setSize(event);
            }), this.api.onDidRendererChange(() => {
                this.group.model.rerender(this);
            }));
        }
        init(params) {
            this._params = params.params;
            this.view.init(Object.assign(Object.assign({}, params), { api: this.api, containerApi: this.containerApi }));
            this.setTitle(params.title);
        }
        focus() {
            const event = new WillFocusEvent();
            this.api._onWillFocus.fire(event);
            if (event.defaultPrevented) {
                return;
            }
            if (!this.api.isActive) {
                this.api.setActive();
            }
        }
        toJSON() {
            return {
                id: this.id,
                contentComponent: this.view.contentComponent,
                tabComponent: this.view.tabComponent,
                params: Object.keys(this._params || {}).length > 0
                    ? this._params
                    : undefined,
                title: this.title,
                renderer: this._renderer,
                minimumHeight: this._minimumHeight,
                maximumHeight: this._maximumHeight,
                minimumWidth: this._minimumWidth,
                maximumWidth: this._maximumWidth,
            };
        }
        setTitle(title) {
            const didTitleChange = title !== this.title;
            if (didTitleChange) {
                this._title = title;
                this.api._onDidTitleChange.fire({ title });
            }
        }
        setRenderer(renderer) {
            const didChange = renderer !== this.renderer;
            if (didChange) {
                this._renderer = renderer;
                this.api._onDidRendererChange.fire({
                    renderer: renderer,
                });
            }
        }
        update(event) {
            var _a;
            // merge the new parameters with the existing parameters
            this._params = Object.assign(Object.assign({}, ((_a = this._params) !== null && _a !== void 0 ? _a : {})), event.params);
            /**
             * delete new keys that have a value of undefined,
             * allow values of null
             */
            for (const key of Object.keys(event.params)) {
                if (event.params[key] === undefined) {
                    delete this._params[key];
                }
            }
            // update the view with the updated props
            this.view.update({
                params: this._params,
            });
        }
        updateParentGroup(group, options) {
            this._group = group;
            this.api.group = this._group;
            const isPanelVisible = this._group.model.isPanelActive(this);
            const isActive = this.group.api.isActive && isPanelVisible;
            if (!(options === null || options === void 0 ? void 0 : options.skipSetActive)) {
                if (this.api.isActive !== isActive) {
                    this.api._onDidActiveChange.fire({
                        isActive: this.group.api.isActive && isPanelVisible,
                    });
                }
            }
            if (this.api.isVisible !== isPanelVisible) {
                this.api._onDidVisibilityChange.fire({
                    isVisible: isPanelVisible,
                });
            }
        }
        runEvents() {
            const isPanelVisible = this._group.model.isPanelActive(this);
            const isActive = this.group.api.isActive && isPanelVisible;
            if (this.api.isActive !== isActive) {
                this.api._onDidActiveChange.fire({
                    isActive: this.group.api.isActive && isPanelVisible,
                });
            }
            if (this.api.isVisible !== isPanelVisible) {
                this.api._onDidVisibilityChange.fire({
                    isVisible: isPanelVisible,
                });
            }
        }
        layout(width, height) {
            // TODO: Can we somehow do height without header height or indicate what the header height is?
            this.api._onDidDimensionChange.fire({
                width,
                height: height,
            });
            this.view.layout(width, height);
        }
        dispose() {
            this.api.dispose();
            this.view.dispose();
        }
    }

    class DefaultTab extends CompositeDisposable {
        get element() {
            return this._element;
        }
        constructor() {
            super();
            this._element = document.createElement('div');
            this._element.className = 'dv-default-tab';
            this._content = document.createElement('div');
            this._content.className = 'dv-default-tab-content';
            this.action = document.createElement('div');
            this.action.className = 'dv-default-tab-action';
            this.action.appendChild(createCloseButton());
            this._element.appendChild(this._content);
            this._element.appendChild(this.action);
            this.render();
        }
        init(params) {
            this._title = params.title;
            this.addDisposables(params.api.onDidTitleChange((event) => {
                this._title = event.title;
                this.render();
            }), addDisposableListener(this.action, 'pointerdown', (ev) => {
                ev.preventDefault();
            }), addDisposableListener(this.action, 'click', (ev) => {
                if (ev.defaultPrevented) {
                    return;
                }
                ev.preventDefault();
                params.api.close();
            }));
            this.render();
        }
        render() {
            var _a;
            if (this._content.textContent !== this._title) {
                this._content.textContent = (_a = this._title) !== null && _a !== void 0 ? _a : '';
            }
        }
    }

    class DockviewPanelModel {
        get content() {
            return this._content;
        }
        get tab() {
            return this._tab;
        }
        constructor(accessor, id, contentComponent, tabComponent) {
            this.accessor = accessor;
            this.id = id;
            this.contentComponent = contentComponent;
            this.tabComponent = tabComponent;
            this._content = this.createContentComponent(this.id, contentComponent);
            this._tab = this.createTabComponent(this.id, tabComponent);
        }
        createTabRenderer(tabLocation) {
            var _a;
            const cmp = this.createTabComponent(this.id, this.tabComponent);
            if (this._params) {
                cmp.init(Object.assign(Object.assign({}, this._params), { tabLocation }));
            }
            if (this._updateEvent) {
                (_a = cmp.update) === null || _a === void 0 ? void 0 : _a.call(cmp, this._updateEvent);
            }
            return cmp;
        }
        init(params) {
            this._params = params;
            this.content.init(params);
            this.tab.init(Object.assign(Object.assign({}, params), { tabLocation: 'header' }));
        }
        layout(width, height) {
            var _a, _b;
            (_b = (_a = this.content).layout) === null || _b === void 0 ? void 0 : _b.call(_a, width, height);
        }
        update(event) {
            var _a, _b, _c, _d;
            this._updateEvent = event;
            (_b = (_a = this.content).update) === null || _b === void 0 ? void 0 : _b.call(_a, event);
            (_d = (_c = this.tab).update) === null || _d === void 0 ? void 0 : _d.call(_c, event);
        }
        dispose() {
            var _a, _b, _c, _d;
            (_b = (_a = this.content).dispose) === null || _b === void 0 ? void 0 : _b.call(_a);
            (_d = (_c = this.tab).dispose) === null || _d === void 0 ? void 0 : _d.call(_c);
        }
        createContentComponent(id, componentName) {
            return this.accessor.options.createComponent({
                id,
                name: componentName,
            });
        }
        createTabComponent(id, componentName) {
            const name = componentName !== null && componentName !== void 0 ? componentName : this.accessor.options.defaultTabComponent;
            if (name) {
                if (this.accessor.options.createTabComponent) {
                    const component = this.accessor.options.createTabComponent({
                        id,
                        name,
                    });
                    if (component) {
                        return component;
                    }
                    else {
                        return new DefaultTab();
                    }
                }
                console.warn(`dockview: tabComponent '${componentName}' was not found. falling back to the default tab.`);
            }
            return new DefaultTab();
        }
    }

    class DefaultDockviewDeserialzier {
        constructor(accessor) {
            this.accessor = accessor;
        }
        fromJSON(panelData, group) {
            var _a, _b;
            const panelId = panelData.id;
            const params = panelData.params;
            const title = panelData.title;
            const viewData = panelData.view;
            const contentComponent = viewData
                ? viewData.content.id
                : (_a = panelData.contentComponent) !== null && _a !== void 0 ? _a : 'unknown';
            const tabComponent = viewData
                ? (_b = viewData.tab) === null || _b === void 0 ? void 0 : _b.id
                : panelData.tabComponent;
            const view = new DockviewPanelModel(this.accessor, panelId, contentComponent, tabComponent);
            const panel = new DockviewPanel(panelId, contentComponent, tabComponent, this.accessor, new DockviewApi(this.accessor), group, view, {
                renderer: panelData.renderer,
                minimumWidth: panelData.minimumWidth,
                minimumHeight: panelData.minimumHeight,
                maximumWidth: panelData.maximumWidth,
                maximumHeight: panelData.maximumHeight,
            });
            panel.init({
                title: title !== null && title !== void 0 ? title : panelId,
                params: params !== null && params !== void 0 ? params : {},
            });
            return panel;
        }
    }

    class Watermark extends CompositeDisposable {
        get element() {
            return this._element;
        }
        constructor() {
            super();
            this._element = document.createElement('div');
            this._element.className = 'dv-watermark';
        }
        init(_params) {
            // noop
        }
    }

    class AriaLevelTracker {
        constructor() {
            this._orderedList = [];
        }
        push(element) {
            this._orderedList = [
                ...this._orderedList.filter((item) => item !== element),
                element,
            ];
            this.update();
        }
        destroy(element) {
            this._orderedList = this._orderedList.filter((item) => item !== element);
            this.update();
        }
        update() {
            for (let i = 0; i < this._orderedList.length; i++) {
                this._orderedList[i].setAttribute('aria-level', `${i}`);
                this._orderedList[i].style.zIndex = `calc(var(--dv-overlay-z-index, 999) + ${i * 2})`;
            }
        }
    }
    const arialLevelTracker = new AriaLevelTracker();
    class Overlay extends CompositeDisposable {
        set minimumInViewportWidth(value) {
            this.options.minimumInViewportWidth = value;
        }
        set minimumInViewportHeight(value) {
            this.options.minimumInViewportHeight = value;
        }
        get element() {
            return this._element;
        }
        get isVisible() {
            return this._isVisible;
        }
        constructor(options) {
            super();
            this.options = options;
            this._element = document.createElement('div');
            this._onDidChange = new Emitter();
            this.onDidChange = this._onDidChange.event;
            this._onDidChangeEnd = new Emitter();
            this.onDidChangeEnd = this._onDidChangeEnd.event;
            this.addDisposables(this._onDidChange, this._onDidChangeEnd);
            this._element.className = 'dv-resize-container';
            this._isVisible = true;
            this.setupResize('top');
            this.setupResize('bottom');
            this.setupResize('left');
            this.setupResize('right');
            this.setupResize('topleft');
            this.setupResize('topright');
            this.setupResize('bottomleft');
            this.setupResize('bottomright');
            this._element.appendChild(this.options.content);
            this.options.container.appendChild(this._element);
            // if input bad resize within acceptable boundaries
            this.setBounds(Object.assign(Object.assign(Object.assign(Object.assign({ height: this.options.height, width: this.options.width }, ('top' in this.options && { top: this.options.top })), ('bottom' in this.options && { bottom: this.options.bottom })), ('left' in this.options && { left: this.options.left })), ('right' in this.options && { right: this.options.right })));
            arialLevelTracker.push(this._element);
        }
        setVisible(isVisible) {
            if (isVisible === this.isVisible) {
                return;
            }
            this._isVisible = isVisible;
            toggleClass(this.element, 'dv-hidden', !this.isVisible);
        }
        bringToFront() {
            arialLevelTracker.push(this._element);
        }
        setBounds(bounds = {}) {
            if (typeof bounds.height === 'number') {
                this._element.style.height = `${bounds.height}px`;
            }
            if (typeof bounds.width === 'number') {
                this._element.style.width = `${bounds.width}px`;
            }
            if ('top' in bounds && typeof bounds.top === 'number') {
                this._element.style.top = `${bounds.top}px`;
                this._element.style.bottom = 'auto';
                this.verticalAlignment = 'top';
            }
            if ('bottom' in bounds && typeof bounds.bottom === 'number') {
                this._element.style.bottom = `${bounds.bottom}px`;
                this._element.style.top = 'auto';
                this.verticalAlignment = 'bottom';
            }
            if ('left' in bounds && typeof bounds.left === 'number') {
                this._element.style.left = `${bounds.left}px`;
                this._element.style.right = 'auto';
                this.horiziontalAlignment = 'left';
            }
            if ('right' in bounds && typeof bounds.right === 'number') {
                this._element.style.right = `${bounds.right}px`;
                this._element.style.left = 'auto';
                this.horiziontalAlignment = 'right';
            }
            const containerRect = this.options.container.getBoundingClientRect();
            const overlayRect = this._element.getBoundingClientRect();
            // region: ensure bounds within allowable limits
            // a minimum width of minimumViewportWidth must be inside the viewport
            const xOffset = Math.max(0, this.getMinimumWidth(overlayRect.width));
            // a minimum height of minimumViewportHeight must be inside the viewport
            const yOffset = Math.max(0, this.getMinimumHeight(overlayRect.height));
            if (this.verticalAlignment === 'top') {
                const top = clamp(overlayRect.top - containerRect.top, -yOffset, Math.max(0, containerRect.height - overlayRect.height + yOffset));
                this._element.style.top = `${top}px`;
                this._element.style.bottom = 'auto';
            }
            if (this.verticalAlignment === 'bottom') {
                const bottom = clamp(containerRect.bottom - overlayRect.bottom, -yOffset, Math.max(0, containerRect.height - overlayRect.height + yOffset));
                this._element.style.bottom = `${bottom}px`;
                this._element.style.top = 'auto';
            }
            if (this.horiziontalAlignment === 'left') {
                const left = clamp(overlayRect.left - containerRect.left, -xOffset, Math.max(0, containerRect.width - overlayRect.width + xOffset));
                this._element.style.left = `${left}px`;
                this._element.style.right = 'auto';
            }
            if (this.horiziontalAlignment === 'right') {
                const right = clamp(containerRect.right - overlayRect.right, -xOffset, Math.max(0, containerRect.width - overlayRect.width + xOffset));
                this._element.style.right = `${right}px`;
                this._element.style.left = 'auto';
            }
            this._onDidChange.fire();
        }
        toJSON() {
            const container = this.options.container.getBoundingClientRect();
            const element = this._element.getBoundingClientRect();
            const result = {};
            if (this.verticalAlignment === 'top') {
                result.top = parseFloat(this._element.style.top);
            }
            else if (this.verticalAlignment === 'bottom') {
                result.bottom = parseFloat(this._element.style.bottom);
            }
            else {
                result.top = element.top - container.top;
            }
            if (this.horiziontalAlignment === 'left') {
                result.left = parseFloat(this._element.style.left);
            }
            else if (this.horiziontalAlignment === 'right') {
                result.right = parseFloat(this._element.style.right);
            }
            else {
                result.left = element.left - container.left;
            }
            result.width = element.width;
            result.height = element.height;
            return result;
        }
        setupDrag(dragTarget, options = { inDragMode: false }) {
            const move = new MutableDisposable();
            const track = () => {
                let offset = null;
                const iframes = disableIframePointEvents();
                move.value = new CompositeDisposable({
                    dispose: () => {
                        iframes.release();
                    },
                }, addDisposableListener(window, 'pointermove', (e) => {
                    const containerRect = this.options.container.getBoundingClientRect();
                    const x = e.clientX - containerRect.left;
                    const y = e.clientY - containerRect.top;
                    toggleClass(this._element, 'dv-resize-container-dragging', true);
                    const overlayRect = this._element.getBoundingClientRect();
                    if (offset === null) {
                        offset = {
                            x: e.clientX - overlayRect.left,
                            y: e.clientY - overlayRect.top,
                        };
                    }
                    const xOffset = Math.max(0, this.getMinimumWidth(overlayRect.width));
                    const yOffset = Math.max(0, this.getMinimumHeight(overlayRect.height));
                    const top = clamp(y - offset.y, -yOffset, Math.max(0, containerRect.height - overlayRect.height + yOffset));
                    const bottom = clamp(offset.y -
                        y +
                        containerRect.height -
                        overlayRect.height, -yOffset, Math.max(0, containerRect.height - overlayRect.height + yOffset));
                    const left = clamp(x - offset.x, -xOffset, Math.max(0, containerRect.width - overlayRect.width + xOffset));
                    const right = clamp(offset.x - x + containerRect.width - overlayRect.width, -xOffset, Math.max(0, containerRect.width - overlayRect.width + xOffset));
                    const bounds = {};
                    // Anchor to top or to bottom depending on which one is closer
                    if (top <= bottom) {
                        bounds.top = top;
                    }
                    else {
                        bounds.bottom = bottom;
                    }
                    // Anchor to left or to right depending on which one is closer
                    if (left <= right) {
                        bounds.left = left;
                    }
                    else {
                        bounds.right = right;
                    }
                    this.setBounds(bounds);
                }), addDisposableListener(window, 'pointerup', () => {
                    toggleClass(this._element, 'dv-resize-container-dragging', false);
                    move.dispose();
                    this._onDidChangeEnd.fire();
                }));
            };
            this.addDisposables(move, addDisposableListener(dragTarget, 'pointerdown', (event) => {
                if (event.defaultPrevented) {
                    event.preventDefault();
                    return;
                }
                // if somebody has marked this event then treat as a defaultPrevented
                // without actually calling event.preventDefault()
                if (quasiDefaultPrevented(event)) {
                    return;
                }
                track();
            }), addDisposableListener(this.options.content, 'pointerdown', (event) => {
                if (event.defaultPrevented) {
                    return;
                }
                // if somebody has marked this event then treat as a defaultPrevented
                // without actually calling event.preventDefault()
                if (quasiDefaultPrevented(event)) {
                    return;
                }
                if (event.shiftKey) {
                    track();
                }
            }), addDisposableListener(this.options.content, 'pointerdown', () => {
                arialLevelTracker.push(this._element);
            }, true));
            if (options.inDragMode) {
                track();
            }
        }
        setupResize(direction) {
            const resizeHandleElement = document.createElement('div');
            resizeHandleElement.className = `dv-resize-handle-${direction}`;
            this._element.appendChild(resizeHandleElement);
            const move = new MutableDisposable();
            this.addDisposables(move, addDisposableListener(resizeHandleElement, 'pointerdown', (e) => {
                e.preventDefault();
                let startPosition = null;
                const iframes = disableIframePointEvents();
                move.value = new CompositeDisposable(addDisposableListener(window, 'pointermove', (e) => {
                    const containerRect = this.options.container.getBoundingClientRect();
                    const overlayRect = this._element.getBoundingClientRect();
                    const y = e.clientY - containerRect.top;
                    const x = e.clientX - containerRect.left;
                    if (startPosition === null) {
                        // record the initial dimensions since as all subsequence moves are relative to this
                        startPosition = {
                            originalY: y,
                            originalHeight: overlayRect.height,
                            originalX: x,
                            originalWidth: overlayRect.width,
                        };
                    }
                    let top = undefined;
                    let bottom = undefined;
                    let height = undefined;
                    let left = undefined;
                    let right = undefined;
                    let width = undefined;
                    const moveTop = () => {
                        top = clamp(y, -Number.MAX_VALUE, startPosition.originalY +
                            startPosition.originalHeight >
                            containerRect.height
                            ? this.getMinimumHeight(containerRect.height)
                            : Math.max(0, startPosition.originalY +
                                startPosition.originalHeight -
                                Overlay.MINIMUM_HEIGHT));
                        height =
                            startPosition.originalY +
                                startPosition.originalHeight -
                                top;
                        bottom = containerRect.height - top - height;
                    };
                    const moveBottom = () => {
                        top =
                            startPosition.originalY -
                                startPosition.originalHeight;
                        height = clamp(y - top, top < 0 &&
                            typeof this.options
                                .minimumInViewportHeight === 'number'
                            ? -top +
                                this.options.minimumInViewportHeight
                            : Overlay.MINIMUM_HEIGHT, Number.MAX_VALUE);
                        bottom = containerRect.height - top - height;
                    };
                    const moveLeft = () => {
                        left = clamp(x, -Number.MAX_VALUE, startPosition.originalX +
                            startPosition.originalWidth >
                            containerRect.width
                            ? this.getMinimumWidth(containerRect.width)
                            : Math.max(0, startPosition.originalX +
                                startPosition.originalWidth -
                                Overlay.MINIMUM_WIDTH));
                        width =
                            startPosition.originalX +
                                startPosition.originalWidth -
                                left;
                        right = containerRect.width - left - width;
                    };
                    const moveRight = () => {
                        left =
                            startPosition.originalX -
                                startPosition.originalWidth;
                        width = clamp(x - left, left < 0 &&
                            typeof this.options
                                .minimumInViewportWidth === 'number'
                            ? -left +
                                this.options.minimumInViewportWidth
                            : Overlay.MINIMUM_WIDTH, Number.MAX_VALUE);
                        right = containerRect.width - left - width;
                    };
                    switch (direction) {
                        case 'top':
                            moveTop();
                            break;
                        case 'bottom':
                            moveBottom();
                            break;
                        case 'left':
                            moveLeft();
                            break;
                        case 'right':
                            moveRight();
                            break;
                        case 'topleft':
                            moveTop();
                            moveLeft();
                            break;
                        case 'topright':
                            moveTop();
                            moveRight();
                            break;
                        case 'bottomleft':
                            moveBottom();
                            moveLeft();
                            break;
                        case 'bottomright':
                            moveBottom();
                            moveRight();
                            break;
                    }
                    const bounds = {};
                    // Anchor to top or to bottom depending on which one is closer
                    if (top <= bottom) {
                        bounds.top = top;
                    }
                    else {
                        bounds.bottom = bottom;
                    }
                    // Anchor to left or to right depending on which one is closer
                    if (left <= right) {
                        bounds.left = left;
                    }
                    else {
                        bounds.right = right;
                    }
                    bounds.height = height;
                    bounds.width = width;
                    this.setBounds(bounds);
                }), {
                    dispose: () => {
                        iframes.release();
                    },
                }, addDisposableListener(window, 'pointerup', () => {
                    move.dispose();
                    this._onDidChangeEnd.fire();
                }));
            }));
        }
        getMinimumWidth(width) {
            if (typeof this.options.minimumInViewportWidth === 'number') {
                return width - this.options.minimumInViewportWidth;
            }
            return 0;
        }
        getMinimumHeight(height) {
            if (typeof this.options.minimumInViewportHeight === 'number') {
                return height - this.options.minimumInViewportHeight;
            }
            return 0;
        }
        dispose() {
            arialLevelTracker.destroy(this._element);
            this._element.remove();
            super.dispose();
        }
    }
    Overlay.MINIMUM_HEIGHT = 20;
    Overlay.MINIMUM_WIDTH = 20;

    class DockviewFloatingGroupPanel extends CompositeDisposable {
        constructor(group, overlay) {
            super();
            this.group = group;
            this.overlay = overlay;
            this.addDisposables(overlay);
        }
        position(bounds) {
            this.overlay.setBounds(bounds);
        }
    }

    const DEFAULT_FLOATING_GROUP_OVERFLOW_SIZE = 100;
    const DEFAULT_FLOATING_GROUP_POSITION = { left: 100, top: 100, width: 300, height: 300 };
    const DESERIALIZATION_POPOUT_DELAY_MS = 100;

    class PositionCache {
        constructor() {
            this.cache = new Map();
            this.currentFrameId = 0;
            this.rafId = null;
        }
        getPosition(element) {
            const cached = this.cache.get(element);
            if (cached && cached.frameId === this.currentFrameId) {
                return cached.rect;
            }
            this.scheduleFrameUpdate();
            const rect = getDomNodePagePosition(element);
            this.cache.set(element, { rect, frameId: this.currentFrameId });
            return rect;
        }
        invalidate() {
            this.currentFrameId++;
        }
        scheduleFrameUpdate() {
            if (this.rafId)
                return;
            this.rafId = requestAnimationFrame(() => {
                this.currentFrameId++;
                this.rafId = null;
            });
        }
    }
    function createFocusableElement() {
        const element = document.createElement('div');
        element.tabIndex = -1;
        return element;
    }
    class OverlayRenderContainer extends CompositeDisposable {
        constructor(element, accessor) {
            super();
            this.element = element;
            this.accessor = accessor;
            this.map = {};
            this._disposed = false;
            this.positionCache = new PositionCache();
            this.pendingUpdates = new Set();
            this.addDisposables(exports.DockviewDisposable.from(() => {
                for (const value of Object.values(this.map)) {
                    value.disposable.dispose();
                    value.destroy.dispose();
                }
                this._disposed = true;
            }));
        }
        updateAllPositions() {
            if (this._disposed) {
                return;
            }
            // Invalidate position cache to force recalculation
            this.positionCache.invalidate();
            // Call resize function directly for all visible panels
            for (const entry of Object.values(this.map)) {
                if (entry.panel.api.isVisible && entry.resize) {
                    entry.resize();
                }
            }
        }
        detatch(panel) {
            if (this.map[panel.api.id]) {
                const { disposable, destroy } = this.map[panel.api.id];
                disposable.dispose();
                destroy.dispose();
                delete this.map[panel.api.id];
                return true;
            }
            return false;
        }
        attach(options) {
            const { panel, referenceContainer } = options;
            if (!this.map[panel.api.id]) {
                const element = createFocusableElement();
                element.className = 'dv-render-overlay';
                this.map[panel.api.id] = {
                    panel,
                    disposable: exports.DockviewDisposable.NONE,
                    destroy: exports.DockviewDisposable.NONE,
                    element,
                };
            }
            const focusContainer = this.map[panel.api.id].element;
            if (panel.view.content.element.parentElement !== focusContainer) {
                focusContainer.appendChild(panel.view.content.element);
            }
            if (focusContainer.parentElement !== this.element) {
                this.element.appendChild(focusContainer);
            }
            const resize = () => {
                const panelId = panel.api.id;
                if (this.pendingUpdates.has(panelId)) {
                    return; // Update already scheduled
                }
                this.pendingUpdates.add(panelId);
                requestAnimationFrame(() => {
                    this.pendingUpdates.delete(panelId);
                    if (this.isDisposed || !this.map[panelId]) {
                        return;
                    }
                    const box = this.positionCache.getPosition(referenceContainer.element);
                    const box2 = this.positionCache.getPosition(this.element);
                    // Use traditional positioning for overlay containers
                    const left = box.left - box2.left;
                    const top = box.top - box2.top;
                    const width = box.width;
                    const height = box.height;
                    focusContainer.style.left = `${left}px`;
                    focusContainer.style.top = `${top}px`;
                    focusContainer.style.width = `${width}px`;
                    focusContainer.style.height = `${height}px`;
                    toggleClass(focusContainer, 'dv-render-overlay-float', panel.group.api.location.type === 'floating');
                });
            };
            const visibilityChanged = () => {
                if (panel.api.isVisible) {
                    this.positionCache.invalidate();
                    resize();
                }
                focusContainer.style.display = panel.api.isVisible ? '' : 'none';
            };
            const observerDisposable = new MutableDisposable();
            const correctLayerPosition = () => {
                if (panel.api.location.type === 'floating') {
                    queueMicrotask(() => {
                        const floatingGroup = this.accessor.floatingGroups.find((group) => group.group === panel.api.group);
                        if (!floatingGroup) {
                            return;
                        }
                        const element = floatingGroup.overlay.element;
                        const update = () => {
                            const level = Number(element.getAttribute('aria-level'));
                            focusContainer.style.zIndex = `calc(var(--dv-overlay-z-index, 999) + ${level * 2 + 1})`;
                        };
                        const observer = new MutationObserver(() => {
                            update();
                        });
                        observerDisposable.value = exports.DockviewDisposable.from(() => observer.disconnect());
                        observer.observe(element, {
                            attributeFilter: ['aria-level'],
                            attributes: true,
                        });
                        update();
                    });
                }
                else {
                    focusContainer.style.zIndex = ''; // reset the z-index, perhaps CSS will take over here
                }
            };
            const disposable = new CompositeDisposable(observerDisposable, 
            /**
             * since container is positioned absoutely we must explicitly forward
             * the dnd events for the expect behaviours to continue to occur in terms of dnd
             *
             * the dnd observer does not need to be conditional on whether the panel is visible since
             * non-visible panels are 'display: none' and in such case the dnd observer will not fire.
             */
            new DragAndDropObserver(focusContainer, {
                onDragEnd: (e) => {
                    referenceContainer.dropTarget.dnd.onDragEnd(e);
                },
                onDragEnter: (e) => {
                    referenceContainer.dropTarget.dnd.onDragEnter(e);
                },
                onDragLeave: (e) => {
                    referenceContainer.dropTarget.dnd.onDragLeave(e);
                },
                onDrop: (e) => {
                    referenceContainer.dropTarget.dnd.onDrop(e);
                },
                onDragOver: (e) => {
                    referenceContainer.dropTarget.dnd.onDragOver(e);
                },
            }), panel.api.onDidVisibilityChange(() => {
                /**
                 * Control the visibility of the content, however even when not visible (display: none)
                 * the content is still maintained within the DOM hence DOM specific attributes
                 * such as scroll position are maintained when next made visible.
                 */
                visibilityChanged();
            }), panel.api.onDidDimensionsChange(() => {
                if (!panel.api.isVisible) {
                    return;
                }
                resize();
            }), panel.api.onDidLocationChange(() => {
                correctLayerPosition();
            }));
            this.map[panel.api.id].destroy = exports.DockviewDisposable.from(() => {
                var _a;
                if (panel.view.content.element.parentElement === focusContainer) {
                    focusContainer.removeChild(panel.view.content.element);
                }
                (_a = focusContainer.parentElement) === null || _a === void 0 ? void 0 : _a.removeChild(focusContainer);
            });
            correctLayerPosition();
            queueMicrotask(() => {
                if (this.isDisposed) {
                    return;
                }
                /**
                 * wait until everything has finished in the current stack-frame call before
                 * calling the first resize as other size-altering events may still occur before
                 * the end of the stack-frame.
                 */
                visibilityChanged();
            });
            // dispose of logic asoccciated with previous reference-container
            this.map[panel.api.id].disposable.dispose();
            // and reset the disposable to the active reference-container
            this.map[panel.api.id].disposable = disposable;
            // store the resize function for direct access
            this.map[panel.api.id].resize = resize;
            return focusContainer;
        }
    }

    /******************************************************************************
    Copyright (c) Microsoft Corporation.

    Permission to use, copy, modify, and/or distribute this software for any
    purpose with or without fee is hereby granted.

    THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
    REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
    AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
    INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
    LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
    OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
    PERFORMANCE OF THIS SOFTWARE.
    ***************************************************************************** */
    /* global Reflect, Promise, SuppressedError, Symbol */


    function __awaiter(thisArg, _arguments, P, generator) {
        function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
        return new (P || (P = Promise))(function (resolve, reject) {
            function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
            function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
            function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
            step((generator = generator.apply(thisArg, _arguments || [])).next());
        });
    }

    typeof SuppressedError === "function" ? SuppressedError : function (error, suppressed, message) {
        var e = new Error(message);
        return e.name = "SuppressedError", e.error = error, e.suppressed = suppressed, e;
    };

    class PopoutWindow extends CompositeDisposable {
        get window() {
            var _a, _b;
            return (_b = (_a = this._window) === null || _a === void 0 ? void 0 : _a.value) !== null && _b !== void 0 ? _b : null;
        }
        constructor(target, className, options) {
            super();
            this.target = target;
            this.className = className;
            this.options = options;
            this._onWillClose = new Emitter();
            this.onWillClose = this._onWillClose.event;
            this._onDidClose = new Emitter();
            this.onDidClose = this._onDidClose.event;
            this._window = null;
            this.addDisposables(this._onWillClose, this._onDidClose, {
                dispose: () => {
                    this.close();
                },
            });
        }
        dimensions() {
            if (!this._window) {
                return null;
            }
            const left = this._window.value.screenX;
            const top = this._window.value.screenY;
            const width = this._window.value.innerWidth;
            const height = this._window.value.innerHeight;
            return { top, left, width, height };
        }
        close() {
            var _a, _b;
            if (this._window) {
                this._onWillClose.fire();
                (_b = (_a = this.options).onWillClose) === null || _b === void 0 ? void 0 : _b.call(_a, {
                    id: this.target,
                    window: this._window.value,
                });
                this._window.disposable.dispose();
                this._window = null;
                this._onDidClose.fire();
            }
        }
        open() {
            var _a, _b;
            return __awaiter(this, void 0, void 0, function* () {
                if (this._window) {
                    throw new Error('instance of popout window is already open');
                }
                const url = `${this.options.url}`;
                const features = Object.entries({
                    top: this.options.top,
                    left: this.options.left,
                    width: this.options.width,
                    height: this.options.height,
                })
                    .map(([key, value]) => `${key}=${value}`)
                    .join(',');
                /**
                 * @see https://developer.mozilla.org/en-US/docs/Web/API/Window/open
                 */
                const externalWindow = window.open(url, this.target, features);
                if (!externalWindow) {
                    /**
                     * Popup blocked
                     */
                    return null;
                }
                const disposable = new CompositeDisposable();
                this._window = { value: externalWindow, disposable };
                disposable.addDisposables(exports.DockviewDisposable.from(() => {
                    externalWindow.close();
                }), addDisposableListener(window, 'beforeunload', () => {
                    /**
                     * before the main window closes we should close this popup too
                     * to be good citizens
                     *
                     * @see https://developer.mozilla.org/en-US/docs/Web/API/Window/beforeunload_event
                     */
                    this.close();
                }));
                const container = this.createPopoutWindowContainer();
                if (this.className) {
                    container.classList.add(this.className);
                }
                (_b = (_a = this.options).onDidOpen) === null || _b === void 0 ? void 0 : _b.call(_a, {
                    id: this.target,
                    window: externalWindow,
                });
                return new Promise((resolve, reject) => {
                    externalWindow.addEventListener('unload', (e) => {
                        // if page fails to load before unloading
                        // this.close();
                    });
                    externalWindow.addEventListener('load', () => {
                        /**
                         * @see https://developer.mozilla.org/en-US/docs/Web/API/Window/load_event
                         */
                        try {
                            const externalDocument = externalWindow.document;
                            externalDocument.title = document.title;
                            externalDocument.body.appendChild(container);
                            addStyles(externalDocument, window.document.styleSheets);
                            /**
                             * beforeunload must be registered after load for reasons I could not determine
                             * otherwise the beforeunload event will not fire when the window is closed
                             */
                            addDisposableListener(externalWindow, 'beforeunload', () => {
                                /**
                                 * @see https://developer.mozilla.org/en-US/docs/Web/API/Window/beforeunload_event
                                 */
                                this.close();
                            });
                            resolve(container);
                        }
                        catch (err) {
                            // only except this is the DOM isn't setup. e.g. in a in correctly configured test
                            reject(err);
                        }
                    });
                });
            });
        }
        createPopoutWindowContainer() {
            const el = document.createElement('div');
            el.classList.add('dv-popout-window');
            el.id = 'dv-popout-window';
            el.style.position = 'absolute';
            el.style.width = '100%';
            el.style.height = '100%';
            el.style.top = '0px';
            el.style.left = '0px';
            return el;
        }
    }

    class StrictEventsSequencing extends CompositeDisposable {
        constructor(accessor) {
            super();
            this.accessor = accessor;
            this.init();
        }
        init() {
            const panels = new Set();
            const groups = new Set();
            this.addDisposables(this.accessor.onDidAddPanel((panel) => {
                if (panels.has(panel.api.id)) {
                    throw new Error(`dockview: Invalid event sequence. [onDidAddPanel] called for panel ${panel.api.id} but panel already exists`);
                }
                else {
                    panels.add(panel.api.id);
                }
            }), this.accessor.onDidRemovePanel((panel) => {
                if (!panels.has(panel.api.id)) {
                    throw new Error(`dockview: Invalid event sequence. [onDidRemovePanel] called for panel ${panel.api.id} but panel does not exists`);
                }
                else {
                    panels.delete(panel.api.id);
                }
            }), this.accessor.onDidAddGroup((group) => {
                if (groups.has(group.api.id)) {
                    throw new Error(`dockview: Invalid event sequence. [onDidAddGroup] called for group ${group.api.id} but group already exists`);
                }
                else {
                    groups.add(group.api.id);
                }
            }), this.accessor.onDidRemoveGroup((group) => {
                if (!groups.has(group.api.id)) {
                    throw new Error(`dockview: Invalid event sequence. [onDidRemoveGroup] called for group ${group.api.id} but group does not exists`);
                }
                else {
                    groups.delete(group.api.id);
                }
            }));
        }
    }

    class PopupService extends CompositeDisposable {
        constructor(root) {
            super();
            this.root = root;
            this._active = null;
            this._activeDisposable = new MutableDisposable();
            this._element = document.createElement('div');
            this._element.className = 'dv-popover-anchor';
            this._element.style.position = 'relative';
            this.root.prepend(this._element);
            this.addDisposables(exports.DockviewDisposable.from(() => {
                this.close();
            }), this._activeDisposable);
        }
        openPopover(element, position) {
            var _a;
            this.close();
            const wrapper = document.createElement('div');
            wrapper.style.position = 'absolute';
            wrapper.style.zIndex = (_a = position.zIndex) !== null && _a !== void 0 ? _a : 'var(--dv-overlay-z-index)';
            wrapper.appendChild(element);
            const anchorBox = this._element.getBoundingClientRect();
            const offsetX = anchorBox.left;
            const offsetY = anchorBox.top;
            wrapper.style.top = `${position.y - offsetY}px`;
            wrapper.style.left = `${position.x - offsetX}px`;
            this._element.appendChild(wrapper);
            this._active = wrapper;
            this._activeDisposable.value = new CompositeDisposable(addDisposableListener(window, 'pointerdown', (event) => {
                var _a;
                const target = event.target;
                if (!(target instanceof HTMLElement)) {
                    return;
                }
                let el = target;
                while (el && el !== wrapper) {
                    el = (_a = el === null || el === void 0 ? void 0 : el.parentElement) !== null && _a !== void 0 ? _a : null;
                }
                if (el) {
                    return; // clicked within popover
                }
                this.close();
            }));
            requestAnimationFrame(() => {
                shiftAbsoluteElementIntoView(wrapper, this.root);
            });
        }
        close() {
            if (this._active) {
                this._active.remove();
                this._activeDisposable.dispose();
                this._active = null;
            }
        }
    }

    class DropTargetAnchorContainer extends CompositeDisposable {
        get disabled() {
            return this._disabled;
        }
        set disabled(value) {
            var _a;
            if (this.disabled === value) {
                return;
            }
            this._disabled = value;
            if (value) {
                (_a = this.model) === null || _a === void 0 ? void 0 : _a.clear();
            }
        }
        get model() {
            if (this.disabled) {
                return undefined;
            }
            return {
                clear: () => {
                    var _a;
                    if (this._model) {
                        (_a = this._model.root.parentElement) === null || _a === void 0 ? void 0 : _a.removeChild(this._model.root);
                    }
                    this._model = undefined;
                },
                exists: () => {
                    return !!this._model;
                },
                getElements: (event, outline) => {
                    const changed = this._outline !== outline;
                    this._outline = outline;
                    if (this._model) {
                        this._model.changed = changed;
                        return this._model;
                    }
                    const container = this.createContainer();
                    const anchor = this.createAnchor();
                    this._model = { root: container, overlay: anchor, changed };
                    container.appendChild(anchor);
                    this.element.appendChild(container);
                    if ((event === null || event === void 0 ? void 0 : event.target) instanceof HTMLElement) {
                        const targetBox = event.target.getBoundingClientRect();
                        const box = this.element.getBoundingClientRect();
                        anchor.style.left = `${targetBox.left - box.left}px`;
                        anchor.style.top = `${targetBox.top - box.top}px`;
                    }
                    return this._model;
                },
            };
        }
        constructor(element, options) {
            super();
            this.element = element;
            this._disabled = false;
            this._disabled = options.disabled;
            this.addDisposables(exports.DockviewDisposable.from(() => {
                var _a;
                (_a = this.model) === null || _a === void 0 ? void 0 : _a.clear();
            }));
        }
        createContainer() {
            const el = document.createElement('div');
            el.className = 'dv-drop-target-container';
            return el;
        }
        createAnchor() {
            const el = document.createElement('div');
            el.className = 'dv-drop-target-anchor';
            el.style.visibility = 'hidden';
            return el;
        }
    }

    const DEFAULT_ROOT_OVERLAY_MODEL = {
        activationSize: { type: 'pixels', value: 10 },
        size: { type: 'pixels', value: 20 },
    };
    function moveGroupWithoutDestroying(options) {
        const activePanel = options.from.activePanel;
        const panels = [...options.from.panels].map((panel) => {
            const removedPanel = options.from.model.removePanel(panel);
            options.from.model.renderContainer.detatch(panel);
            return removedPanel;
        });
        panels.forEach((panel) => {
            options.to.model.openPanel(panel, {
                skipSetActive: activePanel !== panel,
                skipSetGroupActive: true,
            });
        });
    }
    class DockviewComponent extends BaseGrid {
        get orientation() {
            return this.gridview.orientation;
        }
        get totalPanels() {
            return this.panels.length;
        }
        get panels() {
            return this.groups.flatMap((group) => group.panels);
        }
        get options() {
            return this._options;
        }
        get activePanel() {
            const activeGroup = this.activeGroup;
            if (!activeGroup) {
                return undefined;
            }
            return activeGroup.activePanel;
        }
        get renderer() {
            var _a;
            return (_a = this.options.defaultRenderer) !== null && _a !== void 0 ? _a : 'onlyWhenVisible';
        }
        get api() {
            return this._api;
        }
        get floatingGroups() {
            return this._floatingGroups;
        }
        /**
         * Promise that resolves when all popout groups from the last fromJSON call are restored.
         * Useful for tests that need to wait for delayed popout creation.
         */
        get popoutRestorationPromise() {
            return this._popoutRestorationPromise;
        }
        constructor(container, options) {
            var _a, _b, _c;
            super(container, {
                proportionalLayout: true,
                orientation: exports.Orientation.HORIZONTAL,
                styles: options.hideBorders
                    ? { separatorBorder: 'transparent' }
                    : undefined,
                disableAutoResizing: options.disableAutoResizing,
                locked: options.locked,
                margin: (_b = (_a = options.theme) === null || _a === void 0 ? void 0 : _a.gap) !== null && _b !== void 0 ? _b : 0,
                className: options.className,
            });
            this.nextGroupId = sequentialNumberGenerator();
            this._deserializer = new DefaultDockviewDeserialzier(this);
            this._watermark = null;
            this._onWillDragPanel = new Emitter();
            this.onWillDragPanel = this._onWillDragPanel.event;
            this._onWillDragGroup = new Emitter();
            this.onWillDragGroup = this._onWillDragGroup.event;
            this._onDidDrop = new Emitter();
            this.onDidDrop = this._onDidDrop.event;
            this._onWillDrop = new Emitter();
            this.onWillDrop = this._onWillDrop.event;
            this._onWillShowOverlay = new Emitter();
            this.onWillShowOverlay = this._onWillShowOverlay.event;
            this._onUnhandledDragOverEvent = new Emitter();
            this.onUnhandledDragOverEvent = this._onUnhandledDragOverEvent.event;
            this._onDidRemovePanel = new Emitter();
            this.onDidRemovePanel = this._onDidRemovePanel.event;
            this._onDidAddPanel = new Emitter();
            this.onDidAddPanel = this._onDidAddPanel.event;
            this._onDidPopoutGroupSizeChange = new Emitter();
            this.onDidPopoutGroupSizeChange = this._onDidPopoutGroupSizeChange.event;
            this._onDidPopoutGroupPositionChange = new Emitter();
            this.onDidPopoutGroupPositionChange = this._onDidPopoutGroupPositionChange.event;
            this._onDidOpenPopoutWindowFail = new Emitter();
            this.onDidOpenPopoutWindowFail = this._onDidOpenPopoutWindowFail.event;
            this._onDidLayoutFromJSON = new Emitter();
            this.onDidLayoutFromJSON = this._onDidLayoutFromJSON.event;
            this._onDidActivePanelChange = new Emitter({ replay: true });
            this.onDidActivePanelChange = this._onDidActivePanelChange.event;
            this._onDidMovePanel = new Emitter();
            this.onDidMovePanel = this._onDidMovePanel.event;
            this._onDidMaximizedGroupChange = new Emitter();
            this.onDidMaximizedGroupChange = this._onDidMaximizedGroupChange.event;
            this._floatingGroups = [];
            this._popoutGroups = [];
            this._popoutRestorationPromise = Promise.resolve();
            this._onDidRemoveGroup = new Emitter();
            this.onDidRemoveGroup = this._onDidRemoveGroup.event;
            this._onDidAddGroup = new Emitter();
            this.onDidAddGroup = this._onDidAddGroup.event;
            this._onDidOptionsChange = new Emitter();
            this.onDidOptionsChange = this._onDidOptionsChange.event;
            this._onDidActiveGroupChange = new Emitter();
            this.onDidActiveGroupChange = this._onDidActiveGroupChange.event;
            this._moving = false;
            this._options = options;
            this.popupService = new PopupService(this.element);
            this._themeClassnames = new Classnames(this.element);
            this._api = new DockviewApi(this);
            this.rootDropTargetContainer = new DropTargetAnchorContainer(this.element, { disabled: true });
            this.overlayRenderContainer = new OverlayRenderContainer(this.gridview.element, this);
            this._rootDropTarget = new Droptarget(this.element, {
                className: 'dv-drop-target-edge',
                canDisplayOverlay: (event, position) => {
                    const data = getPanelData();
                    if (data) {
                        if (data.viewId !== this.id) {
                            return false;
                        }
                        if (position === 'center') {
                            // center drop target is only allowed if there are no panels in the grid
                            // floating panels are allowed
                            return this.gridview.length === 0;
                        }
                        return true;
                    }
                    if (position === 'center' && this.gridview.length !== 0) {
                        /**
                         * for external events only show the four-corner drag overlays, disable
                         * the center position so that external drag events can fall through to the group
                         * and panel drop target handlers
                         */
                        return false;
                    }
                    const firedEvent = new DockviewUnhandledDragOverEvent(event, 'edge', position, getPanelData);
                    this._onUnhandledDragOverEvent.fire(firedEvent);
                    return firedEvent.isAccepted;
                },
                acceptedTargetZones: ['top', 'bottom', 'left', 'right', 'center'],
                overlayModel: (_c = options.rootOverlayModel) !== null && _c !== void 0 ? _c : DEFAULT_ROOT_OVERLAY_MODEL,
                getOverrideTarget: () => { var _a; return (_a = this.rootDropTargetContainer) === null || _a === void 0 ? void 0 : _a.model; },
            });
            this.updateDropTargetModel(options);
            toggleClass(this.gridview.element, 'dv-dockview', true);
            toggleClass(this.element, 'dv-debug', !!options.debug);
            this.updateTheme();
            this.updateWatermark();
            if (options.debug) {
                this.addDisposables(new StrictEventsSequencing(this));
            }
            this.addDisposables(this.rootDropTargetContainer, this.overlayRenderContainer, this._onWillDragPanel, this._onWillDragGroup, this._onWillShowOverlay, this._onDidActivePanelChange, this._onDidAddPanel, this._onDidRemovePanel, this._onDidLayoutFromJSON, this._onDidDrop, this._onWillDrop, this._onDidMovePanel, this._onDidAddGroup, this._onDidRemoveGroup, this._onDidActiveGroupChange, this._onUnhandledDragOverEvent, this._onDidMaximizedGroupChange, this._onDidOptionsChange, this._onDidPopoutGroupSizeChange, this._onDidPopoutGroupPositionChange, this._onDidOpenPopoutWindowFail, this.onDidViewVisibilityChangeMicroTaskQueue(() => {
                this.updateWatermark();
            }), this.onDidAdd((event) => {
                if (!this._moving) {
                    this._onDidAddGroup.fire(event);
                }
            }), this.onDidRemove((event) => {
                if (!this._moving) {
                    this._onDidRemoveGroup.fire(event);
                }
            }), this.onDidActiveChange((event) => {
                if (!this._moving) {
                    this._onDidActiveGroupChange.fire(event);
                }
            }), this.onDidMaximizedChange((event) => {
                this._onDidMaximizedGroupChange.fire({
                    group: event.panel,
                    isMaximized: event.isMaximized,
                });
            }), exports.DockviewEvent.any(this.onDidAdd, this.onDidRemove)(() => {
                this.updateWatermark();
            }), exports.DockviewEvent.any(this.onDidAddPanel, this.onDidRemovePanel, this.onDidAddGroup, this.onDidRemove, this.onDidMovePanel, this.onDidActivePanelChange, this.onDidPopoutGroupPositionChange, this.onDidPopoutGroupSizeChange)(() => {
                this._bufferOnDidLayoutChange.fire();
            }), exports.DockviewDisposable.from(() => {
                // iterate over a copy of the array since .dispose() mutates the original array
                for (const group of [...this._floatingGroups]) {
                    group.dispose();
                }
                // iterate over a copy of the array since .dispose() mutates the original array
                for (const group of [...this._popoutGroups]) {
                    group.disposable.dispose();
                }
            }), this._rootDropTarget, this._rootDropTarget.onWillShowOverlay((event) => {
                if (this.gridview.length > 0 && event.position === 'center') {
                    // option only available when no panels in primary grid
                    return;
                }
                this._onWillShowOverlay.fire(new WillShowOverlayLocationEvent(event, {
                    kind: 'edge',
                    panel: undefined,
                    api: this._api,
                    group: undefined,
                    getData: getPanelData,
                }));
            }), this._rootDropTarget.onDrop((event) => {
                var _a;
                const willDropEvent = new DockviewWillDropEvent({
                    nativeEvent: event.nativeEvent,
                    position: event.position,
                    panel: undefined,
                    api: this._api,
                    group: undefined,
                    getData: getPanelData,
                    kind: 'edge',
                });
                this._onWillDrop.fire(willDropEvent);
                if (willDropEvent.defaultPrevented) {
                    return;
                }
                const data = getPanelData();
                if (data) {
                    this.moveGroupOrPanel({
                        from: {
                            groupId: data.groupId,
                            panelId: (_a = data.panelId) !== null && _a !== void 0 ? _a : undefined,
                        },
                        to: {
                            group: this.orthogonalize(event.position),
                            position: 'center',
                        },
                    });
                }
                else {
                    this._onDidDrop.fire(new DockviewDidDropEvent({
                        nativeEvent: event.nativeEvent,
                        position: event.position,
                        panel: undefined,
                        api: this._api,
                        group: undefined,
                        getData: getPanelData,
                    }));
                }
            }), this._rootDropTarget);
        }
        setVisible(panel, visible) {
            switch (panel.api.location.type) {
                case 'grid':
                    super.setVisible(panel, visible);
                    break;
                case 'floating': {
                    const item = this.floatingGroups.find((floatingGroup) => floatingGroup.group === panel);
                    if (item) {
                        item.overlay.setVisible(visible);
                        panel.api._onDidVisibilityChange.fire({
                            isVisible: visible,
                        });
                    }
                    break;
                }
                case 'popout':
                    console.warn('dockview: You cannot hide a group that is in a popout window');
                    break;
            }
        }
        addPopoutGroup(itemToPopout, options) {
            var _a, _b, _c, _d, _e;
            if (itemToPopout instanceof DockviewPanel &&
                itemToPopout.group.size === 1) {
                return this.addPopoutGroup(itemToPopout.group, options);
            }
            const theme = getDockviewTheme(this.gridview.element);
            const element = this.element;
            function getBox() {
                if (options === null || options === void 0 ? void 0 : options.position) {
                    return options.position;
                }
                if (itemToPopout instanceof DockviewGroupPanel) {
                    return itemToPopout.element.getBoundingClientRect();
                }
                if (itemToPopout.group) {
                    return itemToPopout.group.element.getBoundingClientRect();
                }
                return element.getBoundingClientRect();
            }
            const box = getBox();
            const groupId = (_b = (_a = options === null || options === void 0 ? void 0 : options.overridePopoutGroup) === null || _a === void 0 ? void 0 : _a.id) !== null && _b !== void 0 ? _b : this.getNextGroupId();
            const _window = new PopoutWindow(`${this.id}-${groupId}`, // unique id
            theme !== null && theme !== void 0 ? theme : '', {
                url: (_e = (_c = options === null || options === void 0 ? void 0 : options.popoutUrl) !== null && _c !== void 0 ? _c : (_d = this.options) === null || _d === void 0 ? void 0 : _d.popoutUrl) !== null && _e !== void 0 ? _e : '/popout.html',
                left: window.screenX + box.left,
                top: window.screenY + box.top,
                width: box.width,
                height: box.height,
                onDidOpen: options === null || options === void 0 ? void 0 : options.onDidOpen,
                onWillClose: options === null || options === void 0 ? void 0 : options.onWillClose,
            });
            const popoutWindowDisposable = new CompositeDisposable(_window, _window.onDidClose(() => {
                popoutWindowDisposable.dispose();
            }));
            return _window
                .open()
                .then((popoutContainer) => {
                var _a;
                if (_window.isDisposed) {
                    return false;
                }
                const referenceGroup = (options === null || options === void 0 ? void 0 : options.referenceGroup)
                    ? options.referenceGroup
                    : itemToPopout instanceof DockviewPanel
                        ? itemToPopout.group
                        : itemToPopout;
                const referenceLocation = itemToPopout.api.location.type;
                /**
                 * The group that is being added doesn't already exist within the DOM, the most likely occurrence
                 * of this case is when being called from the `fromJSON(...)` method
                 */
                const isGroupAddedToDom = referenceGroup.element.parentElement !== null;
                let group;
                if (!isGroupAddedToDom) {
                    group = referenceGroup;
                }
                else if (options === null || options === void 0 ? void 0 : options.overridePopoutGroup) {
                    group = options.overridePopoutGroup;
                }
                else {
                    group = this.createGroup({ id: groupId });
                    if (popoutContainer) {
                        this._onDidAddGroup.fire(group);
                    }
                }
                if (popoutContainer === null) {
                    console.error('dockview: failed to create popout. perhaps you need to allow pop-ups for this website');
                    popoutWindowDisposable.dispose();
                    this._onDidOpenPopoutWindowFail.fire();
                    // if the popout window was blocked, we need to move the group back to the reference group
                    // and set it to visible
                    this.movingLock(() => moveGroupWithoutDestroying({
                        from: group,
                        to: referenceGroup,
                    }));
                    if (!referenceGroup.api.isVisible) {
                        referenceGroup.api.setVisible(true);
                    }
                    return false;
                }
                const gready = document.createElement('div');
                gready.className = 'dv-overlay-render-container';
                const overlayRenderContainer = new OverlayRenderContainer(gready, this);
                group.model.renderContainer = overlayRenderContainer;
                group.layout(_window.window.innerWidth, _window.window.innerHeight);
                let floatingBox;
                if (!(options === null || options === void 0 ? void 0 : options.overridePopoutGroup) && isGroupAddedToDom) {
                    if (itemToPopout instanceof DockviewPanel) {
                        this.movingLock(() => {
                            const panel = referenceGroup.model.removePanel(itemToPopout);
                            group.model.openPanel(panel);
                        });
                    }
                    else {
                        this.movingLock(() => moveGroupWithoutDestroying({
                            from: referenceGroup,
                            to: group,
                        }));
                        switch (referenceLocation) {
                            case 'grid':
                                referenceGroup.api.setVisible(false);
                                break;
                            case 'floating':
                            case 'popout':
                                floatingBox = (_a = this._floatingGroups
                                    .find((value) => value.group.api.id ===
                                    itemToPopout.api.id)) === null || _a === void 0 ? void 0 : _a.overlay.toJSON();
                                this.removeGroup(referenceGroup);
                                break;
                        }
                    }
                }
                popoutContainer.classList.add('dv-dockview');
                popoutContainer.style.overflow = 'hidden';
                popoutContainer.appendChild(gready);
                popoutContainer.appendChild(group.element);
                const anchor = document.createElement('div');
                const dropTargetContainer = new DropTargetAnchorContainer(anchor, { disabled: this.rootDropTargetContainer.disabled });
                popoutContainer.appendChild(anchor);
                group.model.dropTargetContainer = dropTargetContainer;
                group.model.location = {
                    type: 'popout',
                    getWindow: () => _window.window,
                    popoutUrl: options === null || options === void 0 ? void 0 : options.popoutUrl,
                };
                if (isGroupAddedToDom &&
                    itemToPopout.api.location.type === 'grid') {
                    itemToPopout.api.setVisible(false);
                }
                this.doSetGroupAndPanelActive(group);
                popoutWindowDisposable.addDisposables(group.api.onDidActiveChange((event) => {
                    var _a;
                    if (event.isActive) {
                        (_a = _window.window) === null || _a === void 0 ? void 0 : _a.focus();
                    }
                }), group.api.onWillFocus(() => {
                    var _a;
                    (_a = _window.window) === null || _a === void 0 ? void 0 : _a.focus();
                }));
                let returnedGroup;
                const isValidReferenceGroup = isGroupAddedToDom &&
                    referenceGroup &&
                    this.getPanel(referenceGroup.id);
                const value = {
                    window: _window,
                    popoutGroup: group,
                    referenceGroup: isValidReferenceGroup
                        ? referenceGroup.id
                        : undefined,
                    disposable: {
                        dispose: () => {
                            popoutWindowDisposable.dispose();
                            return returnedGroup;
                        },
                    },
                };
                const _onDidWindowPositionChange = onDidWindowMoveEnd(_window.window);
                popoutWindowDisposable.addDisposables(_onDidWindowPositionChange, onDidWindowResizeEnd(_window.window, () => {
                    this._onDidPopoutGroupSizeChange.fire({
                        width: _window.window.innerWidth,
                        height: _window.window.innerHeight,
                        group,
                    });
                }), _onDidWindowPositionChange.event(() => {
                    this._onDidPopoutGroupPositionChange.fire({
                        screenX: _window.window.screenX,
                        screenY: _window.window.screenX,
                        group,
                    });
                }), 
                /**
                 * ResizeObserver seems slow here, I do not know why but we don't need it
                 * since we can reply on the window resize event as we will occupy the full
                 * window dimensions
                 */
                addDisposableListener(_window.window, 'resize', () => {
                    group.layout(_window.window.innerWidth, _window.window.innerHeight);
                }), overlayRenderContainer, exports.DockviewDisposable.from(() => {
                    if (this.isDisposed) {
                        return; // cleanup may run after instance is disposed
                    }
                    if (isGroupAddedToDom &&
                        this.getPanel(referenceGroup.id)) {
                        this.movingLock(() => moveGroupWithoutDestroying({
                            from: group,
                            to: referenceGroup,
                        }));
                        if (!referenceGroup.api.isVisible) {
                            referenceGroup.api.setVisible(true);
                        }
                        if (this.getPanel(group.id)) {
                            this.doRemoveGroup(group, {
                                skipPopoutAssociated: true,
                            });
                        }
                    }
                    else if (this.getPanel(group.id)) {
                        group.model.renderContainer =
                            this.overlayRenderContainer;
                        group.model.dropTargetContainer =
                            this.rootDropTargetContainer;
                        returnedGroup = group;
                        const alreadyRemoved = !this._popoutGroups.find((p) => p.popoutGroup === group);
                        if (alreadyRemoved) {
                            /**
                             * If this popout group was explicitly removed then we shouldn't run the additional
                             * steps. To tell if the running of this disposable is the result of this popout group
                             * being explicitly removed we can check if this popout group is still referenced in
                             * the `this._popoutGroups` list.
                             */
                            return;
                        }
                        if (floatingBox) {
                            this.addFloatingGroup(group, {
                                height: floatingBox.height,
                                width: floatingBox.width,
                                position: floatingBox,
                            });
                        }
                        else {
                            this.doRemoveGroup(group, {
                                skipDispose: true,
                                skipActive: true,
                                skipPopoutReturn: true,
                            });
                            group.model.location = { type: 'grid' };
                            this.movingLock(() => {
                                // suppress group add events since the group already exists
                                this.doAddGroup(group, [0]);
                            });
                        }
                        this.doSetGroupAndPanelActive(group);
                    }
                }));
                this._popoutGroups.push(value);
                this.updateWatermark();
                return true;
            })
                .catch((err) => {
                console.error('dockview: failed to create popout.', err);
                return false;
            });
        }
        addFloatingGroup(item, options) {
            var _a, _b, _c, _d, _e;
            let group;
            if (item instanceof DockviewPanel) {
                group = this.createGroup();
                this._onDidAddGroup.fire(group);
                this.movingLock(() => this.removePanel(item, {
                    removeEmptyGroup: true,
                    skipDispose: true,
                    skipSetActiveGroup: true,
                }));
                this.movingLock(() => group.model.openPanel(item, { skipSetGroupActive: true }));
            }
            else {
                group = item;
                const popoutReferenceGroupId = (_a = this._popoutGroups.find((_) => _.popoutGroup === group)) === null || _a === void 0 ? void 0 : _a.referenceGroup;
                const popoutReferenceGroup = popoutReferenceGroupId
                    ? this.getPanel(popoutReferenceGroupId)
                    : undefined;
                const skip = typeof (options === null || options === void 0 ? void 0 : options.skipRemoveGroup) === 'boolean' &&
                    options.skipRemoveGroup;
                if (!skip) {
                    if (popoutReferenceGroup) {
                        this.movingLock(() => moveGroupWithoutDestroying({
                            from: item,
                            to: popoutReferenceGroup,
                        }));
                        this.doRemoveGroup(item, {
                            skipPopoutReturn: true,
                            skipPopoutAssociated: true,
                        });
                        this.doRemoveGroup(popoutReferenceGroup, {
                            skipDispose: true,
                        });
                        group = popoutReferenceGroup;
                    }
                    else {
                        this.doRemoveGroup(item, {
                            skipDispose: true,
                            skipPopoutReturn: true,
                            skipPopoutAssociated: false,
                        });
                    }
                }
            }
            function getAnchoredBox() {
                if (options === null || options === void 0 ? void 0 : options.position) {
                    const result = {};
                    if ('left' in options.position) {
                        result.left = Math.max(options.position.left, 0);
                    }
                    else if ('right' in options.position) {
                        result.right = Math.max(options.position.right, 0);
                    }
                    else {
                        result.left = DEFAULT_FLOATING_GROUP_POSITION.left;
                    }
                    if ('top' in options.position) {
                        result.top = Math.max(options.position.top, 0);
                    }
                    else if ('bottom' in options.position) {
                        result.bottom = Math.max(options.position.bottom, 0);
                    }
                    else {
                        result.top = DEFAULT_FLOATING_GROUP_POSITION.top;
                    }
                    if (typeof options.width === 'number') {
                        result.width = Math.max(options.width, 0);
                    }
                    else {
                        result.width = DEFAULT_FLOATING_GROUP_POSITION.width;
                    }
                    if (typeof options.height === 'number') {
                        result.height = Math.max(options.height, 0);
                    }
                    else {
                        result.height = DEFAULT_FLOATING_GROUP_POSITION.height;
                    }
                    return result;
                }
                return {
                    left: typeof (options === null || options === void 0 ? void 0 : options.x) === 'number'
                        ? Math.max(options.x, 0)
                        : DEFAULT_FLOATING_GROUP_POSITION.left,
                    top: typeof (options === null || options === void 0 ? void 0 : options.y) === 'number'
                        ? Math.max(options.y, 0)
                        : DEFAULT_FLOATING_GROUP_POSITION.top,
                    width: typeof (options === null || options === void 0 ? void 0 : options.width) === 'number'
                        ? Math.max(options.width, 0)
                        : DEFAULT_FLOATING_GROUP_POSITION.width,
                    height: typeof (options === null || options === void 0 ? void 0 : options.height) === 'number'
                        ? Math.max(options.height, 0)
                        : DEFAULT_FLOATING_GROUP_POSITION.height,
                };
            }
            const anchoredBox = getAnchoredBox();
            const overlay = new Overlay(Object.assign(Object.assign({ container: this.gridview.element, content: group.element }, anchoredBox), { minimumInViewportWidth: this.options.floatingGroupBounds === 'boundedWithinViewport'
                    ? undefined
                    : (_c = (_b = this.options.floatingGroupBounds) === null || _b === void 0 ? void 0 : _b.minimumWidthWithinViewport) !== null && _c !== void 0 ? _c : DEFAULT_FLOATING_GROUP_OVERFLOW_SIZE, minimumInViewportHeight: this.options.floatingGroupBounds === 'boundedWithinViewport'
                    ? undefined
                    : (_e = (_d = this.options.floatingGroupBounds) === null || _d === void 0 ? void 0 : _d.minimumHeightWithinViewport) !== null && _e !== void 0 ? _e : DEFAULT_FLOATING_GROUP_OVERFLOW_SIZE }));
            const el = group.element.querySelector('.dv-void-container');
            if (!el) {
                throw new Error('failed to find drag handle');
            }
            overlay.setupDrag(el, {
                inDragMode: typeof (options === null || options === void 0 ? void 0 : options.inDragMode) === 'boolean'
                    ? options.inDragMode
                    : false,
            });
            const floatingGroupPanel = new DockviewFloatingGroupPanel(group, overlay);
            const disposable = new CompositeDisposable(group.api.onDidActiveChange((event) => {
                if (event.isActive) {
                    overlay.bringToFront();
                }
            }), watchElementResize(group.element, (entry) => {
                const { width, height } = entry.contentRect;
                group.layout(width, height); // let the group know it's size is changing so it can fire events to the panel
            }));
            floatingGroupPanel.addDisposables(overlay.onDidChange(() => {
                // this is either a resize or a move
                // to inform the panels .layout(...) the group with it's current size
                // don't care about resize since the above watcher handles that
                group.layout(group.width, group.height);
            }), overlay.onDidChangeEnd(() => {
                this._bufferOnDidLayoutChange.fire();
            }), group.onDidChange((event) => {
                overlay.setBounds({
                    height: event === null || event === void 0 ? void 0 : event.height,
                    width: event === null || event === void 0 ? void 0 : event.width,
                });
            }), {
                dispose: () => {
                    disposable.dispose();
                    remove(this._floatingGroups, floatingGroupPanel);
                    group.model.location = { type: 'grid' };
                    this.updateWatermark();
                },
            });
            this._floatingGroups.push(floatingGroupPanel);
            group.model.location = { type: 'floating' };
            if (!(options === null || options === void 0 ? void 0 : options.skipActiveGroup)) {
                this.doSetGroupAndPanelActive(group);
            }
            this.updateWatermark();
        }
        orthogonalize(position, options) {
            this.gridview.normalize();
            switch (position) {
                case 'top':
                case 'bottom':
                    if (this.gridview.orientation === exports.Orientation.HORIZONTAL) {
                        // we need to add to a vertical splitview but the current root is a horizontal splitview.
                        // insert a vertical splitview at the root level and add the existing view as a child
                        this.gridview.insertOrthogonalSplitviewAtRoot();
                    }
                    break;
                case 'left':
                case 'right':
                    if (this.gridview.orientation === exports.Orientation.VERTICAL) {
                        // we need to add to a horizontal splitview but the current root is a vertical splitview.
                        // insert a horiziontal splitview at the root level and add the existing view as a child
                        this.gridview.insertOrthogonalSplitviewAtRoot();
                    }
                    break;
            }
            switch (position) {
                case 'top':
                case 'left':
                case 'center':
                    return this.createGroupAtLocation([0], undefined, options); // insert into first position
                case 'bottom':
                case 'right':
                    return this.createGroupAtLocation([this.gridview.length], undefined, options); // insert into last position
                default:
                    throw new Error(`unsupported position ${position}`);
            }
        }
        updateOptions(options) {
            var _a, _b;
            super.updateOptions(options);
            if ('floatingGroupBounds' in options) {
                for (const group of this._floatingGroups) {
                    switch (options.floatingGroupBounds) {
                        case 'boundedWithinViewport':
                            group.overlay.minimumInViewportHeight = undefined;
                            group.overlay.minimumInViewportWidth = undefined;
                            break;
                        case undefined:
                            group.overlay.minimumInViewportHeight =
                                DEFAULT_FLOATING_GROUP_OVERFLOW_SIZE;
                            group.overlay.minimumInViewportWidth =
                                DEFAULT_FLOATING_GROUP_OVERFLOW_SIZE;
                            break;
                        default:
                            group.overlay.minimumInViewportHeight =
                                (_a = options.floatingGroupBounds) === null || _a === void 0 ? void 0 : _a.minimumHeightWithinViewport;
                            group.overlay.minimumInViewportWidth =
                                (_b = options.floatingGroupBounds) === null || _b === void 0 ? void 0 : _b.minimumWidthWithinViewport;
                    }
                    group.overlay.setBounds();
                }
            }
            this.updateDropTargetModel(options);
            const oldDisableDnd = this.options.disableDnd;
            this._options = Object.assign(Object.assign({}, this.options), options);
            const newDisableDnd = this.options.disableDnd;
            if (oldDisableDnd !== newDisableDnd) {
                this.updateDragAndDropState();
            }
            if ('theme' in options) {
                this.updateTheme();
            }
            this.layout(this.gridview.width, this.gridview.height, true);
        }
        layout(width, height, forceResize) {
            super.layout(width, height, forceResize);
            if (this._floatingGroups) {
                for (const floating of this._floatingGroups) {
                    // ensure floting groups stay within visible boundaries
                    floating.overlay.setBounds();
                }
            }
        }
        updateDragAndDropState() {
            // Update draggable state for all tabs and void containers
            for (const group of this.groups) {
                group.model.updateDragAndDropState();
            }
        }
        focus() {
            var _a;
            (_a = this.activeGroup) === null || _a === void 0 ? void 0 : _a.focus();
        }
        getGroupPanel(id) {
            return this.panels.find((panel) => panel.id === id);
        }
        setActivePanel(panel) {
            panel.group.model.openPanel(panel);
            this.doSetGroupAndPanelActive(panel.group);
        }
        moveToNext(options = {}) {
            var _a;
            if (!options.group) {
                if (!this.activeGroup) {
                    return;
                }
                options.group = this.activeGroup;
            }
            if (options.includePanel && options.group) {
                if (options.group.activePanel !==
                    options.group.panels[options.group.panels.length - 1]) {
                    options.group.model.moveToNext({ suppressRoll: true });
                    return;
                }
            }
            const location = getGridLocation(options.group.element);
            const next = (_a = this.gridview.next(location)) === null || _a === void 0 ? void 0 : _a.view;
            this.doSetGroupAndPanelActive(next);
        }
        moveToPrevious(options = {}) {
            var _a;
            if (!options.group) {
                if (!this.activeGroup) {
                    return;
                }
                options.group = this.activeGroup;
            }
            if (options.includePanel && options.group) {
                if (options.group.activePanel !== options.group.panels[0]) {
                    options.group.model.moveToPrevious({ suppressRoll: true });
                    return;
                }
            }
            const location = getGridLocation(options.group.element);
            const next = (_a = this.gridview.previous(location)) === null || _a === void 0 ? void 0 : _a.view;
            if (next) {
                this.doSetGroupAndPanelActive(next);
            }
        }
        /**
         * Serialize the current state of the layout
         *
         * @returns A JSON respresentation of the layout
         */
        toJSON() {
            var _a;
            const data = this.gridview.serialize();
            const panels = this.panels.reduce((collection, panel) => {
                collection[panel.id] = panel.toJSON();
                return collection;
            }, {});
            const floats = this._floatingGroups.map((group) => {
                return {
                    data: group.group.toJSON(),
                    position: group.overlay.toJSON(),
                };
            });
            const popoutGroups = this._popoutGroups.map((group) => {
                return {
                    data: group.popoutGroup.toJSON(),
                    gridReferenceGroup: group.referenceGroup,
                    position: group.window.dimensions(),
                    url: group.popoutGroup.api.location.type === 'popout'
                        ? group.popoutGroup.api.location.popoutUrl
                        : undefined,
                };
            });
            const result = {
                grid: data,
                panels,
                activeGroup: (_a = this.activeGroup) === null || _a === void 0 ? void 0 : _a.id,
            };
            if (floats.length > 0) {
                result.floatingGroups = floats;
            }
            if (popoutGroups.length > 0) {
                result.popoutGroups = popoutGroups;
            }
            return result;
        }
        fromJSON(data) {
            var _a, _b;
            this.clear();
            if (typeof data !== 'object' || data === null) {
                throw new Error('serialized layout must be a non-null object');
            }
            const { grid, panels, activeGroup } = data;
            if (grid.root.type !== 'branch' || !Array.isArray(grid.root.data)) {
                throw new Error('root must be of type branch');
            }
            try {
                // take note of the existing dimensions
                const width = this.width;
                const height = this.height;
                const createGroupFromSerializedState = (data) => {
                    const { id, locked, hideHeader, views, activeView } = data;
                    if (typeof id !== 'string') {
                        throw new Error('group id must be of type string');
                    }
                    const group = this.createGroup({
                        id,
                        locked: !!locked,
                        hideHeader: !!hideHeader,
                    });
                    this._onDidAddGroup.fire(group);
                    const createdPanels = [];
                    for (const child of views) {
                        /**
                         * Run the deserializer step seperately since this may fail to due corrupted external state.
                         * In running this section first we avoid firing lots of 'add' events in the event of a failure
                         * due to a corruption of input data.
                         */
                        const panel = this._deserializer.fromJSON(panels[child], group);
                        createdPanels.push(panel);
                    }
                    for (let i = 0; i < views.length; i++) {
                        const panel = createdPanels[i];
                        const isActive = typeof activeView === 'string' &&
                            activeView === panel.id;
                        group.model.openPanel(panel, {
                            skipSetActive: !isActive,
                            skipSetGroupActive: true,
                        });
                    }
                    if (!group.activePanel && group.panels.length > 0) {
                        group.model.openPanel(group.panels[group.panels.length - 1], {
                            skipSetGroupActive: true,
                        });
                    }
                    return group;
                };
                this.gridview.deserialize(grid, {
                    fromJSON: (node) => {
                        return createGroupFromSerializedState(node.data);
                    },
                });
                this.layout(width, height, true);
                const serializedFloatingGroups = (_a = data.floatingGroups) !== null && _a !== void 0 ? _a : [];
                for (const serializedFloatingGroup of serializedFloatingGroups) {
                    const { data, position } = serializedFloatingGroup;
                    const group = createGroupFromSerializedState(data);
                    this.addFloatingGroup(group, {
                        position: position,
                        width: position.width,
                        height: position.height,
                        skipRemoveGroup: true,
                        inDragMode: false,
                    });
                }
                const serializedPopoutGroups = (_b = data.popoutGroups) !== null && _b !== void 0 ? _b : [];
                // Create a promise that resolves when all popout groups are created
                const popoutPromises = [];
                // Queue popup group creation with delays to avoid browser blocking
                serializedPopoutGroups.forEach((serializedPopoutGroup, index) => {
                    const { data, position, gridReferenceGroup, url } = serializedPopoutGroup;
                    const group = createGroupFromSerializedState(data);
                    // Add a small delay for each popup after the first to avoid browser popup blocking
                    const popoutPromise = new Promise((resolve) => {
                        setTimeout(() => {
                            this.addPopoutGroup(group, {
                                position: position !== null && position !== void 0 ? position : undefined,
                                overridePopoutGroup: gridReferenceGroup ? group : undefined,
                                referenceGroup: gridReferenceGroup
                                    ? this.getPanel(gridReferenceGroup)
                                    : undefined,
                                popoutUrl: url,
                            });
                            resolve();
                        }, index * DESERIALIZATION_POPOUT_DELAY_MS); // 100ms delay between each popup
                    });
                    popoutPromises.push(popoutPromise);
                });
                // Store the promise for tests to wait on
                this._popoutRestorationPromise = Promise.all(popoutPromises).then(() => void 0);
                for (const floatingGroup of this._floatingGroups) {
                    floatingGroup.overlay.setBounds();
                }
                if (typeof activeGroup === 'string') {
                    const panel = this.getPanel(activeGroup);
                    if (panel) {
                        this.doSetGroupAndPanelActive(panel);
                    }
                }
            }
            catch (err) {
                console.error('dockview: failed to deserialize layout. Reverting changes', err);
                /**
                 * Takes all the successfully created groups and remove all of their panels.
                 */
                for (const group of this.groups) {
                    for (const panel of group.panels) {
                        this.removePanel(panel, {
                            removeEmptyGroup: false,
                            skipDispose: false,
                        });
                    }
                }
                /**
                 * To remove a group we cannot call this.removeGroup(...) since this makes assumptions about
                 * the underlying HTMLElement existing in the Gridview.
                 */
                for (const group of this.groups) {
                    group.dispose();
                    this._groups.delete(group.id);
                    this._onDidRemoveGroup.fire(group);
                }
                // iterate over a reassigned array since original array will be modified
                for (const floatingGroup of [...this._floatingGroups]) {
                    floatingGroup.dispose();
                }
                // fires clean-up events and clears the underlying HTML gridview.
                this.clear();
                /**
                 * even though we have cleaned-up we still want to inform the caller of their error
                 * and we'll do this through re-throwing the original error since afterall you would
                 * expect trying to load a corrupted layout to result in an error and not silently fail...
                 */
                throw err;
            }
            this.updateWatermark();
            // Force position updates for always visible panels after DOM layout is complete
            requestAnimationFrame(() => {
                this.overlayRenderContainer.updateAllPositions();
            });
            this._onDidLayoutFromJSON.fire();
        }
        clear() {
            const groups = Array.from(this._groups.values()).map((_) => _.value);
            const hasActiveGroup = !!this.activeGroup;
            for (const group of groups) {
                // remove the group will automatically remove the panels
                this.removeGroup(group, { skipActive: true });
            }
            if (hasActiveGroup) {
                this.doSetGroupAndPanelActive(undefined);
            }
            this.gridview.clear();
        }
        closeAllGroups() {
            for (const entry of this._groups.entries()) {
                const [_, group] = entry;
                group.value.model.closeAllPanels();
            }
        }
        addPanel(options) {
            var _a, _b;
            if (this.panels.find((_) => _.id === options.id)) {
                throw new Error(`panel with id ${options.id} already exists`);
            }
            let referenceGroup;
            if (options.position && options.floating) {
                throw new Error('you can only provide one of: position, floating as arguments to .addPanel(...)');
            }
            const initial = {
                width: options.initialWidth,
                height: options.initialHeight,
            };
            let index;
            if (options.position) {
                if (isPanelOptionsWithPanel(options.position)) {
                    const referencePanel = typeof options.position.referencePanel === 'string'
                        ? this.getGroupPanel(options.position.referencePanel)
                        : options.position.referencePanel;
                    index = options.position.index;
                    if (!referencePanel) {
                        throw new Error(`referencePanel '${options.position.referencePanel}' does not exist`);
                    }
                    referenceGroup = this.findGroup(referencePanel);
                }
                else if (isPanelOptionsWithGroup(options.position)) {
                    referenceGroup =
                        typeof options.position.referenceGroup === 'string'
                            ? (_a = this._groups.get(options.position.referenceGroup)) === null || _a === void 0 ? void 0 : _a.value
                            : options.position.referenceGroup;
                    index = options.position.index;
                    if (!referenceGroup) {
                        throw new Error(`referenceGroup '${options.position.referenceGroup}' does not exist`);
                    }
                }
                else {
                    const group = this.orthogonalize(directionToPosition(options.position.direction));
                    const panel = this.createPanel(options, group);
                    group.model.openPanel(panel, {
                        skipSetActive: options.inactive,
                        skipSetGroupActive: options.inactive,
                        index,
                    });
                    if (!options.inactive) {
                        this.doSetGroupAndPanelActive(group);
                    }
                    group.api.setSize({
                        height: initial === null || initial === void 0 ? void 0 : initial.height,
                        width: initial === null || initial === void 0 ? void 0 : initial.width,
                    });
                    return panel;
                }
            }
            else {
                referenceGroup = this.activeGroup;
            }
            let panel;
            if (referenceGroup) {
                const target = toTarget(((_b = options.position) === null || _b === void 0 ? void 0 : _b.direction) || 'within');
                if (options.floating) {
                    const group = this.createGroup();
                    this._onDidAddGroup.fire(group);
                    const floatingGroupOptions = typeof options.floating === 'object' &&
                        options.floating !== null
                        ? options.floating
                        : {};
                    this.addFloatingGroup(group, Object.assign(Object.assign({}, floatingGroupOptions), { inDragMode: false, skipRemoveGroup: true, skipActiveGroup: true }));
                    panel = this.createPanel(options, group);
                    group.model.openPanel(panel, {
                        skipSetActive: options.inactive,
                        skipSetGroupActive: options.inactive,
                        index,
                    });
                }
                else if (referenceGroup.api.location.type === 'floating' ||
                    target === 'center') {
                    panel = this.createPanel(options, referenceGroup);
                    referenceGroup.model.openPanel(panel, {
                        skipSetActive: options.inactive,
                        skipSetGroupActive: options.inactive,
                        index,
                    });
                    referenceGroup.api.setSize({
                        width: initial === null || initial === void 0 ? void 0 : initial.width,
                        height: initial === null || initial === void 0 ? void 0 : initial.height,
                    });
                    if (!options.inactive) {
                        this.doSetGroupAndPanelActive(referenceGroup);
                    }
                }
                else {
                    const location = getGridLocation(referenceGroup.element);
                    const relativeLocation = getRelativeLocation(this.gridview.orientation, location, target);
                    const group = this.createGroupAtLocation(relativeLocation, this.orientationAtLocation(relativeLocation) ===
                        exports.Orientation.VERTICAL
                        ? initial === null || initial === void 0 ? void 0 : initial.height
                        : initial === null || initial === void 0 ? void 0 : initial.width);
                    panel = this.createPanel(options, group);
                    group.model.openPanel(panel, {
                        skipSetActive: options.inactive,
                        skipSetGroupActive: options.inactive,
                        index,
                    });
                    if (!options.inactive) {
                        this.doSetGroupAndPanelActive(group);
                    }
                }
            }
            else if (options.floating) {
                const group = this.createGroup();
                this._onDidAddGroup.fire(group);
                const coordinates = typeof options.floating === 'object' &&
                    options.floating !== null
                    ? options.floating
                    : {};
                this.addFloatingGroup(group, Object.assign(Object.assign({}, coordinates), { inDragMode: false, skipRemoveGroup: true, skipActiveGroup: true }));
                panel = this.createPanel(options, group);
                group.model.openPanel(panel, {
                    skipSetActive: options.inactive,
                    skipSetGroupActive: options.inactive,
                    index,
                });
            }
            else {
                const group = this.createGroupAtLocation([0], this.gridview.orientation === exports.Orientation.VERTICAL
                    ? initial === null || initial === void 0 ? void 0 : initial.height
                    : initial === null || initial === void 0 ? void 0 : initial.width);
                panel = this.createPanel(options, group);
                group.model.openPanel(panel, {
                    skipSetActive: options.inactive,
                    skipSetGroupActive: options.inactive,
                    index,
                });
                if (!options.inactive) {
                    this.doSetGroupAndPanelActive(group);
                }
            }
            return panel;
        }
        removePanel(panel, options = {
            removeEmptyGroup: true,
        }) {
            const group = panel.group;
            if (!group) {
                throw new Error(`cannot remove panel ${panel.id}. it's missing a group.`);
            }
            group.model.removePanel(panel, {
                skipSetActiveGroup: options.skipSetActiveGroup,
            });
            if (!options.skipDispose) {
                panel.group.model.renderContainer.detatch(panel);
                panel.dispose();
            }
            if (group.size === 0 && options.removeEmptyGroup) {
                this.removeGroup(group, { skipActive: options.skipSetActiveGroup });
            }
        }
        createWatermarkComponent() {
            if (this.options.createWatermarkComponent) {
                return this.options.createWatermarkComponent();
            }
            return new Watermark();
        }
        updateWatermark() {
            var _a, _b;
            if (this.groups.filter((x) => x.api.location.type === 'grid' && x.api.isVisible).length === 0) {
                if (!this._watermark) {
                    this._watermark = this.createWatermarkComponent();
                    this._watermark.init({
                        containerApi: new DockviewApi(this),
                    });
                    const watermarkContainer = document.createElement('div');
                    watermarkContainer.className = 'dv-watermark-container';
                    addTestId(watermarkContainer, 'watermark-component');
                    watermarkContainer.appendChild(this._watermark.element);
                    this.gridview.element.appendChild(watermarkContainer);
                }
            }
            else if (this._watermark) {
                this._watermark.element.parentElement.remove();
                (_b = (_a = this._watermark).dispose) === null || _b === void 0 ? void 0 : _b.call(_a);
                this._watermark = null;
            }
        }
        addGroup(options) {
            var _a;
            if (options) {
                let referenceGroup;
                if (isGroupOptionsWithPanel(options)) {
                    const referencePanel = typeof options.referencePanel === 'string'
                        ? this.panels.find((panel) => panel.id === options.referencePanel)
                        : options.referencePanel;
                    if (!referencePanel) {
                        throw new Error(`reference panel ${options.referencePanel} does not exist`);
                    }
                    referenceGroup = this.findGroup(referencePanel);
                    if (!referenceGroup) {
                        throw new Error(`reference group for reference panel ${options.referencePanel} does not exist`);
                    }
                }
                else if (isGroupOptionsWithGroup(options)) {
                    referenceGroup =
                        typeof options.referenceGroup === 'string'
                            ? (_a = this._groups.get(options.referenceGroup)) === null || _a === void 0 ? void 0 : _a.value
                            : options.referenceGroup;
                    if (!referenceGroup) {
                        throw new Error(`reference group ${options.referenceGroup} does not exist`);
                    }
                }
                else {
                    const group = this.orthogonalize(directionToPosition(options.direction), options);
                    if (!options.skipSetActive) {
                        this.doSetGroupAndPanelActive(group);
                    }
                    return group;
                }
                const target = toTarget(options.direction || 'within');
                const location = getGridLocation(referenceGroup.element);
                const relativeLocation = getRelativeLocation(this.gridview.orientation, location, target);
                const group = this.createGroup(options);
                const size = this.getLocationOrientation(relativeLocation) ===
                    exports.Orientation.VERTICAL
                    ? options.initialHeight
                    : options.initialWidth;
                this.doAddGroup(group, relativeLocation, size);
                if (!options.skipSetActive) {
                    this.doSetGroupAndPanelActive(group);
                }
                return group;
            }
            else {
                const group = this.createGroup(options);
                this.doAddGroup(group);
                this.doSetGroupAndPanelActive(group);
                return group;
            }
        }
        getLocationOrientation(location) {
            return location.length % 2 == 0 &&
                this.gridview.orientation === exports.Orientation.HORIZONTAL
                ? exports.Orientation.HORIZONTAL
                : exports.Orientation.VERTICAL;
        }
        removeGroup(group, options) {
            this.doRemoveGroup(group, options);
        }
        doRemoveGroup(group, options) {
            var _a;
            const panels = [...group.panels]; // reassign since group panels will mutate
            if (!(options === null || options === void 0 ? void 0 : options.skipDispose)) {
                for (const panel of panels) {
                    this.removePanel(panel, {
                        removeEmptyGroup: false,
                        skipDispose: (_a = options === null || options === void 0 ? void 0 : options.skipDispose) !== null && _a !== void 0 ? _a : false,
                    });
                }
            }
            const activePanel = this.activePanel;
            if (group.api.location.type === 'floating') {
                const floatingGroup = this._floatingGroups.find((_) => _.group === group);
                if (floatingGroup) {
                    if (!(options === null || options === void 0 ? void 0 : options.skipDispose)) {
                        floatingGroup.group.dispose();
                        this._groups.delete(group.id);
                        this._onDidRemoveGroup.fire(group);
                    }
                    remove(this._floatingGroups, floatingGroup);
                    floatingGroup.dispose();
                    if (!(options === null || options === void 0 ? void 0 : options.skipActive) && this._activeGroup === group) {
                        const groups = Array.from(this._groups.values());
                        this.doSetGroupAndPanelActive(groups.length > 0 ? groups[0].value : undefined);
                    }
                    return floatingGroup.group;
                }
                throw new Error('failed to find floating group');
            }
            if (group.api.location.type === 'popout') {
                const selectedGroup = this._popoutGroups.find((_) => _.popoutGroup === group);
                if (selectedGroup) {
                    if (!(options === null || options === void 0 ? void 0 : options.skipDispose)) {
                        if (!(options === null || options === void 0 ? void 0 : options.skipPopoutAssociated)) {
                            const refGroup = selectedGroup.referenceGroup
                                ? this.getPanel(selectedGroup.referenceGroup)
                                : undefined;
                            if (refGroup && refGroup.panels.length === 0) {
                                this.removeGroup(refGroup);
                            }
                        }
                        selectedGroup.popoutGroup.dispose();
                        this._groups.delete(group.id);
                        this._onDidRemoveGroup.fire(group);
                    }
                    remove(this._popoutGroups, selectedGroup);
                    const removedGroup = selectedGroup.disposable.dispose();
                    if (!(options === null || options === void 0 ? void 0 : options.skipPopoutReturn) && removedGroup) {
                        this.doAddGroup(removedGroup, [0]);
                        this.doSetGroupAndPanelActive(removedGroup);
                    }
                    if (!(options === null || options === void 0 ? void 0 : options.skipActive) && this._activeGroup === group) {
                        const groups = Array.from(this._groups.values());
                        this.doSetGroupAndPanelActive(groups.length > 0 ? groups[0].value : undefined);
                    }
                    this.updateWatermark();
                    return selectedGroup.popoutGroup;
                }
                throw new Error('failed to find popout group');
            }
            const re = super.doRemoveGroup(group, options);
            if (!(options === null || options === void 0 ? void 0 : options.skipActive)) {
                if (this.activePanel !== activePanel) {
                    this._onDidActivePanelChange.fire(this.activePanel);
                }
            }
            return re;
        }
        movingLock(func) {
            const isMoving = this._moving;
            try {
                this._moving = true;
                return func();
            }
            finally {
                this._moving = isMoving;
            }
        }
        moveGroupOrPanel(options) {
            var _a;
            const destinationGroup = options.to.group;
            const sourceGroupId = options.from.groupId;
            const sourceItemId = options.from.panelId;
            const destinationTarget = options.to.position;
            const destinationIndex = options.to.index;
            const sourceGroup = sourceGroupId
                ? (_a = this._groups.get(sourceGroupId)) === null || _a === void 0 ? void 0 : _a.value
                : undefined;
            if (!sourceGroup) {
                throw new Error(`Failed to find group id ${sourceGroupId}`);
            }
            if (sourceItemId === undefined) {
                /**
                 * Moving an entire group into another group
                 */
                this.moveGroup({
                    from: { group: sourceGroup },
                    to: {
                        group: destinationGroup,
                        position: destinationTarget,
                    },
                    skipSetActive: options.skipSetActive,
                });
                return;
            }
            if (!destinationTarget || destinationTarget === 'center') {
                /**
                 * Dropping a panel within another group
                 */
                const removedPanel = this.movingLock(() => sourceGroup.model.removePanel(sourceItemId, {
                    skipSetActive: false,
                    skipSetActiveGroup: true,
                }));
                if (!removedPanel) {
                    throw new Error(`No panel with id ${sourceItemId}`);
                }
                if (sourceGroup.model.size === 0) {
                    // remove the group and do not set a new group as active
                    this.doRemoveGroup(sourceGroup, { skipActive: true });
                }
                // Check if destination group is empty - if so, force render the component
                const isDestinationGroupEmpty = destinationGroup.model.size === 0;
                this.movingLock(() => {
                    var _a;
                    return destinationGroup.model.openPanel(removedPanel, {
                        index: destinationIndex,
                        skipSetActive: ((_a = options.skipSetActive) !== null && _a !== void 0 ? _a : false) && !isDestinationGroupEmpty,
                        skipSetGroupActive: true,
                    });
                });
                if (!options.skipSetActive) {
                    this.doSetGroupAndPanelActive(destinationGroup);
                }
                this._onDidMovePanel.fire({
                    panel: removedPanel,
                    from: sourceGroup,
                });
            }
            else {
                /**
                 * Dropping a panel to the extremities of a group which will place that panel
                 * into an adjacent group
                 */
                const referenceLocation = getGridLocation(destinationGroup.element);
                const targetLocation = getRelativeLocation(this.gridview.orientation, referenceLocation, destinationTarget);
                if (sourceGroup.size < 2) {
                    /**
                     * If we are moving from a group which only has one panel left we will consider
                     * moving the group itself rather than moving the panel into a newly created group
                     */
                    const [targetParentLocation, to] = tail(targetLocation);
                    if (sourceGroup.api.location.type === 'grid') {
                        const sourceLocation = getGridLocation(sourceGroup.element);
                        const [sourceParentLocation, from] = tail(sourceLocation);
                        if (sequenceEquals(sourceParentLocation, targetParentLocation)) {
                            // special case when 'swapping' two views within same grid location
                            // if a group has one tab - we are essentially moving the 'group'
                            // which is equivalent to swapping two views in this case
                            this.gridview.moveView(sourceParentLocation, from, to);
                            this._onDidMovePanel.fire({
                                panel: this.getGroupPanel(sourceItemId),
                                from: sourceGroup,
                            });
                            return;
                        }
                    }
                    if (sourceGroup.api.location.type === 'popout') {
                        /**
                         * the source group is a popout group with a single panel
                         *
                         * 1. remove the panel from the group without triggering any events
                         * 2. remove the popout group
                         * 3. create a new group at the requested location and add that panel
                         */
                        const popoutGroup = this._popoutGroups.find((group) => group.popoutGroup === sourceGroup);
                        const removedPanel = this.movingLock(() => popoutGroup.popoutGroup.model.removePanel(popoutGroup.popoutGroup.panels[0], {
                            skipSetActive: true,
                            skipSetActiveGroup: true,
                        }));
                        this.doRemoveGroup(sourceGroup, { skipActive: true });
                        const newGroup = this.createGroupAtLocation(targetLocation);
                        this.movingLock(() => newGroup.model.openPanel(removedPanel));
                        this.doSetGroupAndPanelActive(newGroup);
                        this._onDidMovePanel.fire({
                            panel: this.getGroupPanel(sourceItemId),
                            from: sourceGroup,
                        });
                        return;
                    }
                    // source group will become empty so delete the group
                    const targetGroup = this.movingLock(() => this.doRemoveGroup(sourceGroup, {
                        skipActive: true,
                        skipDispose: true,
                    }));
                    // after deleting the group we need to re-evaulate the ref location
                    const updatedReferenceLocation = getGridLocation(destinationGroup.element);
                    const location = getRelativeLocation(this.gridview.orientation, updatedReferenceLocation, destinationTarget);
                    this.movingLock(() => this.doAddGroup(targetGroup, location));
                    this.doSetGroupAndPanelActive(targetGroup);
                    this._onDidMovePanel.fire({
                        panel: this.getGroupPanel(sourceItemId),
                        from: sourceGroup,
                    });
                }
                else {
                    /**
                     * The group we are removing from has many panels, we need to remove the panels we are moving,
                     * create a new group, add the panels to that new group and add the new group in an appropiate position
                     */
                    const removedPanel = this.movingLock(() => sourceGroup.model.removePanel(sourceItemId, {
                        skipSetActive: false,
                        skipSetActiveGroup: true,
                    }));
                    if (!removedPanel) {
                        throw new Error(`No panel with id ${sourceItemId}`);
                    }
                    const dropLocation = getRelativeLocation(this.gridview.orientation, referenceLocation, destinationTarget);
                    const group = this.createGroupAtLocation(dropLocation);
                    this.movingLock(() => group.model.openPanel(removedPanel, {
                        skipSetGroupActive: true,
                    }));
                    this.doSetGroupAndPanelActive(group);
                    this._onDidMovePanel.fire({
                        panel: removedPanel,
                        from: sourceGroup,
                    });
                }
            }
        }
        moveGroup(options) {
            const from = options.from.group;
            const to = options.to.group;
            const target = options.to.position;
            if (target === 'center') {
                const activePanel = from.activePanel;
                const panels = this.movingLock(() => [...from.panels].map((p) => from.model.removePanel(p.id, {
                    skipSetActive: true,
                })));
                if ((from === null || from === void 0 ? void 0 : from.model.size) === 0) {
                    this.doRemoveGroup(from, { skipActive: true });
                }
                this.movingLock(() => {
                    for (const panel of panels) {
                        to.model.openPanel(panel, {
                            skipSetActive: panel !== activePanel,
                            skipSetGroupActive: true,
                        });
                    }
                });
                // Ensure group becomes active after move
                if (options.skipSetActive !== true) {
                    // For center moves (merges), we need to ensure the target group is active
                    // unless explicitly told not to (skipSetActive: true)
                    this.doSetGroupAndPanelActive(to);
                }
                else if (!this.activePanel) {
                    // Even with skipSetActive: true, ensure there's an active panel if none exists
                    // This maintains basic functionality while respecting skipSetActive
                    this.doSetGroupAndPanelActive(to);
                }
            }
            else {
                switch (from.api.location.type) {
                    case 'grid':
                        this.gridview.removeView(getGridLocation(from.element));
                        break;
                    case 'floating': {
                        const selectedFloatingGroup = this._floatingGroups.find((x) => x.group === from);
                        if (!selectedFloatingGroup) {
                            throw new Error('failed to find floating group');
                        }
                        selectedFloatingGroup.dispose();
                        break;
                    }
                    case 'popout': {
                        const selectedPopoutGroup = this._popoutGroups.find((x) => x.popoutGroup === from);
                        if (!selectedPopoutGroup) {
                            throw new Error('failed to find popout group');
                        }
                        // Remove from popout groups list to prevent automatic restoration
                        const index = this._popoutGroups.indexOf(selectedPopoutGroup);
                        if (index >= 0) {
                            this._popoutGroups.splice(index, 1);
                        }
                        // Clean up the reference group (ghost) if it exists and is hidden
                        if (selectedPopoutGroup.referenceGroup) {
                            const referenceGroup = this.getPanel(selectedPopoutGroup.referenceGroup);
                            if (referenceGroup && !referenceGroup.api.isVisible) {
                                this.doRemoveGroup(referenceGroup, {
                                    skipActive: true,
                                });
                            }
                        }
                        // Manually dispose the window without triggering restoration
                        selectedPopoutGroup.window.dispose();
                        // Update group's location and containers for target
                        if (to.api.location.type === 'grid') {
                            from.model.renderContainer =
                                this.overlayRenderContainer;
                            from.model.dropTargetContainer =
                                this.rootDropTargetContainer;
                            from.model.location = { type: 'grid' };
                        }
                        else if (to.api.location.type === 'floating') {
                            from.model.renderContainer =
                                this.overlayRenderContainer;
                            from.model.dropTargetContainer =
                                this.rootDropTargetContainer;
                            from.model.location = { type: 'floating' };
                        }
                        break;
                    }
                }
                // For moves to grid locations
                if (to.api.location.type === 'grid') {
                    const referenceLocation = getGridLocation(to.element);
                    const dropLocation = getRelativeLocation(this.gridview.orientation, referenceLocation, target);
                    // Add to grid for all moves targeting grid location
                    let size;
                    switch (this.gridview.orientation) {
                        case exports.Orientation.VERTICAL:
                            size =
                                referenceLocation.length % 2 == 0
                                    ? from.api.width
                                    : from.api.height;
                            break;
                        case exports.Orientation.HORIZONTAL:
                            size =
                                referenceLocation.length % 2 == 0
                                    ? from.api.height
                                    : from.api.width;
                            break;
                    }
                    this.gridview.addView(from, size, dropLocation);
                }
                else if (to.api.location.type === 'floating') {
                    // For moves to floating locations, add as floating group
                    // Get the position/size from the target floating group
                    const targetFloatingGroup = this._floatingGroups.find((x) => x.group === to);
                    if (targetFloatingGroup) {
                        const box = targetFloatingGroup.overlay.toJSON();
                        // Calculate position based on available properties
                        let left, top;
                        if ('left' in box) {
                            left = box.left + 50;
                        }
                        else if ('right' in box) {
                            left = Math.max(0, box.right - box.width - 50);
                        }
                        else {
                            left = 50; // Default fallback
                        }
                        if ('top' in box) {
                            top = box.top + 50;
                        }
                        else if ('bottom' in box) {
                            top = Math.max(0, box.bottom - box.height - 50);
                        }
                        else {
                            top = 50; // Default fallback
                        }
                        this.addFloatingGroup(from, {
                            height: box.height,
                            width: box.width,
                            position: {
                                left,
                                top,
                            },
                        });
                    }
                }
            }
            from.panels.forEach((panel) => {
                this._onDidMovePanel.fire({ panel, from });
            });
            // Ensure group becomes active after move
            if (options.skipSetActive === false) {
                // Only activate when explicitly requested (skipSetActive: false)
                // Use 'to' group for non-center moves since 'from' may have been destroyed
                const targetGroup = to !== null && to !== void 0 ? to : from;
                this.doSetGroupAndPanelActive(targetGroup);
            }
        }
        doSetGroupActive(group) {
            super.doSetGroupActive(group);
            const activePanel = this.activePanel;
            if (!this._moving &&
                activePanel !== this._onDidActivePanelChange.value) {
                this._onDidActivePanelChange.fire(activePanel);
            }
        }
        doSetGroupAndPanelActive(group) {
            super.doSetGroupActive(group);
            const activePanel = this.activePanel;
            if (group &&
                this.hasMaximizedGroup() &&
                !this.isMaximizedGroup(group)) {
                this.exitMaximizedGroup();
            }
            if (!this._moving &&
                activePanel !== this._onDidActivePanelChange.value) {
                this._onDidActivePanelChange.fire(activePanel);
            }
        }
        getNextGroupId() {
            let id = this.nextGroupId.next();
            while (this._groups.has(id)) {
                id = this.nextGroupId.next();
            }
            return id;
        }
        createGroup(options) {
            if (!options) {
                options = {};
            }
            let id = options === null || options === void 0 ? void 0 : options.id;
            if (id && this._groups.has(options.id)) {
                console.warn(`dockview: Duplicate group id ${options === null || options === void 0 ? void 0 : options.id}. reassigning group id to avoid errors`);
                id = undefined;
            }
            if (!id) {
                id = this.nextGroupId.next();
                while (this._groups.has(id)) {
                    id = this.nextGroupId.next();
                }
            }
            const view = new DockviewGroupPanel(this, id, options);
            view.init({ params: {}, accessor: this });
            if (!this._groups.has(view.id)) {
                const disposable = new CompositeDisposable(view.model.onTabDragStart((event) => {
                    this._onWillDragPanel.fire(event);
                }), view.model.onGroupDragStart((event) => {
                    this._onWillDragGroup.fire(event);
                }), view.model.onMove((event) => {
                    const { groupId, itemId, target, index } = event;
                    this.moveGroupOrPanel({
                        from: { groupId: groupId, panelId: itemId },
                        to: {
                            group: view,
                            position: target,
                            index,
                        },
                    });
                }), view.model.onDidDrop((event) => {
                    this._onDidDrop.fire(event);
                }), view.model.onWillDrop((event) => {
                    this._onWillDrop.fire(event);
                }), view.model.onWillShowOverlay((event) => {
                    if (this.options.disableDnd) {
                        event.preventDefault();
                        return;
                    }
                    this._onWillShowOverlay.fire(event);
                }), view.model.onUnhandledDragOverEvent((event) => {
                    this._onUnhandledDragOverEvent.fire(event);
                }), view.model.onDidAddPanel((event) => {
                    if (this._moving) {
                        return;
                    }
                    this._onDidAddPanel.fire(event.panel);
                }), view.model.onDidRemovePanel((event) => {
                    if (this._moving) {
                        return;
                    }
                    this._onDidRemovePanel.fire(event.panel);
                }), view.model.onDidActivePanelChange((event) => {
                    if (this._moving) {
                        return;
                    }
                    if (event.panel !== this.activePanel) {
                        return;
                    }
                    if (this._onDidActivePanelChange.value !== event.panel) {
                        this._onDidActivePanelChange.fire(event.panel);
                    }
                }), exports.DockviewEvent.any(view.model.onDidPanelTitleChange, view.model.onDidPanelParametersChange)(() => {
                    this._bufferOnDidLayoutChange.fire();
                }));
                this._groups.set(view.id, { value: view, disposable });
            }
            // TODO: must be called after the above listeners have been setup, not an ideal pattern
            view.initialize();
            return view;
        }
        createPanel(options, group) {
            var _a, _b, _c;
            const contentComponent = options.component;
            const tabComponent = (_a = options.tabComponent) !== null && _a !== void 0 ? _a : this.options.defaultTabComponent;
            const view = new DockviewPanelModel(this, options.id, contentComponent, tabComponent);
            const panel = new DockviewPanel(options.id, contentComponent, tabComponent, this, this._api, group, view, {
                renderer: options.renderer,
                minimumWidth: options.minimumWidth,
                minimumHeight: options.minimumHeight,
                maximumWidth: options.maximumWidth,
                maximumHeight: options.maximumHeight,
            });
            panel.init({
                title: (_b = options.title) !== null && _b !== void 0 ? _b : options.id,
                params: (_c = options === null || options === void 0 ? void 0 : options.params) !== null && _c !== void 0 ? _c : {},
            });
            return panel;
        }
        createGroupAtLocation(location, size, options) {
            const group = this.createGroup(options);
            this.doAddGroup(group, location, size);
            return group;
        }
        findGroup(panel) {
            var _a;
            return (_a = Array.from(this._groups.values()).find((group) => group.value.model.containsPanel(panel))) === null || _a === void 0 ? void 0 : _a.value;
        }
        orientationAtLocation(location) {
            const rootOrientation = this.gridview.orientation;
            return location.length % 2 == 1
                ? rootOrientation
                : orthogonal(rootOrientation);
        }
        updateDropTargetModel(options) {
            if ('dndEdges' in options) {
                this._rootDropTarget.disabled =
                    typeof options.dndEdges === 'boolean' &&
                        options.dndEdges === false;
                if (typeof options.dndEdges === 'object' &&
                    options.dndEdges !== null) {
                    this._rootDropTarget.setOverlayModel(options.dndEdges);
                }
                else {
                    this._rootDropTarget.setOverlayModel(DEFAULT_ROOT_OVERLAY_MODEL);
                }
            }
            if ('rootOverlayModel' in options) {
                this.updateDropTargetModel({ dndEdges: options.dndEdges });
            }
        }
        updateTheme() {
            var _a, _b;
            const theme = (_a = this._options.theme) !== null && _a !== void 0 ? _a : themeAbyss;
            this._themeClassnames.setClassNames(theme.className);
            this.gridview.margin = (_b = theme.gap) !== null && _b !== void 0 ? _b : 0;
            switch (theme.dndOverlayMounting) {
                case 'absolute':
                    this.rootDropTargetContainer.disabled = false;
                    break;
                case 'relative':
                default:
                    this.rootDropTargetContainer.disabled = true;
                    break;
            }
        }
    }

    class GridviewComponent extends BaseGrid {
        get orientation() {
            return this.gridview.orientation;
        }
        set orientation(value) {
            this.gridview.orientation = value;
        }
        get options() {
            return this._options;
        }
        get deserializer() {
            return this._deserializer;
        }
        set deserializer(value) {
            this._deserializer = value;
        }
        constructor(container, options) {
            var _a;
            super(container, {
                proportionalLayout: (_a = options.proportionalLayout) !== null && _a !== void 0 ? _a : true,
                orientation: options.orientation,
                styles: options.hideBorders
                    ? { separatorBorder: 'transparent' }
                    : undefined,
                disableAutoResizing: options.disableAutoResizing,
                className: options.className,
            });
            this._onDidLayoutfromJSON = new Emitter();
            this.onDidLayoutFromJSON = this._onDidLayoutfromJSON.event;
            this._onDidRemoveGroup = new Emitter();
            this.onDidRemoveGroup = this._onDidRemoveGroup.event;
            this._onDidAddGroup = new Emitter();
            this.onDidAddGroup = this._onDidAddGroup.event;
            this._onDidActiveGroupChange = new Emitter();
            this.onDidActiveGroupChange = this._onDidActiveGroupChange.event;
            this._options = options;
            this.addDisposables(this._onDidAddGroup, this._onDidRemoveGroup, this._onDidActiveGroupChange, this.onDidAdd((event) => {
                this._onDidAddGroup.fire(event);
            }), this.onDidRemove((event) => {
                this._onDidRemoveGroup.fire(event);
            }), this.onDidActiveChange((event) => {
                this._onDidActiveGroupChange.fire(event);
            }));
        }
        updateOptions(options) {
            super.updateOptions(options);
            const hasOrientationChanged = typeof options.orientation === 'string' &&
                this.gridview.orientation !== options.orientation;
            this._options = Object.assign(Object.assign({}, this.options), options);
            if (hasOrientationChanged) {
                this.gridview.orientation = options.orientation;
            }
            this.layout(this.gridview.width, this.gridview.height, true);
        }
        removePanel(panel) {
            this.removeGroup(panel);
        }
        /**
         * Serialize the current state of the layout
         *
         * @returns A JSON respresentation of the layout
         */
        toJSON() {
            var _a;
            const data = this.gridview.serialize();
            return {
                grid: data,
                activePanel: (_a = this.activeGroup) === null || _a === void 0 ? void 0 : _a.id,
            };
        }
        setVisible(panel, visible) {
            this.gridview.setViewVisible(getGridLocation(panel.element), visible);
        }
        setActive(panel) {
            this._groups.forEach((value, _key) => {
                value.value.setActive(panel === value.value);
            });
        }
        focus() {
            var _a;
            (_a = this.activeGroup) === null || _a === void 0 ? void 0 : _a.focus();
        }
        fromJSON(serializedGridview) {
            this.clear();
            const { grid, activePanel } = serializedGridview;
            try {
                const queue = [];
                // take note of the existing dimensions
                const width = this.width;
                const height = this.height;
                this.gridview.deserialize(grid, {
                    fromJSON: (node) => {
                        const { data } = node;
                        const view = this.options.createComponent({
                            id: data.id,
                            name: data.component,
                        });
                        queue.push(() => view.init({
                            params: data.params,
                            minimumWidth: data.minimumWidth,
                            maximumWidth: data.maximumWidth,
                            minimumHeight: data.minimumHeight,
                            maximumHeight: data.maximumHeight,
                            priority: data.priority,
                            snap: !!data.snap,
                            accessor: this,
                            isVisible: node.visible,
                        }));
                        this._onDidAddGroup.fire(view);
                        this.registerPanel(view);
                        return view;
                    },
                });
                this.layout(width, height, true);
                queue.forEach((f) => f());
                if (typeof activePanel === 'string') {
                    const panel = this.getPanel(activePanel);
                    if (panel) {
                        this.doSetGroupActive(panel);
                    }
                }
            }
            catch (err) {
                /**
                 * To remove a group we cannot call this.removeGroup(...) since this makes assumptions about
                 * the underlying HTMLElement existing in the Gridview.
                 */
                for (const group of this.groups) {
                    group.dispose();
                    this._groups.delete(group.id);
                    this._onDidRemoveGroup.fire(group);
                }
                // fires clean-up events and clears the underlying HTML gridview.
                this.clear();
                /**
                 * even though we have cleaned-up we still want to inform the caller of their error
                 * and we'll do this through re-throwing the original error since afterall you would
                 * expect trying to load a corrupted layout to result in an error and not silently fail...
                 */
                throw err;
            }
            this._onDidLayoutfromJSON.fire();
        }
        clear() {
            const hasActiveGroup = this.activeGroup;
            const groups = Array.from(this._groups.values()); // reassign since group panels will mutate
            for (const group of groups) {
                group.disposable.dispose();
                this.doRemoveGroup(group.value, { skipActive: true });
            }
            if (hasActiveGroup) {
                this.doSetGroupActive(undefined);
            }
            this.gridview.clear();
        }
        movePanel(panel, options) {
            var _a;
            let relativeLocation;
            const removedPanel = this.gridview.remove(panel);
            const referenceGroup = (_a = this._groups.get(options.reference)) === null || _a === void 0 ? void 0 : _a.value;
            if (!referenceGroup) {
                throw new Error(`reference group ${options.reference} does not exist`);
            }
            const target = toTarget(options.direction);
            if (target === 'center') {
                throw new Error(`${target} not supported as an option`);
            }
            else {
                const location = getGridLocation(referenceGroup.element);
                relativeLocation = getRelativeLocation(this.gridview.orientation, location, target);
            }
            this.doAddGroup(removedPanel, relativeLocation, options.size);
        }
        addPanel(options) {
            var _a, _b, _c, _d;
            let relativeLocation = (_a = options.location) !== null && _a !== void 0 ? _a : [0];
            if ((_b = options.position) === null || _b === void 0 ? void 0 : _b.referencePanel) {
                const referenceGroup = (_c = this._groups.get(options.position.referencePanel)) === null || _c === void 0 ? void 0 : _c.value;
                if (!referenceGroup) {
                    throw new Error(`reference group ${options.position.referencePanel} does not exist`);
                }
                const target = toTarget(options.position.direction);
                if (target === 'center') {
                    throw new Error(`${target} not supported as an option`);
                }
                else {
                    const location = getGridLocation(referenceGroup.element);
                    relativeLocation = getRelativeLocation(this.gridview.orientation, location, target);
                }
            }
            const view = this.options.createComponent({
                id: options.id,
                name: options.component,
            });
            view.init({
                params: (_d = options.params) !== null && _d !== void 0 ? _d : {},
                minimumWidth: options.minimumWidth,
                maximumWidth: options.maximumWidth,
                minimumHeight: options.minimumHeight,
                maximumHeight: options.maximumHeight,
                priority: options.priority,
                snap: !!options.snap,
                accessor: this,
                isVisible: true,
            });
            this.registerPanel(view);
            this.doAddGroup(view, relativeLocation, options.size);
            this.doSetGroupActive(view);
            return view;
        }
        registerPanel(panel) {
            const disposable = new CompositeDisposable(panel.api.onDidFocusChange((event) => {
                if (!event.isFocused) {
                    return;
                }
                this._groups.forEach((groupItem) => {
                    const group = groupItem.value;
                    if (group !== panel) {
                        group.setActive(false);
                    }
                    else {
                        group.setActive(true);
                    }
                });
            }));
            this._groups.set(panel.id, {
                value: panel,
                disposable,
            });
        }
        moveGroup(referenceGroup, groupId, target) {
            const sourceGroup = this.getPanel(groupId);
            if (!sourceGroup) {
                throw new Error('invalid operation');
            }
            const referenceLocation = getGridLocation(referenceGroup.element);
            const targetLocation = getRelativeLocation(this.gridview.orientation, referenceLocation, target);
            const [targetParentLocation, to] = tail(targetLocation);
            const sourceLocation = getGridLocation(sourceGroup.element);
            const [sourceParentLocation, from] = tail(sourceLocation);
            if (sequenceEquals(sourceParentLocation, targetParentLocation)) {
                // special case when 'swapping' two views within same grid location
                // if a group has one tab - we are essentially moving the 'group'
                // which is equivalent to swapping two views in this case
                this.gridview.moveView(sourceParentLocation, from, to);
                return;
            }
            // source group will become empty so delete the group
            const targetGroup = this.doRemoveGroup(sourceGroup, {
                skipActive: true,
                skipDispose: true,
            });
            // after deleting the group we need to re-evaulate the ref location
            const updatedReferenceLocation = getGridLocation(referenceGroup.element);
            const location = getRelativeLocation(this.gridview.orientation, updatedReferenceLocation, target);
            this.doAddGroup(targetGroup, location);
        }
        removeGroup(group) {
            super.removeGroup(group);
        }
        dispose() {
            super.dispose();
            this._onDidLayoutfromJSON.dispose();
        }
    }

    /**
     * A high-level implementation of splitview that works using 'panels'
     */
    class SplitviewComponent extends Resizable {
        get panels() {
            return this.splitview.getViews();
        }
        get options() {
            return this._options;
        }
        get length() {
            return this._panels.size;
        }
        get orientation() {
            return this.splitview.orientation;
        }
        get splitview() {
            return this._splitview;
        }
        set splitview(value) {
            if (this._splitview) {
                this._splitview.dispose();
            }
            this._splitview = value;
            this._splitviewChangeDisposable.value = new CompositeDisposable(this._splitview.onDidSashEnd(() => {
                this._onDidLayoutChange.fire(undefined);
            }), this._splitview.onDidAddView((e) => this._onDidAddView.fire(e)), this._splitview.onDidRemoveView((e) => this._onDidRemoveView.fire(e)));
        }
        get minimumSize() {
            return this.splitview.minimumSize;
        }
        get maximumSize() {
            return this.splitview.maximumSize;
        }
        get height() {
            return this.splitview.orientation === exports.Orientation.HORIZONTAL
                ? this.splitview.orthogonalSize
                : this.splitview.size;
        }
        get width() {
            return this.splitview.orientation === exports.Orientation.HORIZONTAL
                ? this.splitview.size
                : this.splitview.orthogonalSize;
        }
        constructor(container, options) {
            var _a;
            super(document.createElement('div'), options.disableAutoResizing);
            this._splitviewChangeDisposable = new MutableDisposable();
            this._panels = new Map();
            this._onDidLayoutfromJSON = new Emitter();
            this.onDidLayoutFromJSON = this._onDidLayoutfromJSON.event;
            this._onDidAddView = new Emitter();
            this.onDidAddView = this._onDidAddView.event;
            this._onDidRemoveView = new Emitter();
            this.onDidRemoveView = this._onDidRemoveView.event;
            this._onDidLayoutChange = new Emitter();
            this.onDidLayoutChange = this._onDidLayoutChange.event;
            this.element.style.height = '100%';
            this.element.style.width = '100%';
            this._classNames = new Classnames(this.element);
            this._classNames.setClassNames((_a = options.className) !== null && _a !== void 0 ? _a : '');
            // the container is owned by the third-party, do not modify/delete it
            container.appendChild(this.element);
            this._options = options;
            this.splitview = new Splitview(this.element, options);
            this.addDisposables(this._onDidAddView, this._onDidLayoutfromJSON, this._onDidRemoveView, this._onDidLayoutChange);
        }
        updateOptions(options) {
            var _a, _b;
            if ('className' in options) {
                this._classNames.setClassNames((_a = options.className) !== null && _a !== void 0 ? _a : '');
            }
            if ('disableResizing' in options) {
                this.disableResizing = (_b = options.disableAutoResizing) !== null && _b !== void 0 ? _b : false;
            }
            if (typeof options.orientation === 'string') {
                this.splitview.orientation = options.orientation;
            }
            this._options = Object.assign(Object.assign({}, this.options), options);
            this.splitview.layout(this.splitview.size, this.splitview.orthogonalSize);
        }
        focus() {
            var _a;
            (_a = this._activePanel) === null || _a === void 0 ? void 0 : _a.focus();
        }
        movePanel(from, to) {
            this.splitview.moveView(from, to);
        }
        setVisible(panel, visible) {
            const index = this.panels.indexOf(panel);
            this.splitview.setViewVisible(index, visible);
        }
        setActive(panel, skipFocus) {
            this._activePanel = panel;
            this.panels
                .filter((v) => v !== panel)
                .forEach((v) => {
                v.api._onDidActiveChange.fire({ isActive: false });
                if (!skipFocus) {
                    v.focus();
                }
            });
            panel.api._onDidActiveChange.fire({ isActive: true });
            if (!skipFocus) {
                panel.focus();
            }
        }
        removePanel(panel, sizing) {
            const item = this._panels.get(panel.id);
            if (!item) {
                throw new Error(`unknown splitview panel ${panel.id}`);
            }
            item.dispose();
            this._panels.delete(panel.id);
            const index = this.panels.findIndex((_) => _ === panel);
            const removedView = this.splitview.removeView(index, sizing);
            removedView.dispose();
            const panels = this.panels;
            if (panels.length > 0) {
                this.setActive(panels[panels.length - 1]);
            }
        }
        getPanel(id) {
            return this.panels.find((view) => view.id === id);
        }
        addPanel(options) {
            var _a;
            if (this._panels.has(options.id)) {
                throw new Error(`panel ${options.id} already exists`);
            }
            const view = this.options.createComponent({
                id: options.id,
                name: options.component,
            });
            view.orientation = this.splitview.orientation;
            view.init({
                params: (_a = options.params) !== null && _a !== void 0 ? _a : {},
                minimumSize: options.minimumSize,
                maximumSize: options.maximumSize,
                snap: options.snap,
                priority: options.priority,
                accessor: this,
            });
            const size = typeof options.size === 'number' ? options.size : exports.Sizing.Distribute;
            const index = typeof options.index === 'number' ? options.index : undefined;
            this.splitview.addView(view, size, index);
            this.doAddView(view);
            this.setActive(view);
            return view;
        }
        layout(width, height) {
            const [size, orthogonalSize] = this.splitview.orientation === exports.Orientation.HORIZONTAL
                ? [width, height]
                : [height, width];
            this.splitview.layout(size, orthogonalSize);
        }
        doAddView(view) {
            const disposable = view.api.onDidFocusChange((event) => {
                if (!event.isFocused) {
                    return;
                }
                this.setActive(view, true);
            });
            this._panels.set(view.id, disposable);
        }
        toJSON() {
            var _a;
            const views = this.splitview
                .getViews()
                .map((view, i) => {
                const size = this.splitview.getViewSize(i);
                return {
                    size,
                    data: view.toJSON(),
                    snap: !!view.snap,
                    priority: view.priority,
                };
            });
            return {
                views,
                activeView: (_a = this._activePanel) === null || _a === void 0 ? void 0 : _a.id,
                size: this.splitview.size,
                orientation: this.splitview.orientation,
            };
        }
        fromJSON(serializedSplitview) {
            this.clear();
            const { views, orientation, size, activeView } = serializedSplitview;
            const queue = [];
            // take note of the existing dimensions
            const width = this.width;
            const height = this.height;
            this.splitview = new Splitview(this.element, {
                orientation,
                proportionalLayout: this.options.proportionalLayout,
                descriptor: {
                    size,
                    views: views.map((view) => {
                        const data = view.data;
                        if (this._panels.has(data.id)) {
                            throw new Error(`panel ${data.id} already exists`);
                        }
                        const panel = this.options.createComponent({
                            id: data.id,
                            name: data.component,
                        });
                        queue.push(() => {
                            var _a;
                            panel.init({
                                params: (_a = data.params) !== null && _a !== void 0 ? _a : {},
                                minimumSize: data.minimumSize,
                                maximumSize: data.maximumSize,
                                snap: view.snap,
                                priority: view.priority,
                                accessor: this,
                            });
                        });
                        panel.orientation = orientation;
                        this.doAddView(panel);
                        setTimeout(() => {
                            // the original onDidAddView events are missed since they are fired before we can subcribe to them
                            this._onDidAddView.fire(panel);
                        }, 0);
                        return { size: view.size, view: panel };
                    }),
                },
            });
            this.layout(width, height);
            queue.forEach((f) => f());
            if (typeof activeView === 'string') {
                const panel = this.getPanel(activeView);
                if (panel) {
                    this.setActive(panel);
                }
            }
            this._onDidLayoutfromJSON.fire();
        }
        clear() {
            for (const disposable of this._panels.values()) {
                disposable.dispose();
            }
            this._panels.clear();
            while (this.splitview.length > 0) {
                const view = this.splitview.removeView(0, exports.Sizing.Distribute, true);
                view.dispose();
            }
        }
        dispose() {
            for (const disposable of this._panels.values()) {
                disposable.dispose();
            }
            this._panels.clear();
            const views = this.splitview.getViews();
            this._splitviewChangeDisposable.dispose();
            this.splitview.dispose();
            for (const view of views) {
                view.dispose();
            }
            this.element.remove();
            super.dispose();
        }
    }

    class DefaultHeader extends CompositeDisposable {
        get element() {
            return this._element;
        }
        constructor() {
            super();
            this._expandedIcon = createExpandMoreButton();
            this._collapsedIcon = createChevronRightButton();
            this.disposable = new MutableDisposable();
            this.apiRef = {
                api: null,
            };
            this._element = document.createElement('div');
            this.element.className = 'dv-default-header';
            this._content = document.createElement('span');
            this._expander = document.createElement('div');
            this._expander.className = 'dv-pane-header-icon';
            this.element.appendChild(this._expander);
            this.element.appendChild(this._content);
            this.addDisposables(addDisposableListener(this._element, 'click', () => {
                var _a;
                (_a = this.apiRef.api) === null || _a === void 0 ? void 0 : _a.setExpanded(!this.apiRef.api.isExpanded);
            }));
        }
        init(params) {
            this.apiRef.api = params.api;
            this._content.textContent = params.title;
            this.updateIcon();
            this.disposable.value = params.api.onDidExpansionChange(() => {
                this.updateIcon();
            });
        }
        updateIcon() {
            var _a;
            const isExpanded = !!((_a = this.apiRef.api) === null || _a === void 0 ? void 0 : _a.isExpanded);
            toggleClass(this._expander, 'collapsed', !isExpanded);
            if (isExpanded) {
                if (this._expander.contains(this._collapsedIcon)) {
                    this._collapsedIcon.remove();
                }
                if (!this._expander.contains(this._expandedIcon)) {
                    this._expander.appendChild(this._expandedIcon);
                }
            }
            else {
                if (this._expander.contains(this._expandedIcon)) {
                    this._expandedIcon.remove();
                }
                if (!this._expander.contains(this._collapsedIcon)) {
                    this._expander.appendChild(this._collapsedIcon);
                }
            }
        }
        update(_params) {
            //
        }
        dispose() {
            this.disposable.dispose();
            super.dispose();
        }
    }

    const nextLayoutId = sequentialNumberGenerator();
    const HEADER_SIZE = 22;
    const MINIMUM_BODY_SIZE = 0;
    const MAXIMUM_BODY_SIZE = Number.MAX_SAFE_INTEGER;
    class PaneFramework extends DraggablePaneviewPanel {
        constructor(options) {
            super({
                accessor: options.accessor,
                id: options.id,
                component: options.component,
                headerComponent: options.headerComponent,
                orientation: options.orientation,
                isExpanded: options.isExpanded,
                disableDnd: options.disableDnd,
                headerSize: options.headerSize,
                minimumBodySize: options.minimumBodySize,
                maximumBodySize: options.maximumBodySize,
            });
            this.options = options;
        }
        getBodyComponent() {
            return this.options.body;
        }
        getHeaderComponent() {
            return this.options.header;
        }
    }
    class PaneviewComponent extends Resizable {
        get id() {
            return this._id;
        }
        get panels() {
            return this.paneview.getPanes();
        }
        set paneview(value) {
            this._paneview = value;
            this._disposable.value = new CompositeDisposable(this._paneview.onDidChange(() => {
                this._onDidLayoutChange.fire(undefined);
            }), this._paneview.onDidAddView((e) => this._onDidAddView.fire(e)), this._paneview.onDidRemoveView((e) => this._onDidRemoveView.fire(e)));
        }
        get paneview() {
            return this._paneview;
        }
        get minimumSize() {
            return this.paneview.minimumSize;
        }
        get maximumSize() {
            return this.paneview.maximumSize;
        }
        get height() {
            return this.paneview.orientation === exports.Orientation.HORIZONTAL
                ? this.paneview.orthogonalSize
                : this.paneview.size;
        }
        get width() {
            return this.paneview.orientation === exports.Orientation.HORIZONTAL
                ? this.paneview.size
                : this.paneview.orthogonalSize;
        }
        get options() {
            return this._options;
        }
        constructor(container, options) {
            var _a;
            super(document.createElement('div'), options.disableAutoResizing);
            this._id = nextLayoutId.next();
            this._disposable = new MutableDisposable();
            this._viewDisposables = new Map();
            this._onDidLayoutfromJSON = new Emitter();
            this.onDidLayoutFromJSON = this._onDidLayoutfromJSON.event;
            this._onDidLayoutChange = new Emitter();
            this.onDidLayoutChange = this._onDidLayoutChange.event;
            this._onDidDrop = new Emitter();
            this.onDidDrop = this._onDidDrop.event;
            this._onDidAddView = new Emitter();
            this.onDidAddView = this._onDidAddView.event;
            this._onDidRemoveView = new Emitter();
            this.onDidRemoveView = this._onDidRemoveView.event;
            this._onUnhandledDragOverEvent = new Emitter();
            this.onUnhandledDragOverEvent = this._onUnhandledDragOverEvent.event;
            this.element.style.height = '100%';
            this.element.style.width = '100%';
            this.addDisposables(this._onDidLayoutChange, this._onDidLayoutfromJSON, this._onDidDrop, this._onDidAddView, this._onDidRemoveView, this._onUnhandledDragOverEvent);
            this._classNames = new Classnames(this.element);
            this._classNames.setClassNames((_a = options.className) !== null && _a !== void 0 ? _a : '');
            // the container is owned by the third-party, do not modify/delete it
            container.appendChild(this.element);
            this._options = options;
            this.paneview = new Paneview(this.element, {
                // only allow paneview in the vertical orientation for now
                orientation: exports.Orientation.VERTICAL,
            });
            this.addDisposables(this._disposable);
        }
        setVisible(panel, visible) {
            const index = this.panels.indexOf(panel);
            this.paneview.setViewVisible(index, visible);
        }
        focus() {
            //noop
        }
        updateOptions(options) {
            var _a, _b;
            if ('className' in options) {
                this._classNames.setClassNames((_a = options.className) !== null && _a !== void 0 ? _a : '');
            }
            if ('disableResizing' in options) {
                this.disableResizing = (_b = options.disableAutoResizing) !== null && _b !== void 0 ? _b : false;
            }
            this._options = Object.assign(Object.assign({}, this.options), options);
        }
        addPanel(options) {
            var _a, _b;
            const body = this.options.createComponent({
                id: options.id,
                name: options.component,
            });
            let header;
            if (options.headerComponent && this.options.createHeaderComponent) {
                header = this.options.createHeaderComponent({
                    id: options.id,
                    name: options.headerComponent,
                });
            }
            if (!header) {
                header = new DefaultHeader();
            }
            const view = new PaneFramework({
                id: options.id,
                component: options.component,
                headerComponent: options.headerComponent,
                header,
                body,
                orientation: exports.Orientation.VERTICAL,
                isExpanded: !!options.isExpanded,
                disableDnd: !!this.options.disableDnd,
                accessor: this,
                headerSize: (_a = options.headerSize) !== null && _a !== void 0 ? _a : HEADER_SIZE,
                minimumBodySize: MINIMUM_BODY_SIZE,
                maximumBodySize: MAXIMUM_BODY_SIZE,
            });
            this.doAddPanel(view);
            const size = typeof options.size === 'number' ? options.size : exports.Sizing.Distribute;
            const index = typeof options.index === 'number' ? options.index : undefined;
            view.init({
                params: (_b = options.params) !== null && _b !== void 0 ? _b : {},
                minimumBodySize: options.minimumBodySize,
                maximumBodySize: options.maximumBodySize,
                isExpanded: options.isExpanded,
                title: options.title,
                containerApi: new PaneviewApi(this),
                accessor: this,
            });
            this.paneview.addPane(view, size, index);
            view.orientation = this.paneview.orientation;
            return view;
        }
        removePanel(panel) {
            const views = this.panels;
            const index = views.findIndex((_) => _ === panel);
            this.paneview.removePane(index);
            this.doRemovePanel(panel);
        }
        movePanel(from, to) {
            this.paneview.moveView(from, to);
        }
        getPanel(id) {
            return this.panels.find((view) => view.id === id);
        }
        layout(width, height) {
            const [size, orthogonalSize] = this.paneview.orientation === exports.Orientation.HORIZONTAL
                ? [width, height]
                : [height, width];
            this.paneview.layout(size, orthogonalSize);
        }
        toJSON() {
            const maximum = (value) => value === Number.MAX_SAFE_INTEGER ||
                value === Number.POSITIVE_INFINITY
                ? undefined
                : value;
            const minimum = (value) => (value <= 0 ? undefined : value);
            const views = this.paneview
                .getPanes()
                .map((view, i) => {
                const size = this.paneview.getViewSize(i);
                return {
                    size,
                    data: view.toJSON(),
                    minimumSize: minimum(view.minimumBodySize),
                    maximumSize: maximum(view.maximumBodySize),
                    headerSize: view.headerSize,
                    expanded: view.isExpanded(),
                };
            });
            return {
                views,
                size: this.paneview.size,
            };
        }
        fromJSON(serializedPaneview) {
            this.clear();
            const { views, size } = serializedPaneview;
            const queue = [];
            // take note of the existing dimensions
            const width = this.width;
            const height = this.height;
            this.paneview = new Paneview(this.element, {
                orientation: exports.Orientation.VERTICAL,
                descriptor: {
                    size,
                    views: views.map((view) => {
                        var _a, _b, _c;
                        const data = view.data;
                        const body = this.options.createComponent({
                            id: data.id,
                            name: data.component,
                        });
                        let header;
                        if (data.headerComponent &&
                            this.options.createHeaderComponent) {
                            header = this.options.createHeaderComponent({
                                id: data.id,
                                name: data.headerComponent,
                            });
                        }
                        if (!header) {
                            header = new DefaultHeader();
                        }
                        const panel = new PaneFramework({
                            id: data.id,
                            component: data.component,
                            headerComponent: data.headerComponent,
                            header,
                            body,
                            orientation: exports.Orientation.VERTICAL,
                            isExpanded: !!view.expanded,
                            disableDnd: !!this.options.disableDnd,
                            accessor: this,
                            headerSize: (_a = view.headerSize) !== null && _a !== void 0 ? _a : HEADER_SIZE,
                            minimumBodySize: (_b = view.minimumSize) !== null && _b !== void 0 ? _b : MINIMUM_BODY_SIZE,
                            maximumBodySize: (_c = view.maximumSize) !== null && _c !== void 0 ? _c : MAXIMUM_BODY_SIZE,
                        });
                        this.doAddPanel(panel);
                        queue.push(() => {
                            var _a;
                            panel.init({
                                params: (_a = data.params) !== null && _a !== void 0 ? _a : {},
                                minimumBodySize: view.minimumSize,
                                maximumBodySize: view.maximumSize,
                                title: data.title,
                                isExpanded: !!view.expanded,
                                containerApi: new PaneviewApi(this),
                                accessor: this,
                            });
                            panel.orientation = this.paneview.orientation;
                        });
                        setTimeout(() => {
                            // the original onDidAddView events are missed since they are fired before we can subcribe to them
                            this._onDidAddView.fire(panel);
                        }, 0);
                        return { size: view.size, view: panel };
                    }),
                },
            });
            this.layout(width, height);
            queue.forEach((f) => f());
            this._onDidLayoutfromJSON.fire();
        }
        clear() {
            for (const [_, value] of this._viewDisposables.entries()) {
                value.dispose();
            }
            this._viewDisposables.clear();
            this.paneview.dispose();
        }
        doAddPanel(panel) {
            const disposable = new CompositeDisposable(panel.onDidDrop((event) => {
                this._onDidDrop.fire(event);
            }), panel.onUnhandledDragOverEvent((event) => {
                this._onUnhandledDragOverEvent.fire(event);
            }));
            this._viewDisposables.set(panel.id, disposable);
        }
        doRemovePanel(panel) {
            const disposable = this._viewDisposables.get(panel.id);
            if (disposable) {
                disposable.dispose();
                this._viewDisposables.delete(panel.id);
            }
        }
        dispose() {
            super.dispose();
            for (const [_, value] of this._viewDisposables.entries()) {
                value.dispose();
            }
            this._viewDisposables.clear();
            this.element.remove();
            this.paneview.dispose();
        }
    }

    class SplitviewPanel extends BasePanelView {
        get priority() {
            return this._priority;
        }
        set orientation(value) {
            this._orientation = value;
        }
        get orientation() {
            return this._orientation;
        }
        get minimumSize() {
            const size = typeof this._minimumSize === 'function'
                ? this._minimumSize()
                : this._minimumSize;
            if (size !== this._evaluatedMinimumSize) {
                this._evaluatedMinimumSize = size;
                this.updateConstraints();
            }
            return size;
        }
        get maximumSize() {
            const size = typeof this._maximumSize === 'function'
                ? this._maximumSize()
                : this._maximumSize;
            if (size !== this._evaluatedMaximumSize) {
                this._evaluatedMaximumSize = size;
                this.updateConstraints();
            }
            return size;
        }
        get snap() {
            return this._snap;
        }
        constructor(id, componentName) {
            super(id, componentName, new SplitviewPanelApiImpl(id, componentName));
            this._evaluatedMinimumSize = 0;
            this._evaluatedMaximumSize = Number.POSITIVE_INFINITY;
            this._minimumSize = 0;
            this._maximumSize = Number.POSITIVE_INFINITY;
            this._snap = false;
            this._onDidChange = new Emitter();
            this.onDidChange = this._onDidChange.event;
            this.api.initialize(this);
            this.addDisposables(this._onDidChange, this.api.onWillVisibilityChange((event) => {
                const { isVisible } = event;
                const { accessor } = this._params;
                accessor.setVisible(this, isVisible);
            }), this.api.onActiveChange(() => {
                const { accessor } = this._params;
                accessor.setActive(this);
            }), this.api.onDidConstraintsChangeInternal((event) => {
                if (typeof event.minimumSize === 'number' ||
                    typeof event.minimumSize === 'function') {
                    this._minimumSize = event.minimumSize;
                }
                if (typeof event.maximumSize === 'number' ||
                    typeof event.maximumSize === 'function') {
                    this._maximumSize = event.maximumSize;
                }
                this.updateConstraints();
            }), this.api.onDidSizeChange((event) => {
                this._onDidChange.fire({ size: event.size });
            }));
        }
        setVisible(isVisible) {
            this.api._onDidVisibilityChange.fire({ isVisible });
        }
        setActive(isActive) {
            this.api._onDidActiveChange.fire({ isActive });
        }
        layout(size, orthogonalSize) {
            const [width, height] = this.orientation === exports.Orientation.HORIZONTAL
                ? [size, orthogonalSize]
                : [orthogonalSize, size];
            super.layout(width, height);
        }
        init(parameters) {
            super.init(parameters);
            this._priority = parameters.priority;
            if (parameters.minimumSize) {
                this._minimumSize = parameters.minimumSize;
            }
            if (parameters.maximumSize) {
                this._maximumSize = parameters.maximumSize;
            }
            if (parameters.snap) {
                this._snap = parameters.snap;
            }
        }
        toJSON() {
            const maximum = (value) => value === Number.MAX_SAFE_INTEGER ||
                value === Number.POSITIVE_INFINITY
                ? undefined
                : value;
            const minimum = (value) => (value <= 0 ? undefined : value);
            return Object.assign(Object.assign({}, super.toJSON()), { minimumSize: minimum(this.minimumSize), maximumSize: maximum(this.maximumSize) });
        }
        updateConstraints() {
            this.api._onDidConstraintsChange.fire({
                maximumSize: this._evaluatedMaximumSize,
                minimumSize: this._evaluatedMinimumSize,
            });
        }
    }

    function createDockview(element, options) {
        const component = new DockviewComponent(element, options);
        return component.api;
    }
    function createSplitview(element, options) {
        const component = new SplitviewComponent(element, options);
        return new SplitviewApi(component);
    }
    function createGridview(element, options) {
        const component = new GridviewComponent(element, options);
        return new GridviewApi(component);
    }
    function createPaneview(element, options) {
        const component = new PaneviewComponent(element, options);
        return new PaneviewApi(component);
    }

    exports.BaseGrid = BaseGrid;
    exports.ContentContainer = ContentContainer;
    exports.DefaultDockviewDeserialzier = DefaultDockviewDeserialzier;
    exports.DefaultTab = DefaultTab;
    exports.DockviewApi = DockviewApi;
    exports.DockviewComponent = DockviewComponent;
    exports.DockviewCompositeDisposable = CompositeDisposable;
    exports.DockviewDidDropEvent = DockviewDidDropEvent;
    exports.DockviewEmitter = Emitter;
    exports.DockviewGroupPanel = DockviewGroupPanel;
    exports.DockviewGroupPanelModel = DockviewGroupPanelModel;
    exports.DockviewMutableDisposable = MutableDisposable;
    exports.DockviewPanel = DockviewPanel;
    exports.DockviewUnhandledDragOverEvent = DockviewUnhandledDragOverEvent;
    exports.DockviewWillDropEvent = DockviewWillDropEvent;
    exports.DraggablePaneviewPanel = DraggablePaneviewPanel;
    exports.Gridview = Gridview;
    exports.GridviewApi = GridviewApi;
    exports.GridviewComponent = GridviewComponent;
    exports.GridviewPanel = GridviewPanel;
    exports.PROPERTY_KEYS_DOCKVIEW = PROPERTY_KEYS_DOCKVIEW;
    exports.PROPERTY_KEYS_GRIDVIEW = PROPERTY_KEYS_GRIDVIEW;
    exports.PROPERTY_KEYS_PANEVIEW = PROPERTY_KEYS_PANEVIEW;
    exports.PROPERTY_KEYS_SPLITVIEW = PROPERTY_KEYS_SPLITVIEW;
    exports.PaneFramework = PaneFramework;
    exports.PaneTransfer = PaneTransfer;
    exports.PanelTransfer = PanelTransfer;
    exports.Paneview = Paneview;
    exports.PaneviewApi = PaneviewApi;
    exports.PaneviewComponent = PaneviewComponent;
    exports.PaneviewPanel = PaneviewPanel;
    exports.PaneviewUnhandledDragOverEvent = PaneviewUnhandledDragOverEvent;
    exports.Splitview = Splitview;
    exports.SplitviewApi = SplitviewApi;
    exports.SplitviewComponent = SplitviewComponent;
    exports.SplitviewPanel = SplitviewPanel;
    exports.Tab = Tab;
    exports.createDockview = createDockview;
    exports.createGridview = createGridview;
    exports.createPaneview = createPaneview;
    exports.createSplitview = createSplitview;
    exports.directionToPosition = directionToPosition;
    exports.getDirectionOrientation = getDirectionOrientation;
    exports.getGridLocation = getGridLocation;
    exports.getLocationOrientation = getLocationOrientation;
    exports.getPaneData = getPaneData;
    exports.getPanelData = getPanelData;
    exports.getRelativeLocation = getRelativeLocation;
    exports.indexInParent = indexInParent;
    exports.isGridBranchNode = isGridBranchNode;
    exports.isGroupOptionsWithGroup = isGroupOptionsWithGroup;
    exports.isGroupOptionsWithPanel = isGroupOptionsWithPanel;
    exports.isPanelOptionsWithGroup = isPanelOptionsWithGroup;
    exports.isPanelOptionsWithPanel = isPanelOptionsWithPanel;
    exports.orthogonal = orthogonal;
    exports.positionToDirection = positionToDirection;
    exports.themeAbyss = themeAbyss;
    exports.themeAbyssSpaced = themeAbyssSpaced;
    exports.themeDark = themeDark;
    exports.themeDracula = themeDracula;
    exports.themeLight = themeLight;
    exports.themeLightSpaced = themeLightSpaced;
    exports.themeReplit = themeReplit;
    exports.themeVisualStudio = themeVisualStudio;
    exports.toTarget = toTarget;

}));
//# sourceMappingURL=dockview-core.noStyle.js.map
