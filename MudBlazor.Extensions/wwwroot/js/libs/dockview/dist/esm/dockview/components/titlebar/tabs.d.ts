import { Event } from '../../../events';
import { CompositeDisposable } from '../../../lifecycle';
import { DockviewComponent } from '../../dockviewComponent';
import { DockviewGroupPanel } from '../../dockviewGroupPanel';
import { WillShowOverlayLocationEvent } from '../../events';
import { IDockviewPanel } from '../../dockviewPanel';
import { Tab } from '../tab/tab';
import { TabDragEvent, TabDropIndexEvent } from './tabsContainer';
export declare class Tabs extends CompositeDisposable {
    private readonly group;
    private readonly accessor;
    private readonly _element;
    private readonly _tabsList;
    private readonly _observerDisposable;
    private _tabs;
    private selectedIndex;
    private _showTabsOverflowControl;
    private readonly _onTabDragStart;
    readonly onTabDragStart: Event<TabDragEvent>;
    private readonly _onDrop;
    readonly onDrop: Event<TabDropIndexEvent>;
    private readonly _onWillShowOverlay;
    readonly onWillShowOverlay: Event<WillShowOverlayLocationEvent>;
    private readonly _onOverflowTabsChange;
    readonly onOverflowTabsChange: Event<{
        tabs: string[];
        reset: boolean;
    }>;
    get showTabsOverflowControl(): boolean;
    set showTabsOverflowControl(value: boolean);
    get element(): HTMLElement;
    get panels(): string[];
    get size(): number;
    get tabs(): Tab[];
    constructor(group: DockviewGroupPanel, accessor: DockviewComponent, options: {
        showTabsOverflowControl: boolean;
    });
    indexOf(id: string): number;
    isActive(tab: Tab): boolean;
    setActivePanel(panel: IDockviewPanel): void;
    openPanel(panel: IDockviewPanel, index?: number): void;
    delete(id: string): void;
    private addTab;
    private toggleDropdown;
    updateDragAndDropState(): void;
}
