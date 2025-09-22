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
exports.ContentContainer = void 0;
var lifecycle_1 = require("../../../lifecycle");
var events_1 = require("../../../events");
var dom_1 = require("../../../dom");
var droptarget_1 = require("../../../dnd/droptarget");
var dataTransfer_1 = require("../../../dnd/dataTransfer");
var ContentContainer = /** @class */ (function (_super) {
    __extends(ContentContainer, _super);
    function ContentContainer(accessor, group) {
        var _this = _super.call(this) || this;
        _this.accessor = accessor;
        _this.group = group;
        _this.disposable = new lifecycle_1.MutableDisposable();
        _this._onDidFocus = new events_1.Emitter();
        _this.onDidFocus = _this._onDidFocus.event;
        _this._onDidBlur = new events_1.Emitter();
        _this.onDidBlur = _this._onDidBlur.event;
        _this._element = document.createElement('div');
        _this._element.className = 'dv-content-container';
        _this._element.tabIndex = -1;
        _this.addDisposables(_this._onDidFocus, _this._onDidBlur);
        var target = group.dropTargetContainer;
        _this.dropTarget = new droptarget_1.Droptarget(_this.element, {
            getOverlayOutline: function () {
                var _a;
                return ((_a = accessor.options.theme) === null || _a === void 0 ? void 0 : _a.dndPanelOverlay) === 'group'
                    ? _this.element.parentElement
                    : null;
            },
            className: 'dv-drop-target-content',
            acceptedTargetZones: ['top', 'bottom', 'left', 'right', 'center'],
            canDisplayOverlay: function (event, position) {
                if (_this.group.locked === 'no-drop-target' ||
                    (_this.group.locked && position === 'center')) {
                    return false;
                }
                var data = (0, dataTransfer_1.getPanelData)();
                if (!data &&
                    event.shiftKey &&
                    _this.group.location.type !== 'floating') {
                    return false;
                }
                if (data && data.viewId === _this.accessor.id) {
                    return true;
                }
                return _this.group.canDisplayOverlay(event, position, 'content');
            },
            getOverrideTarget: target ? function () { return target.model; } : undefined,
        });
        _this.addDisposables(_this.dropTarget);
        return _this;
    }
    Object.defineProperty(ContentContainer.prototype, "element", {
        get: function () {
            return this._element;
        },
        enumerable: false,
        configurable: true
    });
    ContentContainer.prototype.show = function () {
        this.element.style.display = '';
    };
    ContentContainer.prototype.hide = function () {
        this.element.style.display = 'none';
    };
    ContentContainer.prototype.renderPanel = function (panel, options) {
        var _this = this;
        if (options === void 0) { options = { asActive: true }; }
        var doRender = options.asActive ||
            (this.panel && this.group.isPanelActive(this.panel));
        if (this.panel &&
            this.panel.view.content.element.parentElement === this._element) {
            /**
             * If the currently attached panel is mounted directly to the content then remove it
             */
            this._element.removeChild(this.panel.view.content.element);
        }
        this.panel = panel;
        var container;
        switch (panel.api.renderer) {
            case 'onlyWhenVisible':
                this.group.renderContainer.detatch(panel);
                if (this.panel) {
                    if (doRender) {
                        this._element.appendChild(this.panel.view.content.element);
                    }
                }
                container = this._element;
                break;
            case 'always':
                if (panel.view.content.element.parentElement === this._element) {
                    this._element.removeChild(panel.view.content.element);
                }
                container = this.group.renderContainer.attach({
                    panel: panel,
                    referenceContainer: this,
                });
                break;
            default:
                throw new Error("dockview: invalid renderer type '".concat(panel.api.renderer, "'"));
        }
        if (doRender) {
            var focusTracker = (0, dom_1.trackFocus)(container);
            var disposable = new lifecycle_1.CompositeDisposable();
            disposable.addDisposables(focusTracker, focusTracker.onDidFocus(function () { return _this._onDidFocus.fire(); }), focusTracker.onDidBlur(function () { return _this._onDidBlur.fire(); }));
            this.disposable.value = disposable;
        }
    };
    ContentContainer.prototype.openPanel = function (panel) {
        if (this.panel === panel) {
            return;
        }
        this.renderPanel(panel);
    };
    ContentContainer.prototype.layout = function (_width, _height) {
        // noop
    };
    ContentContainer.prototype.closePanel = function () {
        var _a;
        if (this.panel) {
            if (this.panel.api.renderer === 'onlyWhenVisible') {
                (_a = this.panel.view.content.element.parentElement) === null || _a === void 0 ? void 0 : _a.removeChild(this.panel.view.content.element);
            }
        }
        this.panel = undefined;
    };
    ContentContainer.prototype.dispose = function () {
        this.disposable.dispose();
        _super.prototype.dispose.call(this);
    };
    return ContentContainer;
}(lifecycle_1.CompositeDisposable));
exports.ContentContainer = ContentContainer;
