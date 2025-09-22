export const PROPERTY_KEYS_SPLITVIEW = (() => {
    /**
     * by readong the keys from an empty value object TypeScript will error
     * when we add or remove new properties to `DockviewOptions`
     */
    const properties = {
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
