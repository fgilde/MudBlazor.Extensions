// the assets ship with the package, so every url is resolved below the static web asset root.
// A host that serves monaco from somewhere else can override the paths before this script runs.
window.Playzor = window.Playzor || {};
window.Playzor.assetRoot = window.Playzor.assetRoot || '_content/Playzor.Blazor.Editor';
window.Playzor.snippetUrls = window.Playzor.snippetUrls || {};
window.Playzor.snippetUrls.csharp = window.Playzor.snippetUrls.csharp || `${window.Playzor.assetRoot}/editor/snippets/csharp.json`;
window.Playzor.snippetUrls.markup = window.Playzor.snippetUrls.markup || `${window.Playzor.assetRoot}/editor/snippets/mudblazor.json`;

require.config({ paths: { 'vs': `${window.Playzor.assetRoot}/lib/monaco-editor/min/vs` } });

let _dotNetInstance;

const throttleLastTimeFuncNameMappings = {};

function isScrollAtBottom(containerOrId) {
    if (typeof containerOrId === 'string' || containerOrId instanceof String) {
        containerOrId = document.querySelector(containerOrId);
    }

    return containerOrId.scrollHeight - containerOrId.scrollTop === containerOrId.clientHeight;
}

function registerLangugageProvider(language) {
    monaco.languages.registerCompletionItemProvider(language, {
        provideCompletionItems: async function (model, position) {
            var textUntilPosition = model.getValueInRange({
                startLineNumber: 1,
                startColumn: 1,
                endLineNumber: position.lineNumber,
                endColumn: position.column,
            });

            const urls = window.Playzor.snippetUrls;
            // inside a razor code block the c# snippets apply, outside them the markup ones
            const inCodeBlock = (textUntilPosition.match(/{/g) || []).length !== (textUntilPosition.match(/}/g) || []).length;
            const url = language !== 'razor' || inCodeBlock ? urls.csharp : urls.markup;
            var data = await fetch(url).then((response) => response.json());

            var word = model.getWordUntilPosition(position);
            var range = {
                startLineNumber: position.lineNumber,
                endLineNumber: position.lineNumber,
                startColumn: word.startColumn,
                endColumn: word.endColumn,
            };

            var response = Object.keys(data).map(key => {
                return {
                    label: data[key].prefix,
                    detail: data[key].description,
                    documentation: data[key].body.join('\n'),
                    insertText: data[key].body.join('\n'),
                    kind: monaco.languages.CompletionItemKind.Snippet,
                    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                    range: range
                }
            });
            return {
                suggestions: response,
            };
        },
    });
}

function onKeyDown(e) {
    if (e.ctrlKey && e.keyCode === 83) {
        e.preventDefault();

        if (_dotNetInstance && _dotNetInstance.invokeMethodAsync) {
            throttle(() => _dotNetInstance.invokeMethodAsync('TriggerCompileAsync'), 1000, 'compile');
        }
    }
}

// the preview iframe is a second wasm instance and cannot call our services directly,
// so its run button arrives as a message
function onWindowMessage(e) {
    if (e.origin !== window.location.origin || !e.data || e.data.__playzor !== 'run') { return; }

    if (_dotNetInstance && _dotNetInstance.invokeMethodAsync) {
        throttle(() => _dotNetInstance.invokeMethodAsync('TriggerCompileAsync'), 1000, 'compile');
    }
}

function throttle(func, timeFrame, id) {
    const now = new Date();
    if (now - throttleLastTimeFuncNameMappings[id] >= timeFrame) {
        func();

        throttleLastTimeFuncNameMappings[id] = now;
    }
}

