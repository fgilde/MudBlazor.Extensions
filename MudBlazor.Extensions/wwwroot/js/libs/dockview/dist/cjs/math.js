"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.range = exports.sequentialNumberGenerator = exports.clamp = void 0;
var clamp = function (value, min, max) {
    if (min > max) {
        /**
         * caveat: an error should be thrown here if this was a proper `clamp` function but we need to handle
         * cases where `min` > `max` and in those cases return `min`.
         */
        return min;
    }
    return Math.min(max, Math.max(value, min));
};
exports.clamp = clamp;
var sequentialNumberGenerator = function () {
    var value = 1;
    return { next: function () { return (value++).toString(); } };
};
exports.sequentialNumberGenerator = sequentialNumberGenerator;
var range = function (from, to) {
    var result = [];
    if (typeof to !== 'number') {
        to = from;
        from = 0;
    }
    if (from <= to) {
        for (var i = from; i < to; i++) {
            result.push(i);
        }
    }
    else {
        for (var i = from; i > to; i--) {
            result.push(i);
        }
    }
    return result;
};
exports.range = range;
