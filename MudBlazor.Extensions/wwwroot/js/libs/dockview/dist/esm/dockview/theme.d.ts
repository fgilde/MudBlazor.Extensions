export interface DockviewTheme {
    /**
     *  The name of the theme
     */
    name: string;
    /**
     * The class name to apply to the theme containing the CSS variables settings.
     */
    className: string;
    /**
     * The gap between the groups
     */
    gap?: number;
    /**
     * The mouting position of the overlay shown when dragging a panel. `absolute`
     * will mount the overlay to root of the dockview component whereas `relative` will mount the overlay to the group container.
     */
    dndOverlayMounting?: 'absolute' | 'relative';
    /**
     * When dragging a panel, the overlay can either encompass the panel contents or the entire group including the tab header space.
     */
    dndPanelOverlay?: 'content' | 'group';
}
export declare const themeDark: DockviewTheme;
export declare const themeLight: DockviewTheme;
export declare const themeVisualStudio: DockviewTheme;
export declare const themeAbyss: DockviewTheme;
export declare const themeDracula: DockviewTheme;
export declare const themeReplit: DockviewTheme;
export declare const themeAbyssSpaced: DockviewTheme;
export declare const themeLightSpaced: DockviewTheme;
