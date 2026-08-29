/*!
 * MudBlazor.Extensions web components loader.
 *
 *   <script src="https://www.mudex.org/wc/mudex.js"></script>
 *   <mudex-file-display url="https://example.org/demo.pdf"></mudex-file-display>
 *
 * Loads the MudEx WebAssembly host from wherever this script came from, so it also works
 * when the page itself is hosted somewhere else entirely.
 */
(function () {
    if (window.MudEx && window.MudEx.__started) {
        return;
    }

    var currentScript = document.currentScript;
    var base = new URL('.', currentScript.src).href;

    var resolve;
    var ready = new Promise(function (r) { resolve = r; });

    window.MudEx = window.MudEx || {};
    window.MudEx.__started = true;
    window.MudEx.base = base;
    window.MudEx.assetBase = base;
    window.MudEx.getAssetBase = function () { return base; };
    window.MudEx.ready = ready;
    var tagsKnown;
    var tagsPromise = new Promise(function (r) { tagsKnown = r; });
    window.MudEx.tags = [];
    window.MudEx.__setTags = function (tags) { window.MudEx.tags = tags; tagsKnown(); };
    window.MudEx.setDarkMode = function (isDarkMode) {
        return ready.then(function () {
            return DotNet.invokeMethodAsync('MudEx.WebComponents', 'SetDarkMode', !!isDarkMode);
        });
    };

    function addCss(href, id) {
        if (id && document.getElementById(id)) return;
        var link = document.createElement('link');
        link.rel = 'stylesheet';
        link.href = href;
        if (id) link.id = id;
        document.head.appendChild(link);
    }

    function addScript(src, attributes) {
        return new Promise(function (done, fail) {
            var script = document.createElement('script');
            script.src = src;
            script.async = false;
            Object.keys(attributes || {}).forEach(function (key) {
                script.setAttribute(key, attributes[key]);
            });
            script.onload = function () { done(); };
            script.onerror = function () { fail(new Error('MudEx: failed to load ' + src)); };
            document.head.appendChild(script);
        });
    }

    addCss('https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap');
    addCss(base + '_content/MudBlazor/MudBlazor.min.css');
    addCss(base + '_content/MudBlazor.Markdown/MudBlazor.Markdown.min.css');
    // same id MudEx itself uses, so it does not inject its embedded copy a second time
    addCss(base + '_content/MudBlazor.Extensions/mudBlazorExtensions.min.css', 'mudex-styles');
    // scoped component css of every razor library, bundled under the host project name. Without it
    // the audio visualizer and other components with their own .razor.css render unstyled.
    addCss(base + 'MudEx.WebComponents.styles.css');

    // everything the runtime needs lives next to this script, not next to the page.
    // defaultUri keeps the relative path including culture folders of satellite assemblies.
    function bootResource(type, name, defaultUri, integrity) {
        var path = defaultUri;
        var marker = path.indexOf('_framework/');
        if (marker >= 0) {
            path = path.substring(marker);
        } else if (/^[a-z]+:\/\//i.test(path)) {
            return path; // absolute url we do not own, leave it alone
        }
        return new URL(path, base).href;
    }

    // Blazor resolves library initializers (_content/**/*.lib.module.js) against document.baseURI
    // with no hook to change it. An import map redirects those absolute urls to where the bundle is.
    function ensureImportMap() {
        var pageContent = new URL('_content/', document.baseURI).href;
        var bundleContent = base + '_content/';
        if (pageContent === bundleContent) {
            return;
        }
        if (document.querySelector('script[type="importmap"]')) {
            console.warn('[MudEx] the page already declares an import map - skipping the MudEx one. ' +
                'Add "' + pageContent + '": "' + bundleContent + '" to it if components stay empty.');
            return;
        }
        var imports = {};
        imports[pageContent] = bundleContent;
        var map = document.createElement('script');
        map.type = 'importmap';
        map.textContent = JSON.stringify({ imports: imports });
        document.head.appendChild(map);
    }

    function ensureProviderRoot() {
        var root = document.getElementById('mudex-wc-root');
        if (!root) {
            root = document.createElement('div');
            root.id = 'mudex-wc-root';
            document.body.appendChild(root);
        }
        return root;
    }

    function boot() {
        ensureImportMap();
        ensureProviderRoot();
        // blazor.webassembly.js first: the MudBlazor/MudEx scripts expect Blazor to exist when they run
        return addScript(base + '_framework/blazor.webassembly.js', { autostart: 'false' }).then(function () {
            return Promise.all([
                addScript(base + '_content/MudBlazor/MudBlazor.min.js'),
                addScript(base + '_content/MudBlazor.Markdown/MudBlazor.Markdown.min.js'),
                addScript(base + '_content/MudBlazor.Extensions/js/mudBlazorExtensions.all.min.js')
            ]);
        }).then(function () {
            return Blazor.start({
                loadBootResource: bootResource,
                webAssembly: {
                    configureRuntime: function (builder) {
                        // library initializers (_content/**/*.lib.module.js) are resolved by the runtime
                        // itself, not through loadBootResource - they need the same rebasing
                        builder.withModuleConfig({
                            locateFile: function (path) { return new URL(path, base + '_framework/').href; }
                        });
                    }
                }
            });
        }).then(function () {
            // .NET reports the registered tags right after the host started - wait shortly for it
            return Promise.race([tagsPromise, new Promise(function (r) { setTimeout(r, 3000); })]);
        }).then(function () {
            resolve(window.MudEx);
        }).catch(function (error) {
            console.error('[MudEx] web components failed to start', error);
            throw error;
        });
    }

    if (document.body) {
        boot();
    } else {
        document.addEventListener('DOMContentLoaded', boot);
    }
})();
