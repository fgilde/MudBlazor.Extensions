import { AcceptableEvent } from '../events';
export const PROPERTY_KEYS_PANEVIEW = (() => {
    /**
     * by readong the keys from an empty value object TypeScript will error
     * when we add or remove new properties to `DockviewOptions`
     */
    const properties = {
        disableAutoResizing: undefined,
        disableDnd: undefined,
        className: undefined,
    };
    return Object.keys(properties);
})();
export class PaneviewUnhandledDragOverEvent extends AcceptableEvent {
    constructor(nativeEvent, position, getData, panel) {
        super();
        this.nativeEvent = nativeEvent;
        this.position = position;
        this.getData = getData;
        this.panel = panel;
    }
}
