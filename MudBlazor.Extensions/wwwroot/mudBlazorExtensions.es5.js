'use strict';

var _createClass = (function () { function defineProperties(target, props) { for (var i = 0; i < props.length; i++) { var descriptor = props[i]; descriptor.enumerable = descriptor.enumerable || false; descriptor.configurable = true; if ('value' in descriptor) descriptor.writable = true; Object.defineProperty(target, descriptor.key, descriptor); } } return function (Constructor, protoProps, staticProps) { if (protoProps) defineProperties(Constructor.prototype, protoProps); if (staticProps) defineProperties(Constructor, staticProps); return Constructor; }; })();

function _classCallCheck(instance, Constructor) { if (!(instance instanceof Constructor)) { throw new TypeError('Cannot call a class as a function'); } }

var MudBlazorExtensionHelper = (function () {
    function MudBlazorExtensionHelper(options, dotNet, onDone) {
        var _this = this;

        _classCallCheck(this, MudBlazorExtensionHelper);

        this.dotnet = dotNet;
        this.onDone = onDone;
        this.options = options;
        this.mudDialogSelector = options.mudDialogSelector || '.mud-dialog';
        this.mudDialogHeaderSelector = options.mudDialogHeaderSelector || '.mud-dialog-title';
        this.dialog = document.querySelector(this.mudDialogSelector);

        if (options.disablePositionMargin) {
            this.dialog.classList.add('mud-dialog-position-fixed');
        }

        var names = options.dialogPositionNames;
        // For animations
        var fromCls = 'mud-dialog-from-right';
        var toCls = 'mud-dialog-to-right';
        this.dialog.classList.add(fromCls, 'mud-dialog-animate-ex'); // order is important
        setTimeout(function () {
            _this.dialog.classList.remove(fromCls);
            _this.dialog.classList.add(toCls);
            //setTimeout(() => this.dialog.classList.remove('mud-dialog-animate-ex', toCls), 500); // need to fit transition-duration from css
            setTimeout(function () {
                return _this.dialog.classList.remove('mud-dialog-animate-ex');
            }, 500); // need to fit transition-duration from css
        }, 5);

        // Full height ext
        if (options.fullHeight) {
            var cls = options.disableSizeMargin ? 'mud-dialog-height-full-no-margin' : 'mud-dialog-height-full';
            this.dialog.classList.add(cls);
        }
        if (options.fullWidth && options.disableSizeMargin) {
            this.dialog.classList.remove('mud-dialog-width-full');
            this.dialog.classList.add('mud-dialog-width-full-no-margin');
        }

        this.dialogHeader = this.dialog.querySelector(this.mudDialogHeaderSelector);
    }

    _createClass(MudBlazorExtensionHelper, [{
        key: 'handleExtensions',
        value: function handleExtensions() {
            var _this2 = this;

            // Inject buttons
            this.options.buttons.forEach(function (b) {
                _this2.dialogHeader.insertAdjacentHTML('beforeend', b.html);
                var btnEl = _this2.dialogHeader.querySelector('#' + b.id);
                btnEl.onclick = function () {
                    if (b.id.indexOf('mud-button-maximize') >= 0) {
                        _this2.maximize();
                    } else {
                        b.callBackReference.invokeMethodAsync(b.callbackName);
                    }
                };
            });

            // Handle drag
            if (this.options.dragMode !== 0 && this.dialog) {
                this.dragElement(this.dialog, this.dialogHeader);
                this.done();
            }

            // Resize
            if (this.options.resizeable) {
                this.dialog.style.position = 'absolute';
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
                this.dialog.style.left = "0px";
                this.dialog.style.top = "0px";

                this.dialog.style.maxWidth = this.dialog.style.width = window.innerWidth + 'px';
                this.dialog.style.maxHeight = this.dialog.style.height = window.innerHeight + 'px';
            }
        }
    }, {
        key: 'done',
        value: function done() {
            if (this.observer) this.observer.disconnect();
            if (this.onDone) this.onDone();
        }
    }, {
        key: 'init',
        value: function init() {
            var _this3 = this;

            this.dialog = this.dialog || document.querySelector(this.mudDialogSelector);
            if (!this.dialog) {
                this.observer = new MutationObserver(function (e) {
                    var res = e.filter(function (x) {
                        return x.addedNodes && x.addedNodes[0] === document.querySelector(_this3.mudDialogSelector);
                    });
                    if (res.length) {
                        _this3.dialog = res[0].addedNodes[0];
                        _this3.observer.disconnect();
                        _this3.handleExtensions();
                    }
                });
                this.observer.observe(document, { characterData: true, childList: true, subtree: true });
            } else {
                this.handleExtensions();
            }
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
    }]);

    return MudBlazorExtensionHelper;
})();

window.MudBlazorExtensions = {
    helper: null,
    setNextDialogOptions: function setNextDialogOptions(options, dotNet) {
        MudBlazorExtensions.helper = new MudBlazorExtensionHelper(options, dotNet, function () {
            MudBlazorExtensions.helper = null;
            delete MudBlazorExtensions.helper;
        });
        MudBlazorExtensions.helper.init();
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
    }
};

