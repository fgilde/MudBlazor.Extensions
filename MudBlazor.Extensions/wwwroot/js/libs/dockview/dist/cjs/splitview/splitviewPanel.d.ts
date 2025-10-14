import { PanelViewInitParameters } from './options';
import { BasePanelView, BasePanelViewExported } from '../gridview/basePanelView';
import { SplitviewPanelApiImpl } from '../api/splitviewPanelApi';
import { LayoutPriority, Orientation } from './splitview';
import { Event } from '../events';
export interface ISplitviewPanel extends BasePanelViewExported<SplitviewPanelApiImpl> {
    readonly priority: LayoutPriority | undefined;
    readonly minimumSize: number;
    readonly maximumSize: number;
    readonly snap: boolean;
    readonly orientation: Orientation;
}
export declare abstract class SplitviewPanel extends BasePanelView<SplitviewPanelApiImpl> implements ISplitviewPanel {
    private _evaluatedMinimumSize;
    private _evaluatedMaximumSize;
    private _minimumSize;
    private _maximumSize;
    private _priority?;
    private _snap;
    private _orientation?;
    private readonly _onDidChange;
    readonly onDidChange: Event<{
        size?: number;
        orthogonalSize?: number;
    }>;
    get priority(): LayoutPriority | undefined;
    set orientation(value: Orientation);
    get orientation(): Orientation;
    get minimumSize(): number;
    get maximumSize(): number;
    get snap(): boolean;
    constructor(id: string, componentName: string);
    setVisible(isVisible: boolean): void;
    setActive(isActive: boolean): void;
    layout(size: number, orthogonalSize: number): void;
    init(parameters: PanelViewInitParameters): void;
    toJSON(): {
        minimumSize: number | undefined;
        maximumSize: number | undefined;
        id: string;
        component: string;
        params?: import("..").Parameters | undefined;
    };
    private updateConstraints;
}
