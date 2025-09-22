import { DockviewApi, GridviewApi, PaneviewApi, SplitviewApi } from '../api/component.api';
import { DockviewComponentOptions } from '../dockview/options';
import { GridviewComponentOptions } from '../gridview/options';
import { PaneviewComponentOptions } from '../paneview/options';
import { SplitviewComponentOptions } from '../splitview/options';
export declare function createDockview(element: HTMLElement, options: DockviewComponentOptions): DockviewApi;
export declare function createSplitview(element: HTMLElement, options: SplitviewComponentOptions): SplitviewApi;
export declare function createGridview(element: HTMLElement, options: GridviewComponentOptions): GridviewApi;
export declare function createPaneview(element: HTMLElement, options: PaneviewComponentOptions): PaneviewApi;
