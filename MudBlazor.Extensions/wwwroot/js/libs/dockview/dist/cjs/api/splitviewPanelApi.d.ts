import { Emitter, Event } from '../events';
import { IDisposable } from '../lifecycle';
import { FunctionOrValue } from '../types';
import { PanelApiImpl, PanelApi } from './panelApi';
interface PanelConstraintChangeEvent2 {
    readonly minimumSize?: FunctionOrValue<number>;
    readonly maximumSize?: FunctionOrValue<number>;
}
export interface PanelConstraintChangeEvent {
    readonly minimumSize?: number;
    readonly maximumSize?: number;
}
export interface PanelSizeEvent {
    readonly size: number;
}
export interface SplitviewPanelApi extends PanelApi {
    readonly onDidConstraintsChange: Event<PanelConstraintChangeEvent>;
    setConstraints(value: PanelConstraintChangeEvent2): void;
    setSize(event: PanelSizeEvent): void;
}
export declare class SplitviewPanelApiImpl extends PanelApiImpl implements SplitviewPanelApi, IDisposable {
    readonly _onDidConstraintsChangeInternal: Emitter<PanelConstraintChangeEvent2>;
    readonly onDidConstraintsChangeInternal: Event<PanelConstraintChangeEvent2>;
    readonly _onDidConstraintsChange: Emitter<PanelConstraintChangeEvent>;
    readonly onDidConstraintsChange: Event<PanelConstraintChangeEvent>;
    readonly _onDidSizeChange: Emitter<PanelSizeEvent>;
    readonly onDidSizeChange: Event<PanelSizeEvent>;
    constructor(id: string, component: string);
    setConstraints(value: PanelConstraintChangeEvent2): void;
    setSize(event: PanelSizeEvent): void;
}
export {};
