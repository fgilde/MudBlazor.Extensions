"use strict";
var __extends = (this && this.__extends) || (function () {
    var extendStatics = function (d, b) {
        extendStatics = Object.setPrototypeOf ||
            ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
            function (d, b) { for (var p in b) if (Object.prototype.hasOwnProperty.call(b, p)) d[p] = b[p]; };
        return extendStatics(d, b);
    };
    return function (d, b) {
        if (typeof b !== "function" && b !== null)
            throw new TypeError("Class extends value " + String(b) + " is not a constructor or null");
        extendStatics(d, b);
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };
})();
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
exports.findRelativeZIndexParent = exports.shiftAbsoluteElementIntoView = exports.onDidWindowResizeEnd = exports.onDidWindowMoveEnd = exports.isChildEntirelyVisibleWithinParent = exports.Classnames = exports.getDockviewTheme = exports.disableIframePointEvents = exports.addTestId = exports.isInDocument = exports.getDomNodePagePosition = exports.addStyles = exports.quasiDefaultPrevented = exports.quasiPreventDefault = exports.trackFocus = exports.getElementsByTagName = exports.isAncestor = exports.toggleClass = exports.addClasses = exports.removeClasses = exports.watchElementResize = exports.OverflowObserver = void 0;
var events_1 = require("./events");
var lifecycle_1 = require("./lifecycle");
var OverflowObserver = /** @class */ (function (_super) {
    __extends(OverflowObserver, _super);
    function OverflowObserver(el) {
        var _this = _super.call(this) || this;
        _this._onDidChange = new events_1.Emitter();
        _this.onDidChange = _this._onDidChange.event;
        _this._value = null;
        _this.addDisposables(_this._onDidChange, watchElementResize(el, function (entry) {
            var hasScrollX = entry.target.scrollWidth > entry.target.clientWidth;
            var hasScrollY = entry.target.scrollHeight > entry.target.clientHeight;
            _this._value = { hasScrollX: hasScrollX, hasScrollY: hasScrollY };
            _this._onDidChange.fire(_this._value);
        }));
        return _this;
    }
    return OverflowObserver;
}(lifecycle_1.CompositeDisposable));
exports.OverflowObserver = OverflowObserver;
function watchElementResize(element, cb) {
    var observer = new ResizeObserver(function (entires) {
        /**
         * Fast browser window resize produces Error: ResizeObserver loop limit exceeded.
         * The error isn't visible in browser console, doesn't affect functionality, but degrades performance.
         * See https://stackoverflow.com/questions/49384120/resizeobserver-loop-limit-exceeded/58701523#58701523
         */
        requestAnimationFrame(function () {
            var firstEntry = entires[0];
            cb(firstEntry);
        });
    });
    observer.observe(element);
    return {
        dispose: function () {
            observer.unobserve(element);
            observer.disconnect();
        },
    };
}
exports.watchElementResize = watchElementResize;
var removeClasses = function (element) {
    var e_1, _a;
    var classes = [];
    for (var _i = 1; _i < arguments.length; _i++) {
        classes[_i - 1] = arguments[_i];
    }
    try {
        for (var classes_1 = __values(classes), classes_1_1 = classes_1.next(); !classes_1_1.done; classes_1_1 = classes_1.next()) {
            var classname = classes_1_1.value;
            if (element.classList.contains(classname)) {
                element.classList.remove(classname);
            }
        }
    }
    catch (e_1_1) { e_1 = { error: e_1_1 }; }
    finally {
        try {
            if (classes_1_1 && !classes_1_1.done && (_a = classes_1.return)) _a.call(classes_1);
        }
        finally { if (e_1) throw e_1.error; }
    }
};
exports.removeClasses = removeClasses;
var addClasses = function (element) {
    var e_2, _a;
    var classes = [];
    for (var _i = 1; _i < arguments.length; _i++) {
        classes[_i - 1] = arguments[_i];
    }
    try {
        for (var classes_2 = __values(classes), classes_2_1 = classes_2.next(); !classes_2_1.done; classes_2_1 = classes_2.next()) {
            var classname = classes_2_1.value;
            if (!element.classList.contains(classname)) {
                element.classList.add(classname);
            }
        }
    }
    catch (e_2_1) { e_2 = { error: e_2_1 }; }
    finally {
        try {
            if (classes_2_1 && !classes_2_1.done && (_a = classes_2.return)) _a.call(classes_2);
        }
        finally { if (e_2) throw e_2.error; }
    }
};
exports.addClasses = addClasses;
var toggleClass = function (element, className, isToggled) {
    var hasClass = element.classList.contains(className);
    if (isToggled && !hasClass) {
        element.classList.add(className);
    }
    if (!isToggled && hasClass) {
        element.classList.remove(className);
    }
};
exports.toggleClass = toggleClass;
function isAncestor(testChild, testAncestor) {
    while (testChild) {
        if (testChild === testAncestor) {
            return true;
        }
        testChild = testChild.parentNode;
    }
    return false;
}
exports.isAncestor = isAncestor;
function getElementsByTagName(tag, document) {
    return Array.prototype.slice.call(document.querySelectorAll(tag), 0);
}
exports.getElementsByTagName = getElementsByTagName;
function trackFocus(element) {
    return new FocusTracker(element);
}
exports.trackFocus = trackFocus;
/**
 * Track focus on an element. Ensure tabIndex is set when an HTMLElement is not focusable by default
 */
