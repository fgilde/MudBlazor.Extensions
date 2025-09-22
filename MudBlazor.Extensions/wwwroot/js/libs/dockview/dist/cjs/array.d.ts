export declare function tail<T>(arr: T[]): [T[], T];
export declare function last<T>(arr: T[]): T | undefined;
export declare function sequenceEquals<T>(arr1: T[], arr2: T[]): boolean;
/**
 * Pushes an element to the start of the array, if found.
 */
export declare function pushToStart<T>(arr: T[], value: T): void;
/**
 * Pushes an element to the end of the array, if found.
 */
export declare function pushToEnd<T>(arr: T[], value: T): void;
export declare function firstIndex<T>(array: T[] | ReadonlyArray<T>, fn: (item: T) => boolean): number;
export declare function remove<T>(array: T[], value: T): boolean;
