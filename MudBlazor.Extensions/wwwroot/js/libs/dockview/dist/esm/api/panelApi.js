import { DockviewEvent, Emitter } from '../events';
import { CompositeDisposable, MutableDisposable } from '../lifecycle';
export class WillFocusEvent extends DockviewEvent {
    constructor() {
        super();
    }
}
/**
 * A core api implementation that should be used across all panel-like objects
 */
export class PanelApiImpl extends CompositeDisposable {
    get isFocused() {
        return this._isFocused;
    }
    get isActive() {
        return this._isActive;
    }
    get isVisible() {
        return this._isVisible;
    }
    get width() {
        return this._width;
    }
    get height() {
        return this._height;
    }
    constructor(id, component) {
        super();
        this.id = id;
        this.component = component;
        this._isFocused = false;
        this._isActive = false;
        this._isVisible = true;
        this._width = 0;
        this._height = 0;
        this._parameters = {};
        this.panelUpdatesDisposable = new MutableDisposable();
        this._onDidDimensionChange = new Emitter();
        this.onDidDimensionsChange = this._onDidDimensionChange.event;
        this._onDidChangeFocus = new Emitter();
        this.onDidFocusChange = this._onDidChangeFocus.event;
        //
        this._onWillFocus = new Emitter();
        this.onWillFocus = this._onWillFocus.event;
        //
        this._onDidVisibilityChange = new Emitter();
        this.onDidVisibilityChange = this._onDidVisibilityChange.event;
        this._onWillVisibilityChange = new Emitter();
        this.onWillVisibilityChange = this._onWillVisibilityChange.event;
        this._onDidActiveChange = new Emitter();
        this.onDidActiveChange = this._onDidActiveChange.event;
        this._onActiveChange = new Emitter();
        this.onActiveChange = this._onActiveChange.event;
        this._onDidParametersChange = new Emitter();
        this.onDidParametersChange = this._onDidParametersChange.event;
        this.addDisposables(this.onDidFocusChange((event) => {
            this._isFocused = event.isFocused;
        }), this.onDidActiveChange((event) => {
            this._isActive = event.isActive;
        }), this.onDidVisibilityChange((event) => {
            this._isVisible = event.isVisible;
        }), this.onDidDimensionsChange((event) => {
            this._width = event.width;
            this._height = event.height;
        }), this.panelUpdatesDisposable, this._onDidDimensionChange, this._onDidChangeFocus, this._onDidVisibilityChange, this._onDidActiveChange, this._onWillFocus, this._onActiveChange, this._onWillFocus, this._onWillVisibilityChange, this._onDidParametersChange);
    }
    getParameters() {
        return this._parameters;
    }
    initialize(panel) {
        this.panelUpdatesDisposable.value = this._onDidParametersChange.event((parameters) => {
            this._parameters = parameters;
            panel.update({
                params: parameters,
            });
        });
    }
    setVisible(isVisible) {
        this._onWillVisibilityChange.fire({ isVisible });
    }
    setActive() {
        this._onActiveChange.fire();
    }
    updateParameters(parameters) {
        this._onDidParametersChange.fire(parameters);
    }
}
