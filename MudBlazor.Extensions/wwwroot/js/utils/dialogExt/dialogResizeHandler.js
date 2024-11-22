class MudExDialogResizeHandler extends MudExDialogHandlerBase {

    handle(dialog) {
        this.resizeTimeout = null;
        this.resizedSometimes = false;
        super.handle(dialog);
        this.dialog = dialog;
        this.dialog.addEventListener('mousedown', this.onMouseDown.bind(this));
        this.dialog.addEventListener('mouseup', this.onMouseUp.bind(this));
        

        this.resizeObserver = new ResizeObserver(entries => {
            for (let entry of entries) {
                if (entry.target === this.dialog) {
                    if (!this.isInternalHandler()) {
                        if (!this.resizedSometimes && this.mouseDown) {
                            this.resizedSometimes = true;
                            this.setBounds();
                        }
                        this.raiseDialogEvent('OnResizing');
                        this.debounceResizeCompleted();
                    }
                }
            }
        });
        this.awaitAnimation(() => this.checkResizeable());
    }

    onMouseUp() {
        this.mouseDown = false;
    }

    onMouseDown() {
        this.mouseDown = true;
    }

    debounceResizeCompleted() {
        if (this.resizeTimeout) {
            clearTimeout(this.resizeTimeout);
        }
        this.resizeTimeout = setTimeout(() => {
            this.setRelativeIf();
            this.raiseDialogEvent('OnResized');
        }, 500); // debounce
    }

    checkResizeable() {
        MudExDomHelper.toAbsolute(this.dialog, false);
        this.setRelativeIf();
        if (this.options.resizeable) {
            this.resizeObserver.observe(this.dialog);
            this.dialog.style['resize'] = 'both';
            this.dialog.style['overflow'] = 'auto';
        }
    }

    setBounds() {
        if (!this.options.keepMaxSizeConstraints) {
            if (!this.dialog.style.maxWidth)
                this.dialog.style.maxWidth = this.dialog.style.maxWidth || window.innerWidth + 'px';
            if (!this.dialog.style.maxHeight)
                this.dialog.style.maxHeight = this.dialog.style.maxHeight || window.innerHeight + 'px';
        }
        if (!this.dialog.style.minWidth)
            this.dialog.style.minWidth = this.dialog.style.minWidth || '150px';
        if (!this.dialog.style.minHeight)
            this.dialog.style.minHeight = this.dialog.style.minHeight || '150px';
    }

    dispose() {
        if (this.resizeObserver) {
            this.resizeObserver.unobserve(this.dialog);
        }
        if (this.resizeTimeout) {
            clearTimeout(this.resizeTimeout);
        }
        this.dialog.removeEventListener('mousedown', this.onMouseDown);
        this.dialog.removeEventListener('mouseup', this.onMouseUp);
    }
}

window.MudExDialogResizeHandler = MudExDialogResizeHandler;
