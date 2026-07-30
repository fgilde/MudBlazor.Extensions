// Preview side of the playground. Loaded by the page that hosts the compiled user component
// (the iframe the editor points at) and forwards its console output plus unhandled errors to the
// editor around it, where the console panel collects them. Must run before blazor starts so early
// startup errors are captured too.
(function () {
    if (window.self === window.top) { return; }

    var post = function (level, text) {
        try { parent.postMessage({ __playzor: 'log', level: level, text: text, ts: Date.now() }, location.origin); } catch (e) { }
    };

    var stringify = function (args) {
        return Array.prototype.map.call(args, function (a) {
            if (a instanceof Error) { return a.stack || (a.name + ': ' + a.message); }
            if (typeof a === 'object' && a !== null) { try { return JSON.stringify(a); } catch (e) { return String(a); } }
            return String(a);
        }).join(' ');
    };

    ['log', 'info', 'warn', 'error', 'debug'].forEach(function (level) {
        var original = console[level];
        console[level] = function () {
            post(level, stringify(arguments));
            if (original) { original.apply(console, arguments); }
        };
    });

    window.addEventListener('error', function (e) {
        post('error', e.error ? (e.error.stack || e.error.message) : (e.message + ' (' + e.filename + ':' + e.lineno + ')'));
    });

    window.addEventListener('unhandledrejection', function (e) {
        var r = e.reason;
        post('error', 'Unhandled rejection: ' + (r && (r.stack || r.message) ? (r.stack || r.message) : String(r)));
    });
})();
