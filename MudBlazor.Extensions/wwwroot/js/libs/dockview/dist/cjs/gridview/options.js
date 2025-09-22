"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.PROPERTY_KEYS_GRIDVIEW = void 0;
exports.PROPERTY_KEYS_GRIDVIEW = (function () {
    /**
     * by readong the keys from an empty value object TypeScript will error
     * when we add or remove new properties to `DockviewOptions`
     */
    var properties = {
        disableAutoResizing: undefined,
        proportionalLayout: undefined,
        orientation: undefined,
        hideBorders: undefined,
        className: undefined,
    };
    return Object.keys(properties);
})();
