import { CompositeDisposable } from '../../../lifecycle';
import { ITabRenderer, GroupPanelPartInitParameters } from '../../types';
export declare class DefaultTab extends CompositeDisposable implements ITabRenderer {
    private readonly _element;
    private readonly _content;
    private readonly action;
    private _title;
    get element(): HTMLElement;
    constructor();
    init(params: GroupPanelPartInitParameters): void;
    private render;
}
