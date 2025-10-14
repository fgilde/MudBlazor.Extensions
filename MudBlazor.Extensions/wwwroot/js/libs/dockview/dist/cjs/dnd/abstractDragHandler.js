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
exports.DragHandler = void 0;
var dom_1 = require("../dom");
var events_1 = require("../events");
var lifecycle_1 = require("../lifecycle");
var DragHandler = /** @class */ (function (_super) {
    __extends(DragHandler, _super);
    function DragHandler(el, disabled) {
        var _this = _super.call(this) || this;
        _this.el = el;
        _this.disabled = disabled;
        _this.dataDisposable = new lifecycle_1.MutableDisposable();
        _this.pointerEventsDisposable = new lifecycle_1.MutableDisposable();
        _this._onDragStart = new events_1.Emitter();
        _this.onDragStart = _this._onDragStart.event;
        _this.addDisposables(_this._onDragStart, _this.dataDisposable, _this.pointerEventsDisposable);
        _this.configure();
        return _this;
    }
    DragHandler.prototype.setDisabled = function (disabled) {
        this.disabled = disabled;
    };
    DragHandler.prototype.isCancelled = function (_event) {
        return false;
    };
    DragHandler.prototype.configure = function () {
        var _this = this;
        this.addDisposables(this._onDragStart, (0, events_1.addDisposableListener)(this.el, 'dragstart', function (event) {
            if (event.defaultPrevented || _this.isCancelled(event) || _this.disabled) {
                event.preventDefault();
                return;
            }
            var iframes = (0, dom_1.disableIframePointEvents)();
            _this.pointerEventsDisposable.value = {
                dispose: function () {
                    iframes.release();
                },
            };
            _this.el.classList.add('dv-dragged');
            setTimeout(function () { return _this.el.classList.remove('dv-dragged'); }, 0);
            _this.dataDisposable.value = _this.getData(event);
            _this._onDragStart.fire(event);
            if (event.dataTransfer) {
                event.dataTransfer.effectAllowed = 'move';
                var hasData = event.dataTransfer.items.length > 0;
                if (!hasData) {
                    /**
                     * Although this is not used by dockview many third party dnd libraries will check
                     * dataTransfer.types to determine valid drag events.
                     *
                     * For example: in react-dnd if dataTransfer.types is not set then the dragStart event will be cancelled
                     * through .preventDefault(). Since this is applied globally to all drag events this would break dockviews
                     * dnd logic. You can see the code at
                 P    * https://github.com/react-dnd/react-dnd/blob/main/packages/backend-html5/src/HTML5BackendImpl.ts#L542
                     */
                    event.dataTransfer.setData('text/plain', '');
                }
            }
        }), (0, events_1.addDisposableListener)(this.el, 'dragend', function () {
            _this.pointerEventsDisposable.dispose();
            setTimeout(function () {
                _this.dataDisposable.dispose(); // allow the data to be read by other handlers before disposing
            }, 0);
        }));
    };
    return DragHandler;
}(lifecycle_1.CompositeDisposable));
exports.DragHandler = DragHandler;
