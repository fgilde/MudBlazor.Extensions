import { IDisposable, CompositeDisposable } from '../../../lifecycle';
import { Event } from '../../../events';
import { Tab } from '../tab/tab';
import { DockviewGroupPanel } from '../../dockviewGroupPanel';
import { IDockviewPanel } from '../../dockviewPanel';
import { DockviewComponent } from '../../dockviewComponent';
import { WillShowOverlayLocationEvent } from '../../events';
export interface TabDropIndexEvent {
    readonly event: DragEvent;
    readonly index: number;
}
export interface TabDragEvent {
    readonly nativeEvent: DragEvent;
    readonly panel: IDockviewPanel;
}
export interface GroupDragEvent {
    readonly nativeEvent: DragEvent;
    readonly group: DockviewGroupPanel;
}
export interface ITabsContainer extends IDisposable {
    readonly element: HTMLElement;
    readonly panels: string[];
    readonly size: number;
    readonly onDrop: Event<TabDropIndexEvent>;
    readonly onTabDragStart: Event<TabDragEvent>;
    readonly onGroupDragStart: Event<GroupDragEvent>;
    readonly onWillShowOverlay: Event<WillShowOverlayLocationEvent>;
    hidden: boolean;
    delete(id: string): void;
    indexOf(id: string): number;
    setActive(isGroupActive: boolean): void;
    setActivePanel(panel: IDockviewPanel): void;
    isActive(tab: Tab): boolean;
    closePanel(panel: IDockviewPanel): void;
    openPanel(panel: IDockviewPanel, index?: number): void;
    setRightActionsElement(element: HTMLElement | undefined): void;
    setLeftActionsElement(element: HTMLElement | undefined): void;
    setPrefixActionsElement(element: HTMLElement | undefined): void;
    show(): void;
    hide(): void;
    updateDragAndDropState(): void;
}
export declare class TabsContainer extends CompositeDisposable implements ITabsContainer {
    private readonly accessor;
    private readonly group;
    private readonly _element;
    private readonly tabs;
    private readonly rightActionsContainer;
    private readonly leftActionsContainer;
    private readonly preActionsContainer;
    private readonly voidContainer;
    private rightActions;
    private leftActions;
    private preActions;
    private _hidden;
    private dropdownPart;
    private _overflowTabs;
    private readonly _dropdownDisposable;
    private readonly _onDrop;
    readonly onDrop: Event<TabDropIndexEvent>;
    get onTabDragStart(): Event<TabDragEvent>;
    private readonly _onGroupDragStart;
    readonly onGroupDragStart: Event<GroupDragEvent>;
    private readonly _onWillShowOverlay;
    readonly onWillShowOverlay: Event<WillShowOverlayLocationEvent>;
    get panels(): string[];
    get size(): number;
    get hidden(): boolean;
    set hidden(value: boolean);
    get element(): HTMLElement;
    constructor(accessor: DockviewComponent, group: DockviewGroupPanel);
    show(): void;
    hide(): void;
    setRightActionsElement(element: HTMLElement | undefined): void;
    setLeftActionsElement(element: HTMLElement | undefined): void;
    setPrefixActionsElement(element: HTMLElement | undefined): void;
    isActive(tab: Tab): boolean;
    indexOf(id: string): number;
    setActive(_isGroupActive: boolean): void;
    delete(id: string): void;
    setActivePanel(panel: IDockviewPanel): void;
    openPanel(panel: IDockviewPanel, index?: number): void;
    closePanel(panel: IDockviewPanel): void;
    private updateClassnames;
    private toggleDropdown;
    updateDragAndDropState(): void;
}
