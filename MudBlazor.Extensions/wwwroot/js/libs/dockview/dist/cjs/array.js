"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.remove = exports.firstIndex = exports.pushToEnd = exports.pushToStart = exports.sequenceEquals = exports.last = exports.tail = void 0;
function tail(arr) {
    if (arr.length === 0) {
        throw new Error('Invalid tail call');
    }
    return [arr.slice(0, arr.length - 1), arr[arr.length - 1]];
}
exports.tail = tail;
function last(arr) {
    return arr.length > 0 ? arr[arr.length - 1] : undefined;
}
exports.last = last;
function sequenceEquals(arr1, arr2) {
    if (arr1.length !== arr2.length) {
        return false;
    }
    for (var i = 0; i < arr1.length; i++) {
        if (arr1[i] !== arr2[i]) {
            return false;
        }
    }
    return true;
}
exports.sequenceEquals = sequenceEquals;
/**
 * Pushes an element to the start of the array, if found.
 */
function pushToStart(arr, value) {
    var index = arr.indexOf(value);
    if (index > -1) {
        arr.splice(index, 1);
        arr.unshift(value);
    }
}
exports.pushToStart = pushToStart;
/**
 * Pushes an element to the end of the array, if found.
 */
function pushToEnd(arr, value) {
    var index = arr.indexOf(value);
    if (index > -1) {
        arr.splice(index, 1);
        arr.push(value);
    }
}
exports.pushToEnd = pushToEnd;
function firstIndex(array, fn) {
    for (var i = 0; i < array.length; i++) {
        var element = array[i];
        if (fn(element)) {
            return i;
        }
    }
    return -1;
}
exports.firstIndex = firstIndex;
function remove(array, value) {
    var index = array.findIndex(function (t) { return t === value; });
    if (index > -1) {
        array.splice(index, 1);
        return true;
    }
    return false;
}
exports.remove = remove;
