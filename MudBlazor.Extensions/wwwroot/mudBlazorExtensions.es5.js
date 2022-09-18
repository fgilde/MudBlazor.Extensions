'use strict';

var _createClass = (function () { function defineProperties(target, props) { for (var i = 0; i < props.length; i++) { var descriptor = props[i]; descriptor.enumerable = descriptor.enumerable || false; descriptor.configurable = true; if ('value' in descriptor) descriptor.writable = true; Object.defineProperty(target, descriptor.key, descriptor); } } return function (Constructor, protoProps, staticProps) { if (protoProps) defineProperties(Constructor.prototype, protoProps); if (staticProps) defineProperties(Constructor, staticProps); return Constructor; }; })();

function _classCallCheck(instance, Constructor) { if (!(instance instanceof Constructor)) { throw new TypeError('Cannot call a class as a function'); } }

var MudBlazorExtensionHelper = (function () {
    function MudBlazorExtensionHelper(options, dotNet, onDone) {
        _classCallCheck(this, MudBlazorExtensionHelper);

        this.dotnet = dotNet;
        this.onDone = onDone;
        this.options = options;
        this.mudDialogSelector = options.mudDialogSelector || '.mud-dialog:not([data-mud-extended=true])';
        this.mudDialogHeaderSelector = options.mudDialogHeaderSelector || '.mud-dialog-title';
        this.dialog = document.querySelector(this.mudDialogSelector);
        this.dialogHeader = this.dialog.querySelector(this.mudDialogHeaderSelector);
    }

    _createClass(MudBlazorExtensionHelper, [{
        key: 'findDialog',
        value: function findDialog() {
            return Array.from(document.querySelectorAll(this.mudDialogSelector)).filter(function (d) {
                return !d.__extended;
            })[0];
        }
    }, {
        key: 'init',
        value: function init() {
            var _this = this;

            this.dialog = this.dialog || this.findDialog();
            if (!this.dialog) {
                this.observer = new MutationObserver(function (e) {
                    var res = e.filter(function (x) {
                        return x.addedNodes && x.addedNodes[0] === _this.findDialog();
                    });
                    if (res.length) {
                        _this.dialog = res[0].addedNodes[0];
                        _this.observer.disconnect();
                        _this.handleExtensions();
                    }
                });
                this.observer.observe(document, { characterData: true, childList: true, subtree: true });
            } else {
                this.handleExtensions();
            }
        }
    }, {
        key: 'handleExtensions',
        value: function handleExtensions() {
            var _this2 = this;

            this.dialog.__extended = true;
            this.dialog.setAttribute('data-mud-extended', true);
            setTimeout(function () {
                return _this2.dialog.classList.remove('mud-ex-dialog-initial');
            }, 50);
            // For animations
            if (this.options.animations != null && Array.isArray(this.options.animations) && this.options.animations.length) {
                this.animate(this.options.animationDescriptions);
            }

            if (this.options.disablePositionMargin) {
                this.dialog.classList.add('mud-dialog-position-fixed');
            }

            // Full height ext
            if (this.options.fullHeight) {
                var cls = this.options.disableSizeMarginY ? 'mud-dialog-height-full-no-margin' : 'mud-dialog-height-full';
                this.dialog.classList.add(cls);
                var actions = this.dialog.querySelector('.mud-dialog-actions');
                if (actions) {
                    actions.classList.add('mud-dialog-actions-fixed-bottom');
                }
            }
            if (this.options.fullWidth && this.options.disableSizeMarginX) {
                this.dialog.classList.remove('mud-dialog-width-full');
                this.dialog.classList.add('mud-dialog-width-full-no-margin');
                if (this.dialog.classList.contains('mud-dialog-width-false')) {
                    this.dialog.classList.remove('mud-dialog-width-false');
                }
            }

            // Inject buttons
            if (this.options.buttons && this.options.buttons.length) {
                this.options.buttons.forEach(function (b) {
                    if (_this2.dialogHeader) {
                        _this2.dialogHeader.insertAdjacentHTML('beforeend', b.html);
                        var btnEl = _this2.dialogHeader.querySelector('#' + b.id);
                        btnEl.onclick = function () {
                            if (b.id.indexOf('mud-button-maximize') >= 0) {
                                _this2.maximize();
                            } else {
                                b.callBackReference.invokeMethodAsync(b.callbackName);
                            }
                        };
                    }
                });
            }

            // Handle drag
            if (this.options.dragMode !== 0 && this.dialog) {
                this.dragElement(this.dialog, this.dialogHeader);
                this.done();
            }

            // Resize
            this.checkResizeable();
        }
    }, {
        key: 'checkResizeable',
        value: function checkResizeable() {
            this.dialog.style.position = 'absolute';
            if (this.options.resizeable) {
                //this.dialog.style.position = 'absolute';
                this.dialog.style['resize'] = 'both';
                this.dialog.style['overflow'] = 'auto';
                this.dialog.style.maxWidth = window.innerWidth + 'px';
                this.dialog.style.maxHeight = window.innerHeight + 'px';
                this.dialog.style.minWidth = '100px';
                this.dialog.style.minHeight = '100px';
            }
        }
    }, {
        key: 'maximize',
        value: function maximize() {
            if (this._oldStyle) {
                this.dialog.style = this._oldStyle;
                delete this._oldStyle;
            } else {
                this._oldStyle = this.dialog.style;
                this.dialog.style.position = 'absolute';
                this.dialog.style.left = "0";
                this.dialog.style.top = "0";

                this.dialog.style.maxWidth = this.dialog.style.width = window.innerWidth + 'px';
                this.dialog.style.maxHeight = this.dialog.style.height = window.innerHeight + 'px';
            }
            this.checkResizeable();
        }
    }, {
        key: 'done',
        value: function done() {
            if (this.observer) this.observer.disconnect();
            if (this.onDone) this.onDone();
        }
    }, {
        key: 'dragElement',
        value: function dragElement(dialogEl, headerEl, container) {
            var pos1 = 0,
                pos2 = 0,
                pos3 = 0,
                pos4 = 0;
            container = container || document.body;
            if (headerEl) {
                // if present, the header is where you move the DIV from:
                headerEl.style.cursor = 'move';
                headerEl.onmousedown = dragMouseDown;
            } else {
                // otherwise, move the DIV from anywhere inside the DIV:
                dialogEl.onmousedown = dragMouseDown;
            }

            function dragMouseDown(e) {
                e = e || window.event;
                e.preventDefault();
                // get the mouse cursor position at startup:
                pos3 = e.clientX;
                pos4 = e.clientY;
                document.onmouseup = closeDragElement;
                // call a function whenever the cursor moves:
                document.onmousemove = elementDrag;
            }

            function elementDrag(e) {
                e = e || window.event;
                e.preventDefault();
                // calculate the new cursor position:

                pos1 = pos3 - e.clientX;
                pos2 = pos4 - e.clientY;
                pos3 = e.clientX;
                pos4 = e.clientY;
                var bounds = {
                    x: container.offsetWidth - dialogEl.offsetWidth,
                    y: (container === document.body ? window.innerHeight : container.offsetHeight) - dialogEl.offsetHeight
                };

                var aY = dialogEl.offsetTop - pos2;
                var aX = dialogEl.offsetLeft - pos1;
                dialogEl.style.position = 'absolute';
                if (aX > 0 && aX < bounds.x) {
                    dialogEl.style.left = aX + "px";
                }
                if (aY > 0 && aY < bounds.y) {
                    dialogEl.style.top = aY + "px";
                }

                pos1 = pos3 - e.clientX;
                pos2 = pos4 - e.clientY;
                pos3 = e.clientX;
                pos4 = e.clientY;
            }

            function closeDragElement() {
                // stop moving when mouse button is released:
                document.onmouseup = null;
                document.onmousemove = null;
            }
        }
    }, {
        key: 'animate',
        value: function animate(types) {
            //var names = types.map(type => this.options.dialogPositionNames.map(n => `kf-mud-dialog-${type}-${n} ${this.options.animationDurationInMs}ms ${this.options.animationTimingFunctionString} 1 alternate`));
            //this.dialog.style.animation = `${names.join(',')}`;
            this.dialog.style.animation = '' + this.options.animationStyle;
        }
    }]);

    return MudBlazorExtensionHelper;
})();

