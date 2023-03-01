window.BlazorJS = {
    loadScripts: function (fileNames, dotNet) {
        var loadedScripts = Array.from(document.querySelectorAll('script'));
        fileNames = typeof fileNames === 'string' ? [fileNames] : fileNames;
        fileNames.filter(filename => loadedScripts.some(elm => !elm.src.endsWith(filename))).forEach(filename => {
            var script = document.createElement('script');
            script.src = filename;
            script.async = true;
            script.addEventListener('load', function () {
                if (dotNet) {
                    dotNet.invokeMethodAsync('Loaded', filename);
                }
            });
            document.getElementsByTagName('head')[0].appendChild(script);
        });
    },

    execute: function (script, params) {
        return eval(script)(window, ...params);
    },

    unloadScripts: function (fileNames) {
        var loadedScripts = Array.from(document.querySelectorAll('script'));
        fileNames = typeof fileNames === 'string' ? [fileNames] : fileNames;
        loadedScripts.filter(s => fileNames.some(f => s.src.endsWith(f))).forEach(script => {
            script.parentNode.removeChild(script);
        });
    },

    addCss: function (cssContent) {
        var css = cssContent,
            head = document.head || document.getElementsByTagName('head')[0],
            style = document.createElement('style');

        head.appendChild(style);

        style.type = 'text/css';
        if (style.styleSheet) {
            // This is required for IE8 and below.
            style.styleSheet.cssText = css;
        } else {
            style.appendChild(document.createTextNode(css));
        }
    },
};