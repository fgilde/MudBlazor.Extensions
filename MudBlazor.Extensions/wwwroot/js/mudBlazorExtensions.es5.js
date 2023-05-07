'use strict';

var _createClass = (function () { function defineProperties(target, props) { for (var i = 0; i < props.length; i++) { var descriptor = props[i]; descriptor.enumerable = descriptor.enumerable || false; descriptor.configurable = true; if ('value' in descriptor) descriptor.writable = true; Object.defineProperty(target, descriptor.key, descriptor); } } return function (Constructor, protoProps, staticProps) { if (protoProps) defineProperties(Constructor.prototype, protoProps); if (staticProps) defineProperties(Constructor, staticProps); return Constructor; }; })();

function _classCallCheck(instance, Constructor) { if (!(instance instanceof Constructor)) { throw new TypeError('Cannot call a class as a function'); } }

var MudBlazorExtensionHelper = (function () {
    function MudBlazorExtensionHelper(options, dotNet, onDone) {
        _classCallCheck(this, MudBlazorExtensionHelper);

        this.dialogFinder = new MudExDialogFinder(options);
        this.dialogHandler = new MudExDialogHandler(options, dotNet, onDone);
    }

    _createClass(MudBlazorExtensionHelper, [{
        key: 'init',
        value: function init() {
            var _this = this;

            var dialog = this.dialogFinder.findDialog();
            if (dialog) {
                this.dialogHandler.handle(dialog);
            } else {
                this.dialogFinder.observeDialog(function (dialog) {
                    return _this.dialogHandler.handle(dialog);
                });
            }
        }
    }]);

    return MudBlazorExtensionHelper;
})();

window.MudBlazorExtensionHelper = MudBlazorExtensionHelper;

window.MudBlazorExtensions = {
    helper: null,
    currentMouseArgs: null,

    __bindEvents: function __bindEvents() {
        var onMouseUpdate = function onMouseUpdate(e) {
            window.MudBlazorExtensions.currentMouseArgs = e;
        };
        document.addEventListener('mousemove', onMouseUpdate, false);
        document.addEventListener('mouseenter', onMouseUpdate, false);
    },

    getCurrentMousePosition: function getCurrentMousePosition() {
        return window.MudBlazorExtensions.currentMouseArgs;
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
    },

    attachDialog: function attachDialog(dialogId) {
        if (dialogId) {
            var dialog = document.getElementById(dialogId);
            if (dialog) {
                var titleCmp = dialog.querySelector('.mud-dialog-title');
                var iconCmp = null;
                if (titleCmp) {
                    var svgElements = titleCmp.querySelectorAll('svg');
                    var filteredSvgElements = Array.from(svgElements).filter(function (c) {
                        return !c.parentElement.classList.contains('mud-ex-dialog-header-actions');
                    });

                    if (filteredSvgElements.length > 0) {
                        iconCmp = filteredSvgElements[0];
                    }
                }

                var res = {
                    title: titleCmp ? titleCmp.innerText : 'Unnamed window',
                    icon: iconCmp ? iconCmp.innerHTML : ''
                };
                return res;
            }
        }
        return null;
    }

};

window.MudBlazorExtensions.__bindEvents();

