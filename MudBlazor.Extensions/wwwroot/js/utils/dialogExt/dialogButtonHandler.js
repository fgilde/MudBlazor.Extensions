class MudExDialogButtonHandler extends MudExDialogHandlerBase {

    handle(dialog) {
        super.handle(dialog);
        if (this.options.maximizeButton && this.dialogHeader) {
            this.dialogHeader.addEventListener('dblclick', this.onDoubleClick.bind(this));
        }
        if (this.options.buttons && this.options.buttons.length) {
            var dialogButtonWrapper = document.createElement('div');
            dialogButtonWrapper.classList.add('mud-ex-dialog-header-actions');
            if (!this.options.closeButton) {
                dialogButtonWrapper.style.right = '8px'; // No close button, so we need to move the buttons to the right (48 - button width of 40)
            }

            if (this.dialogHeader) {
                dialogButtonWrapper = this.dialogHeader.insertAdjacentElement('beforeend', dialogButtonWrapper);
            }
            this.options.buttons.reverse().forEach(b => {
                if (dialogButtonWrapper) {
                    dialogButtonWrapper.insertAdjacentHTML('beforeend', b.html);
                    var btnEl = dialogButtonWrapper.querySelector('#' + b.id);
                    btnEl.onclick = () => {
                        if (b.id.indexOf('mud-button-maximize') >= 0) {
                            this.getHandler(MudExDialogPositionHandler).maximize();
                            return;
                        }
                        if (b.id.indexOf('mud-button-minimize') >= 0) {
                            this.getHandler(MudExDialogPositionHandler).minimize();
                            return;

                        } else if (b.callBackReference && b.callbackName) {
                            b.callBackReference.invokeMethodAsync(b.callbackName);
                        }
                    }
                }
            });
        }
    }

    onDoubleClick(e) {
        if (this.dialogHeader && MudExEventHelper.isWithin(e, this.dialogHeader)) {
            this.getHandler(MudExDialogPositionHandler).maximize();
        }
    }

    dispose() {
        if (this.dialogHeader) {
            this.dialogHeader.removeEventListener('dblclick', this.onDoubleClick);
        }
    }
}


window.MudExDialogButtonHandler = MudExDialogButtonHandler;