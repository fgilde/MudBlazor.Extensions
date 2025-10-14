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
exports.OverlayRenderContainer = void 0;
var dnd_1 = require("../dnd/dnd");
var dom_1 = require("../dom");
var lifecycle_1 = require("../lifecycle");
var PositionCache = /** @class */ (function () {
    function PositionCache() {
        this.cache = new Map();
        this.currentFrameId = 0;
        this.rafId = null;
    }
    PositionCache.prototype.getPosition = function (element) {
        var cached = this.cache.get(element);
        if (cached && cached.frameId === this.currentFrameId) {
            return cached.rect;
        }
        this.scheduleFrameUpdate();
        var rect = (0, dom_1.getDomNodePagePosition)(element);
        this.cache.set(element, { rect: rect, frameId: this.currentFrameId });
        return rect;
    };
    PositionCache.prototype.invalidate = function () {
        this.currentFrameId++;
    };
    PositionCache.prototype.scheduleFrameUpdate = function () {
        var _this = this;
        if (this.rafId)
            return;
        this.rafId = requestAnimationFrame(function () {
            _this.currentFrameId++;
            _this.rafId = null;
        });
    };
    return PositionCache;
}());
function createFocusableElement() {
    var element = document.createElement('div');
    element.tabIndex = -1;
    return element;
}
var OverlayRenderContainer = /** @class */ (function (_super) {
    __extends(OverlayRenderContainer, _super);
    function OverlayRenderContainer(element, accessor) {
        var _this = _super.call(this) || this;
        _this.element = element;
        _this.accessor = accessor;
        _this.map = {};
        _this._disposed = false;
        _this.positionCache = new PositionCache();
        _this.pendingUpdates = new Set();
        _this.addDisposables(lifecycle_1.Disposable.from(function () {
            var e_1, _a;
            try {
                for (var _b = __values(Object.values(_this.map)), _c = _b.next(); !_c.done; _c = _b.next()) {
                    var value = _c.value;
                    value.disposable.dispose();
                    value.destroy.dispose();
                }
            }
            catch (e_1_1) { e_1 = { error: e_1_1 }; }
            finally {
                try {
                    if (_c && !_c.done && (_a = _b.return)) _a.call(_b);
                }
                finally { if (e_1) throw e_1.error; }
            }
            _this._disposed = true;
        }));
        return _this;
    }
    OverlayRenderContainer.prototype.updateAllPositions = function () {
        var e_2, _a;
        if (this._disposed) {
            return;
        }
        // Invalidate position cache to force recalculation
        this.positionCache.invalidate();
        try {
            // Call resize function directly for all visible panels
            for (var _b = __values(Object.values(this.map)), _c = _b.next(); !_c.done; _c = _b.next()) {
                var entry = _c.value;
                if (entry.panel.api.isVisible && entry.resize) {
                    entry.resize();
                }
            }
        }
        catch (e_2_1) { e_2 = { error: e_2_1 }; }
        finally {
            try {
                if (_c && !_c.done && (_a = _b.return)) _a.call(_b);
            }
            finally { if (e_2) throw e_2.error; }
        }
    };
    OverlayRenderContainer.prototype.detatch = function (panel) {
        if (this.map[panel.api.id]) {
            var _a = this.map[panel.api.id], disposable = _a.disposable, destroy = _a.destroy;
            disposable.dispose();
            destroy.dispose();
            delete this.map[panel.api.id];
            return true;
        }
        return false;
    };
    OverlayRenderContainer.prototype.attach = function (options) {
        var _this = this;
        var panel = options.panel, referenceContainer = options.referenceContainer;
        if (!this.map[panel.api.id]) {
            var element = createFocusableElement();
            element.className = 'dv-render-overlay';
            this.map[panel.api.id] = {
                panel: panel,
                disposable: lifecycle_1.Disposable.NONE,
                destroy: lifecycle_1.Disposable.NONE,
                element: element,
            };
        }
        var focusContainer = this.map[panel.api.id].element;
        if (panel.view.content.element.parentElement !== focusContainer) {
            focusContainer.appendChild(panel.view.content.element);
        }
        if (focusContainer.parentElement !== this.element) {
            this.element.appendChild(focusContainer);
        }
        var resize = function () {
            var panelId = panel.api.id;
            if (_this.pendingUpdates.has(panelId)) {
                return; // Update already scheduled
            }
            _this.pendingUpdates.add(panelId);
            requestAnimationFrame(function () {
                _this.pendingUpdates.delete(panelId);
                if (_this.isDisposed || !_this.map[panelId]) {
                    return;
                }
                var box = _this.positionCache.getPosition(referenceContainer.element);
                var box2 = _this.positionCache.getPosition(_this.element);
                // Use traditional positioning for overlay containers
                var left = box.left - box2.left;
                var top = box.top - box2.top;
                var width = box.width;
                var height = box.height;
                focusContainer.style.left = "".concat(left, "px");
                focusContainer.style.top = "".concat(top, "px");
                focusContainer.style.width = "".concat(width, "px");
                focusContainer.style.height = "".concat(height, "px");
                (0, dom_1.toggleClass)(focusContainer, 'dv-render-overlay-float', panel.group.api.location.type === 'floating');
            });
        };
        var visibilityChanged = function () {
            if (panel.api.isVisible) {
                _this.positionCache.invalidate();
                resize();
            }
            focusContainer.style.display = panel.api.isVisible ? '' : 'none';
        };
        var observerDisposable = new lifecycle_1.MutableDisposable();
        var correctLayerPosition = function () {
            if (panel.api.location.type === 'floating') {
                queueMicrotask(function () {
                    var floatingGroup = _this.accessor.floatingGroups.find(function (group) { return group.group === panel.api.group; });
                    if (!floatingGroup) {
                        return;
                    }
                    var element = floatingGroup.overlay.element;
                    var update = function () {
                        var level = Number(element.getAttribute('aria-level'));
                        focusContainer.style.zIndex = "calc(var(--dv-overlay-z-index, 999) + ".concat(level * 2 + 1, ")");
                    };
                    var observer = new MutationObserver(function () {
                        update();
                    });
                    observerDisposable.value = lifecycle_1.Disposable.from(function () {
                        return observer.disconnect();
                    });
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
        var disposable = new lifecycle_1.CompositeDisposable(observerDisposable, 
        /**
         * since container is positioned absoutely we must explicitly forward
         * the dnd events for the expect behaviours to continue to occur in terms of dnd
         *
         * the dnd observer does not need to be conditional on whether the panel is visible since
         * non-visible panels are 'display: none' and in such case the dnd observer will not fire.
         */
        new dnd_1.DragAndDropObserver(focusContainer, {
            onDragEnd: function (e) {
                referenceContainer.dropTarget.dnd.onDragEnd(e);
            },
            onDragEnter: function (e) {
                referenceContainer.dropTarget.dnd.onDragEnter(e);
            },
            onDragLeave: function (e) {
                referenceContainer.dropTarget.dnd.onDragLeave(e);
            },
            onDrop: function (e) {
                referenceContainer.dropTarget.dnd.onDrop(e);
            },
            onDragOver: function (e) {
                referenceContainer.dropTarget.dnd.onDragOver(e);
            },
        }), panel.api.onDidVisibilityChange(function () {
            /**
             * Control the visibility of the content, however even when not visible (display: none)
             * the content is still maintained within the DOM hence DOM specific attributes
             * such as scroll position are maintained when next made visible.
             */
            visibilityChanged();
        }), panel.api.onDidDimensionsChange(function () {
            if (!panel.api.isVisible) {
                return;
            }
            resize();
        }), panel.api.onDidLocationChange(function () {
            correctLayerPosition();
        }));
        this.map[panel.api.id].destroy = lifecycle_1.Disposable.from(function () {
            var _a;
            if (panel.view.content.element.parentElement === focusContainer) {
                focusContainer.removeChild(panel.view.content.element);
            }
            (_a = focusContainer.parentElement) === null || _a === void 0 ? void 0 : _a.removeChild(focusContainer);
        });
        correctLayerPosition();
        queueMicrotask(function () {
            if (_this.isDisposed) {
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
    };
    return OverlayRenderContainer;
}(lifecycle_1.CompositeDisposable));
exports.OverlayRenderContainer = OverlayRenderContainer;
