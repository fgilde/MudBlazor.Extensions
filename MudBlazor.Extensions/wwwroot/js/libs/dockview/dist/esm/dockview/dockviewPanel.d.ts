import { DockviewApi } from '../api/component.api';
import { DockviewPanelApi, DockviewPanelApiImpl } from '../api/dockviewPanelApi';
import { GroupviewPanelState, IGroupPanelInitParameters } from './types';
import { DockviewGroupPanel } from './dockviewGroupPanel';
import { CompositeDisposable, IDisposable } from '../lifecycle';
import { IPanel, PanelUpdateEvent, Parameters } from '../panel/types';
import { IDockviewPanelModel } from './dockviewPanelModel';
import { DockviewComponent } from './dockviewComponent';
import { DockviewPanelRenderer } from '../overlay/overlayRenderContainer';
import { Contraints } from '../gridview/gridviewPanel';
export interface IDockviewPanel extends IDisposable, IPanel {
    readonly view: IDockviewPanelModel;
    readonly group: DockviewGroupPanel;
    readonly api: DockviewPanelApi;
    readonly title: string | undefined;
    readonly params: Parameters | undefined;
    readonly minimumWidth?: number;
    readonly minimumHeight?: number;
    readonly maximumWidth?: number;
    readonly maximumHeight?: number;
    updateParentGroup(group: DockviewGroupPanel, options?: {
        skipSetActive?: boolean;
    }): void;
    init(params: IGroupPanelInitParameters): void;
    toJSON(): GroupviewPanelState;
    setTitle(title: string): void;
    update(event: PanelUpdateEvent): void;
    runEvents(): void;
}
export declare class DockviewPanel extends CompositeDisposable implements IDockviewPanel {
    readonly id: string;
    private readonly accessor;
    private readonly containerApi;
    readonly view: IDockviewPanelModel;
    readonly api: DockviewPanelApiImpl;
    private _group;
    private _params?;
    private _title;
    private _renderer;
    private readonly _minimumWidth;
    private readonly _minimumHeight;
    private readonly _maximumWidth;
    private readonly _maximumHeight;
    get params(): Parameters | undefined;
    get title(): string | undefined;
    get group(): DockviewGroupPanel;
    get renderer(): DockviewPanelRenderer;
    get minimumWidth(): number | undefined;
    get minimumHeight(): number | undefined;
    get maximumWidth(): number | undefined;
    get maximumHeight(): number | undefined;
    constructor(id: string, component: string, tabComponent: string | undefined, accessor: DockviewComponent, containerApi: DockviewApi, group: DockviewGroupPanel, view: IDockviewPanelModel, options: {
        renderer?: DockviewPanelRenderer;
    } & Partial<Contraints>);
    init(params: IGroupPanelInitParameters): void;
    focus(): void;
    toJSON(): GroupviewPanelState;
    setTitle(title: string): void;
    setRenderer(renderer: DockviewPanelRenderer): void;
    update(event: PanelUpdateEvent): void;
    updateParentGroup(group: DockviewGroupPanel, options?: {
        skipSetActive?: boolean;
    }): void;
    runEvents(): void;
    layout(width: number, height: number): void;
    dispose(): void;
}
