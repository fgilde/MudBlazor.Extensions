import { addDisposableListener, Emitter } from '../../../events';
import { CompositeDisposable } from '../../../lifecycle';
import { getPanelData, LocalSelectionTransfer, PanelTransfer, } from '../../../dnd/dataTransfer';
import { toggleClass } from '../../../dom';
import { Droptarget, } from '../../../dnd/droptarget';
import { DragHandler } from '../../../dnd/abstractDragHandler';
import { addGhostImage } from '../../../dnd/ghost';
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
export class Tab extends CompositeDisposable {
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