var FocusTracker = /** @class */ (function (_super) {
    __extends(FocusTracker, _super);
    function FocusTracker(element) {
        var _this = _super.call(this) || this;
        _this._onDidFocus = new events_1.Emitter();
        _this.onDidFocus = _this._onDidFocus.event;
        _this._onDidBlur = new events_1.Emitter();
        _this.onDidBlur = _this._onDidBlur.event;
        _this.addDisposables(_this._onDidFocus, _this._onDidBlur);
        var hasFocus = isAncestor(document.activeElement, element);
        var loosingFocus = false;
        var onFocus = function () {
            loosingFocus = false;
            if (!hasFocus) {
                hasFocus = true;
                _this._onDidFocus.fire();
            }
        };
        var onBlur = function () {
            if (hasFocus) {
                loosingFocus = true;
                window.setTimeout(function () {
                    if (loosingFocus) {
                        loosingFocus = false;
                        hasFocus = false;
                        _this._onDidBlur.fire();
                    }
                }, 0);
            }
        };
        _this._refreshStateHandler = function () {
            var currentNodeHasFocus = isAncestor(document.activeElement, element);
            if (currentNodeHasFocus !== hasFocus) {
                if (hasFocus) {
                    onBlur();
                }
                else {
                    onFocus();
                }
            }
        };
        _this.addDisposables((0, events_1.addDisposableListener)(element, 'focus', onFocus, true));
        _this.addDisposables((0, events_1.addDisposableListener)(element, 'blur', onBlur, true));
        return _this;
    }
    FocusTracker.prototype.refreshState = function () {
        this._refreshStateHandler();
    };
    return FocusTracker;
}(lifecycle_1.CompositeDisposable));
// quasi: apparently, but not really; seemingly
var QUASI_PREVENT_DEFAULT_KEY = 'dv-quasiPreventDefault';
// mark an event directly for other listeners to check
function quasiPreventDefault(event) {
    event[QUASI_PREVENT_DEFAULT_KEY] = true;
}
exports.quasiPreventDefault = quasiPreventDefault;
// check if this event has been marked
function quasiDefaultPrevented(event) {
    return event[QUASI_PREVENT_DEFAULT_KEY];
}
exports.quasiDefaultPrevented = quasiDefaultPrevented;
function addStyles(document, styleSheetList) {
    var e_3, _a, e_4, _b;
    var styleSheets = Array.from(styleSheetList);
    try {
        for (var styleSheets_1 = __values(styleSheets), styleSheets_1_1 = styleSheets_1.next(); !styleSheets_1_1.done; styleSheets_1_1 = styleSheets_1.next()) {
            var styleSheet = styleSheets_1_1.value;
            if (styleSheet.href) {
                var link = document.createElement('link');
                link.href = styleSheet.href;
                link.type = styleSheet.type;
                link.rel = 'stylesheet';
                document.head.appendChild(link);
            }
            var cssTexts = [];
            try {
                if (styleSheet.cssRules) {
                    cssTexts = Array.from(styleSheet.cssRules).map(function (rule) { return rule.cssText; });
                }
            }
            catch (err) {
                // security errors (lack of permissions), ignore
            }
            try {
                for (var cssTexts_1 = (e_4 = void 0, __values(cssTexts)), cssTexts_1_1 = cssTexts_1.next(); !cssTexts_1_1.done; cssTexts_1_1 = cssTexts_1.next()) {
                    var rule = cssTexts_1_1.value;
                    var style = document.createElement('style');
                    style.appendChild(document.createTextNode(rule));
                    document.head.appendChild(style);
                }
            }
            catch (e_4_1) { e_4 = { error: e_4_1 }; }
            finally {
                try {
                    if (cssTexts_1_1 && !cssTexts_1_1.done && (_b = cssTexts_1.return)) _b.call(cssTexts_1);
                }
                finally { if (e_4) throw e_4.error; }
            }
        }
    }
    catch (e_3_1) { e_3 = { error: e_3_1 }; }
    finally {
        try {
            if (styleSheets_1_1 && !styleSheets_1_1.done && (_a = styleSheets_1.return)) _a.call(styleSheets_1);
        }
        finally { if (e_3) throw e_3.error; }
    }
}
exports.addStyles = addStyles;
function getDomNodePagePosition(domNode) {
    var _a = domNode.getBoundingClientRect(), left = _a.left, top = _a.top, width = _a.width, height = _a.height;
    return {
        left: left + window.scrollX,
        top: top + window.scrollY,
        width: width,
        height: height,
    };
}
exports.getDomNodePagePosition = getDomNodePagePosition;
/**
 * Check whether an element is in the DOM (including the Shadow DOM)
 * @see https://terodox.tech/how-to-tell-if-an-element-is-in-the-dom-including-the-shadow-dom/
 */
