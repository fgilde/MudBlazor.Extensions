"use strict";
/*---------------------------------------------------------------------------------------------
 * Accreditation: This file is largly based upon the MIT licenced VSCode sourcecode found at:
 * https://github.com/microsoft/vscode/tree/main/src/vs/base/browser/ui/grid
 *--------------------------------------------------------------------------------------------*/
var __read = (this && this.__read) || function (o, n) {
    var m = typeof Symbol === "function" && o[Symbol.iterator];
    if (!m) return o;
    var i = m.call(o), r, ar = [], e;
    try {
        while ((n === void 0 || n-- > 0) && !(r = i.next()).done) ar.push(r.value);
    }
    catch (error) { e = { error: error }; }
    finally {
        try {
            if (r && !r.done && (m = i["return"])) m.call(i);
        }
        finally { if (e) throw e.error; }
    }
    return ar;
};
var __spreadArray = (this && this.__spreadArray) || function (to, from, pack) {
    if (pack || arguments.length === 2) for (var i = 0, l = from.length, ar; i < l; i++) {
        if (ar || !(i in from)) {
            if (!ar) ar = Array.prototype.slice.call(from, 0, i);
            ar[i] = from[i];
        }
    }
    return to.concat(ar || Array.prototype.slice.call(from));
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.Gridview = exports.isGridBranchNode = exports.orthogonal = exports.getLocationOrientation = exports.getDirectionOrientation = exports.getRelativeLocation = exports.getGridLocation = exports.indexInParent = void 0;
var splitview_1 = require("../splitview/splitview");
var array_1 = require("../array");
var leafNode_1 = require("./leafNode");
var branchNode_1 = require("./branchNode");
var events_1 = require("../events");
var lifecycle_1 = require("../lifecycle");
function findLeaf(candiateNode, last) {
    if (candiateNode instanceof leafNode_1.LeafNode) {
        return candiateNode;
    }
    if (candiateNode instanceof branchNode_1.BranchNode) {
        return findLeaf(candiateNode.children[last ? candiateNode.children.length - 1 : 0], last);
    }
    throw new Error('invalid node');
}
function cloneNode(node, size, orthogonalSize) {
    if (node instanceof branchNode_1.BranchNode) {
        var result = new branchNode_1.BranchNode(node.orientation, node.proportionalLayout, node.styles, size, orthogonalSize, node.disabled, node.margin);
        for (var i = node.children.length - 1; i >= 0; i--) {
            var child = node.children[i];
            result.addChild(cloneNode(child, child.size, child.orthogonalSize), child.size, 0, true);
        }
        return result;
    }
    else {
        return new leafNode_1.LeafNode(node.view, node.orientation, orthogonalSize);
    }
}
function flipNode(node, size, orthogonalSize) {
    if (node instanceof branchNode_1.BranchNode) {
        var result = new branchNode_1.BranchNode((0, exports.orthogonal)(node.orientation), node.proportionalLayout, node.styles, size, orthogonalSize, node.disabled, node.margin);
        var totalSize = 0;
        for (var i = node.children.length - 1; i >= 0; i--) {
            var child = node.children[i];
            var childSize = child instanceof branchNode_1.BranchNode ? child.orthogonalSize : child.size;
            var newSize = node.size === 0
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
        return new leafNode_1.LeafNode(node.view, (0, exports.orthogonal)(node.orientation), orthogonalSize);
    }
}
function indexInParent(element) {
    var parentElement = element.parentElement;
    if (!parentElement) {
        throw new Error('Invalid grid element');
    }
    var el = parentElement.firstElementChild;
    var index = 0;
    while (el !== element && el !== parentElement.lastElementChild && el) {
        el = el.nextElementSibling;
        index++;
    }
    return index;
}
exports.indexInParent = indexInParent;
/**
 * Find the grid location of a specific DOM element by traversing the parent
 * chain and finding each child index on the way.
 *
 * This will break as soon as DOM structures of the Splitview or Gridview change.
 */
function getGridLocation(element) {
    var parentElement = element.parentElement;
    if (!parentElement) {
        throw new Error('Invalid grid element');
    }
    if (/\bdv-grid-view\b/.test(parentElement.className)) {
        return [];
    }
    var index = indexInParent(parentElement);
    var ancestor = parentElement.parentElement.parentElement.parentElement;
    return __spreadArray(__spreadArray([], __read(getGridLocation(ancestor)), false), [index], false);
}
exports.getGridLocation = getGridLocation;
function getRelativeLocation(rootOrientation, location, direction) {
    var orientation = getLocationOrientation(rootOrientation, location);
    var directionOrientation = getDirectionOrientation(direction);
    if (orientation === directionOrientation) {
        var _a = __read((0, array_1.tail)(location), 2), rest = _a[0], _index = _a[1];
        var index = _index;
        if (direction === 'right' || direction === 'bottom') {
            index += 1;
        }
        return __spreadArray(__spreadArray([], __read(rest), false), [index], false);
    }
    else {
        var index = direction === 'right' || direction === 'bottom' ? 1 : 0;
        return __spreadArray(__spreadArray([], __read(location), false), [index], false);
    }
}
exports.getRelativeLocation = getRelativeLocation;
function getDirectionOrientation(direction) {
    return direction === 'top' || direction === 'bottom'
        ? splitview_1.Orientation.VERTICAL
        : splitview_1.Orientation.HORIZONTAL;
}
exports.getDirectionOrientation = getDirectionOrientation;
function getLocationOrientation(rootOrientation, location) {
    return location.length % 2 === 0
        ? (0, exports.orthogonal)(rootOrientation)
        : rootOrientation;
}
exports.getLocationOrientation = getLocationOrientation;
var orthogonal = function (orientation) {
    return orientation === splitview_1.Orientation.HORIZONTAL
        ? splitview_1.Orientation.VERTICAL
        : splitview_1.Orientation.HORIZONTAL;
};
exports.orthogonal = orthogonal;
function isGridBranchNode(node) {
    return !!node.children;
}
exports.isGridBranchNode = isGridBranchNode;
var serializeBranchNode = function (node, orientation) {
    var size = orientation === splitview_1.Orientation.VERTICAL ? node.box.width : node.box.height;
    if (!isGridBranchNode(node)) {
        if (typeof node.cachedVisibleSize === 'number') {
            return {
                type: 'leaf',
                data: node.view.toJSON(),
                size: node.cachedVisibleSize,
                visible: false,
            };
        }
        return { type: 'leaf', data: node.view.toJSON(), size: size };
    }
    return {
        type: 'branch',
        data: node.children.map(function (c) {
            return serializeBranchNode(c, (0, exports.orthogonal)(orientation));
        }),
        size: size,
    };
};
var Gridview = /** @class */ (function () {
    function Gridview(proportionalLayout, styles, orientation, locked, margin) {
        this.proportionalLayout = proportionalLayout;
        this.styles = styles;
        this._locked = false;
        this._margin = 0;
        this._maximizedNode = undefined;
        this.disposable = new lifecycle_1.MutableDisposable();
        this._onDidChange = new events_1.Emitter();
        this.onDidChange = this._onDidChange.event;
        this._onDidViewVisibilityChange = new events_1.Emitter();
        this.onDidViewVisibilityChange = this._onDidViewVisibilityChange.event;
        this._onDidMaximizedNodeChange = new events_1.Emitter();
        this.onDidMaximizedNodeChange = this._onDidMaximizedNodeChange.event;
        this.element = document.createElement('div');
        this.element.className = 'dv-grid-view';
        this._locked = locked !== null && locked !== void 0 ? locked : false;
        this._margin = margin !== null && margin !== void 0 ? margin : 0;
        this.root = new branchNode_1.BranchNode(orientation, proportionalLayout, styles, 0, 0, this.locked, this.margin);
    }
    Object.defineProperty(Gridview.prototype, "length", {
        get: function () {
            return this._root ? this._root.children.length : 0;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(Gridview.prototype, "orientation", {
        get: function () {
            return this.root.orientation;
        },
        set: function (orientation) {
            if (this.root.orientation === orientation) {
                return;
            }
            var _a = this.root, size = _a.size, orthogonalSize = _a.orthogonalSize;
            this.root = flipNode(this.root, orthogonalSize, size);
            this.root.layout(size, orthogonalSize);
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(Gridview.prototype, "width", {
        get: function () {
            return this.root.width;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(Gridview.prototype, "height", {
        get: function () {
            return this.root.height;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(Gridview.prototype, "minimumWidth", {
        get: function () {
            return this.root.minimumWidth;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(Gridview.prototype, "minimumHeight", {
        get: function () {
            return this.root.minimumHeight;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(Gridview.prototype, "maximumWidth", {
        get: function () {
            return this.root.maximumHeight;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(Gridview.prototype, "maximumHeight", {
        get: function () {
            return this.root.maximumHeight;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(Gridview.prototype, "locked", {
        get: function () {
            return this._locked;
        },
        set: function (value) {
            this._locked = value;
            var branch = [this.root];
            /**
             * simple depth-first-search to cover all nodes
             *
             * @see https://en.wikipedia.org/wiki/Depth-first_search
             */
            while (branch.length > 0) {
                var node = branch.pop();
                if (node instanceof branchNode_1.BranchNode) {
                    node.disabled = value;
                    branch.push.apply(branch, __spreadArray([], __read(node.children), false));
                }
            }
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(Gridview.prototype, "margin", {
        get: function () {
            return this._margin;
        },
        set: function (value) {
            this._margin = value;
            this.root.margin = value;
        },
        enumerable: false,
        configurable: true
    });
    Gridview.prototype.maximizedView = function () {
        var _a;
        return (_a = this._maximizedNode) === null || _a === void 0 ? void 0 : _a.leaf.view;
    };
    Gridview.prototype.hasMaximizedView = function () {
        return this._maximizedNode !== undefined;
    };
    Gridview.prototype.maximizeView = function (view) {
        var _a;
        var location = getGridLocation(view.element);
        var _b = __read(this.getNode(location), 2), _ = _b[0], node = _b[1];
        if (!(node instanceof leafNode_1.LeafNode)) {
            return;
        }
        if (((_a = this._maximizedNode) === null || _a === void 0 ? void 0 : _a.leaf) === node) {
            return;
        }
        if (this.hasMaximizedView()) {
            this.exitMaximizedView();
        }
        serializeBranchNode(this.getView(), this.orientation);
        var hiddenOnMaximize = [];
        function hideAllViewsBut(parent, exclude) {
            for (var i = 0; i < parent.children.length; i++) {
                var child = parent.children[i];
                if (child instanceof leafNode_1.LeafNode) {
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
        this._maximizedNode = { leaf: node, hiddenOnMaximize: hiddenOnMaximize };
        this._onDidMaximizedNodeChange.fire({
            view: node.view,
            isMaximized: true,
        });
    };
    Gridview.prototype.exitMaximizedView = function () {
        if (!this._maximizedNode) {
            return;
        }
        var hiddenOnMaximize = this._maximizedNode.hiddenOnMaximize;
        function showViewsInReverseOrder(parent) {
            for (var index = parent.children.length - 1; index >= 0; index--) {
                var child = parent.children[index];
                if (child instanceof leafNode_1.LeafNode) {
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
        var tmp = this._maximizedNode.leaf;
        this._maximizedNode = undefined;
        this._onDidMaximizedNodeChange.fire({
            view: tmp.view,
            isMaximized: false,
        });
    };
    Gridview.prototype.serialize = function () {
        var maximizedView = this.maximizedView();
        var maxmizedViewLocation;
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
        var root = serializeBranchNode(this.getView(), this.orientation);
        var resullt = {
            root: root,
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
    };
    Gridview.prototype.dispose = function () {
        this.disposable.dispose();
        this._onDidChange.dispose();
        this._onDidMaximizedNodeChange.dispose();
        this._onDidViewVisibilityChange.dispose();
        this.root.dispose();
        this._maximizedNode = undefined;
        this.element.remove();
    };
    Gridview.prototype.clear = function () {
        var orientation = this.root.orientation;
        this.root = new branchNode_1.BranchNode(orientation, this.proportionalLayout, this.styles, this.root.size, this.root.orthogonalSize, this.locked, this.margin);
    };
    Gridview.prototype.deserialize = function (json, deserializer) {
        var orientation = json.orientation;
        var height = orientation === splitview_1.Orientation.VERTICAL ? json.height : json.width;
        this._deserialize(json.root, orientation, deserializer, height);
        /**
         * The deserialied layout must be positioned through this.layout(...)
         * before any maximizedNode can be positioned
         */
        this.layout(json.width, json.height);
        if (json.maximizedNode) {
            var location_1 = json.maximizedNode.location;
            var _a = __read(this.getNode(location_1), 2), _ = _a[0], node = _a[1];
            if (!(node instanceof leafNode_1.LeafNode)) {
                return;
            }
            this.maximizeView(node.view);
        }
    };
    Gridview.prototype._deserialize = function (root, orientation, deserializer, orthogonalSize) {
        this.root = this._deserializeNode(root, orientation, deserializer, orthogonalSize);
    };
    Gridview.prototype._deserializeNode = function (node, orientation, deserializer, orthogonalSize) {
        var _this = this;
        var _a;
        var result;
        if (node.type === 'branch') {
            var serializedChildren = node.data;
            var children = serializedChildren.map(function (serializedChild) {
                return {
                    node: _this._deserializeNode(serializedChild, (0, exports.orthogonal)(orientation), deserializer, node.size),
                    visible: serializedChild.visible,
                };
            });
            result = new branchNode_1.BranchNode(orientation, this.proportionalLayout, this.styles, node.size, // <- orthogonal size - flips at each depth
            orthogonalSize, // <- size - flips at each depth,
            this.locked, this.margin, children);
        }
        else {
            var view = deserializer.fromJSON(node);
            if (typeof node.visible === 'boolean') {
                (_a = view.setVisible) === null || _a === void 0 ? void 0 : _a.call(view, node.visible);
            }
            result = new leafNode_1.LeafNode(view, orientation, orthogonalSize, node.size);
        }
        return result;
    };
    Object.defineProperty(Gridview.prototype, "root", {
        get: function () {
            return this._root;
        },
        set: function (root) {
            var _this = this;
            var oldRoot = this._root;
            if (oldRoot) {
                oldRoot.dispose();
                this._maximizedNode = undefined;
                this.element.removeChild(oldRoot.element);
            }
            this._root = root;
            this.element.appendChild(this._root.element);
            this.disposable.value = this._root.onDidChange(function (e) {
                _this._onDidChange.fire(e);
            });
        },
        enumerable: false,
        configurable: true
    });
    Gridview.prototype.normalize = function () {
        var _this = this;
        if (!this._root) {
            return;
        }
        if (this._root.children.length !== 1) {
            return;
        }
        var oldRoot = this.root;
        // can remove one level of redundant branching if there is only a single child
        var childReference = oldRoot.children[0];
        if (childReference instanceof leafNode_1.LeafNode) {
            return;
        }
        oldRoot.element.remove();
        var child = oldRoot.removeChild(0); // Remove child to prevent double disposal
        oldRoot.dispose(); // Dispose old root (won't dispose removed child)
        child.dispose(); // Dispose the removed child
        this._root = cloneNode(childReference, childReference.size, childReference.orthogonalSize);
        this.element.appendChild(this._root.element);
        this.disposable.value = this._root.onDidChange(function (e) {
            _this._onDidChange.fire(e);
        });
    };
    /**
     * If the root is orientated as a VERTICAL node then nest the existing root within a new HORIZIONTAL root node
     * If the root is orientated as a HORIZONTAL node then nest the existing root within a new VERITCAL root node
     */
    Gridview.prototype.insertOrthogonalSplitviewAtRoot = function () {
        var _this = this;
        if (!this._root) {
            return;
        }
        var oldRoot = this.root;
        oldRoot.element.remove();
        this._root = new branchNode_1.BranchNode((0, exports.orthogonal)(oldRoot.orientation), this.proportionalLayout, this.styles, this.root.orthogonalSize, this.root.size, this.locked, this.margin);
        if (oldRoot.children.length === 0) {
            // no data so no need to add anything back in
        }
        else if (oldRoot.children.length === 1) {
            // can remove one level of redundant branching if there is only a single child
            var childReference = oldRoot.children[0];
            var child = oldRoot.removeChild(0); // remove to prevent disposal when disposing of unwanted root
            child.dispose();
            oldRoot.dispose();
            this._root.addChild(
            /**
             * the child node will have the same orientation as the new root since
             * we are removing the inbetween node.
             * the entire 'tree' must be flipped recursively to ensure that the orientation
             * flips at each level
             */
            flipNode(childReference, childReference.orthogonalSize, childReference.size), splitview_1.Sizing.Distribute, 0);
        }
        else {
            this._root.addChild(oldRoot, splitview_1.Sizing.Distribute, 0);
        }
        this.element.appendChild(this._root.element);
        this.disposable.value = this._root.onDidChange(function (e) {
            _this._onDidChange.fire(e);
        });
    };
    Gridview.prototype.next = function (location) {
        return this.progmaticSelect(location);
    };
    Gridview.prototype.previous = function (location) {
        return this.progmaticSelect(location, true);
    };
    Gridview.prototype.getView = function (location) {
        var node = location ? this.getNode(location)[1] : this.root;
        return this._getViews(node, this.orientation);
    };
    Gridview.prototype._getViews = function (node, orientation, cachedVisibleSize) {
        var box = { height: node.height, width: node.width };
        if (node instanceof leafNode_1.LeafNode) {
            return { box: box, view: node.view, cachedVisibleSize: cachedVisibleSize };
        }
        var children = [];
        for (var i = 0; i < node.children.length; i++) {
            var child = node.children[i];
            var nodeCachedVisibleSize = node.getChildCachedVisibleSize(i);
            children.push(this._getViews(child, (0, exports.orthogonal)(orientation), nodeCachedVisibleSize));
        }
        return { box: box, children: children };
    };
    Gridview.prototype.progmaticSelect = function (location, reverse) {
        if (reverse === void 0) { reverse = false; }
        var _a = __read(this.getNode(location), 2), path = _a[0], node = _a[1];
        if (!(node instanceof leafNode_1.LeafNode)) {
            throw new Error('invalid location');
        }
        for (var i = path.length - 1; i > -1; i--) {
            var n = path[i];
            var l = location[i] || 0;
            var canProgressInCurrentLevel = reverse
                ? l - 1 > -1
                : l + 1 < n.children.length;
            if (canProgressInCurrentLevel) {
                return findLeaf(n.children[reverse ? l - 1 : l + 1], reverse);
            }
        }
        return findLeaf(this.root, reverse);
    };
    Gridview.prototype.isViewVisible = function (location) {
        var _a = __read((0, array_1.tail)(location), 2), rest = _a[0], index = _a[1];
        var _b = __read(this.getNode(rest), 2), parent = _b[1];
        if (!(parent instanceof branchNode_1.BranchNode)) {
            throw new Error('Invalid from location');
        }
        return parent.isChildVisible(index);
    };
    Gridview.prototype.setViewVisible = function (location, visible) {
        if (this.hasMaximizedView()) {
            this.exitMaximizedView();
        }
        var _a = __read((0, array_1.tail)(location), 2), rest = _a[0], index = _a[1];
        var _b = __read(this.getNode(rest), 2), parent = _b[1];
        if (!(parent instanceof branchNode_1.BranchNode)) {
            throw new Error('Invalid from location');
        }
        this._onDidViewVisibilityChange.fire();
        parent.setChildVisible(index, visible);
    };
    Gridview.prototype.moveView = function (parentLocation, from, to) {
        if (this.hasMaximizedView()) {
            this.exitMaximizedView();
        }
        var _a = __read(this.getNode(parentLocation), 2), parent = _a[1];
        if (!(parent instanceof branchNode_1.BranchNode)) {
            throw new Error('Invalid location');
        }
        parent.moveChild(from, to);
    };
    Gridview.prototype.addView = function (view, size, location) {
        if (this.hasMaximizedView()) {
            this.exitMaximizedView();
        }
        var _a = __read((0, array_1.tail)(location), 2), rest = _a[0], index = _a[1];
        var _b = __read(this.getNode(rest), 2), pathToParent = _b[0], parent = _b[1];
        if (parent instanceof branchNode_1.BranchNode) {
            var node = new leafNode_1.LeafNode(view, (0, exports.orthogonal)(parent.orientation), parent.orthogonalSize);
            parent.addChild(node, size, index);
        }
        else {
            var _c = __read(__spreadArray([], __read(pathToParent), false).reverse()), grandParent = _c[0], _ = _c.slice(1);
            var _d = __read(__spreadArray([], __read(rest), false).reverse()), parentIndex = _d[0], __ = _d.slice(1);
            var newSiblingSize = 0;
            var newSiblingCachedVisibleSize = grandParent.getChildCachedVisibleSize(parentIndex);
            if (typeof newSiblingCachedVisibleSize === 'number') {
                newSiblingSize = splitview_1.Sizing.Invisible(newSiblingCachedVisibleSize);
            }
            var child = grandParent.removeChild(parentIndex);
            child.dispose();
            var newParent = new branchNode_1.BranchNode(parent.orientation, this.proportionalLayout, this.styles, parent.size, parent.orthogonalSize, this.locked, this.margin);
            grandParent.addChild(newParent, parent.size, parentIndex);
            var newSibling = new leafNode_1.LeafNode(parent.view, grandParent.orientation, parent.size);
            newParent.addChild(newSibling, newSiblingSize, 0);
            if (typeof size !== 'number' && size.type === 'split') {
                size = { type: 'split', index: 0 };
            }
            var node = new leafNode_1.LeafNode(view, grandParent.orientation, parent.size);
            newParent.addChild(node, size, index);
        }
    };
    Gridview.prototype.remove = function (view, sizing) {
        var location = getGridLocation(view.element);
        return this.removeView(location, sizing);
    };
    Gridview.prototype.removeView = function (location, sizing) {
        if (this.hasMaximizedView()) {
            this.exitMaximizedView();
        }
        var _a = __read((0, array_1.tail)(location), 2), rest = _a[0], index = _a[1];
        var _b = __read(this.getNode(rest), 2), pathToParent = _b[0], parent = _b[1];
        if (!(parent instanceof branchNode_1.BranchNode)) {
            throw new Error('Invalid location');
        }
        var nodeToRemove = parent.children[index];
        if (!(nodeToRemove instanceof leafNode_1.LeafNode)) {
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
        var sibling = parent.children[0];
        if (pathToParent.length === 0) {
            // if the parent is root
            if (sibling instanceof leafNode_1.LeafNode) {
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
        var _c = __read(__spreadArray([], __read(pathToParent), false).reverse()), grandParent = _c[0], _ = _c.slice(1);
        var _d = __read(__spreadArray([], __read(rest), false).reverse()), parentIndex = _d[0], __ = _d.slice(1);
        var isSiblingVisible = parent.isChildVisible(0);
        // either way we need to remove the sibling from it's existing tree
        parent.removeChild(0, sizing);
        // note the sizes of all of the grandparents children
        var sizes = grandParent.children.map(function (_size, i) {
            return grandParent.getChildSize(i);
        });
        // remove the parent from the grandparent since we are moving the sibling to take the parents place
        // this parent is no longer used and can be disposed of
        grandParent.removeChild(parentIndex, sizing).dispose();
        if (sibling instanceof branchNode_1.BranchNode) {
            // replace the parent with the siblings children
            sizes.splice.apply(sizes, __spreadArray([parentIndex,
                1], __read(sibling.children.map(function (c) { return c.size; })), false));
            // and add those siblings to the grandparent
            for (var i = 0; i < sibling.children.length; i++) {
                var child = sibling.children[i];
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
            var newSibling = new leafNode_1.LeafNode(sibling.view, (0, exports.orthogonal)(sibling.orientation), sibling.size);
            var siblingSizing = isSiblingVisible
                ? sibling.orthogonalSize
                : splitview_1.Sizing.Invisible(sibling.orthogonalSize);
            grandParent.addChild(newSibling, siblingSizing, parentIndex);
        }
        // the containing node of the sibling is no longer required and can be disposed of
        sibling.dispose();
        // resize everything
        for (var i = 0; i < sizes.length; i++) {
            grandParent.resizeChild(i, sizes[i]);
        }
        return nodeToRemove.view;
    };
    Gridview.prototype.layout = function (width, height) {
        var _a = __read(this.root.orientation === splitview_1.Orientation.HORIZONTAL
            ? [height, width]
            : [width, height], 2), size = _a[0], orthogonalSize = _a[1];
        this.root.layout(size, orthogonalSize);
    };
    Gridview.prototype.getNode = function (location, node, path) {
        if (node === void 0) { node = this.root; }
        if (path === void 0) { path = []; }
        if (location.length === 0) {
            return [path, node];
        }
        if (!(node instanceof branchNode_1.BranchNode)) {
            throw new Error('Invalid location');
        }
        var _a = __read(location), index = _a[0], rest = _a.slice(1);
        if (index < 0 || index >= node.children.length) {
            throw new Error('Invalid location');
        }
        var child = node.children[index];
        path.push(node);
        return this.getNode(rest, child, path);
    };
    return Gridview;
}());
exports.Gridview = Gridview;
