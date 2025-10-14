"use strict";
var __assign = (this && this.__assign) || function () {
    __assign = Object.assign || function(t) {
        for (var s, i = 1, n = arguments.length; i < n; i++) {
            s = arguments[i];
            for (var p in s) if (Object.prototype.hasOwnProperty.call(s, p))
                t[p] = s[p];
        }
        return t;
    };
    return __assign.apply(this, arguments);
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.DockviewPanelModel = void 0;
var defaultTab_1 = require("./components/tab/defaultTab");
var DockviewPanelModel = /** @class */ (function () {
    function DockviewPanelModel(accessor, id, contentComponent, tabComponent) {
        this.accessor = accessor;
        this.id = id;
        this.contentComponent = contentComponent;
        this.tabComponent = tabComponent;
        this._content = this.createContentComponent(this.id, contentComponent);
        this._tab = this.createTabComponent(this.id, tabComponent);
    }
    Object.defineProperty(DockviewPanelModel.prototype, "content", {
        get: function () {
            return this._content;
        },
        enumerable: false,
        configurable: true
    });
    Object.defineProperty(DockviewPanelModel.prototype, "tab", {
        get: function () {
            return this._tab;
        },
        enumerable: false,
        configurable: true
    });
    DockviewPanelModel.prototype.createTabRenderer = function (tabLocation) {
        var _a;
        var cmp = this.createTabComponent(this.id, this.tabComponent);
        if (this._params) {
            cmp.init(__assign(__assign({}, this._params), { tabLocation: tabLocation }));
        }
        if (this._updateEvent) {
            (_a = cmp.update) === null || _a === void 0 ? void 0 : _a.call(cmp, this._updateEvent);
        }
        return cmp;
    };
    DockviewPanelModel.prototype.init = function (params) {
        this._params = params;
        this.content.init(params);
        this.tab.init(__assign(__assign({}, params), { tabLocation: 'header' }));
    };
    DockviewPanelModel.prototype.layout = function (width, height) {
        var _a, _b;
        (_b = (_a = this.content).layout) === null || _b === void 0 ? void 0 : _b.call(_a, width, height);
    };
    DockviewPanelModel.prototype.update = function (event) {
        var _a, _b, _c, _d;
        this._updateEvent = event;
        (_b = (_a = this.content).update) === null || _b === void 0 ? void 0 : _b.call(_a, event);
        (_d = (_c = this.tab).update) === null || _d === void 0 ? void 0 : _d.call(_c, event);
    };
    DockviewPanelModel.prototype.dispose = function () {
        var _a, _b, _c, _d;
        (_b = (_a = this.content).dispose) === null || _b === void 0 ? void 0 : _b.call(_a);
        (_d = (_c = this.tab).dispose) === null || _d === void 0 ? void 0 : _d.call(_c);
    };
    DockviewPanelModel.prototype.createContentComponent = function (id, componentName) {
        return this.accessor.options.createComponent({
            id: id,
            name: componentName,
        });
    };
    DockviewPanelModel.prototype.createTabComponent = function (id, componentName) {
        var name = componentName !== null && componentName !== void 0 ? componentName : this.accessor.options.defaultTabComponent;
        if (name) {
            if (this.accessor.options.createTabComponent) {
                var component = this.accessor.options.createTabComponent({
                    id: id,
                    name: name,
                });
                if (component) {
                    return component;
                }
                else {
                    return new defaultTab_1.DefaultTab();
                }
            }
            console.warn("dockview: tabComponent '".concat(componentName, "' was not found. falling back to the default tab."));
        }
        return new defaultTab_1.DefaultTab();
    };
    return DockviewPanelModel;
}());
exports.DockviewPanelModel = DockviewPanelModel;
