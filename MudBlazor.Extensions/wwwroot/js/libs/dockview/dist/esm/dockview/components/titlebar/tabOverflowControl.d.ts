export type DropdownElement = {
    element: HTMLElement;
    update: (params: {
        tabs: number;
    }) => void;
    dispose?: () => void;
};
export declare function createDropdownElementHandle(): DropdownElement;
