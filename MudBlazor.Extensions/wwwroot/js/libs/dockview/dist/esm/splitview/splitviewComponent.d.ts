import { IDisposable } from '../lifecycle';
import { IView, LayoutPriority, Orientation, Sizing, Splitview, SplitViewOptions } from './splitview';
import { SplitviewComponentOptions } from './options';
import { BaseComponentOptions, Parameters } from '../panel/types';
import { Event } from '../events';
import { SplitviewPanel, ISplitviewPanel } from './splitviewPanel';
import { Resizable } from '../resizable';
export interface SerializedSplitviewPanelData {
    id: string;
    component: string;
    minimumSize?: number;
    maximumSize?: number;
    params?: {
        [index: string]: any;
    };
}
export interface SerializedSplitviewPanel {
    snap?: boolean;
    priority?: LayoutPriority;
    data: SerializedSplitviewPanelData;
    size: number;
}
export interface SerializedSplitview {
    orientation: Orientation;
    size: number;
    activeView?: string;
    views: SerializedSplitviewPanel[];
}
export interface AddSplitviewComponentOptions<T extends Parameters = Parameters> extends BaseComponentOptions<T> {
    index?: number;
    minimumSize?: number;
    maximumSize?: number;
}
export interface ISplitviewComponent extends IDisposable {
    readonly minimumSize: number;
    readonly maximumSize: number;
    readonly height: number;
    readonly width: number;
    readonly length: number;
    readonly orientation: Orientation;
    readonly onDidAddView: Event<IView>;
    readonly onDidRemoveView: Event<IView>;
    readonly onDidLayoutFromJSON: Event<void>;
    readonly panels: SplitviewPanel[];
    updateOptions(options: Partial<SplitViewOptions>): void;
    addPanel<T extends object = Parameters>(options: AddSplitviewComponentOptions<T>): ISplitviewPanel;
    layout(width: number, height: number): void;
    onDidLayoutChange: Event<void>;
    toJSON(): SerializedSplitview;
    fromJSON(serializedSplitview: SerializedSplitview): void;
    focus(): void;
    getPanel(id: string): ISplitviewPanel | undefined;
    removePanel(panel: ISplitviewPanel, sizing?: Sizing): void;
    setVisible(panel: ISplitviewPanel, visible: boolean): void;
    movePanel(from: number, to: number): void;
    clear(): void;
}
/**
 * A high-level implementation of splitview that works using 'panels'
 */
export declare class SplitviewComponent extends Resizable implements ISplitviewComponent {
    private readonly _splitviewChangeDisposable;
    private _splitview;
    private _activePanel;
    private readonly _panels;
    private _options;
    private readonly _onDidLayoutfromJSON;
    readonly onDidLayoutFromJSON: Event<void>;
    private readonly _onDidAddView;
    readonly onDidAddView: Event<IView>;
    private readonly _onDidRemoveView;
    readonly onDidRemoveView: Event<IView>;
    private readonly _onDidLayoutChange;
    readonly onDidLayoutChange: Event<void>;
    private readonly _classNames;
    get panels(): SplitviewPanel[];
    get options(): SplitviewComponentOptions;
    get length(): number;
    get orientation(): Orientation;
    get splitview(): Splitview;
    set splitview(value: Splitview);
    get minimumSize(): number;
    get maximumSize(): number;
    get height(): number;
    get width(): number;
    constructor(container: HTMLElement, options: SplitviewComponentOptions);
    updateOptions(options: Partial<SplitviewComponentOptions>): void;
    focus(): void;
    movePanel(from: number, to: number): void;
    setVisible(panel: SplitviewPanel, visible: boolean): void;
    setActive(panel: SplitviewPanel, skipFocus?: boolean): void;
    removePanel(panel: SplitviewPanel, sizing?: Sizing): void;
    getPanel(id: string): SplitviewPanel | undefined;
    addPanel<T extends object = Parameters>(options: AddSplitviewComponentOptions<T>): SplitviewPanel;
    layout(width: number, height: number): void;
    private doAddView;
    toJSON(): SerializedSplitview;
    fromJSON(serializedSplitview: SerializedSplitview): void;
    clear(): void;
    dispose(): void;
}
