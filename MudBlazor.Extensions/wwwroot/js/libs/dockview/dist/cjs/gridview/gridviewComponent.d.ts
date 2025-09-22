import { SerializedGridview } from './gridview';
import { IPanelDeserializer } from '../dockview/deserializer';
import { GridviewComponentOptions } from './options';
import { BaseGrid, Direction, IBaseGrid, IGridPanelView } from './baseComponentGridview';
import { GridviewPanel, GridviewInitParameters, GridPanelViewState, IGridviewPanel } from './gridviewPanel';
import { BaseComponentOptions, Parameters } from '../panel/types';
import { Orientation, Sizing } from '../splitview/splitview';
import { Emitter, Event } from '../events';
import { Position } from '../dnd/droptarget';
export interface SerializedGridviewComponent {
    grid: SerializedGridview<GridPanelViewState>;
    activePanel?: string;
}
export interface AddComponentOptions<T extends object = Parameters> extends BaseComponentOptions<T> {
    minimumWidth?: number;
    maximumWidth?: number;
    minimumHeight?: number;
    maximumHeight?: number;
    position?: {
        direction: Direction;
        referencePanel: string;
    };
    location?: number[];
}
export interface IGridPanelComponentView extends IGridPanelView {
    init: (params: GridviewInitParameters) => void;
}
export interface IGridviewComponent extends IBaseGrid<GridviewPanel> {
    readonly orientation: Orientation;
    readonly onDidLayoutFromJSON: Event<void>;
    updateOptions(options: Partial<GridviewComponentOptions>): void;
    addPanel<T extends object = Parameters>(options: AddComponentOptions<T>): IGridviewPanel;
    removePanel(panel: IGridviewPanel, sizing?: Sizing): void;
    focus(): void;
    fromJSON(serializedGridview: SerializedGridviewComponent): void;
    toJSON(): SerializedGridviewComponent;
    movePanel(panel: IGridviewPanel, options: {
        direction: Direction;
        reference: string;
        size?: number;
    }): void;
    setVisible(panel: IGridviewPanel, visible: boolean): void;
    setActive(panel: IGridviewPanel): void;
    readonly onDidRemoveGroup: Event<GridviewPanel>;
    readonly onDidAddGroup: Event<GridviewPanel>;
    readonly onDidActiveGroupChange: Event<GridviewPanel | undefined>;
}
export declare class GridviewComponent extends BaseGrid<GridviewPanel> implements IGridviewComponent {
    private _options;
    private _deserializer;
    private readonly _onDidLayoutfromJSON;
    readonly onDidLayoutFromJSON: Event<void>;
    private readonly _onDidRemoveGroup;
    readonly onDidRemoveGroup: Event<GridviewPanel>;
    protected readonly _onDidAddGroup: Emitter<GridviewPanel<import("../api/gridviewPanelApi").GridviewPanelApiImpl>>;
    readonly onDidAddGroup: Event<GridviewPanel>;
    private readonly _onDidActiveGroupChange;
    readonly onDidActiveGroupChange: Event<GridviewPanel | undefined>;
    get orientation(): Orientation;
    set orientation(value: Orientation);
    get options(): GridviewComponentOptions;
    get deserializer(): IPanelDeserializer | undefined;
    set deserializer(value: IPanelDeserializer | undefined);
    constructor(container: HTMLElement, options: GridviewComponentOptions);
    updateOptions(options: Partial<GridviewComponentOptions>): void;
    removePanel(panel: GridviewPanel): void;
    /**
     * Serialize the current state of the layout
     *
     * @returns A JSON respresentation of the layout
     */
    toJSON(): SerializedGridviewComponent;
    setVisible(panel: GridviewPanel, visible: boolean): void;
    setActive(panel: GridviewPanel): void;
    focus(): void;
    fromJSON(serializedGridview: SerializedGridviewComponent): void;
    clear(): void;
    movePanel(panel: GridviewPanel, options: {
        direction: Direction;
        reference: string;
        size?: number;
    }): void;
    addPanel<T extends object = Parameters>(options: AddComponentOptions<T>): IGridviewPanel;
    private registerPanel;
    moveGroup(referenceGroup: IGridPanelComponentView, groupId: string, target: Position): void;
    removeGroup(group: GridviewPanel): void;
    dispose(): void;
}
