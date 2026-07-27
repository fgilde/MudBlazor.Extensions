require.config({ paths: { 'vs': 'lib/monaco-editor/min/vs' } });

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

            if (language == 'razor') {
                if ((textUntilPosition.match(/{/g) || []).length !== (textUntilPosition.match(/}/g) || []).length) {
                    var data = await fetch("editor/snippets/csharp.json").then((response) => response.json());
                } else {
                    //var data = await fetch("editor/snippets/mudblazor.json").then((response) => response.json());
                    var data = await fetch("api/snippets/mudex.json").then((response) => response.json());
                }
            } else {
                var data = await fetch("editor/snippets/csharp.json").then((response) => response.json());
            }

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

function throttle(func, timeFrame, id) {
    const now = new Date();
    if (now - throttleLastTimeFuncNameMappings[id] >= timeFrame) {
        func();

        throttleLastTimeFuncNameMappings[id] = now;
    }
}

window.Try = {

    initialize: function (dotNetInstance) {
        _dotNetInstance = dotNetInstance;
        throttleLastTimeFuncNameMappings['compile'] = new Date();

        window.addEventListener('keydown', onKeyDown);
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
    dispose: function () {
        _dotNetInstance = null;
        window.removeEventListener('keydown', onKeyDown);
    }
}

window.Try.__providerRegistered = false;
window.Try.Editor = window.Try.Editor || (function () {
    // one monaco editor + model per id (dock panel); model per file keeps undo/scroll state
    const _editors = new Map();
    const _pending = new Map(); // value set before async create completed

    function _get(id) { return _editors.get(id); }

    function _registerGlobalsOnce() {
        if (window.Try.__providerRegistered) { return; }
        monaco.languages.html.razorDefaults.setModeConfiguration({
            completionItems: true,
            diagnostics: true,
            documentFormattingEdits: true,
            documentHighlights: true,
            documentRangeFormattingEdits: true,
        });
        registerLangugageProvider('razor');
        registerLangugageProvider('csharp');
        window.Try.__providerRegistered = true;
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

                _disposeEditor(id);

                const model = monaco.editor.createModel(_pending.get(id) ?? value ?? '', language || 'razor');
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
        dispose: function (id) {
            _disposeEditor(id);
        }
    }
}());

window.Try.CodeExecution = window.Try.CodeExecution || (function () {
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
