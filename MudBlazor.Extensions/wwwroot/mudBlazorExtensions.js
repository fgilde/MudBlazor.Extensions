class MudBlazorExtensionHelper {
    #mudDialogSelector;
    #mudDialogHeaderSelector;
    #onDone;
    #observer;
    #options;
    #dialog;
    #dotnet;

    constructor(options, dotNet, onDone) {
        this.dotnet = dotNet;
        this.onDone = onDone;
        this.options = options;
        this.mudDialogSelector = options.mudDialogSelector || '.mud-dialog';
        this.mudDialogHeaderSelector = options.mudDialogHeaderSelector || '.mud-dialog-title';
        this.dialog = document.querySelector(this.mudDialogSelector);
        this.dialogHeader = this.dialog.querySelector(this.mudDialogHeaderSelector);
    }

    handleExtensions() {
        // Inject buttons
        this.options.buttons.forEach(b => {
            this.dialogHeader.insertAdjacentHTML('beforeend', b.html);
            var btnEl = this.dialogHeader.querySelector('#' + b.id);
            btnEl.onclick = () => {
                if (b.id.indexOf('mud-button-maximize') >= 0) {
                    this.maximize();
                } else {
                    b.callBackReference.invokeMethodAsync(b.callbackName);
                }
            }
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

    maximize() {
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

    done() {
        if (this.observer)
            this.observer.disconnect();
        if (this.onDone)
            this.onDone();
    }

    init() {
        this.dialog = this.dialog || document.querySelector(this.mudDialogSelector);
        if (!this.dialog) {
            this.observer = new MutationObserver((e) => {
                var res = e.filter(x => x.addedNodes && x.addedNodes[0] === document.querySelector(this.mudDialogSelector));
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
}

window.MudBlazorExtensions = {
    helper: null,
    setNextDialogOptions: function (options, dotNet) {
        MudBlazorExtensions.helper = new MudBlazorExtensionHelper(options, dotNet, () => {
            MudBlazorExtensions.helper = null;
            delete MudBlazorExtensions.helper;
        });
        MudBlazorExtensions.helper.init();
    }
};