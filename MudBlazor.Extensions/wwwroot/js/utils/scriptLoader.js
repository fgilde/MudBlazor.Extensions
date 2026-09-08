class MudExScriptLoader {

    // Promises per url, so two components asking for the same library share one download
    static _pending = {};

    /**
     * Loads a classic (non module) script and resolves once it has actually executed.
     *
     * UMD builds such as three.js and Leaflet register themselves with an AMD loader when a global "define"
     * exists - and a host page carrying monaco's loader has one. "define" is therefore hidden for exactly the
     * duration of the load so the library falls back to its browser global.
     */
    static loadScript(url) {
        if (this._pending[url]) {
            return this._pending[url];
        }

        this._pending[url] = new Promise((resolve, reject) => {
            const existing = document.querySelector(`script[data-mudex-src="${url}"]`);
            if (existing) {
                resolve();
                return;
            }

            const savedDefine = window.define;
            const restore = () => { window.define = savedDefine; };

            const element = document.createElement('script');
            element.src = url;
            element.async = false;
            element.dataset.mudexSrc = url;
            element.onload = () => { restore(); resolve(); };
            element.onerror = () => {
                restore();
                delete MudExScriptLoader._pending[url];
                reject(new Error('Failed to load ' + url));
            };

            window.define = undefined;
            document.head.appendChild(element);
        });

        return this._pending[url];
    }

    /**
     * Loads the given scripts strictly one after another. Loaders that extend a base library (three.js addons,
     * Leaflet plugins) must not race their base, so this never parallelizes.
     */
    static async loadScripts(urls) {
        for (const url of urls) {
            await this.loadScript(url);
        }
    }

    /**
     * Adds a stylesheet once. Resolves when it is applied, but a failing stylesheet only warns: a missing
     * stylesheet degrades the look, it must not stop the viewer from rendering.
     */
    static loadStyle(url) {
        const key = 'style:' + url;
        if (this._pending[key]) {
            return this._pending[key];
        }

        this._pending[key] = new Promise(resolve => {
            if (document.querySelector(`link[data-mudex-src="${url}"]`)) {
                resolve();
                return;
            }

            const element = document.createElement('link');
            element.rel = 'stylesheet';
            element.href = url;
            element.dataset.mudexSrc = url;
            element.onload = () => resolve();
            element.onerror = () => {
                console.warn('MudExScriptLoader: stylesheet failed to load', url);
                resolve();
            };
            document.head.appendChild(element);
        });

        return this._pending[key];
    }

    /**
     * Resolves a dotted global path, e.g. "THREE.OrbitControls".
     */
    static resolve(path) {
        return path.split('.').reduce((value, part) => (value == null ? value : value[part]), window);
    }

    /**
     * Loads the scripts and then asserts that every expected global really is there. A UMD build that took an
     * unexpected branch loads without error but leaves nothing behind, and the failure would otherwise only
     * show up later as a bare "X is not defined" inside the viewer.
     */
    static async require(urls, globals) {
        await this.loadScripts(urls);

        const missing = (globals || []).filter(name => this.resolve(name) === undefined);
        if (missing.length) {
            throw new Error('Loaded ' + urls.length + ' script(s) but these globals are missing: ' + missing.join(', '));
        }
    }
}

window.MudExScriptLoader = MudExScriptLoader;
