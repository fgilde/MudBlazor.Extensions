import { getPanelData } from '../../../dnd/dataTransfer';
import { isChildEntirelyVisibleWithinParent, OverflowObserver, } from '../../../dom';
import { addDisposableListener, Emitter } from '../../../events';
import { CompositeDisposable, Disposable, MutableDisposable, } from '../../../lifecycle';
import { Scrollbar } from '../../../scrollbar';
import { WillShowOverlayLocationEvent } from '../../events';
import { Tab } from '../tab/tab';
export class Tabs extends CompositeDisposable {
    get showTabsOverflowControl() {
        return this._showTabsOverflowControl;
    }
    set showTabsOverflowControl(value) {
        if (this._showTabsOverflowControl == value) {
            return;
        }
        this._showTabsOverflowControl = value;
        if (value) {
            const observer = new OverflowObserver(this._tabsList);
            this._observerDisposable.value = new CompositeDisposable(observer, observer.onDidChange((event) => {
                const hasOverflow = event.hasScrollX || event.hasScrollY;
                this.toggleDropdown({ reset: !hasOverflow });
            }), addDisposableListener(this._tabsList, 'scroll', () => {
                this.toggleDropdown({ reset: false });
            }));
        }
    }
    get element() {
        return this._element;
    }
    get panels() {
        return this._tabs.map((_) => _.value.panel.id);
    }
    get size() {
        return this._tabs.length;
    }
    get tabs() {
        return this._tabs.map((_) => _.value);
    }
    constructor(group, accessor, options) {
        super();
        this.group = group;
        this.accessor = accessor;
        this._observerDisposable = new MutableDisposable();
        this._tabs = [];
        this.selectedIndex = -1;
        this._showTabsOverflowControl = false;
        this._onTabDragStart = new Emitter();
        this.onTabDragStart = this._onTabDragStart.event;
        this._onDrop = new Emitter();
        this.onDrop = this._onDrop.event;
        this._onWillShowOverlay = new Emitter();
        this.onWillShowOverlay = this._onWillShowOverlay.event;
        this._onOverflowTabsChange = new Emitter();
        this.onOverflowTabsChange = this._onOverflowTabsChange.event;
        this._tabsList = document.createElement('div');
        this._tabsList.className = 'dv-tabs-container dv-horizontal';
        this.showTabsOverflowControl = options.showTabsOverflowControl;
        if (accessor.options.scrollbars === 'native') {
            this._element = this._tabsList;
        }
        else {
            const scrollbar = new Scrollbar(this._tabsList);
            this._element = scrollbar.element;
            this.addDisposables(scrollbar);
        }
        this.addDisposables(this._onOverflowTabsChange, this._observerDisposable, this._onWillShowOverlay, this._onDrop, this._onTabDragStart, addDisposableListener(this.element, 'pointerdown', (event) => {
            if (event.defaultPrevented) {
                return;
            }
            const isLeftClick = event.button === 0;
            if (isLeftClick) {
                this.accessor.doSetGroupActive(this.group);
            }
        }), Disposable.from(() => {
            for (const { value, disposable } of this._tabs) {
                disposable.dispose();
                value.dispose();
            }
            this._tabs = [];
        }));
    }
    indexOf(id) {
        return this._tabs.findIndex((tab) => tab.value.panel.id === id);
    }
    isActive(tab) {
        return (this.selectedIndex > -1 &&
            this._tabs[this.selectedIndex].value === tab);
    }
    setActivePanel(panel) {
        let runningWidth = 0;
        for (const tab of this._tabs) {
            const isActivePanel = panel.id === tab.value.panel.id;
            tab.value.setActive(isActivePanel);
            if (isActivePanel) {
                const element = tab.value.element;
                const parentElement = element.parentElement;
                if (runningWidth < parentElement.scrollLeft ||
                    runningWidth + element.clientWidth >
                        parentElement.scrollLeft + parentElement.clientWidth) {
                    parentElement.scrollLeft = runningWidth;
                }
            }
            runningWidth += tab.value.element.clientWidth;
        }
    }
    openPanel(panel, index = this._tabs.length) {
        if (this._tabs.find((tab) => tab.value.panel.id === panel.id)) {
            return;
        }
        const tab = new Tab(panel, this.accessor, this.group);
        tab.setContent(panel.view.tab);
        const disposable = new CompositeDisposable(tab.onDragStart((event) => {
            this._onTabDragStart.fire({ nativeEvent: event, panel });
        }), tab.onPointerDown((event) => {
            if (event.defaultPrevented) {
                return;
            }
            const isFloatingGroupsEnabled = !this.accessor.options.disableFloatingGroups;
            const isFloatingWithOnePanel = this.group.api.location.type === 'floating' &&
                this.size === 1;
            if (isFloatingGroupsEnabled &&
                !isFloatingWithOnePanel &&
                event.shiftKey) {
                event.preventDefault();
                const panel = this.accessor.getGroupPanel(tab.panel.id);
                const { top, left } = tab.element.getBoundingClientRect();
                const { top: rootTop, left: rootLeft } = this.accessor.element.getBoundingClientRect();
                this.accessor.addFloatingGroup(panel, {
                    x: left - rootLeft,
                    y: top - rootTop,
                    inDragMode: true,
                });
                return;
            }
            switch (event.button) {
                case 0: // left click or touch
                    if (this.group.activePanel !== panel) {
                        this.group.model.openPanel(panel);
                    }
                    break;
            }
        }), tab.onDrop((event) => {
            this._onDrop.fire({
                event: event.nativeEvent,
                index: this._tabs.findIndex((x) => x.value === tab),
            });
        }), tab.onWillShowOverlay((event) => {
            this._onWillShowOverlay.fire(new WillShowOverlayLocationEvent(event, {
                kind: 'tab',
                panel: this.group.activePanel,
                api: this.accessor.api,
                group: this.group,
                getData: getPanelData,
            }));
        }));
        const value = { value: tab, disposable };
        this.addTab(value, index);
    }
    delete(id) {
        const index = this.indexOf(id);
        const tabToRemove = this._tabs.splice(index, 1)[0];
        const { value, disposable } = tabToRemove;
        disposable.dispose();
        value.dispose();
        value.element.remove();
    }
    addTab(tab, index = this._tabs.length) {
        if (index < 0 || index > this._tabs.length) {
            throw new Error('invalid location');
        }
        this._tabsList.insertBefore(tab.value.element, this._tabsList.children[index]);
        this._tabs = [
            ...this._tabs.slice(0, index),
            tab,
            ...this._tabs.slice(index),
        ];
        if (this.selectedIndex < 0) {
            this.selectedIndex = index;
        }
    }
    toggleDropdown(options) {
        const tabs = options.reset
            ? []
            : this._tabs
                .filter((tab) => !isChildEntirelyVisibleWithinParent(tab.value.element, this._tabsList))
                .map((x) => x.value.panel.id);
        this._onOverflowTabsChange.fire({ tabs, reset: options.reset });
    }
    updateDragAndDropState() {
        for (const tab of this._tabs) {
            tab.value.updateDragAndDropState();
        }
    }
}
