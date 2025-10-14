import { DragAndDropObserver } from '../dnd/dnd';
import { getDomNodePagePosition, toggleClass } from '../dom';
import { CompositeDisposable, Disposable, MutableDisposable, } from '../lifecycle';
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
export class OverlayRenderContainer extends CompositeDisposable {
    constructor(element, accessor) {
        super();
        this.element = element;
        this.accessor = accessor;
        this.map = {};
        this._disposed = false;
        this.positionCache = new PositionCache();
        this.pendingUpdates = new Set();
        this.addDisposables(Disposable.from(() => {
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
                disposable: Disposable.NONE,
                destroy: Disposable.NONE,
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
                    observerDisposable.value = Disposable.from(() => observer.disconnect());
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
        this.map[panel.api.id].destroy = Disposable.from(() => {
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
