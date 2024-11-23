class MudExDialogHandler extends MudExDialogHandlerBase {
    handle(dialog) {
        setTimeout(() => {
            super.handle(dialog);
            setTimeout(() => {
                dialog.classList.remove('mud-ex-dialog-initial');
            }, 50);
            dialog.__extended = true;
            dialog.setAttribute('data-mud-extended', true);
            dialog.classList.add('mud-ex-dialog');
            this.handleAll(dialog);           
            this.awaitAnimation(() => {
                window.MudBlazorExtensions.focusAllAutoFocusElements();
            });
            if (this.onDone) this.onDone();
        }, 50);
    }
}

window.MudExDialogHandler = MudExDialogHandler;