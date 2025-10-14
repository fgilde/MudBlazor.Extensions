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
Object.defineProperty(exports, "__esModule", { value: true });
exports.calculateQuadrantAsPixels = exports.calculateQuadrantAsPercentage = exports.Droptarget = exports.positionToDirection = exports.directionToPosition = exports.WillShowOverlayEvent = void 0;
var dom_1 = require("../dom");
var events_1 = require("../events");
var lifecycle_1 = require("../lifecycle");
var dnd_1 = require("./dnd");
var math_1 = require("../math");
function setGPUOptimizedBounds(element, bounds) {
    var top = bounds.top, left = bounds.left, width = bounds.width, height = bounds.height;
    var topPx = "".concat(Math.round(top), "px");
    var leftPx = "".concat(Math.round(left), "px");
    var widthPx = "".concat(Math.round(width), "px");
    var heightPx = "".concat(Math.round(height), "px");
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
    var top = bounds.top, left = bounds.left, width = bounds.width, height = bounds.height;
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
    var top = bounds.top, left = bounds.left, width = bounds.width, height = bounds.height;
    var topPx = "".concat(Math.round(top), "px");
    var leftPx = "".concat(Math.round(left), "px");
    var widthPx = "".concat(Math.round(width), "px");
    var heightPx = "".concat(Math.round(height), "px");
    // Check if position or size changed (back to traditional method)
    return element.style.top !== topPx ||
        element.style.left !== leftPx ||
        element.style.width !== widthPx ||
        element.style.height !== heightPx;
}
var WillShowOverlayEvent = /** @class */ (function (_super) {
    __extends(WillShowOverlayEvent, _super);
    function WillShowOverlayEvent(options) {
        var _this = _super.call(this) || this;
        _this.options = options;
        return _this;
    }
    Object.defineProperty(WillShowOverlayEvent.prototype, "nativeEvent", {
        get: function () {
            return this.options.nativeEvent;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(WillShowOverlayEvent.prototype, "position", {
        get: function () {
            return this.options.position;
        },
        enumerable: false,
        configurable: true
    });
    return WillShowOverlayEvent;
}(events_1.DockviewEvent));
exports.WillShowOverlayEvent = WillShowOverlayEvent;
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
            throw new Error("invalid direction '".concat(direction, "'"));
    }
}
exports.directionToPosition = directionToPosition;
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
            throw new Error("invalid position '".concat(position, "'"));
    }
}
exports.positionToDirection = positionToDirection;
var DEFAULT_ACTIVATION_SIZE = {
    value: 20,
    type: 'percentage',
};
var DEFAULT_SIZE = {
    value: 50,
    type: 'percentage',
};
var SMALL_WIDTH_BOUNDARY = 100;
var SMALL_HEIGHT_BOUNDARY = 100;
var Droptarget = /** @class */ (function (_super) {
    __extends(Droptarget, _super);
    function Droptarget(element, options) {
        var _this = _super.call(this) || this;
        _this.element = element;
        _this.options = options;
        _this._onDrop = new events_1.Emitter();
        _this.onDrop = _this._onDrop.event;
        _this._onWillShowOverlay = new events_1.Emitter();
        _this.onWillShowOverlay = _this._onWillShowOverlay.event;
        _this._disabled = false;
        // use a set to take advantage of #<set>.has
        _this._acceptedTargetZonesSet = new Set(_this.options.acceptedTargetZones);
        _this.dnd = new dnd_1.DragAndDropObserver(_this.element, {
            onDragEnter: function () {
                var _a, _b, _c;
                (_c = (_b = (_a = _this.options).getOverrideTarget) === null || _b === void 0 ? void 0 : _b.call(_a)) === null || _c === void 0 ? void 0 : _c.getElements();
            },
            onDragOver: function (e) {
                var _a, _b, _c, _d, _e, _f, _g;
                Droptarget.ACTUAL_TARGET = _this;
                var overrideTarget = (_b = (_a = _this.options).getOverrideTarget) === null || _b === void 0 ? void 0 : _b.call(_a);
                if (_this._acceptedTargetZonesSet.size === 0) {
                    if (overrideTarget) {
                        return;
                    }
                    _this.removeDropTarget();
                    return;
                }
                var target = (_e = (_d = (_c = _this.options).getOverlayOutline) === null || _d === void 0 ? void 0 : _d.call(_c)) !== null && _e !== void 0 ? _e : _this.element;
                var width = target.offsetWidth;
                var height = target.offsetHeight;
                if (width === 0 || height === 0) {
                    return; // avoid div!0
                }
                var rect = e.currentTarget.getBoundingClientRect();
                var x = ((_f = e.clientX) !== null && _f !== void 0 ? _f : 0) - rect.left;
                var y = ((_g = e.clientY) !== null && _g !== void 0 ? _g : 0) - rect.top;
                var quadrant = _this.calculateQuadrant(_this._acceptedTargetZonesSet, x, y, width, height);
                /**
                 * If the event has already been used by another DropTarget instance
                 * then don't show a second drop target, only one target should be
                 * active at any one time
                 */
                if (_this.isAlreadyUsed(e) || quadrant === null) {
                    // no drop target should be displayed
                    _this.removeDropTarget();
                    return;
                }
                if (!_this.options.canDisplayOverlay(e, quadrant)) {
                    if (overrideTarget) {
                        return;
                    }
                    _this.removeDropTarget();
                    return;
                }
                var willShowOverlayEvent = new WillShowOverlayEvent({
                    nativeEvent: e,
                    position: quadrant,
                });
                /**
                 * Provide an opportunity to prevent the overlay appearing and in turn
                 * any dnd behaviours
                 */
                _this._onWillShowOverlay.fire(willShowOverlayEvent);
                if (willShowOverlayEvent.defaultPrevented) {
                    _this.removeDropTarget();
                    return;
                }
                _this.markAsUsed(e);
                if (overrideTarget) {
                    //
                }
                else if (!_this.targetElement) {
                    _this.targetElement = document.createElement('div');
                    _this.targetElement.className = 'dv-drop-target-dropzone';
                    _this.overlayElement = document.createElement('div');
                    _this.overlayElement.className = 'dv-drop-target-selection';
                    _this._state = 'center';
                    _this.targetElement.appendChild(_this.overlayElement);
                    target.classList.add('dv-drop-target');
                    target.append(_this.targetElement);
                    // this.overlayElement.style.opacity = '0';
                    // requestAnimationFrame(() => {
                    //     if (this.overlayElement) {
                    //         this.overlayElement.style.opacity = '';
                    //     }
                    // });
                }
                _this.toggleClasses(quadrant, width, height);
                _this._state = quadrant;
            },
            onDragLeave: function () {
                var _a, _b;
                var target = (_b = (_a = _this.options).getOverrideTarget) === null || _b === void 0 ? void 0 : _b.call(_a);
                if (target) {
                    return;
                }
                _this.removeDropTarget();
            },
            onDragEnd: function (e) {
                var _a, _b;
                var target = (_b = (_a = _this.options).getOverrideTarget) === null || _b === void 0 ? void 0 : _b.call(_a);
                if (target && Droptarget.ACTUAL_TARGET === _this) {
                    if (_this._state) {
                        // only stop the propagation of the event if we are dealing with it
                        // which is only when the target has state
                        e.stopPropagation();
                        _this._onDrop.fire({
                            position: _this._state,
                            nativeEvent: e,
                        });
                    }
                }
                _this.removeDropTarget();
                target === null || target === void 0 ? void 0 : target.clear();
            },
            onDrop: function (e) {
                var _a, _b, _c;
                e.preventDefault();
                var state = _this._state;
                _this.removeDropTarget();
                (_c = (_b = (_a = _this.options).getOverrideTarget) === null || _b === void 0 ? void 0 : _b.call(_a)) === null || _c === void 0 ? void 0 : _c.clear();
                if (state) {
                    // only stop the propagation of the event if we are dealing with it
                    // which is only when the target has state
                    e.stopPropagation();
                    _this._onDrop.fire({ position: state, nativeEvent: e });
                }
            },
        });
        _this.addDisposables(_this._onDrop, _this._onWillShowOverlay, _this.dnd);
        return _this;
    }
    Object.defineProperty(Droptarget.prototype, "disabled", {
        get: function () {
            return this._disabled;
        },
        set: function (value) {
            this._disabled = value;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(Droptarget.prototype, "state", {
        get: function () {
            return this._state;
        },
        enumerable: false,
        configurable: true
    });
    Droptarget.prototype.setTargetZones = function (acceptedTargetZones) {
        this._acceptedTargetZonesSet = new Set(acceptedTargetZones);
    };
    Droptarget.prototype.setOverlayModel = function (model) {
        this.options.overlayModel = model;
    };
    Droptarget.prototype.dispose = function () {
        this.removeDropTarget();
        _super.prototype.dispose.call(this);
    };
    /**
     * Add a property to the event object for other potential listeners to check
     */
    Droptarget.prototype.markAsUsed = function (event) {
        event[Droptarget.USED_EVENT_ID] = true;
    };
    /**
     * Check is the event has already been used by another instance of DropTarget
     */
    Droptarget.prototype.isAlreadyUsed = function (event) {
        var value = event[Droptarget.USED_EVENT_ID];
        return typeof value === 'boolean' && value;
    };
    Droptarget.prototype.toggleClasses = function (quadrant, width, height) {
        var _a, _b, _c, _d, _e, _f, _g;
        var target = (_b = (_a = this.options).getOverrideTarget) === null || _b === void 0 ? void 0 : _b.call(_a);
        if (!target && !this.overlayElement) {
            return;
        }
        var isSmallX = width < SMALL_WIDTH_BOUNDARY;
        var isSmallY = height < SMALL_HEIGHT_BOUNDARY;
        var isLeft = quadrant === 'left';
        var isRight = quadrant === 'right';
        var isTop = quadrant === 'top';
        var isBottom = quadrant === 'bottom';
        var rightClass = !isSmallX && isRight;
        var leftClass = !isSmallX && isLeft;
        var topClass = !isSmallY && isTop;
        var bottomClass = !isSmallY && isBottom;
        var size = 1;
        var sizeOptions = (_d = (_c = this.options.overlayModel) === null || _c === void 0 ? void 0 : _c.size) !== null && _d !== void 0 ? _d : DEFAULT_SIZE;
        if (sizeOptions.type === 'percentage') {
            size = (0, math_1.clamp)(sizeOptions.value, 0, 100) / 100;
        }
        else {
            if (rightClass || leftClass) {
                size = (0, math_1.clamp)(0, sizeOptions.value, width) / width;
            }
            if (topClass || bottomClass) {
                size = (0, math_1.clamp)(0, sizeOptions.value, height) / height;
            }
        }
        if (target) {
            var outlineEl = (_g = (_f = (_e = this.options).getOverlayOutline) === null || _f === void 0 ? void 0 : _f.call(_e)) !== null && _g !== void 0 ? _g : this.element;
            var elBox = outlineEl.getBoundingClientRect();
            var ta = target.getElements(undefined, outlineEl);
            var el = ta.root;
            var overlay_1 = ta.overlay;
            var bigbox = el.getBoundingClientRect();
            var rootTop = elBox.top - bigbox.top;
            var rootLeft = elBox.left - bigbox.left;
            var box_1 = {
                top: rootTop,
                left: rootLeft,
                width: width,
                height: height,
            };
            if (rightClass) {
                box_1.left = rootLeft + width * (1 - size);
                box_1.width = width * size;
            }
            else if (leftClass) {
                box_1.width = width * size;
            }
            else if (topClass) {
                box_1.height = height * size;
            }
            else if (bottomClass) {
                box_1.top = rootTop + height * (1 - size);
                box_1.height = height * size;
            }
            if (isSmallX && isLeft) {
                box_1.width = 4;
            }
            if (isSmallX && isRight) {
                box_1.left = rootLeft + width - 4;
                box_1.width = 4;
            }
            // Use GPU-optimized bounds checking and setting
            if (!checkBoundsChanged(overlay_1, box_1)) {
                return;
            }
            setGPUOptimizedBounds(overlay_1, box_1);
            overlay_1.className = "dv-drop-target-anchor".concat(this.options.className ? " ".concat(this.options.className) : '');
            (0, dom_1.toggleClass)(overlay_1, 'dv-drop-target-left', isLeft);
            (0, dom_1.toggleClass)(overlay_1, 'dv-drop-target-right', isRight);
            (0, dom_1.toggleClass)(overlay_1, 'dv-drop-target-top', isTop);
            (0, dom_1.toggleClass)(overlay_1, 'dv-drop-target-bottom', isBottom);
            (0, dom_1.toggleClass)(overlay_1, 'dv-drop-target-center', quadrant === 'center');
            if (ta.changed) {
                (0, dom_1.toggleClass)(overlay_1, 'dv-drop-target-anchor-container-changed', true);
                setTimeout(function () {
                    (0, dom_1.toggleClass)(overlay_1, 'dv-drop-target-anchor-container-changed', false);
                }, 10);
            }
            return;
        }
        if (!this.overlayElement) {
            return;
        }
        var box = { top: '0px', left: '0px', width: '100%', height: '100%' };
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
            box.left = "".concat(100 * (1 - size), "%");
            box.width = "".concat(100 * size, "%");
        }
        else if (leftClass) {
            box.width = "".concat(100 * size, "%");
        }
        else if (topClass) {
            box.height = "".concat(100 * size, "%");
        }
        else if (bottomClass) {
            box.top = "".concat(100 * (1 - size), "%");
            box.height = "".concat(100 * size, "%");
        }
        setGPUOptimizedBoundsFromStrings(this.overlayElement, box);
        (0, dom_1.toggleClass)(this.overlayElement, 'dv-drop-target-small-vertical', isSmallY);
        (0, dom_1.toggleClass)(this.overlayElement, 'dv-drop-target-small-horizontal', isSmallX);
        (0, dom_1.toggleClass)(this.overlayElement, 'dv-drop-target-left', isLeft);
        (0, dom_1.toggleClass)(this.overlayElement, 'dv-drop-target-right', isRight);
        (0, dom_1.toggleClass)(this.overlayElement, 'dv-drop-target-top', isTop);
        (0, dom_1.toggleClass)(this.overlayElement, 'dv-drop-target-bottom', isBottom);
        (0, dom_1.toggleClass)(this.overlayElement, 'dv-drop-target-center', quadrant === 'center');
    };
    Droptarget.prototype.calculateQuadrant = function (overlayType, x, y, width, height) {
        var _a, _b;
        var activationSizeOptions = (_b = (_a = this.options.overlayModel) === null || _a === void 0 ? void 0 : _a.activationSize) !== null && _b !== void 0 ? _b : DEFAULT_ACTIVATION_SIZE;
        var isPercentage = activationSizeOptions.type === 'percentage';
        if (isPercentage) {
            return calculateQuadrantAsPercentage(overlayType, x, y, width, height, activationSizeOptions.value);
        }
        return calculateQuadrantAsPixels(overlayType, x, y, width, height, activationSizeOptions.value);
    };
    Droptarget.prototype.removeDropTarget = function () {
        var _a;
        if (this.targetElement) {
            this._state = undefined;
            (_a = this.targetElement.parentElement) === null || _a === void 0 ? void 0 : _a.classList.remove('dv-drop-target');
            this.targetElement.remove();
            this.targetElement = undefined;
            this.overlayElement = undefined;
        }
    };
    Droptarget.USED_EVENT_ID = '__dockview_droptarget_event_is_used__';
    return Droptarget;
}(lifecycle_1.CompositeDisposable));
exports.Droptarget = Droptarget;
function calculateQuadrantAsPercentage(overlayType, x, y, width, height, threshold) {
    var xp = (100 * x) / width;
    var yp = (100 * y) / height;
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
exports.calculateQuadrantAsPercentage = calculateQuadrantAsPercentage;
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
exports.calculateQuadrantAsPixels = calculateQuadrantAsPixels;
