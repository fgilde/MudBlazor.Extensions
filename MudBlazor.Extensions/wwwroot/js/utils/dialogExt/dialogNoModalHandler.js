class MudExDialogNoModalHandler extends MudExDialogHandlerBase {

    static handled = [];

    handle(dialog) {
        super.handle(dialog);

        if (!this.options.modal) {
            this._updateHandledDialogs(dialog);
            this._modifyDialogAppearance();
            this.dialog.onmousedown = this._handleDialogMouseDown.bind(this);
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
            MudExDomHelper.toAbsolute(this.dialog, false);
            Object.assign(this.dialogContainerReference.style, {
                'pointer-events': 'none'
            });
            Object.assign(this.dialog.style, {
                'pointer-events': 'all'
            });
        });
    }

    _handleDialogMouseDown(e) {
        if (!this.dialogHeader || !Array.from(this.dialogHeader.querySelectorAll('button')).some(b => MudExEventHelper.isWithin(e, b))) {
            MudExDialogNoModalHandler.bringToFront(this.dialogContainerReference);
        }
    }

    
    changeCls() {
        this.dialog.classList.add('mudex-dialog-no-modal');
        this.dialogContainerReference.classList.add('mudex-dialog-ref-no-modal');
        this.dialogContainerReference.setAttribute('data-modal', false);
        this.dialogContainerReference.setAttribute('data-dialog-id', this.dialog.id);
        this.dialogOverlay.remove();
    }

    static closeNonModal(dialogIdStr) {
        return;
        const dialog = document.querySelector(`#${dialogIdStr}`);
        this.reInitOtherDialogs(dialogIdStr);
    }

    static reInitOtherDialogs(exceptId) {
        const dialogsToReInit = Array.from(document.querySelectorAll('.mud-ex-dialog-initial'))
            .filter(d => d.id !== exceptId && d.getAttribute('data-mud-extended') !== 'true');

        dialogsToReInit.forEach(this.reInitDialog.bind(this));
    }

    static reInitDialog(d) {
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

    static bringToFront(dialogContainterRef) {
        if (!dialogContainterRef) return;
        if (dialogContainterRef.getAttribute('role') === 'dialog') {
            dialogContainterRef = dialogContainterRef.parentNode;
        }
        const all = MudExDialogNoModalHandler.getAllDialogReferences();
        let maxZ = 1400;
        for (const d of all) {
            const z = parseInt(window.getComputedStyle(d).zIndex) || 999;
            if (z > maxZ) {
                maxZ = z;
            }
        }
        dialogContainterRef.style.zIndex = (maxZ >= 1400 ? maxZ + 1 : 1400).toString();
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
