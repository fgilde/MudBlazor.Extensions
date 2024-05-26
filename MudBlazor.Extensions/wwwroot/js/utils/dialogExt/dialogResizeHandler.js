class MudExDialogResizeHandler extends MudExDialogHandlerBase {
    
    handle(dialog) {
        this.resizeTimeout = null;
        super.handle(dialog);
        this.dialog = dialog;
        this.resizeObserver = new ResizeObserver(entries => {
            for (let entry of entries) {
                this.raiseDialogEvent('OnResizing');
                this.debounceResizeCompleted();
            }
        });
        this.awaitAnimation(() => this.checkResizeable());
    }

    debounceResizeCompleted() {
        if (this.resizeTimeout) {
            clearTimeout(this.resizeTimeout);
        }
        this.resizeTimeout = setTimeout(() => {
            this.raiseDialogEvent('OnResized');
        }, 500); // debounce
    }

    checkResizeable() {
        MudExDomHelper.toAbsolute(this.dialog);
        if (this.options.resizeable) {
            this.resizeObserver.observe(this.dialog);

            this.dialog.style['resize'] = 'both';
            this.dialog.style['overflow'] = 'auto';

            this.dialog.style.maxWidth = this.dialog.style.maxWidth || window.innerWidth + 'px';
            this.dialog.style.maxHeight = this.dialog.style.maxHeight || window.innerHeight + 'px';
            this.dialog.style.minWidth = this.dialog.style.minWidth || '100px';
            this.dialog.style.minHeight = this.dialog.style.minHeight || '100px';
        }
    }

    dispose() {
        if (this.resizeObserver) {
            this.resizeObserver.unobserve(this.dialog);
        }
        if (this.resizeTimeout) {
            clearTimeout(this.resizeTimeout);
        }
    }
}

window.MudExDialogResizeHandler = MudExDialogResizeHandler;
