import { Emitter } from '../events';
import { GridviewPanelApiImpl } from './gridviewPanelApi';
import { CompositeDisposable, MutableDisposable } from '../lifecycle';
export class DockviewPanelApiImpl extends GridviewPanelApiImpl {
    get location() {
        return this.group.api.location;
    }
    get title() {
        return this.panel.title;
    }
    get isGroupActive() {
        return this.group.isActive;
    }
    get renderer() {
        return this.panel.renderer;
    }
    set group(value) {
        const oldGroup = this._group;
        if (this._group !== value) {
            this._group = value;
            this._onDidGroupChange.fire({});
            this.setupGroupEventListeners(oldGroup);
            this._onDidLocationChange.fire({
                location: this.group.api.location,
            });
        }
    }
    get group() {
        return this._group;
    }
    get tabComponent() {
        return this._tabComponent;
    }
    constructor(panel, group, accessor, component, tabComponent) {
        super(panel.id, component);
        this.panel = panel;
        this.accessor = accessor;
        this._onDidTitleChange = new Emitter();
        this.onDidTitleChange = this._onDidTitleChange.event;
        this._onDidActiveGroupChange = new Emitter();
        this.onDidActiveGroupChange = this._onDidActiveGroupChange.event;
        this._onDidGroupChange = new Emitter();
        this.onDidGroupChange = this._onDidGroupChange.event;
        this._onDidRendererChange = new Emitter();
        this.onDidRendererChange = this._onDidRendererChange.event;
        this._onDidLocationChange = new Emitter();
        this.onDidLocationChange = this._onDidLocationChange.event;
        this.groupEventsDisposable = new MutableDisposable();
        this._tabComponent = tabComponent;
        this.initialize(panel);
        this._group = group;
        this.setupGroupEventListeners();
        this.addDisposables(this.groupEventsDisposable, this._onDidRendererChange, this._onDidTitleChange, this._onDidGroupChange, this._onDidActiveGroupChange, this._onDidLocationChange);
    }
    getWindow() {
        return this.group.api.getWindow();
    }
    moveTo(options) {
        var _a, _b;
        this.accessor.moveGroupOrPanel({
            from: { groupId: this._group.id, panelId: this.panel.id },
            to: {
                group: (_a = options.group) !== null && _a !== void 0 ? _a : this._group,
                position: options.group
                    ? (_b = options.position) !== null && _b !== void 0 ? _b : 'center'
                    : 'center',
                index: options.index,
            },
            skipSetActive: options.skipSetActive,
        });
    }
    setTitle(title) {
        this.panel.setTitle(title);
    }
    setRenderer(renderer) {
        this.panel.setRenderer(renderer);
    }
    close() {
        this.group.model.closePanel(this.panel);
    }
    maximize() {
        this.group.api.maximize();
    }
    isMaximized() {
        return this.group.api.isMaximized();
    }
    exitMaximized() {
        this.group.api.exitMaximized();
    }
    setupGroupEventListeners(previousGroup) {
        var _a;
        let _trackGroupActive = (_a = previousGroup === null || previousGroup === void 0 ? void 0 : previousGroup.isActive) !== null && _a !== void 0 ? _a : false; // prevent duplicate events with same state
        this.groupEventsDisposable.value = new CompositeDisposable(this.group.api.onDidVisibilityChange((event) => {
            const hasBecomeHidden = !event.isVisible && this.isVisible;
            const hasBecomeVisible = event.isVisible && !this.isVisible;
            const isActivePanel = this.group.model.isPanelActive(this.panel);
            if (hasBecomeHidden || (hasBecomeVisible && isActivePanel)) {
                this._onDidVisibilityChange.fire(event);
            }
        }), this.group.api.onDidLocationChange((event) => {
            if (this.group !== this.panel.group) {
                return;
            }
            this._onDidLocationChange.fire(event);
        }), this.group.api.onDidActiveChange(() => {
            if (this.group !== this.panel.group) {
                return;
            }
            if (_trackGroupActive !== this.isGroupActive) {
                _trackGroupActive = this.isGroupActive;
                this._onDidActiveGroupChange.fire({
                    isActive: this.isGroupActive,
                });
            }
        }));
    }
}
