import { Emitter, Event, AsapEvent } from '../events';
import { getGridLocation, Gridview } from './gridview';
import { Disposable } from '../lifecycle';
import { sequentialNumberGenerator } from '../math';
import { Sizing } from '../splitview/splitview';
import { Resizable } from '../resizable';
import { Classnames } from '../dom';
const nextLayoutId = sequentialNumberGenerator();
export function toTarget(direction) {
    switch (direction) {
        case 'left':
            return 'left';
        case 'right':
            return 'right';
        case 'above':
            return 'top';
        case 'below':
            return 'bottom';
        case 'within':
        default:
            return 'center';
    }
}
export class BaseGrid extends Resizable {
    get id() {
        return this._id;
    }
    get size() {
        return this._groups.size;
    }
    get groups() {
        return Array.from(this._groups.values()).map((_) => _.value);
    }
    get width() {
        return this.gridview.width;
    }
    get height() {
        return this.gridview.height;
    }
    get minimumHeight() {
        return this.gridview.minimumHeight;
    }
    get maximumHeight() {
        return this.gridview.maximumHeight;
    }
    get minimumWidth() {
        return this.gridview.minimumWidth;
    }
    get maximumWidth() {
        return this.gridview.maximumWidth;
    }
    get activeGroup() {
        return this._activeGroup;
    }
    get locked() {
        return this.gridview.locked;
    }
    set locked(value) {
        this.gridview.locked = value;
    }
    constructor(container, options) {
        var _a;
        super(document.createElement('div'), options.disableAutoResizing);
        this._id = nextLayoutId.next();
        this._groups = new Map();
        this._onDidRemove = new Emitter();
        this.onDidRemove = this._onDidRemove.event;
        this._onDidAdd = new Emitter();
        this.onDidAdd = this._onDidAdd.event;
        this._onDidMaximizedChange = new Emitter();
        this.onDidMaximizedChange = this._onDidMaximizedChange.event;
        this._onDidActiveChange = new Emitter();
        this.onDidActiveChange = this._onDidActiveChange.event;
        this._bufferOnDidLayoutChange = new AsapEvent();
        this.onDidLayoutChange = this._bufferOnDidLayoutChange.onEvent;
        this._onDidViewVisibilityChangeMicroTaskQueue = new AsapEvent();
        this.onDidViewVisibilityChangeMicroTaskQueue = this._onDidViewVisibilityChangeMicroTaskQueue.onEvent;
        this.element.style.height = '100%';
        this.element.style.width = '100%';
        this._classNames = new Classnames(this.element);
        this._classNames.setClassNames((_a = options.className) !== null && _a !== void 0 ? _a : '');
        // the container is owned by the third-party, do not modify/delete it
        container.appendChild(this.element);
        this.gridview = new Gridview(!!options.proportionalLayout, options.styles, options.orientation, options.locked, options.margin);
        this.gridview.locked = !!options.locked;
        this.element.appendChild(this.gridview.element);
        this.layout(0, 0, true); // set some elements height/widths
        this.addDisposables(this.gridview.onDidMaximizedNodeChange((event) => {
            this._onDidMaximizedChange.fire({
                panel: event.view,
                isMaximized: event.isMaximized,
            });
        }), this.gridview.onDidViewVisibilityChange(() => this._onDidViewVisibilityChangeMicroTaskQueue.fire()), this.onDidViewVisibilityChangeMicroTaskQueue(() => {
            this.layout(this.width, this.height, true);
        }), Disposable.from(() => {
            var _a;
            (_a = this.element.parentElement) === null || _a === void 0 ? void 0 : _a.removeChild(this.element);
        }), this.gridview.onDidChange(() => {
            this._bufferOnDidLayoutChange.fire();
        }), Event.any(this.onDidAdd, this.onDidRemove, this.onDidActiveChange)(() => {
            this._bufferOnDidLayoutChange.fire();
        }), this._onDidMaximizedChange, this._onDidViewVisibilityChangeMicroTaskQueue, this._bufferOnDidLayoutChange);
    }
    setVisible(panel, visible) {
        this.gridview.setViewVisible(getGridLocation(panel.element), visible);
        this._bufferOnDidLayoutChange.fire();
    }
    isVisible(panel) {
        return this.gridview.isViewVisible(getGridLocation(panel.element));
    }
    updateOptions(options) {
        var _a, _b, _c, _d;
        if (typeof options.proportionalLayout === 'boolean') {
            // this.gridview.proportionalLayout = options.proportionalLayout; // not supported
        }
        if (options.orientation) {
            this.gridview.orientation = options.orientation;
        }
        if ('styles' in options) {
            // this.gridview.styles = options.styles; // not supported
        }
        if ('disableResizing' in options) {
            this.disableResizing = (_a = options.disableAutoResizing) !== null && _a !== void 0 ? _a : false;
        }
        if ('locked' in options) {
            this.locked = (_b = options.locked) !== null && _b !== void 0 ? _b : false;
        }
        if ('margin' in options) {
            this.gridview.margin = (_c = options.margin) !== null && _c !== void 0 ? _c : 0;
        }
        if ('className' in options) {
            this._classNames.setClassNames((_d = options.className) !== null && _d !== void 0 ? _d : '');
        }
    }
    maximizeGroup(panel) {
        this.gridview.maximizeView(panel);
        this.doSetGroupActive(panel);
    }
    isMaximizedGroup(panel) {
        return this.gridview.maximizedView() === panel;
    }
    exitMaximizedGroup() {
        this.gridview.exitMaximizedView();
    }
    hasMaximizedGroup() {
        return this.gridview.hasMaximizedView();
    }
    doAddGroup(group, location = [0], size) {
        this.gridview.addView(group, size !== null && size !== void 0 ? size : Sizing.Distribute, location);
        this._onDidAdd.fire(group);
    }
    doRemoveGroup(group, options) {
        if (!this._groups.has(group.id)) {
            throw new Error('invalid operation');
        }
        const item = this._groups.get(group.id);
        const view = this.gridview.remove(group, Sizing.Distribute);
        if (item && !(options === null || options === void 0 ? void 0 : options.skipDispose)) {
            item.disposable.dispose();
            item.value.dispose();
            this._groups.delete(group.id);
            this._onDidRemove.fire(group);
        }
        if (!(options === null || options === void 0 ? void 0 : options.skipActive) && this._activeGroup === group) {
            const groups = Array.from(this._groups.values());
            this.doSetGroupActive(groups.length > 0 ? groups[0].value : undefined);
        }
        return view;
    }
    getPanel(id) {
        var _a;
        return (_a = this._groups.get(id)) === null || _a === void 0 ? void 0 : _a.value;
    }
    doSetGroupActive(group) {
        if (this._activeGroup === group) {
            return;
        }
        if (this._activeGroup) {
            this._activeGroup.setActive(false);
        }
        if (group) {
            group.setActive(true);
        }
        this._activeGroup = group;
        this._onDidActiveChange.fire(group);
    }
    removeGroup(group) {
        this.doRemoveGroup(group);
    }
    moveToNext(options) {
        var _a;
        if (!options) {
            options = {};
        }
        if (!options.group) {
            if (!this.activeGroup) {
                return;
            }
            options.group = this.activeGroup;
        }
        const location = getGridLocation(options.group.element);
        const next = (_a = this.gridview.next(location)) === null || _a === void 0 ? void 0 : _a.view;
        this.doSetGroupActive(next);
    }
    moveToPrevious(options) {
        var _a;
        if (!options) {
            options = {};
        }
        if (!options.group) {
            if (!this.activeGroup) {
                return;
            }
            options.group = this.activeGroup;
        }
        const location = getGridLocation(options.group.element);
        const next = (_a = this.gridview.previous(location)) === null || _a === void 0 ? void 0 : _a.view;
        this.doSetGroupActive(next);
    }
    layout(width, height, forceResize) {
        const different = forceResize || width !== this.width || height !== this.height;
        if (!different) {
            return;
        }
        this.gridview.element.style.height = `${height}px`;
        this.gridview.element.style.width = `${width}px`;
        this.gridview.layout(width, height);
    }
    dispose() {
        this._onDidActiveChange.dispose();
        this._onDidAdd.dispose();
        this._onDidRemove.dispose();
        for (const group of this.groups) {
            group.dispose();
        }
        this.gridview.dispose();
        super.dispose();
    }
}
