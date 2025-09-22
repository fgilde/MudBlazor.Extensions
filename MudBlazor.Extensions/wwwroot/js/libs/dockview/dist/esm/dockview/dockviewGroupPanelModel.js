import { DockviewApi } from '../api/component.api';
import { getPanelData } from '../dnd/dataTransfer';
import { isAncestor, toggleClass } from '../dom';
import { addDisposableListener, DockviewEvent, Emitter, } from '../events';
import { WillShowOverlayLocationEvent } from './events';
import { CompositeDisposable } from '../lifecycle';
import { ContentContainer, } from './components/panel/content';
import { TabsContainer, } from './components/titlebar/tabsContainer';
import { DockviewUnhandledDragOverEvent, } from './options';
export class DockviewDidDropEvent extends DockviewEvent {
    get nativeEvent() {
        return this.options.nativeEvent;
    }
    get position() {
        return this.options.position;
    }
    get panel() {
        return this.options.panel;
    }
    get group() {
        return this.options.group;
    }
    get api() {
        return this.options.api;
    }
    constructor(options) {
        super();
        this.options = options;
    }
    getData() {
        return this.options.getData();
    }
}
export class DockviewWillDropEvent extends DockviewDidDropEvent {
    get kind() {
        return this._kind;
    }
    constructor(options) {
        super(options);
        this._kind = options.kind;
    }
}
export class DockviewGroupPanelModel extends CompositeDisposable {
    get element() {
        throw new Error('dockview: not supported');
    }
    get activePanel() {
        return this._activePanel;
    }
    get locked() {
        return this._locked;
    }
    set locked(value) {
        this._locked = value;
        toggleClass(this.container, 'dv-locked-groupview', value === 'no-drop-target' || value);
    }
    get isActive() {
        return this._isGroupActive;
    }
    get panels() {
        return this._panels;
    }
    get size() {
        return this._panels.length;
    }
    get isEmpty() {
        return this._panels.length === 0;
    }
    get hasWatermark() {
        return !!(this.watermark && this.container.contains(this.watermark.element));
    }
    get header() {
        return this.tabsContainer;
    }
    get isContentFocused() {
        if (!document.activeElement) {
            return false;
        }
        return isAncestor(document.activeElement, this.contentContainer.element);
    }
    get location() {
        return this._location;
    }
    set location(value) {
        this._location = value;
        toggleClass(this.container, 'dv-groupview-floating', false);
        toggleClass(this.container, 'dv-groupview-popout', false);
        switch (value.type) {
            case 'grid':
                this.contentContainer.dropTarget.setTargetZones([
                    'top',
                    'bottom',
                    'left',
                    'right',
                    'center',
                ]);
                break;
            case 'floating':
                this.contentContainer.dropTarget.setTargetZones(['center']);
                this.contentContainer.dropTarget.setTargetZones(value
                    ? ['center']
                    : ['top', 'bottom', 'left', 'right', 'center']);
                toggleClass(this.container, 'dv-groupview-floating', true);
                break;
            case 'popout':
                this.contentContainer.dropTarget.setTargetZones(['center']);
                toggleClass(this.container, 'dv-groupview-popout', true);
                break;
        }
        this.groupPanel.api._onDidLocationChange.fire({
            location: this.location,
        });
    }
    constructor(container, accessor, id, options, groupPanel) {
        var _a;
        super();
        this.container = container;
        this.accessor = accessor;
        this.id = id;
        this.options = options;
        this.groupPanel = groupPanel;
        this._isGroupActive = false;
        this._locked = false;
        this._location = { type: 'grid' };
        this.mostRecentlyUsed = [];
        this._overwriteRenderContainer = null;
        this._overwriteDropTargetContainer = null;
        this._onDidChange = new Emitter();
        this.onDidChange = this._onDidChange.event;
        this._width = 0;
        this._height = 0;
        this._panels = [];
        this._panelDisposables = new Map();
        this._onMove = new Emitter();
        this.onMove = this._onMove.event;
        this._onDidDrop = new Emitter();
        this.onDidDrop = this._onDidDrop.event;
        this._onWillDrop = new Emitter();
        this.onWillDrop = this._onWillDrop.event;
        this._onWillShowOverlay = new Emitter();
        this.onWillShowOverlay = this._onWillShowOverlay.event;
        this._onTabDragStart = new Emitter();
        this.onTabDragStart = this._onTabDragStart.event;
        this._onGroupDragStart = new Emitter();
        this.onGroupDragStart = this._onGroupDragStart.event;
        this._onDidAddPanel = new Emitter();
        this.onDidAddPanel = this._onDidAddPanel.event;
        this._onDidPanelTitleChange = new Emitter();
        this.onDidPanelTitleChange = this._onDidPanelTitleChange.event;
        this._onDidPanelParametersChange = new Emitter();
        this.onDidPanelParametersChange = this._onDidPanelParametersChange.event;
        this._onDidRemovePanel = new Emitter();
        this.onDidRemovePanel = this._onDidRemovePanel.event;
        this._onDidActivePanelChange = new Emitter();
        this.onDidActivePanelChange = this._onDidActivePanelChange.event;
        this._onUnhandledDragOverEvent = new Emitter();
        this.onUnhandledDragOverEvent = this._onUnhandledDragOverEvent.event;
        toggleClass(this.container, 'dv-groupview', true);
        this._api = new DockviewApi(this.accessor);
        this.tabsContainer = new TabsContainer(this.accessor, this.groupPanel);
        this.contentContainer = new ContentContainer(this.accessor, this);
        container.append(this.tabsContainer.element, this.contentContainer.element);
        this.header.hidden = !!options.hideHeader;
        this.locked = (_a = options.locked) !== null && _a !== void 0 ? _a : false;
        this.addDisposables(this._onTabDragStart, this._onGroupDragStart, this._onWillShowOverlay, this.tabsContainer.onTabDragStart((event) => {
            this._onTabDragStart.fire(event);
        }), this.tabsContainer.onGroupDragStart((event) => {
            this._onGroupDragStart.fire(event);
        }), this.tabsContainer.onDrop((event) => {
            this.handleDropEvent('header', event.event, 'center', event.index);
        }), this.contentContainer.onDidFocus(() => {
            this.accessor.doSetGroupActive(this.groupPanel);
        }), this.contentContainer.onDidBlur(() => {
            // noop
        }), this.contentContainer.dropTarget.onDrop((event) => {
            this.handleDropEvent('content', event.nativeEvent, event.position);
        }), this.tabsContainer.onWillShowOverlay((event) => {
            this._onWillShowOverlay.fire(event);
        }), this.contentContainer.dropTarget.onWillShowOverlay((event) => {
            this._onWillShowOverlay.fire(new WillShowOverlayLocationEvent(event, {
                kind: 'content',
                panel: this.activePanel,
                api: this._api,
                group: this.groupPanel,
                getData: getPanelData,
            }));
        }), this._onMove, this._onDidChange, this._onDidDrop, this._onWillDrop, this._onDidAddPanel, this._onDidRemovePanel, this._onDidActivePanelChange, this._onUnhandledDragOverEvent, this._onDidPanelTitleChange, this._onDidPanelParametersChange);
    }
    focusContent() {
        this.contentContainer.element.focus();
    }
    set renderContainer(value) {
        this.panels.forEach((panel) => {
            this.renderContainer.detatch(panel);
        });
        this._overwriteRenderContainer = value;
        this.panels.forEach((panel) => {
            this.rerender(panel);
        });
    }
    get renderContainer() {
        var _a;
        return ((_a = this._overwriteRenderContainer) !== null && _a !== void 0 ? _a : this.accessor.overlayRenderContainer);
    }
    set dropTargetContainer(value) {
        this._overwriteDropTargetContainer = value;
    }
    get dropTargetContainer() {
        var _a;
        return ((_a = this._overwriteDropTargetContainer) !== null && _a !== void 0 ? _a : this.accessor.rootDropTargetContainer);
    }
    initialize() {
        if (this.options.panels) {
            this.options.panels.forEach((panel) => {
                this.doAddPanel(panel);
            });
        }
        if (this.options.activePanel) {
            this.openPanel(this.options.activePanel);
        }
        // must be run after the constructor otherwise this.parent may not be
        // correctly initialized
        this.setActive(this.isActive, true);
        this.updateContainer();
        if (this.accessor.options.createRightHeaderActionComponent) {
            this._rightHeaderActions =
                this.accessor.options.createRightHeaderActionComponent(this.groupPanel);
            this.addDisposables(this._rightHeaderActions);
            this._rightHeaderActions.init({
                containerApi: this._api,
                api: this.groupPanel.api,
                group: this.groupPanel,
            });
            this.tabsContainer.setRightActionsElement(this._rightHeaderActions.element);
        }
        if (this.accessor.options.createLeftHeaderActionComponent) {
            this._leftHeaderActions =
                this.accessor.options.createLeftHeaderActionComponent(this.groupPanel);
            this.addDisposables(this._leftHeaderActions);
            this._leftHeaderActions.init({
                containerApi: this._api,
                api: this.groupPanel.api,
                group: this.groupPanel,
            });
            this.tabsContainer.setLeftActionsElement(this._leftHeaderActions.element);
        }
        if (this.accessor.options.createPrefixHeaderActionComponent) {
            this._prefixHeaderActions =
                this.accessor.options.createPrefixHeaderActionComponent(this.groupPanel);
            this.addDisposables(this._prefixHeaderActions);
            this._prefixHeaderActions.init({
                containerApi: this._api,
                api: this.groupPanel.api,
                group: this.groupPanel,
            });
            this.tabsContainer.setPrefixActionsElement(this._prefixHeaderActions.element);
        }
    }
    rerender(panel) {
        this.contentContainer.renderPanel(panel, { asActive: false });
    }
    indexOf(panel) {
        return this.tabsContainer.indexOf(panel.id);
    }
    toJSON() {
        var _a;
        const result = {
            views: this.tabsContainer.panels,
            activeView: (_a = this._activePanel) === null || _a === void 0 ? void 0 : _a.id,
            id: this.id,
        };
        if (this.locked !== false) {
            result.locked = this.locked;
        }
        if (this.header.hidden) {
            result.hideHeader = true;
        }
        return result;
    }
    moveToNext(options) {
        if (!options) {
            options = {};
        }
        if (!options.panel) {
            options.panel = this.activePanel;
        }
        const index = options.panel ? this.panels.indexOf(options.panel) : -1;
        let normalizedIndex;
        if (index < this.panels.length - 1) {
            normalizedIndex = index + 1;
        }
        else if (!options.suppressRoll) {
            normalizedIndex = 0;
        }
        else {
            return;
        }
        this.openPanel(this.panels[normalizedIndex]);
    }
    moveToPrevious(options) {
        if (!options) {
            options = {};
        }
        if (!options.panel) {
            options.panel = this.activePanel;
        }
        if (!options.panel) {
            return;
        }
        const index = this.panels.indexOf(options.panel);
        let normalizedIndex;
        if (index > 0) {
            normalizedIndex = index - 1;
        }
        else if (!options.suppressRoll) {
            normalizedIndex = this.panels.length - 1;
        }
        else {
            return;
        }
        this.openPanel(this.panels[normalizedIndex]);
    }
    containsPanel(panel) {
        return this.panels.includes(panel);
    }
    init(_params) {
        //noop
    }
    update(_params) {
        //noop
    }
    focus() {
        var _a;
        (_a = this._activePanel) === null || _a === void 0 ? void 0 : _a.focus();
    }
    openPanel(panel, options = {}) {
        /**
         * set the panel group
         * add the panel
         * check if group active
         * check if panel active
         */
        if (typeof options.index !== 'number' ||
            options.index > this.panels.length) {
            options.index = this.panels.length;
        }
        const skipSetActive = !!options.skipSetActive;
        // ensure the group is updated before we fire any events
        panel.updateParentGroup(this.groupPanel, {
            skipSetActive: options.skipSetActive,
        });
        this.doAddPanel(panel, options.index, {
            skipSetActive: skipSetActive,
        });
        if (this._activePanel === panel) {
            this.contentContainer.renderPanel(panel, { asActive: true });
            return;
        }
        if (!skipSetActive) {
            this.doSetActivePanel(panel);
        }
        if (!options.skipSetGroupActive) {
            this.accessor.doSetGroupActive(this.groupPanel);
        }
        if (!options.skipSetActive) {
            this.updateContainer();
        }
    }
    removePanel(groupItemOrId, options = {
        skipSetActive: false,
    }) {
        const id = typeof groupItemOrId === 'string'
            ? groupItemOrId
            : groupItemOrId.id;
        const panelToRemove = this._panels.find((panel) => panel.id === id);
        if (!panelToRemove) {
            throw new Error('invalid operation');
        }
        return this._removePanel(panelToRemove, options);
    }
    closeAllPanels() {
        if (this.panels.length > 0) {
            // take a copy since we will be edting the array as we iterate through
            const arrPanelCpy = [...this.panels];
            for (const panel of arrPanelCpy) {
                this.doClose(panel);
            }
        }
        else {
            this.accessor.removeGroup(this.groupPanel);
        }
    }
    closePanel(panel) {
        this.doClose(panel);
    }
    doClose(panel) {
        const isLast = this.panels.length === 1 && this.accessor.groups.length === 1;
        this.accessor.removePanel(panel, isLast && this.accessor.options.noPanelsOverlay === 'emptyGroup'
            ? { removeEmptyGroup: false }
            : undefined);
    }
    isPanelActive(panel) {
        return this._activePanel === panel;
    }
    updateActions(element) {
        this.tabsContainer.setRightActionsElement(element);
    }
    setActive(isGroupActive, force = false) {
        if (!force && this.isActive === isGroupActive) {
            return;
        }
        this._isGroupActive = isGroupActive;
        toggleClass(this.container, 'dv-active-group', isGroupActive);
        toggleClass(this.container, 'dv-inactive-group', !isGroupActive);
        this.tabsContainer.setActive(this.isActive);
        if (!this._activePanel && this.panels.length > 0) {
            this.doSetActivePanel(this.panels[0]);
        }
        this.updateContainer();
    }
    layout(width, height) {
        var _a;
        this._width = width;
        this._height = height;
        this.contentContainer.layout(this._width, this._height);
        if ((_a = this._activePanel) === null || _a === void 0 ? void 0 : _a.layout) {
            this._activePanel.layout(this._width, this._height);
        }
    }
    _removePanel(panel, options) {
        const isActivePanel = this._activePanel === panel;
        this.doRemovePanel(panel);
        if (isActivePanel && this.panels.length > 0) {
            const nextPanel = this.mostRecentlyUsed[0];
            this.openPanel(nextPanel, {
                skipSetActive: options.skipSetActive,
                skipSetGroupActive: options.skipSetActiveGroup,
            });
        }
        if (this._activePanel && this.panels.length === 0) {
            this.doSetActivePanel(undefined);
        }
        if (!options.skipSetActive) {
            this.updateContainer();
        }
        return panel;
    }
    doRemovePanel(panel) {
        const index = this.panels.indexOf(panel);
        if (this._activePanel === panel) {
            this.contentContainer.closePanel();
        }
        this.tabsContainer.delete(panel.id);
        this._panels.splice(index, 1);
        if (this.mostRecentlyUsed.includes(panel)) {
            const index = this.mostRecentlyUsed.indexOf(panel);
            this.mostRecentlyUsed.splice(index, 1);
        }
        const disposable = this._panelDisposables.get(panel.id);
        if (disposable) {
            disposable.dispose();
            this._panelDisposables.delete(panel.id);
        }
        this._onDidRemovePanel.fire({ panel });
    }
    doAddPanel(panel, index = this.panels.length, options = { skipSetActive: false }) {
        const existingPanel = this._panels.indexOf(panel);
        const hasExistingPanel = existingPanel > -1;
        this.tabsContainer.show();
        this.contentContainer.show();
        this.tabsContainer.openPanel(panel, index);
        if (!options.skipSetActive) {
            this.contentContainer.openPanel(panel);
        }
        if (hasExistingPanel) {
            // TODO - need to ensure ordering hasn't changed and if it has need to re-order this.panels
            return;
        }
        this.updateMru(panel);
        this.panels.splice(index, 0, panel);
        this._panelDisposables.set(panel.id, new CompositeDisposable(panel.api.onDidTitleChange((event) => this._onDidPanelTitleChange.fire(event)), panel.api.onDidParametersChange((event) => this._onDidPanelParametersChange.fire(event))));
        this._onDidAddPanel.fire({ panel });
    }
    doSetActivePanel(panel) {
        if (this._activePanel === panel) {
            return;
        }
        this._activePanel = panel;
        if (panel) {
            this.tabsContainer.setActivePanel(panel);
            panel.layout(this._width, this._height);
            this.updateMru(panel);
            this._onDidActivePanelChange.fire({
                panel,
            });
        }
    }
    updateMru(panel) {
        if (this.mostRecentlyUsed.includes(panel)) {
            this.mostRecentlyUsed.splice(this.mostRecentlyUsed.indexOf(panel), 1);
        }
        this.mostRecentlyUsed = [panel, ...this.mostRecentlyUsed];
    }
    updateContainer() {
        var _a, _b;
        this.panels.forEach((panel) => panel.runEvents());
        if (this.isEmpty && !this.watermark) {
            const watermark = this.accessor.createWatermarkComponent();
            watermark.init({
                containerApi: this._api,
                group: this.groupPanel,
            });
            this.watermark = watermark;
            addDisposableListener(this.watermark.element, 'pointerdown', () => {
                if (!this.isActive) {
                    this.accessor.doSetGroupActive(this.groupPanel);
                }
            });
            this.contentContainer.element.appendChild(this.watermark.element);
        }
        if (!this.isEmpty && this.watermark) {
            this.watermark.element.remove();
            (_b = (_a = this.watermark).dispose) === null || _b === void 0 ? void 0 : _b.call(_a);
            this.watermark = undefined;
        }
    }
    canDisplayOverlay(event, position, target) {
        const firedEvent = new DockviewUnhandledDragOverEvent(event, target, position, getPanelData, this.accessor.getPanel(this.id));
        this._onUnhandledDragOverEvent.fire(firedEvent);
        return firedEvent.isAccepted;
    }
    handleDropEvent(type, event, position, index) {
        if (this.locked === 'no-drop-target') {
            return;
        }
        function getKind() {
            switch (type) {
                case 'header':
                    return typeof index === 'number' ? 'tab' : 'header_space';
                case 'content':
                    return 'content';
            }
        }
        const panel = typeof index === 'number' ? this.panels[index] : undefined;
        const willDropEvent = new DockviewWillDropEvent({
            nativeEvent: event,
            position,
            panel,
            getData: () => getPanelData(),
            kind: getKind(),
            group: this.groupPanel,
            api: this._api,
        });
        this._onWillDrop.fire(willDropEvent);
        if (willDropEvent.defaultPrevented) {
            return;
        }
        const data = getPanelData();
        if (data && data.viewId === this.accessor.id) {
            if (type === 'content') {
                if (data.groupId === this.id) {
                    // don't allow to drop on self for center position
                    if (position === 'center') {
                        return;
                    }
                    if (data.panelId === null) {
                        // don't allow group move to drop anywhere on self
                        return;
                    }
                }
            }
            if (type === 'header') {
                if (data.groupId === this.id) {
                    if (data.panelId === null) {
                        return;
                    }
                }
            }
            if (data.panelId === null) {
                // this is a group move dnd event
                const { groupId } = data;
                this._onMove.fire({
                    target: position,
                    groupId: groupId,
                    index,
                });
                return;
            }
            const fromSameGroup = this.tabsContainer.indexOf(data.panelId) !== -1;
            if (fromSameGroup && this.tabsContainer.size === 1) {
                return;
            }
            const { groupId, panelId } = data;
            const isSameGroup = this.id === groupId;
            if (isSameGroup && !position) {
                const oldIndex = this.tabsContainer.indexOf(panelId);
                if (oldIndex === index) {
                    return;
                }
            }
            this._onMove.fire({
                target: position,
                groupId: data.groupId,
                itemId: data.panelId,
                index,
            });
        }
        else {
            this._onDidDrop.fire(new DockviewDidDropEvent({
                nativeEvent: event,
                position,
                panel,
                getData: () => getPanelData(),
                group: this.groupPanel,
                api: this._api,
            }));
        }
    }
    updateDragAndDropState() {
        this.tabsContainer.updateDragAndDropState();
    }
    dispose() {
        var _a, _b, _c;
        super.dispose();
        (_a = this.watermark) === null || _a === void 0 ? void 0 : _a.element.remove();
        (_c = (_b = this.watermark) === null || _b === void 0 ? void 0 : _b.dispose) === null || _c === void 0 ? void 0 : _c.call(_b);
        this.watermark = undefined;
        for (const panel of this.panels) {
            panel.dispose();
        }
        this.tabsContainer.dispose();
        this.contentContainer.dispose();
    }
}
