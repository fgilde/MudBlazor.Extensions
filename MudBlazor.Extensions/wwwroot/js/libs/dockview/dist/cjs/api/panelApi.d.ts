import { DockviewEvent, Emitter, Event } from '../events';
import { CompositeDisposable } from '../lifecycle';
import { IPanel, Parameters } from '../panel/types';
export interface FocusEvent {
    readonly isFocused: boolean;
}
export interface PanelDimensionChangeEvent {
    readonly width: number;
    readonly height: number;
}
export interface VisibilityEvent {
    readonly isVisible: boolean;
}
export interface ActiveEvent {
    readonly isActive: boolean;
}
export interface PanelApi {
    readonly onDidDimensionsChange: Event<PanelDimensionChangeEvent>;
    readonly onDidFocusChange: Event<FocusEvent>;
    readonly onDidVisibilityChange: Event<VisibilityEvent>;
    readonly onDidActiveChange: Event<ActiveEvent>;
    readonly onDidParametersChange: Event<Parameters>;
    setActive(): void;
    setVisible(isVisible: boolean): void;
    updateParameters(parameters: Parameters): void;
    /**
     * The id of the component renderer
     */
    readonly component: string;
    /**
     * The id of the panel that would have been assigned when the panel was created
     */
    readonly id: string;
    /**
     * Whether the panel holds the current focus
     */
    readonly isFocused: boolean;
    /**
     * Whether the panel is the actively selected panel
     */
    readonly isActive: boolean;
    /**
     * Whether the panel is visible
     */
    readonly isVisible: boolean;
    /**
     * The panel width in pixels
     */
    readonly width: number;
    /**
     * The panel height in pixels
     */
    readonly height: number;
    readonly onWillFocus: Event<WillFocusEvent>;
    getParameters<T extends Parameters = Parameters>(): T;
}
export declare class WillFocusEvent extends DockviewEvent {
    constructor();
}
/**
 * A core api implementation that should be used across all panel-like objects
 */
export declare class PanelApiImpl extends CompositeDisposable implements PanelApi {
    readonly id: string;
    readonly component: string;
    private _isFocused;
    private _isActive;
    private _isVisible;
    private _width;
    private _height;
    private _parameters;
    private readonly panelUpdatesDisposable;
    readonly _onDidDimensionChange: Emitter<PanelDimensionChangeEvent>;
    readonly onDidDimensionsChange: Event<PanelDimensionChangeEvent>;
    readonly _onDidChangeFocus: Emitter<FocusEvent>;
    readonly onDidFocusChange: Event<FocusEvent>;
    readonly _onWillFocus: Emitter<WillFocusEvent>;
    readonly onWillFocus: Event<WillFocusEvent>;
    readonly _onDidVisibilityChange: Emitter<VisibilityEvent>;
    readonly onDidVisibilityChange: Event<VisibilityEvent>;
    readonly _onWillVisibilityChange: Emitter<VisibilityEvent>;
    readonly onWillVisibilityChange: Event<VisibilityEvent>;
    readonly _onDidActiveChange: Emitter<ActiveEvent>;
    readonly onDidActiveChange: Event<ActiveEvent>;
    readonly _onActiveChange: Emitter<void>;
    readonly onActiveChange: Event<void>;
    readonly _onDidParametersChange: Emitter<Parameters>;
    readonly onDidParametersChange: Event<Parameters>;
    get isFocused(): boolean;
    get isActive(): boolean;
    get isVisible(): boolean;
    get width(): number;
    get height(): number;
    constructor(id: string, component: string);
    getParameters<T extends Parameters = Parameters>(): T;
    initialize(panel: IPanel): void;
    setVisible(isVisible: boolean): void;
    setActive(): void;
    updateParameters(parameters: Parameters): void;
}
