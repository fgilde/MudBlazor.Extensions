"use strict";
/*---------------------------------------------------------------------------------------------
 * Accreditation: This file is largly based upon the MIT licenced VSCode sourcecode found at:
 * https://github.com/microsoft/vscode/tree/main/src/vs/base/browser/ui/grid
 *--------------------------------------------------------------------------------------------*/
Object.defineProperty(exports, "__esModule", { value: true });
exports.LeafNode = void 0;
var splitview_1 = require("../splitview/splitview");
var events_1 = require("../events");
var LeafNode = /** @class */ (function () {
    function LeafNode(view, orientation, orthogonalSize, size) {
        if (size === void 0) { size = 0; }
        var _this = this;
        this.view = view;
        this.orientation = orientation;
        this._onDidChange = new events_1.Emitter();
        this.onDidChange = this._onDidChange.event;
        this._orthogonalSize = orthogonalSize;
        this._size = size;
        this._disposable = this.view.onDidChange(function (event) {
            if (event) {
                _this._onDidChange.fire({
                    size: _this.orientation === splitview_1.Orientation.VERTICAL
                        ? event.width
                        : event.height,
                    orthogonalSize: _this.orientation === splitview_1.Orientation.VERTICAL
                        ? event.height
                        : event.width,
                });
            }
            else {
                _this._onDidChange.fire({});
            }
        });
    }
    Object.defineProperty(LeafNode.prototype, "minimumWidth", {
        get: function () {
            return this.view.minimumWidth;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(LeafNode.prototype, "maximumWidth", {
        get: function () {
            return this.view.maximumWidth;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(LeafNode.prototype, "minimumHeight", {
        get: function () {
            return this.view.minimumHeight;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(LeafNode.prototype, "maximumHeight", {
        get: function () {
            return this.view.maximumHeight;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(LeafNode.prototype, "priority", {
        get: function () {
            return this.view.priority;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(LeafNode.prototype, "snap", {
        get: function () {
            return this.view.snap;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(LeafNode.prototype, "minimumSize", {
        get: function () {
            return this.orientation === splitview_1.Orientation.HORIZONTAL
                ? this.minimumHeight
                : this.minimumWidth;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(LeafNode.prototype, "maximumSize", {
        get: function () {
            return this.orientation === splitview_1.Orientation.HORIZONTAL
                ? this.maximumHeight
                : this.maximumWidth;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(LeafNode.prototype, "minimumOrthogonalSize", {
        get: function () {
            return this.orientation === splitview_1.Orientation.HORIZONTAL
                ? this.minimumWidth
                : this.minimumHeight;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(LeafNode.prototype, "maximumOrthogonalSize", {
        get: function () {
            return this.orientation === splitview_1.Orientation.HORIZONTAL
                ? this.maximumWidth
                : this.maximumHeight;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(LeafNode.prototype, "orthogonalSize", {
        get: function () {
            return this._orthogonalSize;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(LeafNode.prototype, "size", {
        get: function () {
            return this._size;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(LeafNode.prototype, "element", {
        get: function () {
            return this.view.element;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(LeafNode.prototype, "width", {
        get: function () {
            return this.orientation === splitview_1.Orientation.HORIZONTAL
                ? this.orthogonalSize
                : this.size;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(LeafNode.prototype, "height", {
        get: function () {
            return this.orientation === splitview_1.Orientation.HORIZONTAL
                ? this.size
                : this.orthogonalSize;
        },
        enumerable: false,
        configurable: true
    });
    LeafNode.prototype.setVisible = function (visible) {
        if (this.view.setVisible) {
            this.view.setVisible(visible);
        }
    };
    LeafNode.prototype.layout = function (size, orthogonalSize) {
        this._size = size;
        this._orthogonalSize = orthogonalSize;
        this.view.layout(this.width, this.height);
    };
    LeafNode.prototype.dispose = function () {
        this._onDidChange.dispose();
        this._disposable.dispose();
    };
    return LeafNode;
}());
exports.LeafNode = LeafNode;
