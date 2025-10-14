import { GroupPanelPartInitParameters, IContentRenderer, ITabRenderer } from './types';
import { IDisposable } from '../lifecycle';
import { IDockviewComponent } from './dockviewComponent';
import { PanelUpdateEvent } from '../panel/types';
import { TabLocation } from './framework';
export interface IDockviewPanelModel extends IDisposable {
    readonly contentComponent: string;
    readonly tabComponent?: string;
    readonly content: IContentRenderer;
    readonly tab: ITabRenderer;
    update(event: PanelUpdateEvent): void;
    layout(width: number, height: number): void;
    init(params: GroupPanelPartInitParameters): void;
    createTabRenderer(tabLocation: TabLocation): ITabRenderer;
}
export declare class DockviewPanelModel implements IDockviewPanelModel {
    private readonly accessor;
    private readonly id;
    readonly contentComponent: string;
    readonly tabComponent?: string | undefined;
    private readonly _content;
    private readonly _tab;
    private _params;
    private _updateEvent;
    get content(): IContentRenderer;
    get tab(): ITabRenderer;
    constructor(accessor: IDockviewComponent, id: string, contentComponent: string, tabComponent?: string | undefined);
    createTabRenderer(tabLocation: TabLocation): ITabRenderer;
    init(params: GroupPanelPartInitParameters): void;
    layout(width: number, height: number): void;
    update(event: PanelUpdateEvent): void;
    dispose(): void;
    private createContentComponent;
    private createTabComponent;
}
