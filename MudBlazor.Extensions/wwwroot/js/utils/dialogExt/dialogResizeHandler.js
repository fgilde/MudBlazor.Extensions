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


            if (!this.dialog.style.maxWidth || this.dialog.style.maxWidth === 'none') {
                this.dialog.style.maxWidth = window.innerWidth + 'px';
            }

            if (!this.dialog.style.maxHeight || this.dialog.style.maxHeight === 'none') {
                this.dialog.style.maxHeight = window.innerHeight + 'px';
            }

            if (!this.dialog.style.minWidth || this.dialog.style.minWidth === 'none') {
                this.dialog.style.minWidth = '100px';
            }
            
            if (!this.dialog.style.minHeight || this.dialog.style.minHeight === 'none') {
                this.dialog.style.minHeight = '100px';
            }
            
        }
    }
}

window.MudExDialogResizeHandler = MudExDialogResizeHandler;