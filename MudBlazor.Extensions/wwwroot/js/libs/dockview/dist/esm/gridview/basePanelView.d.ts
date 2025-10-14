import { CompositeDisposable } from '../lifecycle';
import { IFrameworkPart, PanelUpdateEvent, PanelInitParameters, IPanel, Parameters } from '../panel/types';
import { PanelApi, PanelApiImpl } from '../api/panelApi';
export interface BasePanelViewState {
    readonly id: string;
    readonly component: string;
    readonly params?: Parameters;
}
export interface BasePanelViewExported<T extends PanelApi> {
    readonly id: string;
    readonly api: T;
    readonly width: number;
    readonly height: number;
    readonly params: Parameters | undefined;
    focus(): void;
    toJSON(): object;
    update(event: PanelUpdateEvent): void;
}
export declare abstract class BasePanelView<T extends PanelApiImpl> extends CompositeDisposable implements IPanel, BasePanelViewExported<T> {
    readonly id: string;
    protected readonly component: string;
    readonly api: T;
    private _height;
    private _width;
    private readonly _element;
    protected part?: IFrameworkPart;
    protected _params?: PanelInitParameters;
    protected abstract getComponent(): IFrameworkPart;
    get element(): HTMLElement;
    get width(): number;
    get height(): number;
    get params(): Parameters | undefined;
    constructor(id: string, component: string, api: T);
    focus(): void;
    layout(width: number, height: number): void;
    init(parameters: PanelInitParameters): void;
    update(event: PanelUpdateEvent): void;
    toJSON(): BasePanelViewState;
    dispose(): void;
}
