import { AcceptableEvent } from '../events';
export class DockviewUnhandledDragOverEvent extends AcceptableEvent {
    constructor(nativeEvent, target, position, getData, group) {
        super();
        this.nativeEvent = nativeEvent;
        this.target = target;
        this.position = position;
        this.getData = getData;
        this.group = group;
    }
}
export const PROPERTY_KEYS_DOCKVIEW = (() => {
    /**
     * by readong the keys from an empty value object TypeScript will error
     * when we add or remove new properties to `DockviewOptions`
     */
    const properties = {
        disableAutoResizing: undefined,
        hideBorders: undefined,
        singleTabMode: undefined,
        disableFloatingGroups: undefined,
        floatingGroupBounds: undefined,
        popoutUrl: undefined,
        defaultRenderer: undefined,
        debug: undefined,
        rootOverlayModel: undefined,
        locked: undefined,
        disableDnd: undefined,
        className: undefined,
        noPanelsOverlay: undefined,
        dndEdges: undefined,
        theme: undefined,
        disableTabsOverflowList: undefined,
        scrollbars: undefined,
    };
    return Object.keys(properties);
})();
export function isPanelOptionsWithPanel(data) {
    if (data.referencePanel) {
        return true;
    }
    return false;
}
export function isPanelOptionsWithGroup(data) {
    if (data.referenceGroup) {
        return true;
    }
    return false;
}
export function isGroupOptionsWithPanel(data) {
    if (data.referencePanel) {
        return true;
    }
    return false;
}
export function isGroupOptionsWithGroup(data) {
    if (data.referenceGroup) {
        return true;
    }
    return false;
}