Object.assign(window.Playzor, {

    initialize: function (dotNetInstance) {
        _dotNetInstance = dotNetInstance;
        throttleLastTimeFuncNameMappings['compile'] = new Date();

        window.addEventListener('keydown', onKeyDown);
        window.addEventListener('message', onWindowMessage);
    },
    changeDisplayUrl: function (url) {
        if (!url) { return; }
        window.history.pushState(null, null, url);
    },
    reloadIframe: function (id, newSrc) {
        const iFrame = document.getElementById(id);
        if (!iFrame) { return; }

        // Standard-URL, wenn keine übergeben wurde
        if (!newSrc) {
            newSrc = iFrame.getAttribute('data-base-src') || iFrame.getAttribute('src') || '/user-page';
        }

        // Basis-URL merken (ohne alten Querystring)
        const url = new URL(newSrc, window.location.origin);
        url.searchParams.set('_cb', Date.now().toString()); // Cache-Buster

        const bustedSrc = url.pathname + url.search;

        // Immer komplett neu setzen, damit der Frame wirklich neu lädt
        iFrame.src = '';
        setTimeout(() => {
            iFrame.setAttribute('data-base-src', url.pathname); // Basis-URL merken
            iFrame.src = bustedSrc;
        }, 0);
    },
    prefersDark: function () {
        return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
    },
    // opens a generated html page in a new tab, so an embed snippet can be tried out as it is
    openHtmlInNewTab: function (html) {
        const url = URL.createObjectURL(new Blob([html], { type: 'text/html' }));
        const win = window.open(url, '_blank');
        // the tab keeps the document, the object url can go once it has loaded
        setTimeout(() => URL.revokeObjectURL(url), 60000);
        return !!win;
    },
    dispose: function () {
        _dotNetInstance = null;
        window.removeEventListener('keydown', onKeyDown);
        window.removeEventListener('message', onWindowMessage);
    }
});

window.Playzor.__providerRegistered = false;
window.Playzor.Editor = window.Playzor.Editor || (function () {
    // one monaco editor + model per id (dock panel); model per file keeps undo/scroll state
    const _editors = new Map();
    const _pending = new Map(); // value set before async create completed

    function _get(id) { return _editors.get(id); }

    function _registerGlobalsOnce() {
        if (window.Playzor.__providerRegistered) { return; }
        monaco.languages.html.razorDefaults.setModeConfiguration({
            completionItems: true,
            diagnostics: true,
            documentFormattingEdits: true,
            documentHighlights: true,
            documentRangeFormattingEdits: true,
        });
        registerLangugageProvider('razor');
        registerLangugageProvider('csharp');
        window.Playzor.__providerRegistered = true;
    }

    function _disposeEditor(id) {
        const ed = _editors.get(id);
        if (ed) {
            try { ed.getModel()?.dispose(); } catch (e) { }
            try { ed.dispose(); } catch (e) { }
            _editors.delete(id);
        }
        _pending.delete(id);
    }

    return {
        create: function (id, value, language, readOnly, theme) {
            if (!id) { return; }

            require(['vs/editor/editor.main'], () => {
                const host = document.getElementById(id);
                if (!host) { return; } // panel was closed before monaco finished loading

                // read before dispose: a setValue that arrived while monaco was still
                // loading (late-loaded sample/snippet) lives in _pending, and _disposeEditor clears it
                const pendingValue = _pending.get(id);
                _disposeEditor(id);

                const model = monaco.editor.createModel(pendingValue ?? value ?? '', language || 'razor');
                const editor = monaco.editor.create(host, {
                    model: model,
                    theme: theme,
                    readOnly: readOnly,
                    automaticLayout: true,
                    mouseWheelZoom: true,
                    bracketPairColorization: {
                        enabled: true
                    },
                    minimap: {
                        enabled: false
                    }
                });
                _pending.delete(id);
                _editors.set(id, editor);

                _registerGlobalsOnce();
            })
        },
        getValue: function (id) {
            return _get(id)?.getValue() ?? _pending.get(id) ?? '';
        },
        getValues: function () {
            const result = {};
            for (const [id, editor] of _editors) { result[id] = editor.getValue(); }
            return result;
        },
        setValue: function (id, value) {
            const editor = _get(id);
            if (editor) {
                editor.setValue(value);
            } else {
                _pending.set(id, value);
            }
        },
        setReadOnly: function (id, readOnly) {
            _get(id)?.updateOptions({ readOnly: readOnly });
        },
        focus: function (id) {
            return _get(id)?.focus();
        },
        setLanguage: function (id, language) {
            const editor = _get(id);
            if (editor) {
                monaco.editor.setModelLanguage(editor.getModel(), language);
            }
        },
        setPosition: function (id, line, column) {
            _get(id)?.setPosition({ lineNumber: line, column: column });
        },
        setSelection: function (id, startLineNumber, startColumn, endLineNumber, endColumn) {
            const editor = _get(id);
            if (!editor) { return; }
            editor.setSelection({
                startLineNumber: startLineNumber,
                startColumn: startColumn || 0,
                endLineNumber: endLineNumber || startLineNumber,
                endColumn: endColumn || editor.getModel().getLineMaxColumn(endLineNumber || startLineNumber)
            });
            editor.revealLineInCenter(startLineNumber);
        },
        setTheme: function (theme) {
            if (window.monaco) {
                monaco.editor.setTheme(theme); // monaco themes are global — one call covers all editors
            }
        },
        // markers: [{ line, column, endLine, endColumn, message, severity: 'error'|'warning'|'info' }]
        setMarkers: function (id, markers) {
            const editor = _get(id);
            if (!editor || !window.monaco) { return; }
            const model = editor.getModel();
            if (!model) { return; }

            const severityOf = function (s) {
                if (s === 'error') { return monaco.MarkerSeverity.Error; }
                if (s === 'warning') { return monaco.MarkerSeverity.Warning; }
                return monaco.MarkerSeverity.Info;
            };

            monaco.editor.setModelMarkers(model, 'try-compiler', (markers || []).map(function (m) {
                const line = Math.max(1, Math.min(m.line || 1, model.getLineCount()));
                const endLine = Math.max(line, Math.min(m.endLine || line, model.getLineCount()));
                return {
                    startLineNumber: line,
                    startColumn: m.column > 0 ? m.column : 1,
                    endLineNumber: endLine,
                    endColumn: m.endColumn > 0 ? m.endColumn : model.getLineMaxColumn(endLine),
                    message: m.message || '',
                    severity: severityOf(m.severity)
                };
            }));
        },
        clearMarkers: function (id) {
            const editor = _get(id);
            const model = editor?.getModel();
            if (model && window.monaco) { monaco.editor.setModelMarkers(model, 'try-compiler', []); }
        },
        dispose: function (id) {
            _disposeEditor(id);
        }
    }
}());

