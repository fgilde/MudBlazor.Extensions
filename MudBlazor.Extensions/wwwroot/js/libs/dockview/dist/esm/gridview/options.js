export const PROPERTY_KEYS_GRIDVIEW = (() => {
    /**
     * by readong the keys from an empty value object TypeScript will error
     * when we add or remove new properties to `DockviewOptions`
     */
    const properties = {
        disableAutoResizing: undefined,
        proportionalLayout: undefined,
        orientation: undefined,
        hideBorders: undefined,
        className: undefined,
    };
    return Object.keys(properties);
})();
