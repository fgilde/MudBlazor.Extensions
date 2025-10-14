class TransferObject {
}
export class PanelTransfer extends TransferObject {
    constructor(viewId, groupId, panelId) {
        super();
        this.viewId = viewId;
        this.groupId = groupId;
        this.panelId = panelId;
    }
}
export class PaneTransfer extends TransferObject {
    constructor(viewId, paneId) {
        super();
        this.viewId = viewId;
        this.paneId = paneId;
    }
}
/**
 * A singleton to store transfer data during drag & drop operations that are only valid within the application.
 */
export class LocalSelectionTransfer {
    constructor() {
        // protect against external instantiation
    }
    static getInstance() {
        return LocalSelectionTransfer.INSTANCE;
    }
    hasData(proto) {
        return proto && proto === this.proto;
    }
    clearData(proto) {
        if (this.hasData(proto)) {
            this.proto = undefined;
            this.data = undefined;
        }
    }
    getData(proto) {
        if (this.hasData(proto)) {
            return this.data;
        }
        return undefined;
    }
    setData(data, proto) {
        if (proto) {
            this.data = data;
            this.proto = proto;
        }
    }
}
LocalSelectionTransfer.INSTANCE = new LocalSelectionTransfer();
export function getPanelData() {
    const panelTransfer = LocalSelectionTransfer.getInstance();
    const isPanelEvent = panelTransfer.hasData(PanelTransfer.prototype);
    if (!isPanelEvent) {
        return undefined;
    }
    return panelTransfer.getData(PanelTransfer.prototype)[0];
}
export function getPaneData() {
    const paneTransfer = LocalSelectionTransfer.getInstance();
    const isPanelEvent = paneTransfer.hasData(PaneTransfer.prototype);
    if (!isPanelEvent) {
        return undefined;
    }
    return paneTransfer.getData(PaneTransfer.prototype)[0];
}
