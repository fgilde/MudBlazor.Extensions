import { GridviewPanel } from './gridviewPanel';
import { Orientation } from '../splitview/splitview';
import { CreateComponentOptions } from '../dockview/options';
export interface GridviewOptions {
    disableAutoResizing?: boolean;
    proportionalLayout?: boolean;
    orientation: Orientation;
    className?: string;
    hideBorders?: boolean;
}
export interface GridviewFrameworkOptions {
    createComponent: (options: CreateComponentOptions) => GridviewPanel;
}
export type GridviewComponentOptions = GridviewOptions & GridviewFrameworkOptions;
export declare const PROPERTY_KEYS_GRIDVIEW: (keyof GridviewOptions)[];
