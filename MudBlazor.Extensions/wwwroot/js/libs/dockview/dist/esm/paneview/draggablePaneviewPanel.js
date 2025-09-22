import { PaneviewApi } from '../api/component.api';
import { DragHandler } from '../dnd/abstractDragHandler';
import { getPaneData, LocalSelectionTransfer, PaneTransfer, } from '../dnd/dataTransfer';
import { Droptarget } from '../dnd/droptarget';
import { Emitter } from '../events';
import { PaneviewUnhandledDragOverEvent, } from './options';
import { PaneviewPanel, } from './paneviewPanel';
export class DraggablePaneviewPanel extends PaneviewPanel {
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