function isInDocument(element) {
    var currentElement = element;
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
exports.isInDocument = isInDocument;
function addTestId(element, id) {
    element.setAttribute('data-testid', id);
}
exports.addTestId = addTestId;
/**
 * Should be more efficient than element.querySelectorAll("*") since there
 * is no need to store every element in-memory using this approach
 */
function allTagsNamesInclusiveOfShadowDoms(tagNames) {
    var iframes = [];
    function findIframesInNode(node) {
        var e_5, _a;
        if (node.nodeType === Node.ELEMENT_NODE) {
            if (tagNames.includes(node.tagName)) {
                iframes.push(node);
            }
            if (node.shadowRoot) {
                findIframesInNode(node.shadowRoot);
            }
            try {
                for (var _b = __values(node.children), _c = _b.next(); !_c.done; _c = _b.next()) {
                    var child = _c.value;
                    findIframesInNode(child);
                }
            }
            catch (e_5_1) { e_5 = { error: e_5_1 }; }
            finally {
                try {
                    if (_c && !_c.done && (_a = _b.return)) _a.call(_b);
                }
                finally { if (e_5) throw e_5.error; }
            }
        }
    }
    findIframesInNode(document.documentElement);
    return iframes;
}
function disableIframePointEvents(rootNode) {
    var e_6, _a;
    if (rootNode === void 0) { rootNode = document; }
    var iframes = allTagsNamesInclusiveOfShadowDoms(['IFRAME', 'WEBVIEW']);
    var original = new WeakMap(); // don't hold onto HTMLElement references longer than required
    try {
        for (var iframes_1 = __values(iframes), iframes_1_1 = iframes_1.next(); !iframes_1_1.done; iframes_1_1 = iframes_1.next()) {
            var iframe = iframes_1_1.value;
            original.set(iframe, iframe.style.pointerEvents);
            iframe.style.pointerEvents = 'none';
        }
    }
    catch (e_6_1) { e_6 = { error: e_6_1 }; }
    finally {
        try {
            if (iframes_1_1 && !iframes_1_1.done && (_a = iframes_1.return)) _a.call(iframes_1);
        }
        finally { if (e_6) throw e_6.error; }
    }
    return {
        release: function () {
            var e_7, _a;
            var _b;
            try {
                for (var iframes_2 = __values(iframes), iframes_2_1 = iframes_2.next(); !iframes_2_1.done; iframes_2_1 = iframes_2.next()) {
                    var iframe = iframes_2_1.value;
                    iframe.style.pointerEvents = (_b = original.get(iframe)) !== null && _b !== void 0 ? _b : 'auto';
                }
            }
            catch (e_7_1) { e_7 = { error: e_7_1 }; }
            finally {
                try {
                    if (iframes_2_1 && !iframes_2_1.done && (_a = iframes_2.return)) _a.call(iframes_2);
                }
                finally { if (e_7) throw e_7.error; }
            }
            iframes.splice(0, iframes.length); // don't hold onto HTMLElement references longer than required
        },
    };
}
exports.disableIframePointEvents = disableIframePointEvents;
function getDockviewTheme(element) {
    function toClassList(element) {
        var list = [];
        for (var i = 0; i < element.classList.length; i++) {
            list.push(element.classList.item(i));
        }
        return list;
    }
    var theme = undefined;
    var parent = element;
    while (parent !== null) {
        theme = toClassList(parent).find(function (cls) {
            return cls.startsWith('dockview-theme-');
        });
        if (typeof theme === 'string') {
            break;
        }
        parent = parent.parentElement;
    }
    return theme;
}
exports.getDockviewTheme = getDockviewTheme;
var Classnames = /** @class */ (function () {
    function Classnames(element) {
        this.element = element;
        this._classNames = [];
    }
    Classnames.prototype.setClassNames = function (classNames) {
        var e_8, _a, e_9, _b;
        try {
            for (var _c = __values(this._classNames), _d = _c.next(); !_d.done; _d = _c.next()) {
                var className = _d.value;
                (0, exports.toggleClass)(this.element, className, false);
            }
        }
        catch (e_8_1) { e_8 = { error: e_8_1 }; }
        finally {
            try {
                if (_d && !_d.done && (_a = _c.return)) _a.call(_c);
            }
            finally { if (e_8) throw e_8.error; }
        }
        this._classNames = classNames
            .split(' ')
            .filter(function (v) { return v.trim().length > 0; });
        try {
            for (var _e = __values(this._classNames), _f = _e.next(); !_f.done; _f = _e.next()) {
                var className = _f.value;
                (0, exports.toggleClass)(this.element, className, true);
            }
        }
        catch (e_9_1) { e_9 = { error: e_9_1 }; }
        finally {
            try {
                if (_f && !_f.done && (_b = _e.return)) _b.call(_e);
            }
            finally { if (e_9) throw e_9.error; }
        }
    };
    return Classnames;
}());
exports.Classnames = Classnames;
var DEBOUCE_DELAY = 100;
function isChildEntirelyVisibleWithinParent(child, parent) {
    //
    var childPosition = getDomNodePagePosition(child);
    var parentPosition = getDomNodePagePosition(parent);
    if (childPosition.left < parentPosition.left) {
        return false;
    }
    if (childPosition.left + childPosition.width >
        parentPosition.left + parentPosition.width) {
        return false;
    }
    return true;
}
exports.isChildEntirelyVisibleWithinParent = isChildEntirelyVisibleWithinParent;
function onDidWindowMoveEnd(window) {
    var emitter = new events_1.Emitter();
    var previousScreenX = window.screenX;
    var previousScreenY = window.screenY;
    var timeout;
    var checkMovement = function () {
        if (window.closed) {
            return;
        }
        var currentScreenX = window.screenX;
        var currentScreenY = window.screenY;
        if (currentScreenX !== previousScreenX ||
            currentScreenY !== previousScreenY) {
            clearTimeout(timeout);
            timeout = setTimeout(function () {
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
exports.onDidWindowMoveEnd = onDidWindowMoveEnd;
function onDidWindowResizeEnd(element, cb) {
    var resizeTimeout;
    var disposable = new lifecycle_1.CompositeDisposable((0, events_1.addDisposableListener)(element, 'resize', function () {
        clearTimeout(resizeTimeout);
        resizeTimeout = setTimeout(function () {
            cb();
        }, DEBOUCE_DELAY);
    }));
    return disposable;
}
exports.onDidWindowResizeEnd = onDidWindowResizeEnd;
function shiftAbsoluteElementIntoView(element, root, options) {
    if (options === void 0) { options = { buffer: 10 }; }
    var buffer = options.buffer;
    var rect = element.getBoundingClientRect();
    var rootRect = root.getBoundingClientRect();
    var translateX = 0;
    var translateY = 0;
    var left = rect.left - rootRect.left;
    var top = rect.top - rootRect.top;
    var bottom = rect.bottom - rootRect.bottom;
    var right = rect.right - rootRect.right;
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
        element.style.transform = "translate(".concat(translateX, "px, ").concat(translateY, "px)");
    }
}
exports.shiftAbsoluteElementIntoView = shiftAbsoluteElementIntoView;
function findRelativeZIndexParent(el) {
    var tmp = el;
    while (tmp && (tmp.style.zIndex === 'auto' || tmp.style.zIndex === '')) {
        tmp = tmp.parentElement;
    }
    return tmp;
}
exports.findRelativeZIndexParent = findRelativeZIndexParent;
