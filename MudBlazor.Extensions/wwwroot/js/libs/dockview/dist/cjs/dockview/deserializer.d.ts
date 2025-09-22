import { GroupviewPanelState } from './types';
import { DockviewGroupPanel } from './dockviewGroupPanel';
import { IDockviewPanel } from './dockviewPanel';
import { DockviewComponent } from './dockviewComponent';
export interface IPanelDeserializer {
    fromJSON(panelData: GroupviewPanelState, group: DockviewGroupPanel): IDockviewPanel;
}
export declare class DefaultDockviewDeserialzier implements IPanelDeserializer {
    private readonly accessor;
    constructor(accessor: DockviewComponent);
    fromJSON(panelData: GroupviewPanelState, group: DockviewGroupPanel): IDockviewPanel;
}
