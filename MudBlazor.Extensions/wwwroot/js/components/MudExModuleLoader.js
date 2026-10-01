const modulePromises = new Map();
let importQueue = Promise.resolve();

/**
 * Imports an ES module without allowing embedded UMD wrappers to register anonymous modules
 * in a global AMD loader such as Monaco's loader.js.
 *
 * Imports are serialized because window.define is process-global. The source is fetched first
 * so the AMD shim is disabled only for the short module evaluation phase, not for network I/O.
 */
export function importModuleWithoutAmd(url) {
    const absoluteUrl = new URL(url, document.baseURI).href;
    let modulePromise = modulePromises.get(absoluteUrl);
    if (modulePromise) return modulePromise;

    const importOperation = async () => {
        try {
            await fetch(absoluteUrl, { credentials: 'same-origin' });
        } catch (_) {
            // Dynamic import below provides the useful error. This fetch only warms the browser cache.
        }

        const amdDefine = typeof globalThis.define === 'function' ? globalThis.define : null;
        const amdDescriptor = amdDefine ? Object.getOwnPropertyDescriptor(amdDefine, 'amd') : null;
        try {
            // UMD wrappers only take their AMD branch when define.amd is truthy. Keeping the
            // define function itself intact means Monaco can continue resolving its own modules.
            if (amdDefine) amdDefine.amd = undefined;
            return await import(absoluteUrl);
        } finally {
            if (amdDefine && amdDescriptor) Object.defineProperty(amdDefine, 'amd', amdDescriptor);
            else if (amdDefine) delete amdDefine.amd;
        }
    };

    modulePromise = importQueue.then(importOperation, importOperation);
    importQueue = modulePromise.then(() => undefined, () => undefined);
    modulePromises.set(absoluteUrl, modulePromise);
    modulePromise.catch(() => modulePromises.delete(absoluteUrl));
    return modulePromise;
}

/** Preloads a module for callers that only need it in the browser module cache. */
export async function preloadModuleWithoutAmd(url) {
    await importModuleWithoutAmd(url);
}
