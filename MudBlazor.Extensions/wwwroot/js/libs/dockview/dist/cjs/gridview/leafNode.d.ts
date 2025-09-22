import { IView, LayoutPriority, Orientation } from '../splitview/splitview';
import { Event } from '../events';
import { IGridView } from './gridview';
export declare class LeafNode implements IView {
    readonly view: IGridView;
    readonly orientation: Orientation;
    private readonly _onDidChange;
    readonly onDidChange: Event<{
        size?: number;
        orthogonalSize?: number;
    }>;
    private _size;
    private _orthogonalSize;
    private readonly _disposable;
    private get minimumWidth();
    private get maximumWidth();
    private get minimumHeight();
    private get maximumHeight();
    get priority(): LayoutPriority | undefined;
    get snap(): boolean | undefined;
    get minimumSize(): number;
    get maximumSize(): number;
    get minimumOrthogonalSize(): number;
    get maximumOrthogonalSize(): number;
    get orthogonalSize(): number;
    get size(): number;
    get element(): HTMLElement;
    get width(): number;
    get height(): number;
    constructor(view: IGridView, orientation: Orientation, orthogonalSize: number, size?: number);
    setVisible(visible: boolean): void;
    layout(size: number, orthogonalSize: number): void;
    dispose(): void;
}
