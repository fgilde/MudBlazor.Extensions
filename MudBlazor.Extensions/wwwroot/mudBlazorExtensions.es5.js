'use strict';

var _createClass = (function () { function defineProperties(target, props) { for (var i = 0; i < props.length; i++) { var descriptor = props[i]; descriptor.enumerable = descriptor.enumerable || false; descriptor.configurable = true; if ('value' in descriptor) descriptor.writable = true; Object.defineProperty(target, descriptor.key, descriptor); } } return function (Constructor, protoProps, staticProps) { if (protoProps) defineProperties(Constructor.prototype, protoProps); if (staticProps) defineProperties(Constructor, staticProps); return Constructor; }; })();

function _toConsumableArray(arr) { if (Array.isArray(arr)) { for (var i = 0, arr2 = Array(arr.length); i < arr.length; i++) arr2[i] = arr[i]; return arr2; } else { return Array.from(arr); } }

function _classCallCheck(instance, Constructor) { if (!(instance instanceof Constructor)) { throw new TypeError('Cannot call a class as a function'); } }

var MudBlazorExtensionHelper = (function () {
    function MudBlazorExtensionHelper(options, dotNet, onDone) {
        _classCallCheck(this, MudBlazorExtensionHelper);

        this.dotnet = dotNet;
        this.onDone = onDone;
        this.options = options;
        this.mudDialogSelector = options.mudDialogSelector || '.mud-dialog';
        this.mudDialogHeaderSelector = options.mudDialogHeaderSelector || '.mud-dialog-title';
        this.dialog = document.querySelector(this.mudDialogSelector);
        this.dialogHeader = this.dialog.querySelector(this.mudDialogHeaderSelector);
    }

    _createClass(MudBlazorExtensionHelper, [{
        key: 'init',
        value: function init() {
            var _this = this;

            this.dialog = this.dialog || document.querySelector(this.mudDialogSelector);
            if (!this.dialog) {
                this.observer = new MutationObserver(function (e) {
                    var res = e.filter(function (x) {
                        return x.addedNodes && x.addedNodes[0] === document.querySelector(_this.mudDialogSelector);
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

            // For animations
            if (this.options.animation != null && this.options.animation !== 0) {
                this.animate(this.options.animationDescription);
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
        value: function animate(type) {
            var _dialog$classList,
                _this3 = this;

            var removedClsFromContainer = false,
                toClsNames = this.options.dialogPositionNames.map(function (n) {
                return 'mud-dialog-' + type + '-to-' + n;
            }),
                fromClsNames = this.options.dialogPositionNames.map(function (n) {
                return 'mud-dialog-' + type + '-from-' + n;
            }),
                containerClsName = 'mud-dialog-' + this.options.dialogPositionDescription;

            (_dialog$classList = this.dialog.classList).add.apply(_dialog$classList, _toConsumableArray(fromClsNames));
            if (this.dialog.parentNode.classList.contains(containerClsName)) {
                this.dialog.parentNode.classList.remove(containerClsName);
                removedClsFromContainer = true;
            }

            this.dialog.style['transition-duration'] = this.options.animationDurationInMs + 'ms';
            this.dialog.style['transition-timing-function'] = this.options.animationTimingFunctionString;
            setTimeout(function () {
                var _dialog$classList2, _dialog$classList3;

                _this3.dialog.classList.add('mud-dialog-animate-ex');
                (_dialog$classList2 = _this3.dialog.classList).remove.apply(_dialog$classList2, _toConsumableArray(fromClsNames));
                (_dialog$classList3 = _this3.dialog.classList).add.apply(_dialog$classList3, _toConsumableArray(toClsNames));
                setTimeout(function () {
                    var _dialog$classList4;

                    _this3.dialog.style['transition-duration'] = null;
                    _this3.dialog.style['transition-timing-function'] = null;
                    (_dialog$classList4 = _this3.dialog.classList).remove.apply(_dialog$classList4, _toConsumableArray(['mud-dialog-animate-ex'].concat(_this3.options.disablePositionMargin ? [] : toClsNames)));
                    if (removedClsFromContainer && !_this3.options.disablePositionMargin) {
                        _this3.dialog.parentNode.classList.add(containerClsName);
                    }
                }, _this3.options.animationDurationInMs);
            }, 50);
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

