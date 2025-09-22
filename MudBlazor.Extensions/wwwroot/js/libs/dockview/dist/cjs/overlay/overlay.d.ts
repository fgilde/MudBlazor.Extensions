import { Event } from '../events';
import { CompositeDisposable } from '../lifecycle';
import { AnchoredBox } from '../types';
export declare class Overlay extends CompositeDisposable {
    private readonly options;
    private readonly _element;
    private readonly _onDidChange;
    readonly onDidChange: Event<void>;
    private readonly _onDidChangeEnd;
    readonly onDidChangeEnd: Event<void>;
    private static readonly MINIMUM_HEIGHT;
    private static readonly MINIMUM_WIDTH;
    private verticalAlignment;
    private horiziontalAlignment;
    private _isVisible;
    set minimumInViewportWidth(value: number | undefined);
    set minimumInViewportHeight(value: number | undefined);
    get element(): HTMLElement;
    get isVisible(): boolean;
    constructor(options: AnchoredBox & {
        container: HTMLElement;
        content: HTMLElement;
        minimumInViewportWidth?: number;
        minimumInViewportHeight?: number;
    });
    setVisible(isVisible: boolean): void;
    bringToFront(): void;
    setBounds(bounds?: Partial<AnchoredBox>): void;
    toJSON(): AnchoredBox;
    setupDrag(dragTarget: HTMLElement, options?: {
        inDragMode: boolean;
    }): void;
    private setupResize;
    private getMinimumWidth;
    private getMinimumHeight;
    dispose(): void;
}
