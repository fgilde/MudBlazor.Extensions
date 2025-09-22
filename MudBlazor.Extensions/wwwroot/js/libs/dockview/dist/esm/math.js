export const clamp = (value, min, max) => {
    if (min > max) {
        /**
         * caveat: an error should be thrown here if this was a proper `clamp` function but we need to handle
         * cases where `min` > `max` and in those cases return `min`.
         */
        return min;
    }
    return Math.min(max, Math.max(value, min));
};
export const sequentialNumberGenerator = () => {
    let value = 1;
    return { next: () => (value++).toString() };
};
export const range = (from, to) => {
    const result = [];
    if (typeof to !== 'number') {
        to = from;
        from = 0;
    }
    if (from <= to) {
        for (let i = from; i < to; i++) {
            result.push(i);
        }
    }
    else {
        for (let i = from; i > to; i--) {
            result.push(i);
        }
    }
    return result;
};
