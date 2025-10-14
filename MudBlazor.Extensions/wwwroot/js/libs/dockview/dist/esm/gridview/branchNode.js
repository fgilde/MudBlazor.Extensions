/*---------------------------------------------------------------------------------------------
 * Accreditation: This file is largly based upon the MIT licenced VSCode sourcecode found at:
 * https://github.com/microsoft/vscode/tree/main/src/vs/base/browser/ui/grid
 *--------------------------------------------------------------------------------------------*/
import { Splitview, Orientation, LayoutPriority, } from '../splitview/splitview';
import { Emitter, Event } from '../events';
import { LeafNode } from './leafNode';
import { CompositeDisposable, Disposable } from '../lifecycle';
export class BranchNode extends CompositeDisposable {
    get width() {
        return this.orientation === Orientation.HORIZONTAL
            ? this.size
            : this.orthogonalSize;
    }
    get height() {
        return this.orientation === Orientation.HORIZONTAL
            ? this.orthogonalSize
            : this.size;
    }
    get minimumSize() {
        return this.children.length === 0
            ? 0
            : Math.max(...this.children.map((c, index) => this.splitview.isViewVisible(index)
                ? c.minimumOrthogonalSize
                : 0));
    }
    get maximumSize() {
        return Math.min(...this.children.map((c, index) => this.splitview.isViewVisible(index)
            ? c.maximumOrthogonalSize
            : Number.POSITIVE_INFINITY));
    }
    get minimumOrthogonalSize() {
        return this.splitview.minimumSize;
    }
    get maximumOrthogonalSize() {
        return this.splitview.maximumSize;
    }
    get orthogonalSize() {
        return this._orthogonalSize;
    }
    get size() {
        return this._size;
    }
    get minimumWidth() {
        return this.orientation === Orientation.HORIZONTAL
            ? this.minimumOrthogonalSize
            : this.minimumSize;
    }
    get minimumHeight() {
        return this.orientation === Orientation.HORIZONTAL
            ? this.minimumSize
            : this.minimumOrthogonalSize;
    }
    get maximumWidth() {
        return this.orientation === Orientation.HORIZONTAL
            ? this.maximumOrthogonalSize
            : this.maximumSize;
    }
    get maximumHeight() {
        return this.orientation === Orientation.HORIZONTAL
            ? this.maximumSize
            : this.maximumOrthogonalSize;
    }
    get priority() {
        if (this.children.length === 0) {
            return LayoutPriority.Normal;
        }
        const priorities = this.children.map((c) => typeof c.priority === 'undefined'
            ? LayoutPriority.Normal
            : c.priority);
        if (priorities.some((p) => p === LayoutPriority.High)) {
            return LayoutPriority.High;
        }
        else if (priorities.some((p) => p === LayoutPriority.Low)) {
            return LayoutPriority.Low;
        }
        return LayoutPriority.Normal;
    }
    get disabled() {
        return this.splitview.disabled;
    }
    set disabled(value) {
        this.splitview.disabled = value;
    }
    get margin() {
        return this.splitview.margin;
    }
    set margin(value) {
        this.splitview.margin = value;
        this.children.forEach((child) => {
            if (child instanceof BranchNode) {
                child.margin = value;
            }
        });
    }
    constructor(orientation, proportionalLayout, styles, size, orthogonalSize, disabled, margin, childDescriptors) {
        super();
        this.orientation = orientation;
        this.proportionalLayout = proportionalLayout;
        this.styles = styles;
        this._childrenDisposable = Disposable.NONE;
        this.children = [];
        this._onDidChange = new Emitter();
        this.onDidChange = this._onDidChange.event;
        this._onDidVisibilityChange = new Emitter();
        this.onDidVisibilityChange = this._onDidVisibilityChange.event;
        this._orthogonalSize = orthogonalSize;
        this._size = size;
        this.element = document.createElement('div');
        this.element.className = 'dv-branch-node';
        if (!childDescriptors) {
            this.splitview = new Splitview(this.element, {
                orientation: this.orientation,
                proportionalLayout,
                styles,
                margin,
            });
            this.splitview.layout(this.size, this.orthogonalSize);
        }
        else {
            const descriptor = {
                views: childDescriptors.map((childDescriptor) => {
                    return {
                        view: childDescriptor.node,
                        size: childDescriptor.node.size,
                        visible: childDescriptor.node instanceof LeafNode &&
                            childDescriptor.visible !== undefined
                            ? childDescriptor.visible
                            : true,
                    };
                }),
                size: this.orthogonalSize,
            };
            this.children = childDescriptors.map((c) => c.node);
            this.splitview = new Splitview(this.element, {
                orientation: this.orientation,
                descriptor,
                proportionalLayout,
                styles,
                margin,
            });
        }
        this.disabled = disabled;
        this.addDisposables(this._onDidChange, this._onDidVisibilityChange, this.splitview.onDidSashEnd(() => {
            this._onDidChange.fire({});
        }));
        this.setupChildrenEvents();
    }
    setVisible(_visible) {
        // noop
    }
    isChildVisible(index) {
        if (index < 0 || index >= this.children.length) {
            throw new Error('Invalid index');
        }
        return this.splitview.isViewVisible(index);
    }
    setChildVisible(index, visible) {
        if (index < 0 || index >= this.children.length) {
            throw new Error('Invalid index');
        }
        if (this.splitview.isViewVisible(index) === visible) {
            return;
        }
        const wereAllChildrenHidden = this.splitview.contentSize === 0;
        this.splitview.setViewVisible(index, visible);
        // }
        const areAllChildrenHidden = this.splitview.contentSize === 0;
        // If all children are hidden then the parent should hide the entire splitview
        // If the entire splitview is hidden then the parent should show the splitview when a child is shown
        if ((visible && wereAllChildrenHidden) ||
            (!visible && areAllChildrenHidden)) {
            this._onDidVisibilityChange.fire({ visible });
        }
    }
    moveChild(from, to) {
        if (from === to) {
            return;
        }
        if (from < 0 || from >= this.children.length) {
            throw new Error('Invalid from index');
        }
        if (from < to) {
            to--;
        }
        this.splitview.moveView(from, to);
        const child = this._removeChild(from);
        this._addChild(child, to);
    }
    getChildSize(index) {
        if (index < 0 || index >= this.children.length) {
            throw new Error('Invalid index');
        }
        return this.splitview.getViewSize(index);
    }
    resizeChild(index, size) {
        if (index < 0 || index >= this.children.length) {
            throw new Error('Invalid index');
        }
        this.splitview.resizeView(index, size);
    }
    layout(size, orthogonalSize) {
        this._size = orthogonalSize;
        this._orthogonalSize = size;
        this.splitview.layout(orthogonalSize, size);
    }
    addChild(node, size, index, skipLayout) {
        if (index < 0 || index > this.children.length) {
            throw new Error('Invalid index');
        }
        this.splitview.addView(node, size, index, skipLayout);
        this._addChild(node, index);
    }
    getChildCachedVisibleSize(index) {
        if (index < 0 || index >= this.children.length) {
            throw new Error('Invalid index');
        }
        return this.splitview.getViewCachedVisibleSize(index);
    }
    removeChild(index, sizing) {
        if (index < 0 || index >= this.children.length) {
            throw new Error('Invalid index');
        }
        this.splitview.removeView(index, sizing);
        return this._removeChild(index);
    }
    _addChild(node, index) {
        this.children.splice(index, 0, node);
        this.setupChildrenEvents();
    }
    _removeChild(index) {
        const [child] = this.children.splice(index, 1);
        this.setupChildrenEvents();
        return child;
    }
    setupChildrenEvents() {
        this._childrenDisposable.dispose();
        this._childrenDisposable = new CompositeDisposable(Event.any(...this.children.map((c) => c.onDidChange))((e) => {
            /**
             * indicate a change has occured to allows any re-rendering but don't bubble
             * event because that was specific to this branch
             */
            this._onDidChange.fire({ size: e.orthogonalSize });
        }), ...this.children.map((c, i) => {
            if (c instanceof BranchNode) {
                return c.onDidVisibilityChange(({ visible }) => {
                    this.setChildVisible(i, visible);
                });
            }
            return Disposable.NONE;
        }));
    }
    dispose() {
        this._childrenDisposable.dispose();
        this.splitview.dispose();
        this.children.forEach((child) => child.dispose());
        super.dispose();
    }
}
