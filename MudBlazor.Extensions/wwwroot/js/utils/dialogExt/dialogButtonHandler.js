class MudExDialogButtonHandler extends MudExDialogHandlerBase {

    handle(dialog) {
        super.handle(dialog);
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
                        }
                        if (b.id.indexOf('mud-button-minimize') >= 0) {
                            this.getHandler(MudExDialogPositionHandler).minimize();
                            
                        } else {
                            b.callBackReference.invokeMethodAsync(b.callbackName);
                        }
                    }
                }
            });
        }
    }
}


window.MudExDialogButtonHandler = MudExDialogButtonHandler;