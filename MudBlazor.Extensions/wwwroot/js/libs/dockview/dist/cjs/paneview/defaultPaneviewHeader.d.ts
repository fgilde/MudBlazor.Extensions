import { PaneviewPanelApiImpl } from '../api/paneviewPanelApi';
import { CompositeDisposable } from '../lifecycle';
import { PanelUpdateEvent } from '../panel/types';
import { IPanePart, PanePanelInitParameter } from './paneviewPanel';
export declare class DefaultHeader extends CompositeDisposable implements IPanePart {
    private readonly _expandedIcon;
    private readonly _collapsedIcon;
    private readonly disposable;
    private readonly _element;
    private readonly _content;
    private readonly _expander;
    private readonly apiRef;
    get element(): HTMLElement;
    constructor();
    init(params: PanePanelInitParameter & {
        api: PaneviewPanelApiImpl;
    }): void;
    private updateIcon;
    update(_params: PanelUpdateEvent): void;
    dispose(): void;
}
