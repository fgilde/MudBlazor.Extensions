import { Emitter, Event } from '../events';
import { IPanel } from '../panel/types';
import { FunctionOrValue } from '../types';
import { PanelApiImpl, PanelApi } from './panelApi';
export interface GridConstraintChangeEvent {
    readonly minimumWidth?: number;
    readonly minimumHeight?: number;
    readonly maximumWidth?: number;
    readonly maximumHeight?: number;
}
interface GridConstraintChangeEvent2 {
    readonly minimumWidth?: FunctionOrValue<number>;
    readonly minimumHeight?: FunctionOrValue<number>;
    readonly maximumWidth?: FunctionOrValue<number>;
    readonly maximumHeight?: FunctionOrValue<number>;
}
export interface SizeEvent {
    readonly width?: number;
    readonly height?: number;
}
export interface GridviewPanelApi extends PanelApi {
    readonly onDidConstraintsChange: Event<GridConstraintChangeEvent>;
    setConstraints(value: GridConstraintChangeEvent2): void;
    setSize(event: SizeEvent): void;
}
export declare class GridviewPanelApiImpl extends PanelApiImpl implements GridviewPanelApi {
    private readonly _onDidConstraintsChangeInternal;
    readonly onDidConstraintsChangeInternal: Event<GridConstraintChangeEvent2>;
    readonly _onDidConstraintsChange: Emitter<GridConstraintChangeEvent>;
    readonly onDidConstraintsChange: Event<GridConstraintChangeEvent>;
    private readonly _onDidSizeChange;
    readonly onDidSizeChange: Event<SizeEvent>;
    constructor(id: string, component: string, panel?: IPanel);
    setConstraints(value: GridConstraintChangeEvent): void;
    setSize(event: SizeEvent): void;
}
export {};
