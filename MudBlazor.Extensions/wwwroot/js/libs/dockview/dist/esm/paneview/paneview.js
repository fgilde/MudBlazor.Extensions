import { Splitview, Orientation, } from '../splitview/splitview';
import { CompositeDisposable } from '../lifecycle';
import { Emitter } from '../events';
import { addClasses, removeClasses } from '../dom';
export class Paneview extends CompositeDisposable {
    get onDidAddView() {
        return this.splitview.onDidAddView;
    }
    get onDidRemoveView() {
        return this.splitview.onDidRemoveView;
    }
    get minimumSize() {
        return this.splitview.minimumSize;
    }
    get maximumSize() {
        return this.splitview.maximumSize;
    }
    get orientation() {
        return this.splitview.orientation;
    }
    get size() {
        return this.splitview.size;
    }
    get orthogonalSize() {
        return this.splitview.orthogonalSize;
    }
    constructor(container, options) {
        var _a;
        super();
        this.paneItems = [];
        this.skipAnimation = false;
        this._onDidChange = new Emitter();
        this.onDidChange = this._onDidChange.event;
        this._orientation = (_a = options.orientation) !== null && _a !== void 0 ? _a : Orientation.VERTICAL;
        this.element = document.createElement('div');
        this.element.className = 'dv-pane-container';
        container.appendChild(this.element);
        this.splitview = new Splitview(this.element, {
            orientation: this._orientation,
            proportionalLayout: false,
            descriptor: options.descriptor,
        });
        // if we've added views from the descriptor we need to
        // add the panes to our Pane array and setup animation
        this.getPanes().forEach((pane) => {
            const disposable = new CompositeDisposable(pane.onDidChangeExpansionState(() => {
                this.setupAnimation();
                this._onDidChange.fire(undefined);
            }));
            const paneItem = {
                pane,
                disposable: {
                    dispose: () => {
                        disposable.dispose();
                    },
                },
            };
            this.paneItems.push(paneItem);
            pane.orthogonalSize = this.splitview.orthogonalSize;
        });
        this.addDisposables(this._onDidChange, this.splitview.onDidSashEnd(() => {
            this._onDidChange.fire(undefined);
        }), this.splitview.onDidAddView(() => {
            this._onDidChange.fire();
        }), this.splitview.onDidRemoveView(() => {
            this._onDidChange.fire();
        }));
    }
    setViewVisible(index, visible) {
        this.splitview.setViewVisible(index, visible);
    }
    addPane(pane, size, index = this.splitview.length, skipLayout = false) {
        const disposable = pane.onDidChangeExpansionState(() => {
            this.setupAnimation();
            this._onDidChange.fire(undefined);
        });
        const paneItem = {
            pane,
            disposable: {
                dispose: () => {
                    disposable.dispose();
                },
            },
        };
        this.paneItems.splice(index, 0, paneItem);
        pane.orthogonalSize = this.splitview.orthogonalSize;
        this.splitview.addView(pane, size, index, skipLayout);
    }
    getViewSize(index) {
        return this.splitview.getViewSize(index);
    }
    getPanes() {
        return this.splitview.getViews();
    }
    removePane(index, options = { skipDispose: false }) {
        const paneItem = this.paneItems.splice(index, 1)[0];
        this.splitview.removeView(index);
        if (!options.skipDispose) {
            paneItem.disposable.dispose();
            paneItem.pane.dispose();
        }
        return paneItem;
    }
    moveView(from, to) {
        if (from === to) {
            return;
        }
        const view = this.removePane(from, { skipDispose: true });
        this.skipAnimation = true;
        try {
            this.addPane(view.pane, view.pane.size, to, false);
        }
        finally {
            this.skipAnimation = false;
        }
    }
    layout(size, orthogonalSize) {
        this.splitview.layout(size, orthogonalSize);
    }
    setupAnimation() {
        if (this.skipAnimation) {
            return;
        }
        if (this.animationTimer) {
            clearTimeout(this.animationTimer);
            this.animationTimer = undefined;
        }
        addClasses(this.element, 'dv-animated');
        this.animationTimer = setTimeout(() => {
            this.animationTimer = undefined;
            removeClasses(this.element, 'dv-animated');
        }, 200);
    }
    dispose() {
        super.dispose();
        if (this.animationTimer) {
            clearTimeout(this.animationTimer);
            this.animationTimer = undefined;
        }
        this.paneItems.forEach((paneItem) => {
            paneItem.disposable.dispose();
            paneItem.pane.dispose();
        });
        this.paneItems = [];
        this.splitview.dispose();
        this.element.remove();
    }
}
