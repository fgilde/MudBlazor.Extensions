import { CompositeDisposable } from '../lifecycle';
import { DockviewComponent } from './dockviewComponent';
export declare class StrictEventsSequencing extends CompositeDisposable {
    private readonly accessor;
    constructor(accessor: DockviewComponent);
    private init;
}
