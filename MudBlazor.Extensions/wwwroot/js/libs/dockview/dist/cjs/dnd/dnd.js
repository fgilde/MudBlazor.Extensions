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
exports.DragAndDropObserver = void 0;
var events_1 = require("../events");
var lifecycle_1 = require("../lifecycle");
var DragAndDropObserver = /** @class */ (function (_super) {
    __extends(DragAndDropObserver, _super);
    function DragAndDropObserver(element, callbacks) {
        var _this = _super.call(this) || this;
        _this.element = element;
        _this.callbacks = callbacks;
        _this.target = null;
        _this.registerListeners();
        return _this;
    }
    DragAndDropObserver.prototype.onDragEnter = function (e) {
        this.target = e.target;
        this.callbacks.onDragEnter(e);
    };
    DragAndDropObserver.prototype.onDragOver = function (e) {
        e.preventDefault(); // needed so that the drop event fires (https://stackoverflow.com/questions/21339924/drop-event-not-firing-in-chrome)
        if (this.callbacks.onDragOver) {
            this.callbacks.onDragOver(e);
        }
    };
    DragAndDropObserver.prototype.onDragLeave = function (e) {
        if (this.target === e.target) {
            this.target = null;
            this.callbacks.onDragLeave(e);
        }
    };
    DragAndDropObserver.prototype.onDragEnd = function (e) {
        this.target = null;
        this.callbacks.onDragEnd(e);
    };
    DragAndDropObserver.prototype.onDrop = function (e) {
        this.callbacks.onDrop(e);
    };
    DragAndDropObserver.prototype.registerListeners = function () {
        var _this = this;
        this.addDisposables((0, events_1.addDisposableListener)(this.element, 'dragenter', function (e) {
            _this.onDragEnter(e);
        }, true));
        this.addDisposables((0, events_1.addDisposableListener)(this.element, 'dragover', function (e) {
            _this.onDragOver(e);
        }, true));
        this.addDisposables((0, events_1.addDisposableListener)(this.element, 'dragleave', function (e) {
            _this.onDragLeave(e);
        }));
        this.addDisposables((0, events_1.addDisposableListener)(this.element, 'dragend', function (e) {
            _this.onDragEnd(e);
        }));
        this.addDisposables((0, events_1.addDisposableListener)(this.element, 'drop', function (e) {
            _this.onDrop(e);
        }));
    };
    return DragAndDropObserver;
}(lifecycle_1.CompositeDisposable));
exports.DragAndDropObserver = DragAndDropObserver;