window.Playzor.Embed = window.Playzor.Embed || (function () {
    let _observer = null;

    return {
        // lets the hosting page size the iframe to its content (opt-in on the host side)
        initAutoHeight: function () {
            if (window.self === window.top || _observer) { return; }

            const post = function () {
                const height = Math.ceil(document.documentElement.scrollHeight);
                try { parent.postMessage({ __playzor: 'resize', height: height }, '*'); } catch (e) { }
            };

            _observer = new ResizeObserver(post);
            _observer.observe(document.documentElement);
            post();
        }
    };
}());

window.Playzor.Preview = window.Playzor.Preview || (function () {
    let _dotNetRef = null;
    let _listening = false;
    let _lastPreviewLoad = 0;

    window.addEventListener('message', function (e) {
        if (e.origin !== window.location.origin || !e.data || e.data.__playzor !== 'preview-loaded') { return; }
        _lastPreviewLoad = Date.now();
    });

    return {
        // repl -> preview: the iframe reads dark/light from its url on load, so a live
        // toggle has to be pushed in
        pushTheme: function (isDark) {
            document.querySelectorAll('iframe.playzor-preview-frame').forEach(function (frame) {
                try { frame.contentWindow.postMessage({ __playzor: 'theme', dark: !!isDark }, window.location.origin); } catch (e) { }
            });
        },

        // preview -> repl: run button of the empty preview
        requestRun: function () {
            try { parent.postMessage({ __playzor: 'run' }, window.location.origin); } catch (e) { }
        },

        // true when the preview page announced itself since the given timestamp — the editor uses
        // this to tell a missing preview route from a snippet that simply renders nothing
        loadedSince: function (sinceMs) {
            return _lastPreviewLoad > (sinceMs || 0);
        },

        // called inside the iframe
        listen: function (dotNetRef) {
            _dotNetRef = dotNetRef;
            if (_listening) { return; }
            _listening = true;
            window.addEventListener('message', function (e) {
                if (e.origin !== window.location.origin || !e.data || e.data.__playzor !== 'theme') { return; }
                try { _dotNetRef?.invokeMethodAsync('SetDarkMode', !!e.data.dark); } catch (err) { }
            });
        }
    };
}());

