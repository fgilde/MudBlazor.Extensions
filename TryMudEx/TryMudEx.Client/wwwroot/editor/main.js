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

        if (!newSrc) {
            iFrame.contentWindow.location.reload();
        } else if (iFrame.src !== `${window.location.origin}${newSrc}`) {
            iFrame.src = newSrc;
        } else {
            // There needs to be some change so the iFrame is actually reloaded
            iFrame.src = '';
            setTimeout(() => iFrame.src = newSrc);
        }
    },
    dispose: function () {
        _dotNetInstance = null;
        window.removeEventListener('keydown', onKeyDown);
    }
}

window.Try.__providerRegistered = false;
window.Try.Editor = window.Try.Editor || (function () {
    let _editor;
    let _overrideValue;

    return {
        create: function (id, value, language, readOnly, theme) {
            if (!id) { return; }

            require(['vs/editor/editor.main'], () => {
                _editor = monaco.editor.create(document.getElementById(id), {
                    value: _overrideValue || value || '',
                    language: language || 'razor',
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

                _overrideValue = null;

                monaco.languages.html.razorDefaults.setModeConfiguration({
                    completionItems: true,
                    diagnostics: true,
                    documentFormattingEdits: true,
                    documentHighlights: true,
                    documentRangeFormattingEdits: true,
                });

                if (!window.Try.__providerRegistered) {
                    registerLangugageProvider('razor');
                    registerLangugageProvider('csharp');
                    window.Try.__providerRegistered = true;
                }
            })
        },
        getValue: function () {
            return _editor.getValue();
        },
        setValue: function (value) {
            if (_editor) {
                _editor.setValue(value);
            } else {
                _overrideValue = value;
            }
        },
        setReadOnly: function (readOnly) {
            if (_editor) {
                _editor.updateOptions({ readOnly: readOnly });
            }
        },
        focus: function () {
            return _editor.focus();
        },
        setLanguage: function (language) {
            if (_editor) {
                monaco.editor.setModelLanguage(_editor.getModel(), language);
            }
        },
        setPosition: function (line, column) {
            if (_editor) {
                _editor.setPosition({ lineNumber: line, column: column });
            }
        },
        setSelection: function (startLineNumber, startColumn, endLineNumber, endColumn) {
            if (_editor) {
                _editor.setSelection({
                    startLineNumber: startLineNumber,
                    startColumn: startColumn || 0,
                    endLineNumber: endLineNumber || startLineNumber,
                    endColumn: endColumn || _editor.getModel().getLineMaxColumn(endLineNumber || startLineNumber)
                });
            }
        },
        setTheme: function (theme) {
            if (_editor) {
                monaco.editor.setTheme(theme);
            }
        },
        dispose: function () {
            _editor = null;
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
