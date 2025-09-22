import { CompositeDisposable } from './lifecycle';
import { Box } from './types';
export type PopoutWindowOptions = {
    url: string;
    onDidOpen?: (event: {
        id: string;
        window: Window;
    }) => void;
    onWillClose?: (event: {
        id: string;
        window: Window;
    }) => void;
} & Box;
export declare class PopoutWindow extends CompositeDisposable {
    private readonly target;
    private readonly className;
    private readonly options;
    private readonly _onWillClose;
    readonly onWillClose: import("./events").Event<void>;
    private readonly _onDidClose;
    readonly onDidClose: import("./events").Event<void>;
    private _window;
    get window(): Window | null;
    constructor(target: string, className: string, options: PopoutWindowOptions);
    dimensions(): Box | null;
    close(): void;
    open(): Promise<HTMLElement | null>;
    private createPopoutWindowContainer;
}
