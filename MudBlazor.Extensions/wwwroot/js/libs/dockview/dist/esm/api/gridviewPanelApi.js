import { Emitter } from '../events';
import { PanelApiImpl } from './panelApi';
export class GridviewPanelApiImpl extends PanelApiImpl {
    constructor(id, component, panel) {
        super(id, component);
        this._onDidConstraintsChangeInternal = new Emitter();
        this.onDidConstraintsChangeInternal = this._onDidConstraintsChangeInternal.event;
        this._onDidConstraintsChange = new Emitter();
        this.onDidConstraintsChange = this._onDidConstraintsChange.event;
        this._onDidSizeChange = new Emitter();
        this.onDidSizeChange = this._onDidSizeChange.event;
        this.addDisposables(this._onDidConstraintsChangeInternal, this._onDidConstraintsChange, this._onDidSizeChange);
        if (panel) {
            this.initialize(panel);
        }
    }
    setConstraints(value) {
        this._onDidConstraintsChangeInternal.fire(value);
    }
    setSize(event) {
        this._onDidSizeChange.fire(event);
    }
}