window.MudBlazorExtensions = {
    helper: null,
    disposeMudExFileDisplay: function disposeMudExFileDisplay(id) {
        if (window.__mudExFileDisplay && window.__mudExFileDisplay[id]) {
            window.__mudExFileDisplay[id] = null;
            delete window.__mudExFileDisplay[id];
        }
    },
    initMudExFileDisplay: function initMudExFileDisplay(dotNet, id) {
        window.__mudExFileDisplay = window.__mudExFileDisplay || {};
        window.__mudExFileDisplay[id] = {
            callBackReference: dotNet
        };
    },

    setNextDialogOptions: function setNextDialogOptions(options, dotNet) {
        new MudBlazorExtensionHelper(options, dotNet, function () {
            MudBlazorExtensions.helper = null;
            delete MudBlazorExtensions.helper;
        }).init();
    },

    addCss: function addCss(cssContent) {
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

    downloadFile: function downloadFile(options) {
        var fileUrl = options.url || "data:" + options.mimeType + ";base64," + options.base64String;
        fetch(fileUrl).then(function (response) {
            return response.blob();
        }).then(function (blob) {
            var link = window.document.createElement("a");
            //link.href = window.URL.createObjectURL(blob, { type: options.mimeType });
            link.href = window.URL.createObjectURL(blob);
            link.download = options.fileName;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
        });
    }
};

