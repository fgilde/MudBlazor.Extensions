import { DockviewComponent } from '../dockview/dockviewComponent';
import { DockviewGroupPanel } from '../dockview/dockviewGroupPanel';
import { IDisposable } from '../lifecycle';
import { DragHandler } from './abstractDragHandler';
export declare class GroupDragHandler extends DragHandler {
    private readonly accessor;
    private readonly group;
    private readonly panelTransfer;
    constructor(element: HTMLElement, accessor: DockviewComponent, group: DockviewGroupPanel, disabled?: boolean);
    isCancelled(_event: DragEvent): boolean;
    getData(dragEvent: DragEvent): IDisposable;
}
