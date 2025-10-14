import { Emitter, addDisposableListener, } from './events';
import { CompositeDisposable } from './lifecycle';
export class OverflowObserver extends CompositeDisposable {
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
export function watchElementResize(element, cb) {
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
export const removeClasses = (element, ...classes) => {
    for (const classname of classes) {
        if (element.classList.contains(classname)) {
            element.classList.remove(classname);
        }
    }
};
export const addClasses = (element, ...classes) => {
    for (const classname of classes) {
        if (!element.classList.contains(classname)) {
            element.classList.add(classname);
        }
    }
};
export const toggleClass = (element, className, isToggled) => {
    const hasClass = element.classList.contains(className);
    if (isToggled && !hasClass) {
        element.classList.add(className);
    }
    if (!isToggled && hasClass) {
        element.classList.remove(className);
    }
};
export function isAncestor(testChild, testAncestor) {
    while (testChild) {
        if (testChild === testAncestor) {
            return true;
        }
        testChild = testChild.parentNode;
    }
    return false;
}
export function getElementsByTagName(tag, document) {
    return Array.prototype.slice.call(document.querySelectorAll(tag), 0);
}
export function trackFocus(element) {
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
export function quasiPreventDefault(event) {
    event[QUASI_PREVENT_DEFAULT_KEY] = true;
}
// check if this event has been marked
export function quasiDefaultPrevented(event) {
    return event[QUASI_PREVENT_DEFAULT_KEY];
}
export function addStyles(document, styleSheetList) {
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
export function getDomNodePagePosition(domNode) {
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
export function isInDocument(element) {
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
export function addTestId(element, id) {
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
export function disableIframePointEvents(rootNode = document) {
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
export function getDockviewTheme(element) {
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
export class Classnames {
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
export function isChildEntirelyVisibleWithinParent(child, parent) {
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
export function onDidWindowMoveEnd(window) {
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
export function onDidWindowResizeEnd(element, cb) {
    let resizeTimeout;
    const disposable = new CompositeDisposable(addDisposableListener(element, 'resize', () => {
        clearTimeout(resizeTimeout);
        resizeTimeout = setTimeout(() => {
            cb();
        }, DEBOUCE_DELAY);
    }));
    return disposable;
}
export function shiftAbsoluteElementIntoView(element, root, options = { buffer: 10 }) {
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
export function findRelativeZIndexParent(el) {
    let tmp = el;
    while (tmp && (tmp.style.zIndex === 'auto' || tmp.style.zIndex === '')) {
        tmp = tmp.parentElement;
    }
    return tmp;
}
