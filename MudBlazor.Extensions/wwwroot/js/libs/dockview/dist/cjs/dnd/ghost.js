"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.addGhostImage = void 0;
var dom_1 = require("../dom");
function addGhostImage(dataTransfer, ghostElement, options) {
    var _a, _b;
    // class dockview provides to force ghost image to be drawn on a different layer and prevent weird rendering issues
    (0, dom_1.addClasses)(ghostElement, 'dv-dragged');
    // move the element off-screen initially otherwise it may in some cases be rendered at (0,0) momentarily
    ghostElement.style.top = '-9999px';
    document.body.appendChild(ghostElement);
    dataTransfer.setDragImage(ghostElement, (_a = options === null || options === void 0 ? void 0 : options.x) !== null && _a !== void 0 ? _a : 0, (_b = options === null || options === void 0 ? void 0 : options.y) !== null && _b !== void 0 ? _b : 0);
    setTimeout(function () {
        (0, dom_1.removeClasses)(ghostElement, 'dv-dragged');
        ghostElement.remove();
    }, 0);
}
exports.addGhostImage = addGhostImage;
