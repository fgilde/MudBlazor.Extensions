class MudBlazorExtensionHelper {
    constructor(options, dotNet, onDone) {
        this.dialogFinder = new MudExDialogFinder(options);
        this.dialogHandler = new MudExDialogHandler(options, dotNet, onDone);
    }

    init() {
        const dialog = this.dialogFinder.findDialog();
        if (dialog) {
            this.dialogHandler.handle(dialog);
        } else {
            this.dialogFinder.observeDialog(dialog => this.dialogHandler.handle(dialog));
        }
    }
}



window.MudBlazorExtensionHelper = MudBlazorExtensionHelper;


window.MudBlazorExtensions = {
    helper: null,
    currentMouseArgs: null,

    __bindEvents: function () {
        var onMouseUpdate = function(e) {
            window.MudBlazorExtensions.currentMouseArgs = e;
        }
        document.addEventListener('mousemove', onMouseUpdate, false);
        document.addEventListener('mouseenter', onMouseUpdate, false);
    },
    
    getCurrentMousePosition: function() {
        return window.MudBlazorExtensions.currentMouseArgs;
    },

    setNextDialogOptions: function (options, dotNet) {
        new MudBlazorExtensionHelper(options, dotNet, () => {
            MudBlazorExtensions.helper = null;
            delete MudBlazorExtensions.helper;
        }).init();
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

    openWindowAndPostMessage: function(url, message) {
        var newWindow = window.open(url);
        newWindow.onload = function () {
            console.log("POST "+ message);
            newWindow.postMessage(message, url);
        };
    },

    downloadFile(options) {
        var fileUrl = options.url || "data:" + options.mimeType + ";base64," + options.base64String;
        fetch(fileUrl)
            .then(response => response.blob())
            .then(blob => {
                var link = window.document.createElement("a");
                //link.href = window.URL.createObjectURL(blob, { type: options.mimeType });
                link.href = window.URL.createObjectURL(blob);
                link.download = options.fileName;
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);
            });
    },

    attachDialog(dialogId) {
        if (dialogId) {
            let dialog = document.getElementById(dialogId);            
            if (dialog) {
                let titleCmp = dialog.querySelector('.mud-dialog-title');
                let iconCmp = null;
                if (titleCmp) {
                    const svgElements = titleCmp.querySelectorAll('svg');
                    const filteredSvgElements = Array.from(svgElements).filter(c => !c.parentElement.classList.contains('mud-ex-dialog-header-actions'));

                    if (filteredSvgElements.length > 0) {
                        iconCmp = filteredSvgElements[0];
                    }
                }

                const res = {
                    title: titleCmp ? titleCmp.innerText : 'Unnamed window',
                    icon: iconCmp ? iconCmp.innerHTML : ''
                }
                return res;
            }
        }
        return null;
    },

    getElement(selector) {
        return document.querySelector(selector);
    },

    showDialog(dialogId) {
        if (dialogId) { 
            let dialog = document.getElementById(dialogId);
            if (dialog) {
                dialog.style.visibility = 'visible';
                MudExDialogNoModalHandler.bringToFront(dialog, true);
            }
        }
    }


};

window.MudBlazorExtensions.__bindEvents();