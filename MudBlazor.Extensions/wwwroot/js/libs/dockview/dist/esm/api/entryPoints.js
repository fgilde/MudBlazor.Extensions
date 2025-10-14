import { GridviewApi, PaneviewApi, SplitviewApi, } from '../api/component.api';
import { DockviewComponent } from '../dockview/dockviewComponent';
import { GridviewComponent } from '../gridview/gridviewComponent';
import { PaneviewComponent } from '../paneview/paneviewComponent';
import { SplitviewComponent } from '../splitview/splitviewComponent';
export function createDockview(element, options) {
    const component = new DockviewComponent(element, options);
    return component.api;
}
export function createSplitview(element, options) {
    const component = new SplitviewComponent(element, options);
    return new SplitviewApi(component);
}
export function createGridview(element, options) {
    const component = new GridviewComponent(element, options);
    return new GridviewApi(component);
}
export function createPaneview(element, options) {
    const component = new PaneviewComponent(element, options);
    return new PaneviewApi(component);
}
