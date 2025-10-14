import { getPanelData } from '../../../dnd/dataTransfer';
import { Droptarget, } from '../../../dnd/droptarget';
import { GroupDragHandler } from '../../../dnd/groupDragHandler';
import { addDisposableListener, Emitter } from '../../../events';
import { CompositeDisposable } from '../../../lifecycle';
import { toggleClass } from '../../../dom';
export class VoidContainer extends CompositeDisposable {
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
