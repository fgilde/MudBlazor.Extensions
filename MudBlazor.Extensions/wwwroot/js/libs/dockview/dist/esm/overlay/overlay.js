import { disableIframePointEvents, quasiDefaultPrevented, toggleClass, } from '../dom';
import { Emitter, addDisposableListener, } from '../events';
import { CompositeDisposable, MutableDisposable } from '../lifecycle';
import { clamp } from '../math';
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
export class Overlay extends CompositeDisposable {
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
