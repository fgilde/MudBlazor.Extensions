"use strict";
/*---------------------------------------------------------------------------------------------
 * Accreditation: This file is largly based upon the MIT licenced VSCode sourcecode found at:
 * https://github.com/microsoft/vscode/tree/main/src/vs/base/browser/ui/splitview
 *--------------------------------------------------------------------------------------------*/
var __values = (this && this.__values) || function(o) {
    var s = typeof Symbol === "function" && Symbol.iterator, m = s && o[s], i = 0;
    if (m) return m.call(o);
    if (o && typeof o.length === "number") return {
        next: function () {
            if (o && i >= o.length) o = void 0;
            return { value: o && o[i++], done: !o };
        }
    };
    throw new TypeError(s ? "Object is not iterable." : "Symbol.iterator is not defined.");
};
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
exports.Splitview = exports.Sizing = exports.LayoutPriority = exports.SashState = exports.Orientation = void 0;
var dom_1 = require("../dom");
var events_1 = require("../events");
var array_1 = require("../array");
var math_1 = require("../math");
var viewItem_1 = require("./viewItem");
var Orientation;
(function (Orientation) {
    Orientation["HORIZONTAL"] = "HORIZONTAL";
    Orientation["VERTICAL"] = "VERTICAL";
})(Orientation || (exports.Orientation = Orientation = {}));
var SashState;
(function (SashState) {
    SashState[SashState["MAXIMUM"] = 0] = "MAXIMUM";
    SashState[SashState["MINIMUM"] = 1] = "MINIMUM";
    SashState[SashState["DISABLED"] = 2] = "DISABLED";
    SashState[SashState["ENABLED"] = 3] = "ENABLED";
})(SashState || (exports.SashState = SashState = {}));
var LayoutPriority;
(function (LayoutPriority) {
    LayoutPriority["Low"] = "low";
    LayoutPriority["High"] = "high";
    LayoutPriority["Normal"] = "normal";
})(LayoutPriority || (exports.LayoutPriority = LayoutPriority = {}));
var Sizing;
(function (Sizing) {
    Sizing.Distribute = { type: 'distribute' };
    function Split(index) {
        return { type: 'split', index: index };
    }
    Sizing.Split = Split;
    function Invisible(cachedVisibleSize) {
        return { type: 'invisible', cachedVisibleSize: cachedVisibleSize };
    }
    Sizing.Invisible = Invisible;
})(Sizing || (exports.Sizing = Sizing = {}));
var Splitview = /** @class */ (function () {
    function Splitview(container, options) {
        var _this = this;
        var _a, _b;
        this.container = container;
        this.viewItems = [];
        this.sashes = [];
        this._size = 0;
        this._orthogonalSize = 0;
        this._contentSize = 0;
        this._proportions = undefined;
        this._startSnappingEnabled = true;
        this._endSnappingEnabled = true;
        this._disabled = false;
        this._margin = 0;
        this._onDidSashEnd = new events_1.Emitter();
        this.onDidSashEnd = this._onDidSashEnd.event;
        this._onDidAddView = new events_1.Emitter();
        this.onDidAddView = this._onDidAddView.event;
        this._onDidRemoveView = new events_1.Emitter();
        this.onDidRemoveView = this._onDidRemoveView.event;
        this.resize = function (index, delta, sizes, lowPriorityIndexes, highPriorityIndexes, overloadMinDelta, overloadMaxDelta, snapBefore, snapAfter) {
            var e_1, _a, e_2, _b;
            if (sizes === void 0) { sizes = _this.viewItems.map(function (x) { return x.size; }); }
            if (overloadMinDelta === void 0) { overloadMinDelta = Number.NEGATIVE_INFINITY; }
            if (overloadMaxDelta === void 0) { overloadMaxDelta = Number.POSITIVE_INFINITY; }
            if (index < 0 || index > _this.viewItems.length) {
                return 0;
            }
            var upIndexes = (0, math_1.range)(index, -1);
            var downIndexes = (0, math_1.range)(index + 1, _this.viewItems.length);
            //
            if (highPriorityIndexes) {
                try {
                    for (var highPriorityIndexes_1 = __values(highPriorityIndexes), highPriorityIndexes_1_1 = highPriorityIndexes_1.next(); !highPriorityIndexes_1_1.done; highPriorityIndexes_1_1 = highPriorityIndexes_1.next()) {
                        var i = highPriorityIndexes_1_1.value;
                        (0, array_1.pushToStart)(upIndexes, i);
                        (0, array_1.pushToStart)(downIndexes, i);
                    }
                }
                catch (e_1_1) { e_1 = { error: e_1_1 }; }
                finally {
                    try {
                        if (highPriorityIndexes_1_1 && !highPriorityIndexes_1_1.done && (_a = highPriorityIndexes_1.return)) _a.call(highPriorityIndexes_1);
                    }
                    finally { if (e_1) throw e_1.error; }
                }
            }
            if (lowPriorityIndexes) {
                try {
                    for (var lowPriorityIndexes_1 = __values(lowPriorityIndexes), lowPriorityIndexes_1_1 = lowPriorityIndexes_1.next(); !lowPriorityIndexes_1_1.done; lowPriorityIndexes_1_1 = lowPriorityIndexes_1.next()) {
                        var i = lowPriorityIndexes_1_1.value;
                        (0, array_1.pushToEnd)(upIndexes, i);
                        (0, array_1.pushToEnd)(downIndexes, i);
                    }
                }
                catch (e_2_1) { e_2 = { error: e_2_1 }; }
                finally {
                    try {
                        if (lowPriorityIndexes_1_1 && !lowPriorityIndexes_1_1.done && (_b = lowPriorityIndexes_1.return)) _b.call(lowPriorityIndexes_1);
                    }
                    finally { if (e_2) throw e_2.error; }
                }
            }
            //
            var upItems = upIndexes.map(function (i) { return _this.viewItems[i]; });
            var upSizes = upIndexes.map(function (i) { return sizes[i]; });
            //
            var downItems = downIndexes.map(function (i) { return _this.viewItems[i]; });
            var downSizes = downIndexes.map(function (i) { return sizes[i]; });
            //
            var minDeltaUp = upIndexes.reduce(function (_, i) { return _ + _this.viewItems[i].minimumSize - sizes[i]; }, 0);
            var maxDeltaUp = upIndexes.reduce(function (_, i) { return _ + _this.viewItems[i].maximumSize - sizes[i]; }, 0);
            //
            var maxDeltaDown = downIndexes.length === 0
                ? Number.POSITIVE_INFINITY
                : downIndexes.reduce(function (_, i) { return _ + sizes[i] - _this.viewItems[i].minimumSize; }, 0);
            var minDeltaDown = downIndexes.length === 0
                ? Number.NEGATIVE_INFINITY
                : downIndexes.reduce(function (_, i) { return _ + sizes[i] - _this.viewItems[i].maximumSize; }, 0);
            //
            var minDelta = Math.max(minDeltaUp, minDeltaDown);
            var maxDelta = Math.min(maxDeltaDown, maxDeltaUp);
            //
            var snapped = false;
            if (snapBefore) {
                var snapView = _this.viewItems[snapBefore.index];
                var visible = delta >= snapBefore.limitDelta;
                snapped = visible !== snapView.visible;
                snapView.setVisible(visible, snapBefore.size);
            }
            if (!snapped && snapAfter) {
                var snapView = _this.viewItems[snapAfter.index];
                var visible = delta < snapAfter.limitDelta;
                snapped = visible !== snapView.visible;
                snapView.setVisible(visible, snapAfter.size);
            }
            if (snapped) {
                return _this.resize(index, delta, sizes, lowPriorityIndexes, highPriorityIndexes, overloadMinDelta, overloadMaxDelta);
            }
            //
            var tentativeDelta = (0, math_1.clamp)(delta, minDelta, maxDelta);
            var actualDelta = 0;
            //
            var deltaUp = tentativeDelta;
            for (var i = 0; i < upItems.length; i++) {
                var item = upItems[i];
                var size = (0, math_1.clamp)(upSizes[i] + deltaUp, item.minimumSize, item.maximumSize);
                var viewDelta = size - upSizes[i];
                actualDelta += viewDelta;
                deltaUp -= viewDelta;
                item.size = size;
            }
            //
            var deltaDown = actualDelta;
            for (var i = 0; i < downItems.length; i++) {
                var item = downItems[i];
                var size = (0, math_1.clamp)(downSizes[i] - deltaDown, item.minimumSize, item.maximumSize);
                var viewDelta = size - downSizes[i];
                deltaDown += viewDelta;
                item.size = size;
            }
            //
            return delta;
        };
        this._orientation = (_a = options.orientation) !== null && _a !== void 0 ? _a : Orientation.VERTICAL;
        this.element = this.createContainer();
        this.margin = (_b = options.margin) !== null && _b !== void 0 ? _b : 0;
        this.proportionalLayout =
            options.proportionalLayout === undefined
                ? true
                : !!options.proportionalLayout;
        this.viewContainer = this.createViewContainer();
        this.sashContainer = this.createSashContainer();
        this.element.appendChild(this.sashContainer);
        this.element.appendChild(this.viewContainer);
        this.container.appendChild(this.element);
        this.style(options.styles);
        // We have an existing set of view, add them now
        if (options.descriptor) {
            this._size = options.descriptor.size;
            options.descriptor.views.forEach(function (viewDescriptor, index) {
                var sizing = viewDescriptor.visible === undefined ||
                    viewDescriptor.visible
                    ? viewDescriptor.size
                    : {
                        type: 'invisible',
                        cachedVisibleSize: viewDescriptor.size,
                    };
                var view = viewDescriptor.view;
                _this.addView(view, sizing, index, true
                // true skip layout
                );
            });
            // Initialize content size and proportions for first layout
            this._contentSize = this.viewItems.reduce(function (r, i) { return r + i.size; }, 0);
            this.saveProportions();
        }
    }
    Object.defineProperty(Splitview.prototype, "contentSize", {
        get: function () {
            return this._contentSize;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(Splitview.prototype, "size", {
        get: function () {
            return this._size;
        },
        set: function (value) {
            this._size = value;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(Splitview.prototype, "orthogonalSize", {
        get: function () {
            return this._orthogonalSize;
        },
        set: function (value) {
            this._orthogonalSize = value;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(Splitview.prototype, "length", {
        get: function () {
            return this.viewItems.length;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(Splitview.prototype, "proportions", {
        get: function () {
            return this._proportions ? __spreadArray([], __read(this._proportions), false) : undefined;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(Splitview.prototype, "orientation", {
        get: function () {
            return this._orientation;
        },
        set: function (value) {
            this._orientation = value;
            var tmp = this.size;
            this.size = this.orthogonalSize;
            this.orthogonalSize = tmp;
            (0, dom_1.removeClasses)(this.element, 'dv-horizontal', 'dv-vertical');
            this.element.classList.add(this.orientation == Orientation.HORIZONTAL
                ? 'dv-horizontal'
                : 'dv-vertical');
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(Splitview.prototype, "minimumSize", {
        get: function () {
            return this.viewItems.reduce(function (r, item) { return r + item.minimumSize; }, 0);
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(Splitview.prototype, "maximumSize", {
        get: function () {
            return this.length === 0
                ? Number.POSITIVE_INFINITY
                : this.viewItems.reduce(function (r, item) { return r + item.maximumSize; }, 0);
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(Splitview.prototype, "startSnappingEnabled", {
        get: function () {
            return this._startSnappingEnabled;
        },
        set: function (startSnappingEnabled) {
            if (this._startSnappingEnabled === startSnappingEnabled) {
                return;
            }
            this._startSnappingEnabled = startSnappingEnabled;
            this.updateSashEnablement();
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(Splitview.prototype, "endSnappingEnabled", {
        get: function () {
            return this._endSnappingEnabled;
        },
        set: function (endSnappingEnabled) {
            if (this._endSnappingEnabled === endSnappingEnabled) {
                return;
            }
            this._endSnappingEnabled = endSnappingEnabled;
            this.updateSashEnablement();
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(Splitview.prototype, "disabled", {
        get: function () {
            return this._disabled;
        },
        set: function (value) {
            this._disabled = value;
            (0, dom_1.toggleClass)(this.element, 'dv-splitview-disabled', value);
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(Splitview.prototype, "margin", {
        get: function () {
            return this._margin;
        },
        set: function (value) {
            this._margin = value;
            (0, dom_1.toggleClass)(this.element, 'dv-splitview-has-margin', value !== 0);
        },
        enumerable: false,
        configurable: true
    });
    Splitview.prototype.style = function (styles) {
        if ((styles === null || styles === void 0 ? void 0 : styles.separatorBorder) === 'transparent') {
            (0, dom_1.removeClasses)(this.element, 'dv-separator-border');
            this.element.style.removeProperty('--dv-separator-border');
        }
        else {
            (0, dom_1.addClasses)(this.element, 'dv-separator-border');
            if (styles === null || styles === void 0 ? void 0 : styles.separatorBorder) {
                this.element.style.setProperty('--dv-separator-border', styles.separatorBorder);
            }
        }
    };
    Splitview.prototype.isViewVisible = function (index) {
        if (index < 0 || index >= this.viewItems.length) {
            throw new Error('Index out of bounds');
        }
        var viewItem = this.viewItems[index];
        return viewItem.visible;
    };
    Splitview.prototype.setViewVisible = function (index, visible) {
        if (index < 0 || index >= this.viewItems.length) {
            throw new Error('Index out of bounds');
        }
        var viewItem = this.viewItems[index];
        viewItem.setVisible(visible, viewItem.size);
        this.distributeEmptySpace(index);
        this.layoutViews();
        this.saveProportions();
    };
    Splitview.prototype.getViewSize = function (index) {
        if (index < 0 || index >= this.viewItems.length) {
            return -1;
        }
        return this.viewItems[index].size;
    };
    Splitview.prototype.resizeView = function (index, size) {
        var _this = this;
        if (index < 0 || index >= this.viewItems.length) {
            return;
        }
        var indexes = (0, math_1.range)(this.viewItems.length).filter(function (i) { return i !== index; });
        var lowPriorityIndexes = __spreadArray(__spreadArray([], __read(indexes.filter(function (i) { return _this.viewItems[i].priority === LayoutPriority.Low; })), false), [
            index,
        ], false);
        var highPriorityIndexes = indexes.filter(function (i) { return _this.viewItems[i].priority === LayoutPriority.High; });
        var item = this.viewItems[index];
        size = Math.round(size);
        size = (0, math_1.clamp)(size, item.minimumSize, Math.min(item.maximumSize, this._size));
        item.size = size;
        this.relayout(lowPriorityIndexes, highPriorityIndexes);
    };
    Splitview.prototype.getViews = function () {
        return this.viewItems.map(function (x) { return x.view; });
    };
    Splitview.prototype.onDidChange = function (item, size) {
        var _this = this;
        var index = this.viewItems.indexOf(item);
        if (index < 0 || index >= this.viewItems.length) {
            return;
        }
        size = typeof size === 'number' ? size : item.size;
        size = (0, math_1.clamp)(size, item.minimumSize, item.maximumSize);
        item.size = size;
        var indexes = (0, math_1.range)(this.viewItems.length).filter(function (i) { return i !== index; });
        var lowPriorityIndexes = __spreadArray(__spreadArray([], __read(indexes.filter(function (i) { return _this.viewItems[i].priority === LayoutPriority.Low; })), false), [
            index,
        ], false);
        var highPriorityIndexes = indexes.filter(function (i) { return _this.viewItems[i].priority === LayoutPriority.High; });
        /**
         * add this view we are changing to the low-index list since we have determined the size
         * here and don't want it changed
         */
        this.relayout(__spreadArray(__spreadArray([], __read(lowPriorityIndexes), false), [index], false), highPriorityIndexes);
    };
    Splitview.prototype.addView = function (view, size, index, skipLayout) {
        var _this = this;
        if (size === void 0) { size = { type: 'distribute' }; }
        if (index === void 0) { index = this.viewItems.length; }
        var container = document.createElement('div');
        container.className = 'dv-view';
        container.appendChild(view.element);
        var viewSize;
        if (typeof size === 'number') {
            viewSize = size;
        }
        else if (size.type === 'split') {
            viewSize = this.getViewSize(size.index) / 2;
        }
        else if (size.type === 'invisible') {
            viewSize = { cachedVisibleSize: size.cachedVisibleSize };
        }
        else {
            viewSize = view.minimumSize;
        }
        var disposable = view.onDidChange(function (newSize) {
            return _this.onDidChange(viewItem, newSize.size);
        });
        var viewItem = new viewItem_1.ViewItem(container, view, viewSize, {
            dispose: function () {
                disposable.dispose();
                _this.viewContainer.removeChild(container);
            },
        });
        if (index === this.viewItems.length) {
            this.viewContainer.appendChild(container);
        }
        else {
            this.viewContainer.insertBefore(container, this.viewContainer.children.item(index));
        }
        this.viewItems.splice(index, 0, viewItem);
        if (this.viewItems.length > 1) {
            //add sash
            var sash_1 = document.createElement('div');
            sash_1.className = 'dv-sash';
            var onPointerStart_1 = function (event) {
                var e_3, _a;
                try {
                    for (var _b = __values(_this.viewItems), _c = _b.next(); !_c.done; _c = _b.next()) {
                        var item = _c.value;
                        item.enabled = false;
                    }
                }
                catch (e_3_1) { e_3 = { error: e_3_1 }; }
                finally {
                    try {
                        if (_c && !_c.done && (_a = _b.return)) _a.call(_b);
                    }
                    finally { if (e_3) throw e_3.error; }
                }
                var iframes = (0, dom_1.disableIframePointEvents)();
                var start = _this._orientation === Orientation.HORIZONTAL
                    ? event.clientX
                    : event.clientY;
                var sashIndex = (0, array_1.firstIndex)(_this.sashes, function (s) { return s.container === sash_1; });
                //
                var sizes = _this.viewItems.map(function (x) { return x.size; });
                //
                var snapBefore;
                var snapAfter;
                var upIndexes = (0, math_1.range)(sashIndex, -1);
                var downIndexes = (0, math_1.range)(sashIndex + 1, _this.viewItems.length);
                var minDeltaUp = upIndexes.reduce(function (r, i) { return r + (_this.viewItems[i].minimumSize - sizes[i]); }, 0);
                var maxDeltaUp = upIndexes.reduce(function (r, i) {
                    return r + (_this.viewItems[i].viewMaximumSize - sizes[i]);
                }, 0);
                var maxDeltaDown = downIndexes.length === 0
                    ? Number.POSITIVE_INFINITY
                    : downIndexes.reduce(function (r, i) {
                        return r +
                            (sizes[i] - _this.viewItems[i].minimumSize);
                    }, 0);
                var minDeltaDown = downIndexes.length === 0
                    ? Number.NEGATIVE_INFINITY
                    : downIndexes.reduce(function (r, i) {
                        return r +
                            (sizes[i] -
                                _this.viewItems[i].viewMaximumSize);
                    }, 0);
                var minDelta = Math.max(minDeltaUp, minDeltaDown);
                var maxDelta = Math.min(maxDeltaDown, maxDeltaUp);
                var snapBeforeIndex = _this.findFirstSnapIndex(upIndexes);
                var snapAfterIndex = _this.findFirstSnapIndex(downIndexes);
                if (typeof snapBeforeIndex === 'number') {
                    var snappedViewItem = _this.viewItems[snapBeforeIndex];
                    var halfSize = Math.floor(snappedViewItem.viewMinimumSize / 2);
                    snapBefore = {
                        index: snapBeforeIndex,
                        limitDelta: snappedViewItem.visible
                            ? minDelta - halfSize
                            : minDelta + halfSize,
                        size: snappedViewItem.size,
                    };
                }
                if (typeof snapAfterIndex === 'number') {
                    var snappedViewItem = _this.viewItems[snapAfterIndex];
                    var halfSize = Math.floor(snappedViewItem.viewMinimumSize / 2);
                    snapAfter = {
                        index: snapAfterIndex,
                        limitDelta: snappedViewItem.visible
                            ? maxDelta + halfSize
                            : maxDelta - halfSize,
                        size: snappedViewItem.size,
                    };
                }
                var onPointerMove = function (event) {
                    var current = _this._orientation === Orientation.HORIZONTAL
                        ? event.clientX
                        : event.clientY;
                    var delta = current - start;
                    _this.resize(sashIndex, delta, sizes, undefined, undefined, minDelta, maxDelta, snapBefore, snapAfter);
                    _this.distributeEmptySpace();
                    _this.layoutViews();
                };
                var end = function () {
                    var e_4, _a;
                    try {
                        for (var _b = __values(_this.viewItems), _c = _b.next(); !_c.done; _c = _b.next()) {
                            var item = _c.value;
                            item.enabled = true;
                        }
                    }
                    catch (e_4_1) { e_4 = { error: e_4_1 }; }
                    finally {
                        try {
                            if (_c && !_c.done && (_a = _b.return)) _a.call(_b);
                        }
                        finally { if (e_4) throw e_4.error; }
                    }
                    iframes.release();
                    _this.saveProportions();
                    document.removeEventListener('pointermove', onPointerMove);
                    document.removeEventListener('pointerup', end);
                    document.removeEventListener('pointercancel', end);
                    _this._onDidSashEnd.fire(undefined);
                };
                document.addEventListener('pointermove', onPointerMove);
                document.addEventListener('pointerup', end);
                document.addEventListener('pointercancel', end);
            };
            sash_1.addEventListener('pointerdown', onPointerStart_1);
            var sashItem = {
                container: sash_1,
                disposable: function () {
                    sash_1.removeEventListener('pointerdown', onPointerStart_1);
                    _this.sashContainer.removeChild(sash_1);
                },
            };
            this.sashContainer.appendChild(sash_1);
            this.sashes.push(sashItem);
        }
        if (!skipLayout) {
            this.relayout([index]);
        }
        if (!skipLayout &&
            typeof size !== 'number' &&
            size.type === 'distribute') {
            this.distributeViewSizes();
        }
        this._onDidAddView.fire(view);
    };
    Splitview.prototype.distributeViewSizes = function () {
        var e_5, _a, e_6, _b;
        var _this = this;
        var flexibleViewItems = [];
        var flexibleSize = 0;
        try {
            for (var _c = __values(this.viewItems), _d = _c.next(); !_d.done; _d = _c.next()) {
                var item = _d.value;
                if (item.maximumSize - item.minimumSize > 0) {
                    flexibleViewItems.push(item);
                    flexibleSize += item.size;
                }
            }
        }
        catch (e_5_1) { e_5 = { error: e_5_1 }; }
        finally {
            try {
                if (_d && !_d.done && (_a = _c.return)) _a.call(_c);
            }
            finally { if (e_5) throw e_5.error; }
        }
        var size = Math.floor(flexibleSize / flexibleViewItems.length);
        try {
            for (var flexibleViewItems_1 = __values(flexibleViewItems), flexibleViewItems_1_1 = flexibleViewItems_1.next(); !flexibleViewItems_1_1.done; flexibleViewItems_1_1 = flexibleViewItems_1.next()) {
                var item = flexibleViewItems_1_1.value;
                item.size = (0, math_1.clamp)(size, item.minimumSize, item.maximumSize);
            }
        }
        catch (e_6_1) { e_6 = { error: e_6_1 }; }
        finally {
            try {
                if (flexibleViewItems_1_1 && !flexibleViewItems_1_1.done && (_b = flexibleViewItems_1.return)) _b.call(flexibleViewItems_1);
            }
            finally { if (e_6) throw e_6.error; }
        }
        var indexes = (0, math_1.range)(this.viewItems.length);
        var lowPriorityIndexes = indexes.filter(function (i) { return _this.viewItems[i].priority === LayoutPriority.Low; });
        var highPriorityIndexes = indexes.filter(function (i) { return _this.viewItems[i].priority === LayoutPriority.High; });
        this.relayout(lowPriorityIndexes, highPriorityIndexes);
    };
    Splitview.prototype.removeView = function (index, sizing, skipLayout) {
        if (skipLayout === void 0) { skipLayout = false; }
        // Remove view
        var viewItem = this.viewItems.splice(index, 1)[0];
        viewItem.dispose();
        // Remove sash
        if (this.viewItems.length >= 1) {
            var sashIndex = Math.max(index - 1, 0);
            var sashItem = this.sashes.splice(sashIndex, 1)[0];
            sashItem.disposable();
        }
        if (!skipLayout) {
            this.relayout();
        }
        if (sizing && sizing.type === 'distribute') {
            this.distributeViewSizes();
        }
        this._onDidRemoveView.fire(viewItem.view);
        return viewItem.view;
    };
    Splitview.prototype.getViewCachedVisibleSize = function (index) {
        if (index < 0 || index >= this.viewItems.length) {
            throw new Error('Index out of bounds');
        }
        var viewItem = this.viewItems[index];
        return viewItem.cachedVisibleSize;
    };
    Splitview.prototype.moveView = function (from, to) {
        var cachedVisibleSize = this.getViewCachedVisibleSize(from);
        var sizing = typeof cachedVisibleSize === 'undefined'
            ? this.getViewSize(from)
            : Sizing.Invisible(cachedVisibleSize);
        var view = this.removeView(from, undefined, true);
        this.addView(view, sizing, to);
    };
    Splitview.prototype.layout = function (size, orthogonalSize) {
        var _this = this;
        var previousSize = Math.max(this.size, this._contentSize);
        this.size = size;
        this.orthogonalSize = orthogonalSize;
        if (!this.proportions) {
            var indexes = (0, math_1.range)(this.viewItems.length);
            var lowPriorityIndexes = indexes.filter(function (i) { return _this.viewItems[i].priority === LayoutPriority.Low; });
            var highPriorityIndexes = indexes.filter(function (i) { return _this.viewItems[i].priority === LayoutPriority.High; });
            this.resize(this.viewItems.length - 1, size - previousSize, undefined, lowPriorityIndexes, highPriorityIndexes);
        }
        else {
            var total = 0;
            for (var i = 0; i < this.viewItems.length; i++) {
                var item = this.viewItems[i];
                var proportion = this.proportions[i];
                if (typeof proportion === 'number') {
                    total += proportion;
                }
                else {
                    size -= item.size;
                }
            }
            for (var i = 0; i < this.viewItems.length; i++) {
                var item = this.viewItems[i];
                var proportion = this.proportions[i];
                if (typeof proportion === 'number' && total > 0) {
                    item.size = (0, math_1.clamp)(Math.round((proportion * size) / total), item.minimumSize, item.maximumSize);
                }
            }
        }
        this.distributeEmptySpace();
        this.layoutViews();
    };
    Splitview.prototype.relayout = function (lowPriorityIndexes, highPriorityIndexes) {
        var contentSize = this.viewItems.reduce(function (r, i) { return r + i.size; }, 0);
        this.resize(this.viewItems.length - 1, this._size - contentSize, undefined, lowPriorityIndexes, highPriorityIndexes);
        this.distributeEmptySpace();
        this.layoutViews();
        this.saveProportions();
    };
    Splitview.prototype.distributeEmptySpace = function (lowPriorityIndex) {
        var e_7, _a, e_8, _b;
        var _this = this;
        var contentSize = this.viewItems.reduce(function (r, i) { return r + i.size; }, 0);
        var emptyDelta = this.size - contentSize;
        var indexes = (0, math_1.range)(this.viewItems.length - 1, -1);
        var lowPriorityIndexes = indexes.filter(function (i) { return _this.viewItems[i].priority === LayoutPriority.Low; });
        var highPriorityIndexes = indexes.filter(function (i) { return _this.viewItems[i].priority === LayoutPriority.High; });
        try {
            for (var highPriorityIndexes_2 = __values(highPriorityIndexes), highPriorityIndexes_2_1 = highPriorityIndexes_2.next(); !highPriorityIndexes_2_1.done; highPriorityIndexes_2_1 = highPriorityIndexes_2.next()) {
                var index = highPriorityIndexes_2_1.value;
                (0, array_1.pushToStart)(indexes, index);
            }
        }
        catch (e_7_1) { e_7 = { error: e_7_1 }; }
        finally {
            try {
                if (highPriorityIndexes_2_1 && !highPriorityIndexes_2_1.done && (_a = highPriorityIndexes_2.return)) _a.call(highPriorityIndexes_2);
            }
            finally { if (e_7) throw e_7.error; }
        }
        try {
            for (var lowPriorityIndexes_2 = __values(lowPriorityIndexes), lowPriorityIndexes_2_1 = lowPriorityIndexes_2.next(); !lowPriorityIndexes_2_1.done; lowPriorityIndexes_2_1 = lowPriorityIndexes_2.next()) {
                var index = lowPriorityIndexes_2_1.value;
                (0, array_1.pushToEnd)(indexes, index);
            }
        }
        catch (e_8_1) { e_8 = { error: e_8_1 }; }
        finally {
            try {
                if (lowPriorityIndexes_2_1 && !lowPriorityIndexes_2_1.done && (_b = lowPriorityIndexes_2.return)) _b.call(lowPriorityIndexes_2);
            }
            finally { if (e_8) throw e_8.error; }
        }
        if (typeof lowPriorityIndex === 'number') {
            (0, array_1.pushToEnd)(indexes, lowPriorityIndex);
        }
        for (var i = 0; emptyDelta !== 0 && i < indexes.length; i++) {
            var item = this.viewItems[indexes[i]];
            var size = (0, math_1.clamp)(item.size + emptyDelta, item.minimumSize, item.maximumSize);
            var viewDelta = size - item.size;
            emptyDelta -= viewDelta;
            item.size = size;
        }
    };
    Splitview.prototype.saveProportions = function () {
        var _this = this;
        if (this.proportionalLayout && this._contentSize > 0) {
            this._proportions = this.viewItems.map(function (i) {
                return i.visible ? i.size / _this._contentSize : undefined;
            });
        }
    };
    /**
     * Margin explain:
     *
     * For `n` views in a splitview there will be `n-1` margins `m`.
     *
     * To fit the margins each view must reduce in size by `(m * (n - 1)) / n`.
     *
     * For each view `i` the offet must be adjusted by `m * i/(n - 1)`.
     */
    Splitview.prototype.layoutViews = function () {
        var _this = this;
        this._contentSize = this.viewItems.reduce(function (r, i) { return r + i.size; }, 0);
        this.updateSashEnablement();
        if (this.viewItems.length === 0) {
            return;
        }
        var visibleViewItems = this.viewItems.filter(function (i) { return i.visible; });
        var sashCount = Math.max(0, visibleViewItems.length - 1);
        var marginReducedSize = (this.margin * sashCount) / Math.max(1, visibleViewItems.length);
        var totalLeftOffset = 0;
        var viewLeftOffsets = [];
        var sashWidth = 4; // hardcoded in css
        var runningVisiblePanelCount = this.viewItems.reduce(function (arr, viewItem, i) {
            var flag = viewItem.visible ? 1 : 0;
            if (i === 0) {
                arr.push(flag);
            }
            else {
                arr.push(arr[i - 1] + flag);
            }
            return arr;
        }, []);
        // calculate both view and cash positions
        this.viewItems.forEach(function (view, i) {
            totalLeftOffset += _this.viewItems[i].size;
            viewLeftOffsets.push(totalLeftOffset);
            var size = view.visible ? view.size - marginReducedSize : 0;
            var visiblePanelsBeforeThisView = Math.max(0, runningVisiblePanelCount[i] - 1);
            var offset = i === 0 || visiblePanelsBeforeThisView === 0
                ? 0
                : viewLeftOffsets[i - 1] +
                    (visiblePanelsBeforeThisView / sashCount) *
                        marginReducedSize;
            if (i < _this.viewItems.length - 1) {
                // calculate sash position
                var newSize = view.visible
                    ? offset + size - sashWidth / 2 + _this.margin / 2
                    : offset;
                if (_this._orientation === Orientation.HORIZONTAL) {
                    _this.sashes[i].container.style.left = "".concat(newSize, "px");
                    _this.sashes[i].container.style.top = "0px";
                }
                if (_this._orientation === Orientation.VERTICAL) {
                    _this.sashes[i].container.style.left = "0px";
                    _this.sashes[i].container.style.top = "".concat(newSize, "px");
                }
            }
            // calculate view position
            if (_this._orientation === Orientation.HORIZONTAL) {
                view.container.style.width = "".concat(size, "px");
                view.container.style.left = "".concat(offset, "px");
                view.container.style.top = '';
                view.container.style.height = '';
            }
            if (_this._orientation === Orientation.VERTICAL) {
                view.container.style.height = "".concat(size, "px");
                view.container.style.top = "".concat(offset, "px");
                view.container.style.width = '';
                view.container.style.left = '';
            }
            view.view.layout(view.size - marginReducedSize, _this._orthogonalSize);
        });
    };
    Splitview.prototype.findFirstSnapIndex = function (indexes) {
        var e_9, _a, e_10, _b;
        try {
            // visible views first
            for (var indexes_1 = __values(indexes), indexes_1_1 = indexes_1.next(); !indexes_1_1.done; indexes_1_1 = indexes_1.next()) {
                var index = indexes_1_1.value;
                var viewItem = this.viewItems[index];
                if (!viewItem.visible) {
                    continue;
                }
                if (viewItem.snap) {
                    return index;
                }
            }
        }
        catch (e_9_1) { e_9 = { error: e_9_1 }; }
        finally {
            try {
                if (indexes_1_1 && !indexes_1_1.done && (_a = indexes_1.return)) _a.call(indexes_1);
            }
            finally { if (e_9) throw e_9.error; }
        }
        try {
            // then, hidden views
            for (var indexes_2 = __values(indexes), indexes_2_1 = indexes_2.next(); !indexes_2_1.done; indexes_2_1 = indexes_2.next()) {
                var index = indexes_2_1.value;
                var viewItem = this.viewItems[index];
                if (viewItem.visible &&
                    viewItem.maximumSize - viewItem.minimumSize > 0) {
                    return undefined;
                }
                if (!viewItem.visible && viewItem.snap) {
                    return index;
                }
            }
        }
        catch (e_10_1) { e_10 = { error: e_10_1 }; }
        finally {
            try {
                if (indexes_2_1 && !indexes_2_1.done && (_b = indexes_2.return)) _b.call(indexes_2);
            }
            finally { if (e_10) throw e_10.error; }
        }
        return undefined;
    };
    Splitview.prototype.updateSashEnablement = function () {
        var previous = false;
        var collapsesDown = this.viewItems.map(function (i) { return (previous = i.size - i.minimumSize > 0 || previous); });
        previous = false;
        var expandsDown = this.viewItems.map(function (i) { return (previous = i.maximumSize - i.size > 0 || previous); });
        var reverseViews = __spreadArray([], __read(this.viewItems), false).reverse();
        previous = false;
        var collapsesUp = reverseViews
            .map(function (i) { return (previous = i.size - i.minimumSize > 0 || previous); })
            .reverse();
        previous = false;
        var expandsUp = reverseViews
            .map(function (i) { return (previous = i.maximumSize - i.size > 0 || previous); })
            .reverse();
        var position = 0;
        for (var index = 0; index < this.sashes.length; index++) {
            var sash = this.sashes[index];
            var viewItem = this.viewItems[index];
            position += viewItem.size;
            var min = !(collapsesDown[index] && expandsUp[index + 1]);
            var max = !(expandsDown[index] && collapsesUp[index + 1]);
            if (min && max) {
                var upIndexes = (0, math_1.range)(index, -1);
                var downIndexes = (0, math_1.range)(index + 1, this.viewItems.length);
                var snapBeforeIndex = this.findFirstSnapIndex(upIndexes);
                var snapAfterIndex = this.findFirstSnapIndex(downIndexes);
                var snappedBefore = typeof snapBeforeIndex === 'number' &&
                    !this.viewItems[snapBeforeIndex].visible;
                var snappedAfter = typeof snapAfterIndex === 'number' &&
                    !this.viewItems[snapAfterIndex].visible;
                if (snappedBefore &&
                    collapsesUp[index] &&
                    (position > 0 || this.startSnappingEnabled)) {
                    this.updateSash(sash, SashState.MINIMUM);
                }
                else if (snappedAfter &&
                    collapsesDown[index] &&
                    (position < this._contentSize || this.endSnappingEnabled)) {
                    this.updateSash(sash, SashState.MAXIMUM);
                }
                else {
                    this.updateSash(sash, SashState.DISABLED);
                }
            }
            else if (min && !max) {
                this.updateSash(sash, SashState.MINIMUM);
            }
            else if (!min && max) {
                this.updateSash(sash, SashState.MAXIMUM);
            }
            else {
                this.updateSash(sash, SashState.ENABLED);
            }
        }
    };
    Splitview.prototype.updateSash = function (sash, state) {
        (0, dom_1.toggleClass)(sash.container, 'dv-disabled', state === SashState.DISABLED);
        (0, dom_1.toggleClass)(sash.container, 'dv-enabled', state === SashState.ENABLED);
        (0, dom_1.toggleClass)(sash.container, 'dv-maximum', state === SashState.MAXIMUM);
        (0, dom_1.toggleClass)(sash.container, 'dv-minimum', state === SashState.MINIMUM);
    };
    Splitview.prototype.createViewContainer = function () {
        var element = document.createElement('div');
        element.className = 'dv-view-container';
        return element;
    };
    Splitview.prototype.createSashContainer = function () {
        var element = document.createElement('div');
        element.className = 'dv-sash-container';
        return element;
    };
    Splitview.prototype.createContainer = function () {
        var element = document.createElement('div');
        var orientationClassname = this._orientation === Orientation.HORIZONTAL
            ? 'dv-horizontal'
            : 'dv-vertical';
        element.className = "dv-split-view-container ".concat(orientationClassname);
        return element;
    };
    Splitview.prototype.dispose = function () {
        var e_11, _a;
        this._onDidSashEnd.dispose();
        this._onDidAddView.dispose();
        this._onDidRemoveView.dispose();
        for (var i = 0; i < this.element.children.length; i++) {
            if (this.element.children.item(i) === this.element) {
                this.element.removeChild(this.element);
                break;
            }
        }
        try {
            for (var _b = __values(this.viewItems), _c = _b.next(); !_c.done; _c = _b.next()) {
                var viewItem = _c.value;
                viewItem.dispose();
            }
        }
        catch (e_11_1) { e_11 = { error: e_11_1 }; }
        finally {
            try {
                if (_c && !_c.done && (_a = _b.return)) _a.call(_b);
            }
            finally { if (e_11) throw e_11.error; }
        }
        this.element.remove();
    };
    return Splitview;
}());
exports.Splitview = Splitview;
