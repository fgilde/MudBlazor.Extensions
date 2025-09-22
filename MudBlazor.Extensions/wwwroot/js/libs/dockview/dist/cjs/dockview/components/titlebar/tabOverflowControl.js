"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.createDropdownElementHandle = void 0;
var svg_1 = require("../../../svg");
function createDropdownElementHandle() {
    var el = document.createElement('div');
    el.className = 'dv-tabs-overflow-dropdown-default';
    var text = document.createElement('span');
    text.textContent = "";
    var icon = (0, svg_1.createChevronRightButton)();
    el.appendChild(icon);
    el.appendChild(text);
    return {
        element: el,
        update: function (params) {
            text.textContent = "".concat(params.tabs);
        },
    };
}
exports.createDropdownElementHandle = createDropdownElementHandle;
