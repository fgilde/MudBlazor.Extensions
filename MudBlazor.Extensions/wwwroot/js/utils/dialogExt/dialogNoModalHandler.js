class MudExDialogNoModalHandler extends MudExDialogHandlerBase {

    handle(dialog) {
        super.handle(dialog);
        if (this.options.modal === false) {
            window.NOMODAL = this;
            this.appOrBody = this.dialogContainerReference.parentElement;

            this.changeCls();

            this.awaitAnimation(() => {
                this.dialog.style.animation = null;
                this.dialog.style['animation-duration'] = '0s';
                MudExDomHelper.toAbsolute(this.dialog);
                this.appOrBody.insertBefore(this.dialog, this.appOrBody.firstChild);
                this.dialogContainerReference.style.display = 'none';
            });


            this.dialog.onmousedown = (e) => {
                this.bringToFront();
            };

            const handleMutations = (mutationsList) => {
                for (const mutation of mutationsList) {
                    if (mutation.type === 'childList') {
                        for (const removedNode of mutation.removedNodes) {
                            if (removedNode === this.dialogContainerReference) {
                                console.log('close ' + this.dialogTitle);
                                this.dialog.remove();
                                /*observer.disconnect();*/
                                //MudExDialogNoModalHandler.observers.forEach(o => o.disconnect());
                                //setTimeout(() => { MudExDialogNoModalHandler.observers.forEach(o => o.observe(this.appOrBody, { childList: true })); }, 500);
                            }
                        }
                    }
                }
            };

            const observer = new MutationObserver(handleMutations);
            //MudExDialogNoModalHandler.observers = MudExDialogNoModalHandler.observers || [];
            //MudExDialogNoModalHandler.observers.push(observer);
            observer.observe(this.appOrBody, { childList: true });
        }
    }

    bringToFront(targetDlg) {
        var allDialogs = this.getAllNonModalDialogs();
        targetDlg = targetDlg || this.dialog;
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

    getAllDialogReferences() {
        return Array.from(document.querySelectorAll('.mud-dialog-container')).filter(c => c.getAttribute('data-modal') === 'false');
        //var targetDlgReference = allDialogReferences.filter(d => d.getAttribute('data-dialog-id') === targetDlg.id)[0];
    }

    getAllNonModalDialogs() {
        return Array.from(document.querySelectorAll('.mudex-dialog-no-modal'));
    }

    changeCls() {
        this.dialog.classList.add('mudex-dialog-no-modal');
        this.dialogContainerReference.classList.add('mudex-dialog-no-modal');
        this.dialogContainerReference.setAttribute('data-modal', false);
        this.dialogContainerReference.setAttribute('data-dialog-id', this.dialog.id);
        /*this.dialogOverlay.style.display = 'none';*/
        this.dialogOverlay.remove();
    }

}

window.MudExDialogNoModalHandler = MudExDialogNoModalHandler;