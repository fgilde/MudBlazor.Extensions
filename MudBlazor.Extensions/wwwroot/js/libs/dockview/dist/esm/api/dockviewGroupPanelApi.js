import { positionToDirection } from '../dnd/droptarget';
import { Emitter } from '../events';
import { GridviewPanelApiImpl } from './gridviewPanelApi';
const NOT_INITIALIZED_MESSAGE = 'dockview: DockviewGroupPanelApiImpl not initialized';
export class DockviewGroupPanelApiImpl extends GridviewPanelApiImpl {
    get location() {
        if (!this._group) {
            throw new Error(NOT_INITIALIZED_MESSAGE);
        }
        return this._group.model.location;
    }
    constructor(id, accessor) {
        super(id, '__dockviewgroup__');
        this.accessor = accessor;
        this._onDidLocationChange = new Emitter();
        this.onDidLocationChange = this._onDidLocationChange.event;
        this._onDidActivePanelChange = new Emitter();
        this.onDidActivePanelChange = this._onDidActivePanelChange.event;
        this.addDisposables(this._onDidLocationChange, this._onDidActivePanelChange);
    }
    close() {
        if (!this._group) {
            return;
        }
        return this.accessor.removeGroup(this._group);
    }
    getWindow() {
        return this.location.type === 'popout'
            ? this.location.getWindow()
            : window;
    }
    moveTo(options) {
        var _a, _b, _c, _d;
        if (!this._group) {
            throw new Error(NOT_INITIALIZED_MESSAGE);
        }
        const group = (_a = options.group) !== null && _a !== void 0 ? _a : this.accessor.addGroup({
            direction: positionToDirection((_b = options.position) !== null && _b !== void 0 ? _b : 'right'),
            skipSetActive: (_c = options.skipSetActive) !== null && _c !== void 0 ? _c : false,
        });
        this.accessor.moveGroupOrPanel({
            from: { groupId: this._group.id },
            to: {
                group,
                position: options.group
                    ? (_d = options.position) !== null && _d !== void 0 ? _d : 'center'
                    : 'center',
                index: options.index,
            },
            skipSetActive: options.skipSetActive,
        });
    }
    maximize() {
        if (!this._group) {
            throw new Error(NOT_INITIALIZED_MESSAGE);
        }
        if (this.location.type !== 'grid') {
            // only grid groups can be maximized
            return;
        }
        this.accessor.maximizeGroup(this._group);
    }
    isMaximized() {
        if (!this._group) {
            throw new Error(NOT_INITIALIZED_MESSAGE);
        }
        return this.accessor.isMaximizedGroup(this._group);
    }
    exitMaximized() {
        if (!this._group) {
            throw new Error(NOT_INITIALIZED_MESSAGE);
        }
        if (this.isMaximized()) {
            this.accessor.exitMaximizedGroup();
        }
    }
    initialize(group) {
        this._group = group;
    }
}
