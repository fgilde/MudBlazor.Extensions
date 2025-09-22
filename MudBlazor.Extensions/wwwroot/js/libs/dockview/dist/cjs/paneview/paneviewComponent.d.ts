import { Event } from '../events';
import { IDisposable } from '../lifecycle';
import { LayoutPriority, Orientation } from '../splitview/splitview';
import { PaneviewComponentOptions, PaneviewDndOverlayEvent } from './options';
import { Paneview } from './paneview';
import { IPanePart, PaneviewPanel, IPaneviewPanel } from './paneviewPanel';
import { DraggablePaneviewPanel, PaneviewDidDropEvent } from './draggablePaneviewPanel';
import { Resizable } from '../resizable';
import { Parameters } from '../panel/types';
export interface SerializedPaneviewPanel {
    snap?: boolean;
    priority?: LayoutPriority;
    minimumSize?: number;
    maximumSize?: number;
    headerSize?: number;
    data: {
        id: string;
        component: string;
        title: string;
        headerComponent?: string;
        params?: {
            [index: string]: any;
        };
    };
    size: number;
    expanded?: boolean;
}
export interface SerializedPaneview {
    size: number;
    views: SerializedPaneviewPanel[];
}
export declare class PaneFramework extends DraggablePaneviewPanel {
    private readonly options;
    constructor(options: {
        id: string;
        component: string;
        headerComponent: string | undefined;
        body: IPanePart;
        header: IPanePart;
        orientation: Orientation;
        isExpanded: boolean;
        disableDnd: boolean;
        accessor: IPaneviewComponent;
        headerSize: number;
        minimumBodySize: number;
        maximumBodySize: number;
    });
    getBodyComponent(): IPanePart;
    getHeaderComponent(): IPanePart;
}
export interface AddPaneviewComponentOptions<T extends object = Parameters> {
    id: string;
    component: string;
    headerComponent?: string;
    params?: T;
    minimumBodySize?: number;
    maximumBodySize?: number;
    headerSize?: number;
    isExpanded?: boolean;
    title: string;
    index?: number;
    size?: number;
}
export interface IPaneviewComponent extends IDisposable {
    readonly id: string;
    readonly width: number;
    readonly height: number;
    readonly minimumSize: number;
    readonly maximumSize: number;
    readonly panels: IPaneviewPanel[];
    readonly options: PaneviewComponentOptions;
    readonly onDidAddView: Event<PaneviewPanel>;
    readonly onDidRemoveView: Event<PaneviewPanel>;
    readonly onDidDrop: Event<PaneviewDidDropEvent>;
    readonly onDidLayoutChange: Event<void>;
    readonly onDidLayoutFromJSON: Event<void>;
    readonly onUnhandledDragOverEvent: Event<PaneviewDndOverlayEvent>;
    addPanel<T extends object = Parameters>(options: AddPaneviewComponentOptions<T>): IPaneviewPanel;
    layout(width: number, height: number): void;
    toJSON(): SerializedPaneview;
    fromJSON(serializedPaneview: SerializedPaneview): void;
    focus(): void;
    removePanel(panel: IPaneviewPanel): void;
    getPanel(id: string): IPaneviewPanel | undefined;
    movePanel(from: number, to: number): void;
    updateOptions(options: Partial<PaneviewComponentOptions>): void;
    setVisible(panel: IPaneviewPanel, visible: boolean): void;
    clear(): void;
}
export declare class PaneviewComponent extends Resizable implements IPaneviewComponent {
    private readonly _id;
    private _options;
    private readonly _disposable;
    private readonly _viewDisposables;
    private _paneview;
    private readonly _onDidLayoutfromJSON;
    readonly onDidLayoutFromJSON: Event<void>;
    private readonly _onDidLayoutChange;
    readonly onDidLayoutChange: Event<void>;
    private readonly _onDidDrop;
    readonly onDidDrop: Event<PaneviewDidDropEvent>;
    private readonly _onDidAddView;
    readonly onDidAddView: Event<PaneviewPanel>;
    private readonly _onDidRemoveView;
    readonly onDidRemoveView: Event<PaneviewPanel>;
    private readonly _onUnhandledDragOverEvent;
    readonly onUnhandledDragOverEvent: Event<PaneviewDndOverlayEvent>;
    private readonly _classNames;
    get id(): string;
    get panels(): PaneviewPanel[];
    set paneview(value: Paneview);
    get paneview(): Paneview;
    get minimumSize(): number;
    get maximumSize(): number;
    get height(): number;
    get width(): number;
    get options(): PaneviewComponentOptions;
    constructor(container: HTMLElement, options: PaneviewComponentOptions);
    setVisible(panel: PaneviewPanel, visible: boolean): void;
    focus(): void;
    updateOptions(options: Partial<PaneviewComponentOptions>): void;
    addPanel<T extends object = Parameters>(options: AddPaneviewComponentOptions<T>): IPaneviewPanel;
    removePanel(panel: PaneviewPanel): void;
    movePanel(from: number, to: number): void;
    getPanel(id: string): PaneviewPanel | undefined;
    layout(width: number, height: number): void;
    toJSON(): SerializedPaneview;
    fromJSON(serializedPaneview: SerializedPaneview): void;
    clear(): void;
    private doAddPanel;
    private doRemovePanel;
    dispose(): void;
}
