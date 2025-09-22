import { Overlay } from '../overlay/overlay';
import { CompositeDisposable } from '../lifecycle';
import { AnchoredBox } from '../types';
import { DockviewGroupPanel, IDockviewGroupPanel } from './dockviewGroupPanel';
export interface IDockviewFloatingGroupPanel {
    readonly group: IDockviewGroupPanel;
    position(bounds: Partial<AnchoredBox>): void;
}
export declare class DockviewFloatingGroupPanel extends CompositeDisposable implements IDockviewFloatingGroupPanel {
    readonly group: DockviewGroupPanel;
    readonly overlay: Overlay;
    constructor(group: DockviewGroupPanel, overlay: Overlay);
    position(bounds: Partial<AnchoredBox>): void;
}
