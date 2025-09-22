import { PanelInitParameters } from '../panel/types';
import { SplitViewOptions, LayoutPriority } from './splitview';
import { SplitviewPanel } from './splitviewPanel';
import { SplitviewComponent } from './splitviewComponent';
import { CreateComponentOptions } from '../dockview/options';
export interface PanelViewInitParameters extends PanelInitParameters {
    minimumSize?: number;
    maximumSize?: number;
    snap?: boolean;
    priority?: LayoutPriority;
    accessor: SplitviewComponent;
}
export interface SplitviewOptions extends SplitViewOptions {
    disableAutoResizing?: boolean;
    className?: string;
}
export interface SplitviewFrameworkOptions {
    createComponent: (options: CreateComponentOptions) => SplitviewPanel;
}
export type SplitviewComponentOptions = SplitviewOptions & SplitviewFrameworkOptions;
export declare const PROPERTY_KEYS_SPLITVIEW: (keyof SplitviewOptions)[];
