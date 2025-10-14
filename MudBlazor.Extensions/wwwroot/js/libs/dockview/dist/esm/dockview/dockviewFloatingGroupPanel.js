import { CompositeDisposable } from '../lifecycle';
export class DockviewFloatingGroupPanel extends CompositeDisposable {
    constructor(group, overlay) {
        super();
        this.group = group;
        this.overlay = overlay;
        this.addDisposables(overlay);
    }
    position(bounds) {
        this.overlay.setBounds(bounds);
    }
}
