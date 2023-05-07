class MudExDialogResizeHandler extends MudExDialogHandlerBase {

    handle(dialog) {
        super.handle(dialog);
        this.awaitAnimation(() => this.checkResizeable());
    }

    checkResizeable() {
        MudExDomHelper.toAbsolute(this.dialog);
        if (this.options.resizeable) {
            this.dialog.style['resize'] = 'both';
            this.dialog.style['overflow'] = 'auto';
            this.dialog.style.maxWidth = window.innerWidth + 'px';
            this.dialog.style.maxHeight = window.innerHeight + 'px';
            this.dialog.style.minWidth = '100px';
            this.dialog.style.minHeight = '100px';
        }
    }
}

window.MudExDialogResizeHandler = MudExDialogResizeHandler;