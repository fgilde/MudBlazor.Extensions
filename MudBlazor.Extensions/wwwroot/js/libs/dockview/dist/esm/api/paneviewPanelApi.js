import { Emitter } from '../events';
import { SplitviewPanelApiImpl } from './splitviewPanelApi';
export class PaneviewPanelApiImpl extends SplitviewPanelApiImpl {
    set pane(pane) {
        this._pane = pane;
    }
    constructor(id, component) {
        super(id, component);
        this._onDidExpansionChange = new Emitter({
            replay: true,
        });
        this.onDidExpansionChange = this._onDidExpansionChange.event;
        this._onMouseEnter = new Emitter({});
        this.onMouseEnter = this._onMouseEnter.event;
        this._onMouseLeave = new Emitter({});
        this.onMouseLeave = this._onMouseLeave.event;
        this.addDisposables(this._onDidExpansionChange, this._onMouseEnter, this._onMouseLeave);
    }
    setExpanded(isExpanded) {
        var _a;
        (_a = this._pane) === null || _a === void 0 ? void 0 : _a.setExpanded(isExpanded);
    }
    get isExpanded() {
        var _a;
        return !!((_a = this._pane) === null || _a === void 0 ? void 0 : _a.isExpanded());
    }
}