window.Playzor.Console = window.Playzor.Console || (function () {
    const MAX_ENTRIES = 2000;
    const FLUSH_MS = 250;

    let _entries = [];
    let _pending = [];
    let _dotNetRef = null;
    let _flushTimer = null;
    let _listening = false;

    function flush() {
        _flushTimer = null;
        if (!_pending.length || !_dotNetRef) { return; }
        const batch = _pending;
        _pending = [];
        _dotNetRef.invokeMethodAsync('OnConsoleBatch', batch).catch(() => { });
    }

    function onMessage(event) {
        if (event.origin !== location.origin) { return; }
        const data = event.data;
        if (!data || data.__playzor !== 'log') { return; }

        const entry = { level: data.level || 'log', text: String(data.text ?? ''), ts: data.ts || Date.now() };
        _entries.push(entry);
        if (_entries.length > MAX_ENTRIES) { _entries = _entries.slice(-MAX_ENTRIES); }

        _pending.push(entry);
        if (!_flushTimer) { _flushTimer = setTimeout(flush, FLUSH_MS); }
    }

    return {
        init: function (dotNetRef) {
            _dotNetRef = dotNetRef;
            if (!_listening) {
                window.addEventListener('message', onMessage);
                _listening = true;
            }
            return _entries;
        },
        getAll: function () { return _entries; },
        clear: function () { _entries = []; _pending = []; },
        dispose: function () {
            _dotNetRef = null;
            if (_flushTimer) { clearTimeout(_flushTimer); _flushTimer = null; }
        },
        isScrollAtBottom: function (selector, threshold) {
            const el = document.querySelector(selector);
            if (!el) { return true; }
            return el.scrollHeight - el.scrollTop - el.clientHeight <= (threshold || 40);
        },
        scrollToBottom: function (selector) {
            const el = document.querySelector(selector);
            if (el) { el.scrollTop = el.scrollHeight; }
        }
    };
}());

window.Playzor.CodeExecution = window.Playzor.CodeExecution || (function () {
    const UNEXPECTED_ERROR_MESSAGE = 'An unexpected error has occurred. Please try again later or contact the team.';

    // Hier halten wir die aktuellen UserComponents in Memory
    let _userComponentsDllBytes = null;
    let _userComponentsDllBase64 = null;

    function convertBase64StringToBytes(base64String) {
        const binaryString = window.atob(base64String);

        const bytesCount = binaryString.length;
        const bytes = new Uint8Array(bytesCount);
        for (let i = 0; i < bytesCount; i++) {
            bytes[i] = binaryString.charCodeAt(i);
        }

        return bytes;
    }

    function ensureBase64FromBytes(bytes) {
        if (!bytes || !bytes.length) {
            return null;
        }
        let binary = "";
        const len = bytes.length;
        for (let i = 0; i < len; i++) {
            binary += String.fromCharCode(bytes[i]);
        }
        return window.btoa(binary);
    }

    return {
        updateUserComponentsDll: async function (fileContent) {
            if (!fileContent) {
                return;
            }

            // alter Code aus deinem Beispiel: Pointer → String
            fileContent = typeof fileContent === 'number'
                ? BINDING.conv_string(fileContent)
                : fileContent; // raw pointer → mono string

            let dllBytes;
            let base64String;

            if (typeof fileContent === 'string') {
                base64String = fileContent;
                dllBytes = convertBase64StringToBytes(base64String);
            } else if (fileContent instanceof Uint8Array) {
                dllBytes = fileContent;
                base64String = ensureBase64FromBytes(dllBytes);
            } else {
                alert(UNEXPECTED_ERROR_MESSAGE);
                return;
            }

            if (!(dllBytes instanceof Uint8Array)) {
                alert(UNEXPECTED_ERROR_MESSAGE);
                return;
            }

            _userComponentsDllBytes = dllBytes;
            _userComponentsDllBase64 = base64String;

            try {
                if (base64String) {
                    sessionStorage.setItem('try-usercomponents-dll', base64String);
                }
            } catch (e) {
                console.warn('Failed to persist user components dll to sessionStorage', e);
            }
        },

        // Wird vom Bootloader (loadBootResource) verwendet
        getUserComponentsDllBytes: function () {
            if (_userComponentsDllBytes && _userComponentsDllBytes.length) {
                return _userComponentsDllBytes;
            }

            try {
                const base64 = _userComponentsDllBase64 || sessionStorage.getItem('try-usercomponents-dll');
                if (base64) {
                    _userComponentsDllBase64 = base64;
                    _userComponentsDllBytes = convertBase64StringToBytes(base64);
                    return _userComponentsDllBytes;
                }
            } catch (e) {
                console.warn('Failed to read user components dll from sessionStorage', e);
            }

            return null;
        }
    };
}());
