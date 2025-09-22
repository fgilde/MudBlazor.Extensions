import { CompositeDisposable, IDisposable } from '../lifecycle';
export declare abstract class DragHandler extends CompositeDisposable {
    protected readonly el: HTMLElement;
    private disabled?;
    private readonly dataDisposable;
    private readonly pointerEventsDisposable;
    private readonly _onDragStart;
    readonly onDragStart: import("../events").Event<DragEvent>;
    constructor(el: HTMLElement, disabled?: boolean | undefined);
    setDisabled(disabled: boolean): void;
    abstract getData(event: DragEvent): IDisposable;
    protected isCancelled(_event: DragEvent): boolean;
    private configure;
}
