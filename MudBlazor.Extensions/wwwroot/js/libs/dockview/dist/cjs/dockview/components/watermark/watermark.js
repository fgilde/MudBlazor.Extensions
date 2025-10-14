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
exports.Watermark = void 0;
var lifecycle_1 = require("../../../lifecycle");
var Watermark = /** @class */ (function (_super) {
    __extends(Watermark, _super);
    function Watermark() {
        var _this = _super.call(this) || this;
        _this._element = document.createElement('div');
        _this._element.className = 'dv-watermark';
        return _this;
    }
    Object.defineProperty(Watermark.prototype, "element", {
        get: function () {
            return this._element;
        },
        enumerable: false,
        configurable: true
    });
    Watermark.prototype.init = function (_params) {
        // noop
    };
    return Watermark;
}(lifecycle_1.CompositeDisposable));
exports.Watermark = Watermark;
