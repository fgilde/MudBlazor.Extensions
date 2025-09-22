import { CompositeDisposable, IDisposable } from '../../../lifecycle';
import { Event } from '../../../events';
import { IDockviewPanel } from '../../dockviewPanel';
import { DockviewComponent } from '../../dockviewComponent';
import { Droptarget } from '../../../dnd/droptarget';
import { DockviewGroupPanelModel } from '../../dockviewGroupPanelModel';
export interface IContentContainer extends IDisposable {
    readonly dropTarget: Droptarget;
    onDidFocus: Event<void>;
    onDidBlur: Event<void>;
    element: HTMLElement;
    layout(width: number, height: number): void;
    openPanel: (panel: IDockviewPanel) => void;
    closePanel: () => void;
    show(): void;
    hide(): void;
    renderPanel(panel: IDockviewPanel, options: {
        asActive: boolean;
    }): void;
}
export declare class ContentContainer extends CompositeDisposable implements IContentContainer {
    private readonly accessor;
    private readonly group;
    private readonly _element;
    private panel;
    private readonly disposable;
    private readonly _onDidFocus;
    readonly onDidFocus: Event<void>;
    private readonly _onDidBlur;
    readonly onDidBlur: Event<void>;
    get element(): HTMLElement;
    readonly dropTarget: Droptarget;
    constructor(accessor: DockviewComponent, group: DockviewGroupPanelModel);
    show(): void;
    hide(): void;
    renderPanel(panel: IDockviewPanel, options?: {
        asActive: boolean;
    }): void;
    openPanel(panel: IDockviewPanel): void;
    layout(_width: number, _height: number): void;
    closePanel(): void;
    dispose(): void;
}
