import { DroptargetEvent, WillShowOverlayEvent } from '../../../dnd/droptarget';
import { DockviewComponent } from '../../dockviewComponent';
import { Event } from '../../../events';
import { CompositeDisposable } from '../../../lifecycle';
import { DockviewGroupPanel } from '../../dockviewGroupPanel';
export declare class VoidContainer extends CompositeDisposable {
    private readonly accessor;
    private readonly group;
    private readonly _element;
    private readonly dropTarget;
    private readonly handler;
    private readonly _onDrop;
    readonly onDrop: Event<DroptargetEvent>;
    private readonly _onDragStart;
    readonly onDragStart: Event<DragEvent>;
    readonly onWillShowOverlay: Event<WillShowOverlayEvent>;
    get element(): HTMLElement;
    constructor(accessor: DockviewComponent, group: DockviewGroupPanel);
    updateDragAndDropState(): void;
}
