class MudExDialogNoModalHandler extends MudExDialogHandlerBase {

    handle(dialog) {
        super.handle(dialog);
        if (this.options.modal === false) {
            window.NOMODAL = this;
            MudExDialogNoModalHandler.handled = MudExDialogNoModalHandler.handled || [];
            var index = MudExDialogNoModalHandler.handled.findIndex(h => h.id === dialog.id);
            if (index !== -1) {
                MudExDialogNoModalHandler.handled.splice(index, 1);
            }
            MudExDialogNoModalHandler.handled.push({
                id: this.dialog.id,
                dialog: this.dialog,
                options: this.options,
                dotNet: this.dotNet
            });
            
            this.appOrBody = this.dialogContainerReference.parentElement;

            this.changeCls();

            this.awaitAnimation(() => {
                this.dialog.style['animation-duration'] = '0s';
                MudExDomHelper.toAbsolute(this.dialog);
                this.appOrBody.insertBefore(this.dialog, this.appOrBody.firstChild);
                this.dialogContainerReference.style.display = 'none';
                this.dialogContainerReference.style.height = '2px;';
                this.dialogContainerReference.style.width = '2px;';
            });


            this.dialog.onmousedown = (e) => {
                MudExDialogNoModalHandler.bringToFront(this.dialog);
            };


            this.observer = new MutationObserver(this.checkMutationsForRemove);
            this.observer.observe(this.appOrBody, { childList: true });
        }
    }

    reInitOtherDialogs() {
        var needReInit = Array.from(document.querySelectorAll('.mud-ex-dialog-initial')).filter(d => d.getAttribute('data-mud-extended') !== 'true');
        needReInit.forEach(d => {
            var index = MudExDialogNoModalHandler.handled.findIndex(h => h.id === d.id);
            if (index !== -1) {
                var handleInfo = MudExDialogNoModalHandler.handled[index];
                MudExDialogNoModalHandler.handled.splice(index, 1);
                var handler = new MudExDialogHandler(handleInfo.options, handleInfo.dotNet, handleInfo.onDone);
                d.style['animation-duration'] = '0s';
                handler.handle(d);
            }
        });
    }


    checkMutationsForRemove = (mutationsList) => {
        for (const mutation of mutationsList) {
            if (mutation.type === 'childList') {
                for (const removedNode of mutation.removedNodes) {
                    if (removedNode === this.dialogContainerReference) {
                        this.observer.disconnect();
                        delete this.observer;

                        var index = MudExDialogNoModalHandler.handled.findIndex(h => h.id === this.dialog.id);
                        if (index !== -1) {
                            MudExDialogNoModalHandler.handled.splice(index, 1);
                        }
                        
                        this.dialog.remove();
                        this.reInitOtherDialogs();
                        break;
                    }
                }
            }
        }
    }
    
    changeCls() {
        this.dialog.classList.add('mudex-dialog-no-modal');
        this.dialogContainerReference.classList.add('mudex-dialog-no-modal');
        this.dialogContainerReference.setAttribute('data-modal', false);
        this.dialogContainerReference.setAttribute('data-dialog-id', this.dialog.id);
        /*this.dialogOverlay.style.display = 'none';*/
        this.dialogOverlay.remove();
    }

    static bringToFront(targetDlg, animate) {
        var allDialogs = this.getAllNonModalDialogs();
        if (targetDlg) {
            // Find the parent element of the target dialog
            var parentElement = targetDlg.parentElement; // Should be this.appOrBody;

            // Find the last dialog element
            var lastDialog = allDialogs[allDialogs.length - 1];

            // If the target dialog is not already the last dialog, move it behind the last dialog
            if (targetDlg !== lastDialog) {
                parentElement.insertBefore(targetDlg, lastDialog.nextSibling);
            }
        }
    }

    static getAllDialogReferences() {
        return Array.from(document.querySelectorAll('.mud-dialog-container')).filter(c => c.getAttribute('data-modal') === 'false');
        //var targetDlgReference = allDialogReferences.filter(d => d.getAttribute('data-dialog-id') === targetDlg.id)[0];
    }

    static getAllNonModalDialogs() {
        return Array.from(document.querySelectorAll('.mudex-dialog-no-modal'));
    }

}

window.MudExDialogNoModalHandler = MudExDialogNoModalHandler;