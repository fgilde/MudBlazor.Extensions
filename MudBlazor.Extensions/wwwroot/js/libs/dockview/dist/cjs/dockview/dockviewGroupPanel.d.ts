import { IFrameworkPart } from '../panel/types';
import { DockviewComponent } from '../dockview/dockviewComponent';
import { DockviewGroupPanelModel, GroupOptions, IDockviewGroupPanelModel, IHeader, DockviewGroupPanelLocked } from './dockviewGroupPanelModel';
import { GridviewPanel, IGridviewPanel } from '../gridview/gridviewPanel';
import { IDockviewPanel } from '../dockview/dockviewPanel';
import { DockviewGroupPanelApi, DockviewGroupPanelApiImpl } from '../api/dockviewGroupPanelApi';
export interface IDockviewGroupPanel extends IGridviewPanel<DockviewGroupPanelApi> {
    model: IDockviewGroupPanelModel;
    locked: DockviewGroupPanelLocked;
    readonly size: number;
    readonly panels: IDockviewPanel[];
    readonly activePanel: IDockviewPanel | undefined;
}
export type IDockviewGroupPanelPublic = IDockviewGroupPanel;
export declare class DockviewGroupPanel extends GridviewPanel<DockviewGroupPanelApiImpl> implements IDockviewGroupPanel {
    private readonly _model;
    get minimumWidth(): number;
    get minimumHeight(): number;
    get maximumWidth(): number;
    get maximumHeight(): number;
    get panels(): IDockviewPanel[];
    get activePanel(): IDockviewPanel | undefined;
    get size(): number;
    get model(): DockviewGroupPanelModel;
    get locked(): DockviewGroupPanelLocked;
    set locked(value: DockviewGroupPanelLocked);
    get header(): IHeader;
    constructor(accessor: DockviewComponent, id: string, options: GroupOptions);
    focus(): void;
    initialize(): void;
    setActive(isActive: boolean): void;
    layout(width: number, height: number): void;
    getComponent(): IFrameworkPart;
    toJSON(): any;
}
