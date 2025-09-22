declare class TransferObject {
}
export declare class PanelTransfer extends TransferObject {
    readonly viewId: string;
    readonly groupId: string;
    readonly panelId: string | null;
    constructor(viewId: string, groupId: string, panelId: string | null);
}
export declare class PaneTransfer extends TransferObject {
    readonly viewId: string;
    readonly paneId: string;
    constructor(viewId: string, paneId: string);
}
/**
 * A singleton to store transfer data during drag & drop operations that are only valid within the application.
 */
export declare class LocalSelectionTransfer<T> {
    private static readonly INSTANCE;
    private data?;
    private proto?;
    private constructor();
    static getInstance<T>(): LocalSelectionTransfer<T>;
    hasData(proto: T): boolean;
    clearData(proto: T): void;
    getData(proto: T): T[] | undefined;
    setData(data: T[], proto: T): void;
}
export declare function getPanelData(): PanelTransfer | undefined;
export declare function getPaneData(): PaneTransfer | undefined;
export {};
