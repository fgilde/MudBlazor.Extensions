import { getRelativeLocation, getGridLocation, } from './gridview';
import { tail, sequenceEquals } from '../array';
import { CompositeDisposable } from '../lifecycle';
import { BaseGrid, toTarget, } from './baseComponentGridview';
import { Emitter } from '../events';
export class GridviewComponent extends BaseGrid {
    get orientation() {
        return this.gridview.orientation;
    }
    set orientation(value) {
        this.gridview.orientation = value;
    }
    get options() {
        return this._options;
    }
    get deserializer() {
        return this._deserializer;
    }
    set deserializer(value) {
        this._deserializer = value;
    }
    constructor(container, options) {
        var _a;
        super(container, {
            proportionalLayout: (_a = options.proportionalLayout) !== null && _a !== void 0 ? _a : true,
            orientation: options.orientation,
            styles: options.hideBorders
                ? { separatorBorder: 'transparent' }
                : undefined,
            disableAutoResizing: options.disableAutoResizing,
            className: options.className,
        });
        this._onDidLayoutfromJSON = new Emitter();
        this.onDidLayoutFromJSON = this._onDidLayoutfromJSON.event;
        this._onDidRemoveGroup = new Emitter();
        this.onDidRemoveGroup = this._onDidRemoveGroup.event;
        this._onDidAddGroup = new Emitter();
        this.onDidAddGroup = this._onDidAddGroup.event;
        this._onDidActiveGroupChange = new Emitter();
        this.onDidActiveGroupChange = this._onDidActiveGroupChange.event;
        this._options = options;
        this.addDisposables(this._onDidAddGroup, this._onDidRemoveGroup, this._onDidActiveGroupChange, this.onDidAdd((event) => {
            this._onDidAddGroup.fire(event);
        }), this.onDidRemove((event) => {
            this._onDidRemoveGroup.fire(event);
        }), this.onDidActiveChange((event) => {
            this._onDidActiveGroupChange.fire(event);
        }));
    }
    updateOptions(options) {
        super.updateOptions(options);
        const hasOrientationChanged = typeof options.orientation === 'string' &&
            this.gridview.orientation !== options.orientation;
        this._options = Object.assign(Object.assign({}, this.options), options);
        if (hasOrientationChanged) {
            this.gridview.orientation = options.orientation;
        }
        this.layout(this.gridview.width, this.gridview.height, true);
    }
    removePanel(panel) {
        this.removeGroup(panel);
    }
    /**
     * Serialize the current state of the layout
     *
     * @returns A JSON respresentation of the layout
     */
    toJSON() {
        var _a;
        const data = this.gridview.serialize();
        return {
            grid: data,
            activePanel: (_a = this.activeGroup) === null || _a === void 0 ? void 0 : _a.id,
        };
    }
    setVisible(panel, visible) {
        this.gridview.setViewVisible(getGridLocation(panel.element), visible);
    }
    setActive(panel) {
        this._groups.forEach((value, _key) => {
            value.value.setActive(panel === value.value);
        });
    }
    focus() {
        var _a;
        (_a = this.activeGroup) === null || _a === void 0 ? void 0 : _a.focus();
    }
    fromJSON(serializedGridview) {
        this.clear();
        const { grid, activePanel } = serializedGridview;
        try {
            const queue = [];
            // take note of the existing dimensions
            const width = this.width;
            const height = this.height;
            this.gridview.deserialize(grid, {
                fromJSON: (node) => {
                    const { data } = node;
                    const view = this.options.createComponent({
                        id: data.id,
                        name: data.component,
                    });
                    queue.push(() => view.init({
                        params: data.params,
                        minimumWidth: data.minimumWidth,
                        maximumWidth: data.maximumWidth,
                        minimumHeight: data.minimumHeight,
                        maximumHeight: data.maximumHeight,
                        priority: data.priority,
                        snap: !!data.snap,
                        accessor: this,
                        isVisible: node.visible,
                    }));
                    this._onDidAddGroup.fire(view);
                    this.registerPanel(view);
                    return view;
                },
            });
            this.layout(width, height, true);
            queue.forEach((f) => f());
            if (typeof activePanel === 'string') {
                const panel = this.getPanel(activePanel);
                if (panel) {
                    this.doSetGroupActive(panel);
                }
            }
        }
        catch (err) {
            /**
             * To remove a group we cannot call this.removeGroup(...) since this makes assumptions about
             * the underlying HTMLElement existing in the Gridview.
             */
            for (const group of this.groups) {
                group.dispose();
                this._groups.delete(group.id);
                this._onDidRemoveGroup.fire(group);
            }
            // fires clean-up events and clears the underlying HTML gridview.
            this.clear();
            /**
             * even though we have cleaned-up we still want to inform the caller of their error
             * and we'll do this through re-throwing the original error since afterall you would
             * expect trying to load a corrupted layout to result in an error and not silently fail...
             */
            throw err;
        }
        this._onDidLayoutfromJSON.fire();
    }
    clear() {
        const hasActiveGroup = this.activeGroup;
        const groups = Array.from(this._groups.values()); // reassign since group panels will mutate
        for (const group of groups) {
            group.disposable.dispose();
            this.doRemoveGroup(group.value, { skipActive: true });
        }
        if (hasActiveGroup) {
            this.doSetGroupActive(undefined);
        }
        this.gridview.clear();
    }
    movePanel(panel, options) {
        var _a;
        let relativeLocation;
        const removedPanel = this.gridview.remove(panel);
        const referenceGroup = (_a = this._groups.get(options.reference)) === null || _a === void 0 ? void 0 : _a.value;
        if (!referenceGroup) {
            throw new Error(`reference group ${options.reference} does not exist`);
        }
        const target = toTarget(options.direction);
        if (target === 'center') {
            throw new Error(`${target} not supported as an option`);
        }
        else {
            const location = getGridLocation(referenceGroup.element);
            relativeLocation = getRelativeLocation(this.gridview.orientation, location, target);
        }
        this.doAddGroup(removedPanel, relativeLocation, options.size);
    }
    addPanel(options) {
        var _a, _b, _c, _d;
        let relativeLocation = (_a = options.location) !== null && _a !== void 0 ? _a : [0];
        if ((_b = options.position) === null || _b === void 0 ? void 0 : _b.referencePanel) {
            const referenceGroup = (_c = this._groups.get(options.position.referencePanel)) === null || _c === void 0 ? void 0 : _c.value;
            if (!referenceGroup) {
                throw new Error(`reference group ${options.position.referencePanel} does not exist`);
            }
            const target = toTarget(options.position.direction);
            if (target === 'center') {
                throw new Error(`${target} not supported as an option`);
            }
            else {
                const location = getGridLocation(referenceGroup.element);
                relativeLocation = getRelativeLocation(this.gridview.orientation, location, target);
            }
        }
        const view = this.options.createComponent({
            id: options.id,
            name: options.component,
        });
        view.init({
            params: (_d = options.params) !== null && _d !== void 0 ? _d : {},
            minimumWidth: options.minimumWidth,
            maximumWidth: options.maximumWidth,
            minimumHeight: options.minimumHeight,
            maximumHeight: options.maximumHeight,
            priority: options.priority,
            snap: !!options.snap,
            accessor: this,
            isVisible: true,
        });
        this.registerPanel(view);
        this.doAddGroup(view, relativeLocation, options.size);
        this.doSetGroupActive(view);
        return view;
    }
    registerPanel(panel) {
        const disposable = new CompositeDisposable(panel.api.onDidFocusChange((event) => {
            if (!event.isFocused) {
                return;
            }
            this._groups.forEach((groupItem) => {
                const group = groupItem.value;
                if (group !== panel) {
                    group.setActive(false);
                }
                else {
                    group.setActive(true);
                }
            });
        }));
        this._groups.set(panel.id, {
            value: panel,
            disposable,
        });
    }
    moveGroup(referenceGroup, groupId, target) {
        const sourceGroup = this.getPanel(groupId);
        if (!sourceGroup) {
            throw new Error('invalid operation');
        }
        const referenceLocation = getGridLocation(referenceGroup.element);
        const targetLocation = getRelativeLocation(this.gridview.orientation, referenceLocation, target);
        const [targetParentLocation, to] = tail(targetLocation);
        const sourceLocation = getGridLocation(sourceGroup.element);
        const [sourceParentLocation, from] = tail(sourceLocation);
        if (sequenceEquals(sourceParentLocation, targetParentLocation)) {
            // special case when 'swapping' two views within same grid location
            // if a group has one tab - we are essentially moving the 'group'
            // which is equivalent to swapping two views in this case
            this.gridview.moveView(sourceParentLocation, from, to);
            return;
        }
        // source group will become empty so delete the group
        const targetGroup = this.doRemoveGroup(sourceGroup, {
            skipActive: true,
            skipDispose: true,
        });
        // after deleting the group we need to re-evaulate the ref location
        const updatedReferenceLocation = getGridLocation(referenceGroup.element);
        const location = getRelativeLocation(this.gridview.orientation, updatedReferenceLocation, target);
        this.doAddGroup(targetGroup, location);
    }
    removeGroup(group) {
        super.removeGroup(group);
    }
    dispose() {
        super.dispose();
        this._onDidLayoutfromJSON.dispose();
    }
}
