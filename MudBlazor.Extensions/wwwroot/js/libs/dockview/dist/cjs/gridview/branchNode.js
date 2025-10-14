"use strict";
/*---------------------------------------------------------------------------------------------
 * Accreditation: This file is largly based upon the MIT licenced VSCode sourcecode found at:
 * https://github.com/microsoft/vscode/tree/main/src/vs/base/browser/ui/grid
 *--------------------------------------------------------------------------------------------*/
var __extends = (this && this.__extends) || (function () {
    var extendStatics = function (d, b) {
        extendStatics = Object.setPrototypeOf ||
            ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
            function (d, b) { for (var p in b) if (Object.prototype.hasOwnProperty.call(b, p)) d[p] = b[p]; };
        return extendStatics(d, b);
    };
    return function (d, b) {
        if (typeof b !== "function" && b !== null)
            throw new TypeError("Class extends value " + String(b) + " is not a constructor or null");
        extendStatics(d, b);
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };
})();
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
exports.BranchNode = void 0;
var splitview_1 = require("../splitview/splitview");
var events_1 = require("../events");
var leafNode_1 = require("./leafNode");
var lifecycle_1 = require("../lifecycle");
var BranchNode = /** @class */ (function (_super) {
    __extends(BranchNode, _super);
    function BranchNode(orientation, proportionalLayout, styles, size, orthogonalSize, disabled, margin, childDescriptors) {
        var _this = _super.call(this) || this;
        _this.orientation = orientation;
        _this.proportionalLayout = proportionalLayout;
        _this.styles = styles;
        _this._childrenDisposable = lifecycle_1.Disposable.NONE;
        _this.children = [];
        _this._onDidChange = new events_1.Emitter();
        _this.onDidChange = _this._onDidChange.event;
        _this._onDidVisibilityChange = new events_1.Emitter();
        _this.onDidVisibilityChange = _this._onDidVisibilityChange.event;
        _this._orthogonalSize = orthogonalSize;
        _this._size = size;
        _this.element = document.createElement('div');
        _this.element.className = 'dv-branch-node';
        if (!childDescriptors) {
            _this.splitview = new splitview_1.Splitview(_this.element, {
                orientation: _this.orientation,
                proportionalLayout: proportionalLayout,
                styles: styles,
                margin: margin,
            });
            _this.splitview.layout(_this.size, _this.orthogonalSize);
        }
        else {
            var descriptor = {
                views: childDescriptors.map(function (childDescriptor) {
                    return {
                        view: childDescriptor.node,
                        size: childDescriptor.node.size,
                        visible: childDescriptor.node instanceof leafNode_1.LeafNode &&
                            childDescriptor.visible !== undefined
                            ? childDescriptor.visible
                            : true,
                    };
                }),
                size: _this.orthogonalSize,
            };
            _this.children = childDescriptors.map(function (c) { return c.node; });
            _this.splitview = new splitview_1.Splitview(_this.element, {
                orientation: _this.orientation,
                descriptor: descriptor,
                proportionalLayout: proportionalLayout,
                styles: styles,
                margin: margin,
            });
        }
        _this.disabled = disabled;
        _this.addDisposables(_this._onDidChange, _this._onDidVisibilityChange, _this.splitview.onDidSashEnd(function () {
            _this._onDidChange.fire({});
        }));
        _this.setupChildrenEvents();
        return _this;
    }
    Object.defineProperty(BranchNode.prototype, "width", {
        get: function () {
            return this.orientation === splitview_1.Orientation.HORIZONTAL
                ? this.size
                : this.orthogonalSize;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(BranchNode.prototype, "height", {
        get: function () {
            return this.orientation === splitview_1.Orientation.HORIZONTAL
                ? this.orthogonalSize
                : this.size;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(BranchNode.prototype, "minimumSize", {
        get: function () {
            var _this = this;
            return this.children.length === 0
                ? 0
                : Math.max.apply(Math, __spreadArray([], __read(this.children.map(function (c, index) {
                    return _this.splitview.isViewVisible(index)
                        ? c.minimumOrthogonalSize
                        : 0;
                })), false));
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(BranchNode.prototype, "maximumSize", {
        get: function () {
            var _this = this;
            return Math.min.apply(Math, __spreadArray([], __read(this.children.map(function (c, index) {
                return _this.splitview.isViewVisible(index)
                    ? c.maximumOrthogonalSize
                    : Number.POSITIVE_INFINITY;
            })), false));
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(BranchNode.prototype, "minimumOrthogonalSize", {
        get: function () {
            return this.splitview.minimumSize;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(BranchNode.prototype, "maximumOrthogonalSize", {
        get: function () {
            return this.splitview.maximumSize;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(BranchNode.prototype, "orthogonalSize", {
        get: function () {
            return this._orthogonalSize;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(BranchNode.prototype, "size", {
        get: function () {
            return this._size;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(BranchNode.prototype, "minimumWidth", {
        get: function () {
            return this.orientation === splitview_1.Orientation.HORIZONTAL
                ? this.minimumOrthogonalSize
                : this.minimumSize;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(BranchNode.prototype, "minimumHeight", {
        get: function () {
            return this.orientation === splitview_1.Orientation.HORIZONTAL
                ? this.minimumSize
                : this.minimumOrthogonalSize;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(BranchNode.prototype, "maximumWidth", {
        get: function () {
            return this.orientation === splitview_1.Orientation.HORIZONTAL
                ? this.maximumOrthogonalSize
                : this.maximumSize;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(BranchNode.prototype, "maximumHeight", {
        get: function () {
            return this.orientation === splitview_1.Orientation.HORIZONTAL
                ? this.maximumSize
                : this.maximumOrthogonalSize;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(BranchNode.prototype, "priority", {
        get: function () {
            if (this.children.length === 0) {
                return splitview_1.LayoutPriority.Normal;
            }
            var priorities = this.children.map(function (c) {
                return typeof c.priority === 'undefined'
                    ? splitview_1.LayoutPriority.Normal
                    : c.priority;
            });
            if (priorities.some(function (p) { return p === splitview_1.LayoutPriority.High; })) {
                return splitview_1.LayoutPriority.High;
            }
            else if (priorities.some(function (p) { return p === splitview_1.LayoutPriority.Low; })) {
                return splitview_1.LayoutPriority.Low;
            }
            return splitview_1.LayoutPriority.Normal;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(BranchNode.prototype, "disabled", {
        get: function () {
            return this.splitview.disabled;
        },
        set: function (value) {
            this.splitview.disabled = value;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(BranchNode.prototype, "margin", {
        get: function () {
            return this.splitview.margin;
        },
        set: function (value) {
            this.splitview.margin = value;
            this.children.forEach(function (child) {
                if (child instanceof BranchNode) {
                    child.margin = value;
                }
            });
        },
        enumerable: false,
        configurable: true
    });
    BranchNode.prototype.setVisible = function (_visible) {
        // noop
    };
    BranchNode.prototype.isChildVisible = function (index) {
        if (index < 0 || index >= this.children.length) {
            throw new Error('Invalid index');
        }
        return this.splitview.isViewVisible(index);
    };
    BranchNode.prototype.setChildVisible = function (index, visible) {
        if (index < 0 || index >= this.children.length) {
            throw new Error('Invalid index');
        }
        if (this.splitview.isViewVisible(index) === visible) {
            return;
        }
        var wereAllChildrenHidden = this.splitview.contentSize === 0;
        this.splitview.setViewVisible(index, visible);
        // }
        var areAllChildrenHidden = this.splitview.contentSize === 0;
        // If all children are hidden then the parent should hide the entire splitview
        // If the entire splitview is hidden then the parent should show the splitview when a child is shown
        if ((visible && wereAllChildrenHidden) ||
            (!visible && areAllChildrenHidden)) {
            this._onDidVisibilityChange.fire({ visible: visible });
        }
    };
    BranchNode.prototype.moveChild = function (from, to) {
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
        var child = this._removeChild(from);
        this._addChild(child, to);
    };
    BranchNode.prototype.getChildSize = function (index) {
        if (index < 0 || index >= this.children.length) {
            throw new Error('Invalid index');
        }
        return this.splitview.getViewSize(index);
    };
    BranchNode.prototype.resizeChild = function (index, size) {
        if (index < 0 || index >= this.children.length) {
            throw new Error('Invalid index');
        }
        this.splitview.resizeView(index, size);
    };
    BranchNode.prototype.layout = function (size, orthogonalSize) {
        this._size = orthogonalSize;
        this._orthogonalSize = size;
        this.splitview.layout(orthogonalSize, size);
    };
    BranchNode.prototype.addChild = function (node, size, index, skipLayout) {
        if (index < 0 || index > this.children.length) {
            throw new Error('Invalid index');
        }
        this.splitview.addView(node, size, index, skipLayout);
        this._addChild(node, index);
    };
    BranchNode.prototype.getChildCachedVisibleSize = function (index) {
        if (index < 0 || index >= this.children.length) {
            throw new Error('Invalid index');
        }
        return this.splitview.getViewCachedVisibleSize(index);
    };
    BranchNode.prototype.removeChild = function (index, sizing) {
        if (index < 0 || index >= this.children.length) {
            throw new Error('Invalid index');
        }
        this.splitview.removeView(index, sizing);
        return this._removeChild(index);
    };
    BranchNode.prototype._addChild = function (node, index) {
        this.children.splice(index, 0, node);
        this.setupChildrenEvents();
    };
    BranchNode.prototype._removeChild = function (index) {
        var _a = __read(this.children.splice(index, 1), 1), child = _a[0];
        this.setupChildrenEvents();
        return child;
    };
    BranchNode.prototype.setupChildrenEvents = function () {
        var _this = this;
        this._childrenDisposable.dispose();
        this._childrenDisposable = new (lifecycle_1.CompositeDisposable.bind.apply(lifecycle_1.CompositeDisposable, __spreadArray([void 0, events_1.Event.any.apply(events_1.Event, __spreadArray([], __read(this.children.map(function (c) { return c.onDidChange; })), false))(function (e) {
                /**
                 * indicate a change has occured to allows any re-rendering but don't bubble
                 * event because that was specific to this branch
                 */
                _this._onDidChange.fire({ size: e.orthogonalSize });
            })], __read(this.children.map(function (c, i) {
            if (c instanceof BranchNode) {
                return c.onDidVisibilityChange(function (_a) {
                    var visible = _a.visible;
                    _this.setChildVisible(i, visible);
                });
            }
            return lifecycle_1.Disposable.NONE;
        })), false)))();
    };
    BranchNode.prototype.dispose = function () {
        this._childrenDisposable.dispose();
        this.splitview.dispose();
        this.children.forEach(function (child) { return child.dispose(); });
        _super.prototype.dispose.call(this);
    };
    return BranchNode;
}(lifecycle_1.CompositeDisposable));
exports.BranchNode = BranchNode;
