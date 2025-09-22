"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.PROPERTY_KEYS_SPLITVIEW = void 0;
exports.PROPERTY_KEYS_SPLITVIEW = (function () {
    /**
     * by readong the keys from an empty value object TypeScript will error
     * when we add or remove new properties to `DockviewOptions`
     */
    var properties = {
        orientation: undefined,
        descriptor: undefined,
        proportionalLayout: undefined,
        styles: undefined,
        margin: undefined,
        disableAutoResizing: undefined,
        className: undefined,
    };
    return Object.keys(properties);
})();
