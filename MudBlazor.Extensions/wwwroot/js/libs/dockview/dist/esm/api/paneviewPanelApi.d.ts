import { Emitter, Event } from '../events';
import { PaneviewPanel } from '../paneview/paneviewPanel';
import { SplitviewPanelApi, SplitviewPanelApiImpl } from './splitviewPanelApi';
export interface ExpansionEvent {
    readonly isExpanded: boolean;
}
export interface PaneviewPanelApi extends SplitviewPanelApi {
    readonly isExpanded: boolean;
    readonly onDidExpansionChange: Event<ExpansionEvent>;
    readonly onMouseEnter: Event<MouseEvent>;
    readonly onMouseLeave: Event<MouseEvent>;
    setExpanded(isExpanded: boolean): void;
}
export declare class PaneviewPanelApiImpl extends SplitviewPanelApiImpl implements PaneviewPanelApi {
    readonly _onDidExpansionChange: Emitter<ExpansionEvent>;
    readonly onDidExpansionChange: Event<ExpansionEvent>;
    readonly _onMouseEnter: Emitter<MouseEvent>;
    readonly onMouseEnter: Event<MouseEvent>;
    readonly _onMouseLeave: Emitter<MouseEvent>;
    readonly onMouseLeave: Event<MouseEvent>;
    private _pane;
    set pane(pane: PaneviewPanel);
    constructor(id: string, component: string);
    setExpanded(isExpanded: boolean): void;
    get isExpanded(): boolean;
}
