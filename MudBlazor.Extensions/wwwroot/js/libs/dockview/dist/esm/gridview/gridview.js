/*---------------------------------------------------------------------------------------------
 * Accreditation: This file is largly based upon the MIT licenced VSCode sourcecode found at:
 * https://github.com/microsoft/vscode/tree/main/src/vs/base/browser/ui/grid
 *--------------------------------------------------------------------------------------------*/
import { Orientation, Sizing, } from '../splitview/splitview';
import { tail } from '../array';
import { LeafNode } from './leafNode';
import { BranchNode } from './branchNode';
import { Emitter } from '../events';
import { MutableDisposable } from '../lifecycle';
function findLeaf(candiateNode, last) {
    if (candiateNode instanceof LeafNode) {
        return candiateNode;
    }
    if (candiateNode instanceof BranchNode) {
        return findLeaf(candiateNode.children[last ? candiateNode.children.length - 1 : 0], last);
    }
    throw new Error('invalid node');
}
function cloneNode(node, size, orthogonalSize) {
    if (node instanceof BranchNode) {
        const result = new BranchNode(node.orientation, node.proportionalLayout, node.styles, size, orthogonalSize, node.disabled, node.margin);
        for (let i = node.children.length - 1; i >= 0; i--) {
            const child = node.children[i];
            result.addChild(cloneNode(child, child.size, child.orthogonalSize), child.size, 0, true);
        }
        return result;
    }
    else {
        return new LeafNode(node.view, node.orientation, orthogonalSize);
    }
}
function flipNode(node, size, orthogonalSize) {
    if (node instanceof BranchNode) {
        const result = new BranchNode(orthogonal(node.orientation), node.proportionalLayout, node.styles, size, orthogonalSize, node.disabled, node.margin);
        let totalSize = 0;
        for (let i = node.children.length - 1; i >= 0; i--) {
            const child = node.children[i];
            const childSize = child instanceof BranchNode ? child.orthogonalSize : child.size;
            let newSize = node.size === 0
                ? 0
                : Math.round((size * childSize) / node.size);
            totalSize += newSize;
            // The last view to add should adjust to rounding errors
            if (i === 0) {
                newSize += size - totalSize;
            }
            result.addChild(flipNode(child, orthogonalSize, newSize), newSize, 0, true);
        }
        return result;
    }
    else {
        return new LeafNode(node.view, orthogonal(node.orientation), orthogonalSize);
    }
}
export function indexInParent(element) {
    const parentElement = element.parentElement;
    if (!parentElement) {
        throw new Error('Invalid grid element');
    }
    let el = parentElement.firstElementChild;
    let index = 0;
    while (el !== element && el !== parentElement.lastElementChild && el) {
        el = el.nextElementSibling;
        index++;
    }
    return index;
}
/**
 * Find the grid location of a specific DOM element by traversing the parent
 * chain and finding each child index on the way.
 *
 * This will break as soon as DOM structures of the Splitview or Gridview change.
 */
export function getGridLocation(element) {
    const parentElement = element.parentElement;
    if (!parentElement) {
        throw new Error('Invalid grid element');
    }
    if (/\bdv-grid-view\b/.test(parentElement.className)) {
        return [];
    }
    const index = indexInParent(parentElement);
    const ancestor = parentElement.parentElement.parentElement.parentElement;
    return [...getGridLocation(ancestor), index];
}
export function getRelativeLocation(rootOrientation, location, direction) {
    const orientation = getLocationOrientation(rootOrientation, location);
    const directionOrientation = getDirectionOrientation(direction);
    if (orientation === directionOrientation) {
        const [rest, _index] = tail(location);
        let index = _index;
        if (direction === 'right' || direction === 'bottom') {
            index += 1;
        }
        return [...rest, index];
    }
    else {
        const index = direction === 'right' || direction === 'bottom' ? 1 : 0;
        return [...location, index];
    }
}
export function getDirectionOrientation(direction) {
    return direction === 'top' || direction === 'bottom'
        ? Orientation.VERTICAL
        : Orientation.HORIZONTAL;
}
export function getLocationOrientation(rootOrientation, location) {
    return location.length % 2 === 0
        ? orthogonal(rootOrientation)
        : rootOrientation;
}
export const orthogonal = (orientation) => orientation === Orientation.HORIZONTAL
    ? Orientation.VERTICAL
    : Orientation.HORIZONTAL;
