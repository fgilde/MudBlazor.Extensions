import { CompositeDisposable } from '../lifecycle';
export class StrictEventsSequencing extends CompositeDisposable {
    constructor(accessor) {
        super();
        this.accessor = accessor;
        this.init();
    }
    init() {
        const panels = new Set();
        const groups = new Set();
        this.addDisposables(this.accessor.onDidAddPanel((panel) => {
            if (panels.has(panel.api.id)) {
                throw new Error(`dockview: Invalid event sequence. [onDidAddPanel] called for panel ${panel.api.id} but panel already exists`);
            }
            else {
                panels.add(panel.api.id);
            }
        }), this.accessor.onDidRemovePanel((panel) => {
            if (!panels.has(panel.api.id)) {
                throw new Error(`dockview: Invalid event sequence. [onDidRemovePanel] called for panel ${panel.api.id} but panel does not exists`);
            }
            else {
                panels.delete(panel.api.id);
            }
        }), this.accessor.onDidAddGroup((group) => {
            if (groups.has(group.api.id)) {
                throw new Error(`dockview: Invalid event sequence. [onDidAddGroup] called for group ${group.api.id} but group already exists`);
            }
            else {
                groups.add(group.api.id);
            }
        }), this.accessor.onDidRemoveGroup((group) => {
            if (!groups.has(group.api.id)) {
                throw new Error(`dockview: Invalid event sequence. [onDidRemoveGroup] called for group ${group.api.id} but group does not exists`);
            }
            else {
                groups.delete(group.api.id);
            }
        }));
    }
}
