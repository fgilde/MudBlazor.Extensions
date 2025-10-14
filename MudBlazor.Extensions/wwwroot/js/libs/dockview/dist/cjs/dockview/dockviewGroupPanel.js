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
exports.DockviewGroupPanel = void 0;
var dockviewGroupPanelModel_1 = require("./dockviewGroupPanelModel");
var gridviewPanel_1 = require("../gridview/gridviewPanel");
var dockviewGroupPanelApi_1 = require("../api/dockviewGroupPanelApi");
var MINIMUM_DOCKVIEW_GROUP_PANEL_WIDTH = 100;
var MINIMUM_DOCKVIEW_GROUP_PANEL_HEIGHT = 100;
var DockviewGroupPanel = /** @class */ (function (_super) {
    __extends(DockviewGroupPanel, _super);
    function DockviewGroupPanel(accessor, id, options) {
        var _a, _b, _c, _d, _e, _f;
        var _this = _super.call(this, id, 'groupview_default', {
            minimumHeight: (_b = (_a = options.constraints) === null || _a === void 0 ? void 0 : _a.minimumHeight) !== null && _b !== void 0 ? _b : MINIMUM_DOCKVIEW_GROUP_PANEL_HEIGHT,
            minimumWidth: (_d = (_c = options.constraints) === null || _c === void 0 ? void 0 : _c.maximumHeight) !== null && _d !== void 0 ? _d : MINIMUM_DOCKVIEW_GROUP_PANEL_WIDTH,
            maximumHeight: (_e = options.constraints) === null || _e === void 0 ? void 0 : _e.maximumHeight,
            maximumWidth: (_f = options.constraints) === null || _f === void 0 ? void 0 : _f.maximumWidth,
        }, new dockviewGroupPanelApi_1.DockviewGroupPanelApiImpl(id, accessor)) || this;
        _this.api.initialize(_this); // cannot use 'this' after after 'super' call
        _this._model = new dockviewGroupPanelModel_1.DockviewGroupPanelModel(_this.element, accessor, id, options, _this);
        _this.addDisposables(_this.model.onDidActivePanelChange(function (event) {
            _this.api._onDidActivePanelChange.fire(event);
        }));
        return _this;
    }
    Object.defineProperty(DockviewGroupPanel.prototype, "minimumWidth", {
        get: function () {
            var _a;
            var activePanelMinimumWidth = (_a = this.activePanel) === null || _a === void 0 ? void 0 : _a.minimumWidth;
            if (typeof activePanelMinimumWidth === 'number') {
                return activePanelMinimumWidth;
            }
            return _super.prototype.__minimumWidth.call(this);
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewGroupPanel.prototype, "minimumHeight", {
        get: function () {
            var _a;
            var activePanelMinimumHeight = (_a = this.activePanel) === null || _a === void 0 ? void 0 : _a.minimumHeight;
            if (typeof activePanelMinimumHeight === 'number') {
                return activePanelMinimumHeight;
            }
            return _super.prototype.__minimumHeight.call(this);
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewGroupPanel.prototype, "maximumWidth", {
        get: function () {
            var _a;
            var activePanelMaximumWidth = (_a = this.activePanel) === null || _a === void 0 ? void 0 : _a.maximumWidth;
            if (typeof activePanelMaximumWidth === 'number') {
                return activePanelMaximumWidth;
            }
            return _super.prototype.__maximumWidth.call(this);
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewGroupPanel.prototype, "maximumHeight", {
        get: function () {
            var _a;
            var activePanelMaximumHeight = (_a = this.activePanel) === null || _a === void 0 ? void 0 : _a.maximumHeight;
            if (typeof activePanelMaximumHeight === 'number') {
                return activePanelMaximumHeight;
            }
            return _super.prototype.__maximumHeight.call(this);
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewGroupPanel.prototype, "panels", {
        get: function () {
            return this._model.panels;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewGroupPanel.prototype, "activePanel", {
        get: function () {
            return this._model.activePanel;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewGroupPanel.prototype, "size", {
        get: function () {
            return this._model.size;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewGroupPanel.prototype, "model", {
        get: function () {
            return this._model;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewGroupPanel.prototype, "locked", {
        get: function () {
            return this._model.locked;
        },
        set: function (value) {
            this._model.locked = value;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewGroupPanel.prototype, "header", {
        get: function () {
            return this._model.header;
        },
        enumerable: false,
        configurable: true
    });
    DockviewGroupPanel.prototype.focus = function () {
        if (!this.api.isActive) {
            this.api.setActive();
        }
        _super.prototype.focus.call(this);
    };
    DockviewGroupPanel.prototype.initialize = function () {
        this._model.initialize();
    };
    DockviewGroupPanel.prototype.setActive = function (isActive) {
        _super.prototype.setActive.call(this, isActive);
        this.model.setActive(isActive);
    };
    DockviewGroupPanel.prototype.layout = function (width, height) {
        _super.prototype.layout.call(this, width, height);
        this.model.layout(width, height);
    };
    DockviewGroupPanel.prototype.getComponent = function () {
        return this._model;
    };
    DockviewGroupPanel.prototype.toJSON = function () {
        return this.model.toJSON();
    };
    return DockviewGroupPanel;
}(gridviewPanel_1.GridviewPanel));
exports.DockviewGroupPanel = DockviewGroupPanel;
