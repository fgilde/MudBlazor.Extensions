"use strict";
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
Object.defineProperty(exports, "__esModule", { value: true });
exports.Paneview = void 0;
var splitview_1 = require("../splitview/splitview");
var lifecycle_1 = require("../lifecycle");
var events_1 = require("../events");
var dom_1 = require("../dom");
var Paneview = /** @class */ (function (_super) {
    __extends(Paneview, _super);
    function Paneview(container, options) {
        var _a;
        var _this = _super.call(this) || this;
        _this.paneItems = [];
        _this.skipAnimation = false;
        _this._onDidChange = new events_1.Emitter();
        _this.onDidChange = _this._onDidChange.event;
        _this._orientation = (_a = options.orientation) !== null && _a !== void 0 ? _a : splitview_1.Orientation.VERTICAL;
        _this.element = document.createElement('div');
        _this.element.className = 'dv-pane-container';
        container.appendChild(_this.element);
        _this.splitview = new splitview_1.Splitview(_this.element, {
            orientation: _this._orientation,
            proportionalLayout: false,
            descriptor: options.descriptor,
        });
        // if we've added views from the descriptor we need to
        // add the panes to our Pane array and setup animation
        _this.getPanes().forEach(function (pane) {
            var disposable = new lifecycle_1.CompositeDisposable(pane.onDidChangeExpansionState(function () {
                _this.setupAnimation();
                _this._onDidChange.fire(undefined);
            }));
            var paneItem = {
                pane: pane,
                disposable: {
                    dispose: function () {
                        disposable.dispose();
                    },
                },
            };
            _this.paneItems.push(paneItem);
            pane.orthogonalSize = _this.splitview.orthogonalSize;
        });
        _this.addDisposables(_this._onDidChange, _this.splitview.onDidSashEnd(function () {
            _this._onDidChange.fire(undefined);
        }), _this.splitview.onDidAddView(function () {
            _this._onDidChange.fire();
        }), _this.splitview.onDidRemoveView(function () {
            _this._onDidChange.fire();
        }));
        return _this;
    }
    Object.defineProperty(Paneview.prototype, "onDidAddView", {
        get: function () {
            return this.splitview.onDidAddView;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(Paneview.prototype, "onDidRemoveView", {
        get: function () {
            return this.splitview.onDidRemoveView;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(Paneview.prototype, "minimumSize", {
        get: function () {
            return this.splitview.minimumSize;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(Paneview.prototype, "maximumSize", {
        get: function () {
            return this.splitview.maximumSize;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(Paneview.prototype, "orientation", {
        get: function () {
            return this.splitview.orientation;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(Paneview.prototype, "size", {
        get: function () {
            return this.splitview.size;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(Paneview.prototype, "orthogonalSize", {
        get: function () {
            return this.splitview.orthogonalSize;
        },
        enumerable: false,
        configurable: true
    });
    Paneview.prototype.setViewVisible = function (index, visible) {
        this.splitview.setViewVisible(index, visible);
    };
    Paneview.prototype.addPane = function (pane, size, index, skipLayout) {
        var _this = this;
        if (index === void 0) { index = this.splitview.length; }
        if (skipLayout === void 0) { skipLayout = false; }
        var disposable = pane.onDidChangeExpansionState(function () {
            _this.setupAnimation();
            _this._onDidChange.fire(undefined);
        });
        var paneItem = {
            pane: pane,
            disposable: {
                dispose: function () {
                    disposable.dispose();
                },
            },
        };
        this.paneItems.splice(index, 0, paneItem);
        pane.orthogonalSize = this.splitview.orthogonalSize;
        this.splitview.addView(pane, size, index, skipLayout);
    };
    Paneview.prototype.getViewSize = function (index) {
        return this.splitview.getViewSize(index);
    };
    Paneview.prototype.getPanes = function () {
        return this.splitview.getViews();
    };
    Paneview.prototype.removePane = function (index, options) {
        if (options === void 0) { options = { skipDispose: false }; }
        var paneItem = this.paneItems.splice(index, 1)[0];
        this.splitview.removeView(index);
        if (!options.skipDispose) {
            paneItem.disposable.dispose();
            paneItem.pane.dispose();
        }
        return paneItem;
    };
    Paneview.prototype.moveView = function (from, to) {
        if (from === to) {
            return;
        }
        var view = this.removePane(from, { skipDispose: true });
        this.skipAnimation = true;
        try {
            this.addPane(view.pane, view.pane.size, to, false);
        }
        finally {
            this.skipAnimation = false;
        }
    };
    Paneview.prototype.layout = function (size, orthogonalSize) {
        this.splitview.layout(size, orthogonalSize);
    };
    Paneview.prototype.setupAnimation = function () {
        var _this = this;
        if (this.skipAnimation) {
            return;
        }
        if (this.animationTimer) {
            clearTimeout(this.animationTimer);
            this.animationTimer = undefined;
        }
        (0, dom_1.addClasses)(this.element, 'dv-animated');
        this.animationTimer = setTimeout(function () {
            _this.animationTimer = undefined;
            (0, dom_1.removeClasses)(_this.element, 'dv-animated');
        }, 200);
    };
    Paneview.prototype.dispose = function () {
        _super.prototype.dispose.call(this);
        if (this.animationTimer) {
            clearTimeout(this.animationTimer);
            this.animationTimer = undefined;
        }
        this.paneItems.forEach(function (paneItem) {
            paneItem.disposable.dispose();
            paneItem.pane.dispose();
        });
        this.paneItems = [];
        this.splitview.dispose();
        this.element.remove();
    };
    return Paneview;
}(lifecycle_1.CompositeDisposable));
exports.Paneview = Paneview;