export function isGridBranchNode(node) {
    return !!node.children;
}
const serializeBranchNode = (node, orientation) => {
    const size = orientation === Orientation.VERTICAL ? node.box.width : node.box.height;
    if (!isGridBranchNode(node)) {
        if (typeof node.cachedVisibleSize === 'number') {
            return {
                type: 'leaf',
                data: node.view.toJSON(),
                size: node.cachedVisibleSize,
                visible: false,
            };
        }
        return { type: 'leaf', data: node.view.toJSON(), size };
    }
    return {
        type: 'branch',
        data: node.children.map((c) => serializeBranchNode(c, orthogonal(orientation))),
        size,
    };
};
export class Gridview {
    get length() {
        return this._root ? this._root.children.length : 0;
    }
    get orientation() {
        return this.root.orientation;
    }
    set orientation(orientation) {
        if (this.root.orientation === orientation) {
            return;
        }
        const { size, orthogonalSize } = this.root;
        this.root = flipNode(this.root, orthogonalSize, size);
        this.root.layout(size, orthogonalSize);
    }
    get width() {
        return this.root.width;
    }
    get height() {
        return this.root.height;
    }
    get minimumWidth() {
        return this.root.minimumWidth;
    }
    get minimumHeight() {
        return this.root.minimumHeight;
    }
    get maximumWidth() {
        return this.root.maximumHeight;
    }
    get maximumHeight() {
        return this.root.maximumHeight;
    }
    get locked() {
        return this._locked;
    }
    set locked(value) {
        this._locked = value;
        const branch = [this.root];
        /**
         * simple depth-first-search to cover all nodes
         *
         * @see https://en.wikipedia.org/wiki/Depth-first_search
         */
        while (branch.length > 0) {
            const node = branch.pop();
            if (node instanceof BranchNode) {
                node.disabled = value;
                branch.push(...node.children);
            }
        }
    }
    get margin() {
        return this._margin;
    }
    set margin(value) {
        this._margin = value;
        this.root.margin = value;
    }
    maximizedView() {
        var _a;
        return (_a = this._maximizedNode) === null || _a === void 0 ? void 0 : _a.leaf.view;
    }
    hasMaximizedView() {
        return this._maximizedNode !== undefined;
    }
    maximizeView(view) {
        var _a;
        const location = getGridLocation(view.element);
        const [_, node] = this.getNode(location);
        if (!(node instanceof LeafNode)) {
            return;
        }
        if (((_a = this._maximizedNode) === null || _a === void 0 ? void 0 : _a.leaf) === node) {
            return;
        }
        if (this.hasMaximizedView()) {
            this.exitMaximizedView();
        }
        serializeBranchNode(this.getView(), this.orientation);
        const hiddenOnMaximize = [];
        function hideAllViewsBut(parent, exclude) {
            for (let i = 0; i < parent.children.length; i++) {
                const child = parent.children[i];
                if (child instanceof LeafNode) {
                    if (child !== exclude) {
                        if (parent.isChildVisible(i)) {
                            parent.setChildVisible(i, false);
                        }
                        else {
                            hiddenOnMaximize.push(child);
                        }
                    }
                }
                else {
                    hideAllViewsBut(child, exclude);
                }
            }
        }
        hideAllViewsBut(this.root, node);
        this._maximizedNode = { leaf: node, hiddenOnMaximize };
        this._onDidMaximizedNodeChange.fire({
            view: node.view,
            isMaximized: true,
        });
    }
    exitMaximizedView() {
        if (!this._maximizedNode) {
            return;
        }
        const hiddenOnMaximize = this._maximizedNode.hiddenOnMaximize;
        function showViewsInReverseOrder(parent) {
            for (let index = parent.children.length - 1; index >= 0; index--) {
                const child = parent.children[index];
                if (child instanceof LeafNode) {
                    if (!hiddenOnMaximize.includes(child)) {
                        parent.setChildVisible(index, true);
                    }
                }
                else {
                    showViewsInReverseOrder(child);
                }
            }
        }
        showViewsInReverseOrder(this.root);
        const tmp = this._maximizedNode.leaf;
        this._maximizedNode = undefined;
        this._onDidMaximizedNodeChange.fire({
            view: tmp.view,
            isMaximized: false,
        });
    }
    serialize() {
        const maximizedView = this.maximizedView();
        let maxmizedViewLocation;
        if (maximizedView) {
            /**
             * The minimum information we can get away with in order to serialize a maxmized view is it's location within the grid
             * which is represented as a branch of indices
             */
            maxmizedViewLocation = getGridLocation(maximizedView.element);
        }
        if (this.hasMaximizedView()) {
            /**
             * the saved layout cannot be in its maxmized state otherwise all of the underlying
             * view dimensions will be wrong
             *
             * To counteract this we temporaily remove the maximized view to compute the serialized output
             * of the grid before adding back the maxmized view as to not alter the layout from the users
             * perspective when `.toJSON()` is called
             */
            this.exitMaximizedView();
        }
        const root = serializeBranchNode(this.getView(), this.orientation);
        const resullt = {
            root,
            width: this.width,
            height: this.height,
            orientation: this.orientation,
        };
        if (maxmizedViewLocation) {
            resullt.maximizedNode = {
                location: maxmizedViewLocation,
            };
        }
        if (maximizedView) {
            // replace any maximzied view that was removed for serialization purposes
            this.maximizeView(maximizedView);
        }
        return resullt;
    }
    dispose() {
        this.disposable.dispose();
        this._onDidChange.dispose();
        this._onDidMaximizedNodeChange.dispose();
        this._onDidViewVisibilityChange.dispose();
        this.root.dispose();
        this._maximizedNode = undefined;
        this.element.remove();
    }
    clear() {
        const orientation = this.root.orientation;
        this.root = new BranchNode(orientation, this.proportionalLayout, this.styles, this.root.size, this.root.orthogonalSize, this.locked, this.margin);
    }
    deserialize(json, deserializer) {
        const orientation = json.orientation;
        const height = orientation === Orientation.VERTICAL ? json.height : json.width;
        this._deserialize(json.root, orientation, deserializer, height);
        /**
         * The deserialied layout must be positioned through this.layout(...)
         * before any maximizedNode can be positioned
         */
        this.layout(json.width, json.height);
        if (json.maximizedNode) {
            const location = json.maximizedNode.location;
            const [_, node] = this.getNode(location);
            if (!(node instanceof LeafNode)) {
                return;
            }
            this.maximizeView(node.view);
        }
    }
    _deserialize(root, orientation, deserializer, orthogonalSize) {
        this.root = this._deserializeNode(root, orientation, deserializer, orthogonalSize);
    }
    _deserializeNode(node, orientation, deserializer, orthogonalSize) {
        var _a;
        let result;
        if (node.type === 'branch') {
            const serializedChildren = node.data;
            const children = serializedChildren.map((serializedChild) => {
                return {
                    node: this._deserializeNode(serializedChild, orthogonal(orientation), deserializer, node.size),
                    visible: serializedChild.visible,
                };
            });
            result = new BranchNode(orientation, this.proportionalLayout, this.styles, node.size, // <- orthogonal size - flips at each depth
            orthogonalSize, // <- size - flips at each depth,
            this.locked, this.margin, children);
        }
        else {
            const view = deserializer.fromJSON(node);
            if (typeof node.visible === 'boolean') {
                (_a = view.setVisible) === null || _a === void 0 ? void 0 : _a.call(view, node.visible);
            }
            result = new LeafNode(view, orientation, orthogonalSize, node.size);
        }
        return result;
    }
    get root() {
        return this._root;
    }
    set root(root) {
        const oldRoot = this._root;
        if (oldRoot) {
            oldRoot.dispose();
            this._maximizedNode = undefined;
            this.element.removeChild(oldRoot.element);
        }
        this._root = root;
        this.element.appendChild(this._root.element);
        this.disposable.value = this._root.onDidChange((e) => {
            this._onDidChange.fire(e);
        });
    }
    normalize() {
        if (!this._root) {
            return;
        }
        if (this._root.children.length !== 1) {
            return;
        }
        const oldRoot = this.root;
        // can remove one level of redundant branching if there is only a single child
        const childReference = oldRoot.children[0];
        if (childReference instanceof LeafNode) {
            return;
        }
        oldRoot.element.remove();
        const child = oldRoot.removeChild(0); // Remove child to prevent double disposal
        oldRoot.dispose(); // Dispose old root (won't dispose removed child)
        child.dispose(); // Dispose the removed child
        this._root = cloneNode(childReference, childReference.size, childReference.orthogonalSize);
        this.element.appendChild(this._root.element);
        this.disposable.value = this._root.onDidChange((e) => {
            this._onDidChange.fire(e);
        });
    }
    /**
     * If the root is orientated as a VERTICAL node then nest the existing root within a new HORIZIONTAL root node
     * If the root is orientated as a HORIZONTAL node then nest the existing root within a new VERITCAL root node
     */
    insertOrthogonalSplitviewAtRoot() {
        if (!this._root) {
            return;
        }
        const oldRoot = this.root;
        oldRoot.element.remove();
        this._root = new BranchNode(orthogonal(oldRoot.orientation), this.proportionalLayout, this.styles, this.root.orthogonalSize, this.root.size, this.locked, this.margin);
        if (oldRoot.children.length === 0) {
            // no data so no need to add anything back in
        }
        else if (oldRoot.children.length === 1) {
            // can remove one level of redundant branching if there is only a single child
            const childReference = oldRoot.children[0];
            const child = oldRoot.removeChild(0); // remove to prevent disposal when disposing of unwanted root
            child.dispose();
            oldRoot.dispose();
            this._root.addChild(
            /**
             * the child node will have the same orientation as the new root since
             * we are removing the inbetween node.
             * the entire 'tree' must be flipped recursively to ensure that the orientation
             * flips at each level
             */
            flipNode(childReference, childReference.orthogonalSize, childReference.size), Sizing.Distribute, 0);
        }
        else {
            this._root.addChild(oldRoot, Sizing.Distribute, 0);
        }
        this.element.appendChild(this._root.element);
        this.disposable.value = this._root.onDidChange((e) => {
            this._onDidChange.fire(e);
        });
    }
    next(location) {
        return this.progmaticSelect(location);
    }
    previous(location) {
        return this.progmaticSelect(location, true);
    }
    getView(location) {
        const node = location ? this.getNode(location)[1] : this.root;
        return this._getViews(node, this.orientation);
    }
    _getViews(node, orientation, cachedVisibleSize) {
        const box = { height: node.height, width: node.width };
        if (node instanceof LeafNode) {
            return { box, view: node.view, cachedVisibleSize };
        }
        const children = [];
        for (let i = 0; i < node.children.length; i++) {
            const child = node.children[i];
            const nodeCachedVisibleSize = node.getChildCachedVisibleSize(i);
            children.push(this._getViews(child, orthogonal(orientation), nodeCachedVisibleSize));
        }
        return { box, children };
    }
    progmaticSelect(location, reverse = false) {
        const [path, node] = this.getNode(location);
        if (!(node instanceof LeafNode)) {
            throw new Error('invalid location');
        }
        for (let i = path.length - 1; i > -1; i--) {
            const n = path[i];
            const l = location[i] || 0;
            const canProgressInCurrentLevel = reverse
                ? l - 1 > -1
                : l + 1 < n.children.length;
            if (canProgressInCurrentLevel) {
                return findLeaf(n.children[reverse ? l - 1 : l + 1], reverse);
            }
        }
        return findLeaf(this.root, reverse);
    }
    constructor(proportionalLayout, styles, orientation, locked, margin) {
        this.proportionalLayout = proportionalLayout;
        this.styles = styles;
        this._locked = false;
        this._margin = 0;
        this._maximizedNode = undefined;
        this.disposable = new MutableDisposable();
        this._onDidChange = new Emitter();
        this.onDidChange = this._onDidChange.event;
        this._onDidViewVisibilityChange = new Emitter();
        this.onDidViewVisibilityChange = this._onDidViewVisibilityChange.event;
        this._onDidMaximizedNodeChange = new Emitter();
        this.onDidMaximizedNodeChange = this._onDidMaximizedNodeChange.event;
        this.element = document.createElement('div');
        this.element.className = 'dv-grid-view';
        this._locked = locked !== null && locked !== void 0 ? locked : false;
        this._margin = margin !== null && margin !== void 0 ? margin : 0;
        this.root = new BranchNode(orientation, proportionalLayout, styles, 0, 0, this.locked, this.margin);
    }
    isViewVisible(location) {
        const [rest, index] = tail(location);
        const [, parent] = this.getNode(rest);
        if (!(parent instanceof BranchNode)) {
            throw new Error('Invalid from location');
        }
        return parent.isChildVisible(index);
    }
    setViewVisible(location, visible) {
        if (this.hasMaximizedView()) {
            this.exitMaximizedView();
        }
        const [rest, index] = tail(location);
        const [, parent] = this.getNode(rest);
        if (!(parent instanceof BranchNode)) {
            throw new Error('Invalid from location');
        }
        this._onDidViewVisibilityChange.fire();
        parent.setChildVisible(index, visible);
    }
    moveView(parentLocation, from, to) {
        if (this.hasMaximizedView()) {
            this.exitMaximizedView();
        }
        const [, parent] = this.getNode(parentLocation);
        if (!(parent instanceof BranchNode)) {
            throw new Error('Invalid location');
        }
        parent.moveChild(from, to);
    }
    addView(view, size, location) {
        if (this.hasMaximizedView()) {
            this.exitMaximizedView();
        }
        const [rest, index] = tail(location);
        const [pathToParent, parent] = this.getNode(rest);
        if (parent instanceof BranchNode) {
            const node = new LeafNode(view, orthogonal(parent.orientation), parent.orthogonalSize);
            parent.addChild(node, size, index);
        }
        else {
            const [grandParent, ..._] = [...pathToParent].reverse();
            const [parentIndex, ...__] = [...rest].reverse();
            let newSiblingSize = 0;
            const newSiblingCachedVisibleSize = grandParent.getChildCachedVisibleSize(parentIndex);
            if (typeof newSiblingCachedVisibleSize === 'number') {
                newSiblingSize = Sizing.Invisible(newSiblingCachedVisibleSize);
            }
            const child = grandParent.removeChild(parentIndex);
            child.dispose();
            const newParent = new BranchNode(parent.orientation, this.proportionalLayout, this.styles, parent.size, parent.orthogonalSize, this.locked, this.margin);
            grandParent.addChild(newParent, parent.size, parentIndex);
            const newSibling = new LeafNode(parent.view, grandParent.orientation, parent.size);
            newParent.addChild(newSibling, newSiblingSize, 0);
            if (typeof size !== 'number' && size.type === 'split') {
                size = { type: 'split', index: 0 };
            }
            const node = new LeafNode(view, grandParent.orientation, parent.size);
            newParent.addChild(node, size, index);
        }
    }
    remove(view, sizing) {
        const location = getGridLocation(view.element);
        return this.removeView(location, sizing);
    }
    removeView(location, sizing) {
        if (this.hasMaximizedView()) {
            this.exitMaximizedView();
        }
        const [rest, index] = tail(location);
        const [pathToParent, parent] = this.getNode(rest);
        if (!(parent instanceof BranchNode)) {
            throw new Error('Invalid location');
        }
        const nodeToRemove = parent.children[index];
        if (!(nodeToRemove instanceof LeafNode)) {
            throw new Error('Invalid location');
        }
        parent.removeChild(index, sizing);
        nodeToRemove.dispose();
        if (parent.children.length !== 1) {
            return nodeToRemove.view;
        }
        // if the parent has only one child and we know the parent is a BranchNode we can make the tree
        // more efficiently spaced by replacing the parent BranchNode with the child.
        // if that child is a LeafNode then we simply replace the BranchNode with the child otherwise if the child
        // is a BranchNode too we should spread it's children into the grandparent.
        // refer to the remaining child as the sibling
        const sibling = parent.children[0];
        if (pathToParent.length === 0) {
            // if the parent is root
            if (sibling instanceof LeafNode) {
                // if the sibling is a leaf node no action is required
                return nodeToRemove.view;
            }
            // otherwise the sibling is a branch node. since the parent is the root and the root has only one child
            // which is a branch node we can just set this branch node to be the new root node
            // for good housekeeping we'll removing the sibling from it's existing tree
            parent.removeChild(0, sizing);
            // and set that sibling node to be root
            this.root = sibling;
            return nodeToRemove.view;
        }
        // otherwise the parent is apart of a large sub-tree
        const [grandParent, ..._] = [...pathToParent].reverse();
        const [parentIndex, ...__] = [...rest].reverse();
        const isSiblingVisible = parent.isChildVisible(0);
        // either way we need to remove the sibling from it's existing tree
        parent.removeChild(0, sizing);
        // note the sizes of all of the grandparents children
        const sizes = grandParent.children.map((_size, i) => grandParent.getChildSize(i));
        // remove the parent from the grandparent since we are moving the sibling to take the parents place
        // this parent is no longer used and can be disposed of
        grandParent.removeChild(parentIndex, sizing).dispose();
        if (sibling instanceof BranchNode) {
            // replace the parent with the siblings children
            sizes.splice(parentIndex, 1, ...sibling.children.map((c) => c.size));
            // and add those siblings to the grandparent
            for (let i = 0; i < sibling.children.length; i++) {
                const child = sibling.children[i];
                grandParent.addChild(child, child.size, parentIndex + i);
            }
            /**
             * clean down the branch node since we need to dipose of it and
             * when .dispose() it called on a branch it will dispose of any
             * views it is holding onto.
             */
            while (sibling.children.length > 0) {
                sibling.removeChild(0);
            }
        }
        else {
            // otherwise create a new leaf node and add that to the grandparent
            const newSibling = new LeafNode(sibling.view, orthogonal(sibling.orientation), sibling.size);
            const siblingSizing = isSiblingVisible
                ? sibling.orthogonalSize
                : Sizing.Invisible(sibling.orthogonalSize);
            grandParent.addChild(newSibling, siblingSizing, parentIndex);
        }
        // the containing node of the sibling is no longer required and can be disposed of
        sibling.dispose();
        // resize everything
        for (let i = 0; i < sizes.length; i++) {
            grandParent.resizeChild(i, sizes[i]);
        }
        return nodeToRemove.view;
    }
    layout(width, height) {
        const [size, orthogonalSize] = this.root.orientation === Orientation.HORIZONTAL
            ? [height, width]
            : [width, height];
        this.root.layout(size, orthogonalSize);
    }
    getNode(location, node = this.root, path = []) {
        if (location.length === 0) {
            return [path, node];
        }
        if (!(node instanceof BranchNode)) {
            throw new Error('Invalid location');
        }
        const [index, ...rest] = location;
        if (index < 0 || index >= node.children.length) {
            throw new Error('Invalid location');
        }
        const child = node.children[index];
        path.push(node);
        return this.getNode(rest, child, path);
    }
}
