class MudBlazorExtensionHelper {
    mudDialogSelector;
    mudDialogHeaderSelector;
    onDone;
    observer;
    options;
    dialog;
    dotnet;

    constructor(options, dotNet, onDone) {
        this.dotnet = dotNet;
        this.onDone = onDone;
        this.options = options;
        this.mudDialogSelector = options.mudDialogSelector || '.mud-dialog:not([data-mud-extended=true])';
        this.mudDialogHeaderSelector = options.mudDialogHeaderSelector || '.mud-dialog-title';
        this.dialog = document.querySelector(this.mudDialogSelector);
        this.dialogHeader = this.dialog.querySelector(this.mudDialogHeaderSelector);
    }

    findDialog() {
        return Array.from(document.querySelectorAll(this.mudDialogSelector)).filter(d => !d.__extended)[0];
    }

    init() {
        this.dialog = this.dialog || this.findDialog();
        if (!this.dialog) {
            this.observer = new MutationObserver((e) => {
                var res = e.filter(x => x.addedNodes && x.addedNodes[0] === this.findDialog());
                if (res.length) {
                    this.dialog = res[0].addedNodes[0];
                    this.observer.disconnect();
                    this.handleExtensions();
                }
            });
            this.observer.observe(document, { characterData: true, childList: true, subtree: true });
        } else {
            this.handleExtensions();
        }
    }

    handleExtensions() {
        this.dialog.__extended = true;
        this.dialog.setAttribute('data-mud-extended', true);
        setTimeout(() => this.dialog.classList.remove('mud-ex-dialog-initial'), 50);
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
            this.options.buttons.forEach(b => {
                if (this.dialogHeader) {
                    this.dialogHeader.insertAdjacentHTML('beforeend', b.html);
                    var btnEl = this.dialogHeader.querySelector('#' + b.id);
                    btnEl.onclick = () => {
                        if (b.id.indexOf('mud-button-maximize') >= 0) {
                            this.maximize();
                        } else {
                            b.callBackReference.invokeMethodAsync(b.callbackName);
                        }
                    }
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

    checkResizeable() {
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

    maximize() {
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

    done() {
        if (this.observer)
            this.observer.disconnect();
        if (this.onDone)
            this.onDone();
    }

    dragElement(dialogEl, headerEl, container) {
        var pos1 = 0, pos2 = 0, pos3 = 0, pos4 = 0;
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
            }

            var aY = (dialogEl.offsetTop - pos2);
            var aX = (dialogEl.offsetLeft - pos1);
            dialogEl.style.position = 'absolute';
            if ((aX > 0) && (aX < bounds.x)) {
                dialogEl.style.left = (aX) + "px";
            }
            if ((aY > 0) && (aY < bounds.y)) {
                dialogEl.style.top = (aY) + "px";
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

    animate(types) {
        //var names = types.map(type => this.options.dialogPositionNames.map(n => `kf-mud-dialog-${type}-${n} ${this.options.animationDurationInMs}ms ${this.options.animationTimingFunctionString} 1 alternate`));
        //this.dialog.style.animation = `${names.join(',')}`;
        this.dialog.style.animation = `${this.options.animationStyle}`;
    }
}

window.MudBlazorExtensions = {
    helper: null,
    disposeMudExFileDisplay: function(id) {
        if (window.__mudExFileDisplay && window.__mudExFileDisplay[id]) {
            window.__mudExFileDisplay[id] = null;
            delete window.__mudExFileDisplay[id];
        }
    },
    initMudExFileDisplay: function (dotNet, id) {
        window.__mudExFileDisplay = window.__mudExFileDisplay || {};
        window.__mudExFileDisplay[id] = {
            callBackReference: dotNet
        }
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
    }
};
