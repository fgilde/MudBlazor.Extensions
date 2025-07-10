class MudExDialogNoModalHandler extends MudExDialogHandlerBase {

    static handled = [];

    handle(dialog) {
        super.handle(dialog);

        if (!this.options.modal) {
            this._updateHandledDialogs(dialog);

            this.appOrBody = this.dialogContainerReference.parentElement;
            this._modifyDialogAppearance();

            this.dialog.onmousedown = this._handleDialogMouseDown.bind(this);

            this.observer = new MutationObserver(this._checkMutationsForRemove.bind(this));
            this.observer.observe(this.appOrBody, { childList: true });
        }
    }

    _updateHandledDialogs(dialog) {
        const index = MudExDialogNoModalHandler.handled.findIndex(h => h.id === dialog.id);
        if (index !== -1) {
            MudExDialogNoModalHandler.handled.splice(index, 1);
        }

        MudExDialogNoModalHandler.handled.push({
            id: this.dialog.id,
            dialog: this.dialog,
            options: this.options,
            dotNet: this.dotNet
        });
    }

    _modifyDialogAppearance() {
        this.changeCls();
        this.awaitAnimation(() => {
            this.dialog.style['animation-duration'] = '0s';
            MudExDomHelper.toAbsolute(this.dialog, !this.options.customSize);
            this.appOrBody.insertBefore(this.dialog, this.appOrBody.firstChild);
            Object.assign(this.dialogContainerReference.style, {
                display: 'none',
                height: '2px',
                width: '2px'
            });
        });
    }

    _handleDialogMouseDown(e) {
        if (!this.dialogHeader || !Array.from(this.dialogHeader.querySelectorAll('button')).some(b => MudExEventHelper.isWithin(e, b))) {
            MudExDialogNoModalHandler.bringToFront(this.dialog);
        }
    }

    reInitOtherDialogs() {
        const dialogsToReInit = Array.from(document.querySelectorAll('.mud-ex-dialog-initial'))
            .filter(d => d.getAttribute('data-mud-extended') !== 'true');

        dialogsToReInit.forEach(this.reInitDialog.bind(this));
    }

    reInitDialog(d) {
        const dialogInfo = MudExDialogNoModalHandler.handled.find(h => h.id === d.id);
        if (dialogInfo) {
            const currentStyle = d.style;
            const savedPosition = {
                top: currentStyle.top,
                left: currentStyle.left,
                width: currentStyle.width,
                height: currentStyle.height,
                position: currentStyle.position
            };
            d.style.display = 'none';

            const handleInfo = { ...dialogInfo, options: { ...dialogInfo.options, animations: null } };
            const index = MudExDialogNoModalHandler.handled.indexOf(dialogInfo);
            MudExDialogNoModalHandler.handled.splice(index, 1);

            const handler = new MudExDialogHandler(handleInfo.options, handleInfo.dotNet, handleInfo.dotNetService, handleInfo.onDone);
            handler.handle(d);

            d.style.display = 'block';
            Object.assign(d.style, savedPosition);
        }
    }

    _checkMutationsForRemove(mutationsList) {
        for (const mutation of mutationsList) {
            if (mutation.type === 'childList') {
                for (const removedNode of mutation.removedNodes) {
                    if (removedNode === this.dialogContainerReference) {
                        this.observer.disconnect();

                        const index = MudExDialogNoModalHandler.handled.findIndex(h => h.id === this.dialog.id);
                        if (index !== -1) {
                            MudExDialogNoModalHandler.handled.splice(index, 1);
                        }
                        // this.dotNetService.invokeMethodAsync('HandleNonModalClose', this.dialog.id, this.dotNet);
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
        this.dialogContainerReference.classList.add('mudex-dialog-ref-no-modal');
        this.dialogContainerReference.setAttribute('data-modal', false);
        this.dialogContainerReference.setAttribute('data-dialog-id', this.dialog.id);
        this.dialogOverlay.remove();
    }

    static bringToFront(targetDlg, animate) {
        const allDialogs = this.getAllNonModalDialogs();
        if (targetDlg) {
            const app = targetDlg.parentElement;            
            //const targetRef = MudExDialogNoModalHandler.getDialogReference(targetDlg);
            const lastDialog = allDialogs[allDialogs.length - 1];
            if (lastDialog && targetDlg && targetDlg !== lastDialog) {
                //const lastDialogRef = MudExDialogNoModalHandler.getDialogReference(lastDialog);
                app.insertBefore(targetDlg, lastDialog.nextSibling);                
            }
        }
    }

    static getDialogReference(dialog) {
        return MudExDialogNoModalHandler.getAllDialogReferences().filter(r => r && r.getAttribute('data-dialog-id') === dialog.id)[0] || dialog.parentElement;
    }

    static getAllDialogReferences() {
        return Array.from(document.querySelectorAll('.mud-dialog-container')).filter(c => c.getAttribute('data-modal') === 'false');
    }

    static getAllNonModalDialogs() {
        return Array.from(document.querySelectorAll('.mudex-dialog-no-modal'));
    }
}

window.MudExDialogNoModalHandler = MudExDialogNoModalHandler;
