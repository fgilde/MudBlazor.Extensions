import { toggleClass } from '../dom';
import { DockviewEvent, Emitter } from '../events';
import { CompositeDisposable } from '../lifecycle';
import { DragAndDropObserver } from './dnd';
import { clamp } from '../math';
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
export class WillShowOverlayEvent extends DockviewEvent {
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
export function directionToPosition(direction) {
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
export function positionToDirection(position) {
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
export class Droptarget extends CompositeDisposable {
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
                if (overrideTarget) {
                    //
                }
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
export function calculateQuadrantAsPercentage(overlayType, x, y, width, height, threshold) {
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
export function calculateQuadrantAsPixels(overlayType, x, y, width, height, threshold) {
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
