import { CompositeDisposable, Disposable, MutableDisposable, } from '../../../lifecycle';
import { addDisposableListener, Emitter } from '../../../events';
import { VoidContainer } from './voidContainer';
import { findRelativeZIndexParent, toggleClass } from '../../../dom';
import { WillShowOverlayLocationEvent } from '../../events';
import { getPanelData } from '../../../dnd/dataTransfer';
import { Tabs } from './tabs';
import { createDropdownElementHandle, } from './tabOverflowControl';
export class TabsContainer extends CompositeDisposable {
    get onTabDragStart() {
        return this.tabs.onTabDragStart;
    }
    get panels() {
        return this.tabs.panels;
    }
    get size() {
        return this.tabs.size;
    }
    get hidden() {
        return this._hidden;
    }
    set hidden(value) {
        this._hidden = value;
        this.element.style.display = value ? 'none' : '';
    }
    get element() {
        return this._element;
    }
    constructor(accessor, group) {
        super();
        this.accessor = accessor;
        this.group = group;
        this._hidden = false;
        this.dropdownPart = null;
        this._overflowTabs = [];
        this._dropdownDisposable = new MutableDisposable();
        this._onDrop = new Emitter();
        this.onDrop = this._onDrop.event;
        this._onGroupDragStart = new Emitter();
        this.onGroupDragStart = this._onGroupDragStart.event;
        this._onWillShowOverlay = new Emitter();
        this.onWillShowOverlay = this._onWillShowOverlay.event;
        this._element = document.createElement('div');
        this._element.className = 'dv-tabs-and-actions-container';
        toggleClass(this._element, 'dv-full-width-single-tab', this.accessor.options.singleTabMode === 'fullwidth');
        this.rightActionsContainer = document.createElement('div');
        this.rightActionsContainer.className = 'dv-right-actions-container';
        this.leftActionsContainer = document.createElement('div');
        this.leftActionsContainer.className = 'dv-left-actions-container';
        this.preActionsContainer = document.createElement('div');
        this.preActionsContainer.className = 'dv-pre-actions-container';
        this.tabs = new Tabs(group, accessor, {
            showTabsOverflowControl: !accessor.options.disableTabsOverflowList,
        });
        this.voidContainer = new VoidContainer(this.accessor, this.group);
        this._element.appendChild(this.preActionsContainer);
        this._element.appendChild(this.tabs.element);
        this._element.appendChild(this.leftActionsContainer);
        this._element.appendChild(this.voidContainer.element);
        this._element.appendChild(this.rightActionsContainer);
        this.addDisposables(this.tabs.onDrop((e) => this._onDrop.fire(e)), this.tabs.onWillShowOverlay((e) => this._onWillShowOverlay.fire(e)), accessor.onDidOptionsChange(() => {
            this.tabs.showTabsOverflowControl =
                !accessor.options.disableTabsOverflowList;
        }), this.tabs.onOverflowTabsChange((event) => {
            this.toggleDropdown(event);
        }), this.tabs, this._onWillShowOverlay, this._onDrop, this._onGroupDragStart, this.voidContainer, this.voidContainer.onDragStart((event) => {
            this._onGroupDragStart.fire({
                nativeEvent: event,
                group: this.group,
            });
        }), this.voidContainer.onDrop((event) => {
            this._onDrop.fire({
                event: event.nativeEvent,
                index: this.tabs.size,
            });
        }), this.voidContainer.onWillShowOverlay((event) => {
            this._onWillShowOverlay.fire(new WillShowOverlayLocationEvent(event, {
                kind: 'header_space',
                panel: this.group.activePanel,
                api: this.accessor.api,
                group: this.group,
                getData: getPanelData,
            }));
        }), addDisposableListener(this.voidContainer.element, 'pointerdown', (event) => {
            if (event.defaultPrevented) {
                return;
            }
            const isFloatingGroupsEnabled = !this.accessor.options.disableFloatingGroups;
            if (isFloatingGroupsEnabled &&
                event.shiftKey &&
                this.group.api.location.type !== 'floating') {
                event.preventDefault();
                const { top, left } = this.element.getBoundingClientRect();
                const { top: rootTop, left: rootLeft } = this.accessor.element.getBoundingClientRect();
                this.accessor.addFloatingGroup(this.group, {
                    x: left - rootLeft + 20,
                    y: top - rootTop + 20,
                    inDragMode: true,
                });
            }
        }));
    }
    show() {
        if (!this.hidden) {
            this.element.style.display = '';
        }
    }
    hide() {
        this._element.style.display = 'none';
    }
    setRightActionsElement(element) {
        if (this.rightActions === element) {
            return;
        }
        if (this.rightActions) {
            this.rightActions.remove();
            this.rightActions = undefined;
        }
        if (element) {
            this.rightActionsContainer.appendChild(element);
            this.rightActions = element;
        }
    }
    setLeftActionsElement(element) {
        if (this.leftActions === element) {
            return;
        }
        if (this.leftActions) {
            this.leftActions.remove();
            this.leftActions = undefined;
        }
        if (element) {
            this.leftActionsContainer.appendChild(element);
            this.leftActions = element;
        }
    }
    setPrefixActionsElement(element) {
        if (this.preActions === element) {
            return;
        }
        if (this.preActions) {
            this.preActions.remove();
            this.preActions = undefined;
        }
        if (element) {
            this.preActionsContainer.appendChild(element);
            this.preActions = element;
        }
    }
    isActive(tab) {
        return this.tabs.isActive(tab);
    }
    indexOf(id) {
        return this.tabs.indexOf(id);
    }
    setActive(_isGroupActive) {
        // noop
    }
    delete(id) {
        this.tabs.delete(id);
        this.updateClassnames();
    }
    setActivePanel(panel) {
        this.tabs.setActivePanel(panel);
    }
    openPanel(panel, index = this.tabs.size) {
        this.tabs.openPanel(panel, index);
        this.updateClassnames();
    }
    closePanel(panel) {
        this.delete(panel.id);
    }
    updateClassnames() {
        toggleClass(this._element, 'dv-single-tab', this.size === 1);
    }
    toggleDropdown(options) {
        const tabs = options.reset ? [] : options.tabs;
        this._overflowTabs = tabs;
        if (this._overflowTabs.length > 0 && this.dropdownPart) {
            this.dropdownPart.update({ tabs: tabs.length });
            return;
        }
        if (this._overflowTabs.length === 0) {
            this._dropdownDisposable.dispose();
            return;
        }
        const root = document.createElement('div');
        root.className = 'dv-tabs-overflow-dropdown-root';
        const part = createDropdownElementHandle();
        part.update({ tabs: tabs.length });
        this.dropdownPart = part;
        root.appendChild(part.element);
        this.rightActionsContainer.prepend(root);
        this._dropdownDisposable.value = new CompositeDisposable(Disposable.from(() => {
            var _a, _b;
            root.remove();
            (_b = (_a = this.dropdownPart) === null || _a === void 0 ? void 0 : _a.dispose) === null || _b === void 0 ? void 0 : _b.call(_a);
            this.dropdownPart = null;
        }), addDisposableListener(root, 'pointerdown', (event) => {
            event.preventDefault();
        }, { capture: true }), addDisposableListener(root, 'click', (event) => {
            const el = document.createElement('div');
            el.style.overflow = 'auto';
            el.className = 'dv-tabs-overflow-container';
            for (const tab of this.tabs.tabs.filter((tab) => this._overflowTabs.includes(tab.panel.id))) {
                const panelObject = this.group.panels.find((panel) => panel === tab.panel);
                const tabComponent = panelObject.view.createTabRenderer('headerOverflow');
                const child = tabComponent.element;
                const wrapper = document.createElement('div');
                toggleClass(wrapper, 'dv-tab', true);
                toggleClass(wrapper, 'dv-active-tab', panelObject.api.isActive);
                toggleClass(wrapper, 'dv-inactive-tab', !panelObject.api.isActive);
                wrapper.addEventListener('click', (event) => {
                    this.accessor.popupService.close();
                    if (event.defaultPrevented) {
                        return;
                    }
                    tab.element.scrollIntoView();
                    tab.panel.api.setActive();
                });
                wrapper.appendChild(child);
                el.appendChild(wrapper);
            }
            const relativeParent = findRelativeZIndexParent(root);
            this.accessor.popupService.openPopover(el, {
                x: event.clientX,
                y: event.clientY,
                zIndex: (relativeParent === null || relativeParent === void 0 ? void 0 : relativeParent.style.zIndex)
                    ? `calc(${relativeParent.style.zIndex} * 2)`
                    : undefined,
            });
        }));
    }
    updateDragAndDropState() {
        this.tabs.updateDragAndDropState();
        this.voidContainer.updateDragAndDropState();
    }
}
