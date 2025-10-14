import { CompositeDisposable } from './lifecycle';
export declare class Scrollbar extends CompositeDisposable {
    private readonly scrollableElement;
    private readonly _element;
    private readonly _horizontalScrollbar;
    private _scrollLeft;
    private _animationTimer;
    static MouseWheelSpeed: number;
    get element(): HTMLElement;
    constructor(scrollableElement: HTMLElement);
    private calculateScrollbarStyles;
}
