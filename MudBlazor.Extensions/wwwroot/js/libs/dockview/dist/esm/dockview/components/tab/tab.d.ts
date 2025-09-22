import { Event } from '../../../events';
import { CompositeDisposable } from '../../../lifecycle';
import { DockviewComponent } from '../../dockviewComponent';
import { ITabRenderer } from '../../types';
import { DockviewGroupPanel } from '../../dockviewGroupPanel';
import { DroptargetEvent, WillShowOverlayEvent } from '../../../dnd/droptarget';
import { IDockviewPanel } from '../../dockviewPanel';
export declare class Tab extends CompositeDisposable {
    readonly panel: IDockviewPanel;
    private readonly accessor;
    private readonly group;
    private readonly _element;
    private readonly dropTarget;
    private content;
    private readonly dragHandler;
    private readonly _onPointDown;
    readonly onPointerDown: Event<MouseEvent>;
    private readonly _onDropped;
    readonly onDrop: Event<DroptargetEvent>;
    private readonly _onDragStart;
    readonly onDragStart: Event<DragEvent>;
    readonly onWillShowOverlay: Event<WillShowOverlayEvent>;
    get element(): HTMLElement;
    constructor(panel: IDockviewPanel, accessor: DockviewComponent, group: DockviewGroupPanel);
    setActive(isActive: boolean): void;
    setContent(part: ITabRenderer): void;
    updateDragAndDropState(): void;
    dispose(): void;
}
