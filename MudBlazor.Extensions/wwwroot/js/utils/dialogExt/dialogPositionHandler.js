class MudExDialogPositionHandler extends MudExDialogHandlerBase {

    handle(dialog) {
        super.handle(dialog);

        if (this.options.showAtCursor) {
            this.moveElementToMousePosition(dialog);
        }

        if (this.options.disablePositionMargin) {
            this.dialog.classList.add('mud-dialog-position-fixed');
            this.dialog.classList.add('mud-ex-dialog-no-margin');
        }

        if (this.options.fullHeight) {
            var cls = this.options.disableSizeMarginY ? 'mud-dialog-height-full-no-margin' : 'mud-dialog-height-full';
            this.dialog.classList.add(cls);
            var actions = this.dialog.querySelector('.mud-dialog-actions');
            if (actions) {
                actions.classList.add('mud-dialog-actions-fixed-bottom');
            }
        }
        if (this.options.fullWidth && this.options.disableSizeMarginX) {
            this.dialog.classList.remove('mud-dialog-width-full');
            this.dialog.classList.add('mud-dialog-width-full-no-margin');
            if (this.dialog.classList.contains('mud-dialog-width-false')) {
                this.dialog.classList.remove('mud-dialog-width-false');
            }
        }
    }
    

    minimize() {
        let targetElement = document.querySelector(`.mud-ex-task-bar-item-for-${this.dialog.id}`);
        this.moveToElement(targetElement); 
    }

    moveToElement(targetElement) {
        var rect = targetElement.getBoundingClientRect();
        this.dialog.style.height = targetElement.style.height;
        this.dialog.style.width = targetElement.style.width;
        this.dialog.style.left = rect.left + "px";
        this.dialog.style.top = rect.top + "px";
    }

    maximize() {
        if (this._oldStyle) {
            this.dialog.style = this._oldStyle;
            delete this._oldStyle;
        } else {
            this._oldStyle = this.dialog.style;
            this.dialog.style.position = 'absolute';
            this.dialog.style.left = "0";
            this.dialog.style.top = "0";

            this.dialog.style.maxWidth = this.dialog.style.width = window.innerWidth + 'px';
            this.dialog.style.maxHeight = this.dialog.style.height = window.innerHeight + 'px';
        }
        this.getHandler(MudExDialogResizeHandler).checkResizeable();
    }
    
    moveElementToMousePosition(element) {
        var e = MudBlazorExtensions.getCurrentMousePosition();
        var x = e.clientX;
        var y = e.clientY;
        var origin = this.options.cursorPositionOriginName.split('-');

        var maxWidthFalseOrLargest = this.options.maxWidth === 6 || this.options.maxWidth === 4; // 4=xxl 6=false
        if (!this.options.fullWidth || !maxWidthFalseOrLargest) {
            if (origin[1] === 'left') {
                element.style.left = x + 'px';
            } else if (origin[1] === 'right') {
                element.style.left = (x - element.offsetWidth) + 'px';
            } else if (origin[1] === 'center') {
                element.style.left = (x - element.offsetWidth / 2) + 'px';
            }
        }
        if (!this.options.fullHeight) {
            if (origin[0] === 'top') {
                element.style.top = y + 'px';
            } else if (origin[0] === 'bottom') {
                element.style.top = (y - element.offsetHeight) + 'px';
            } else if (origin[0] === 'center') {
                element.style.top = (y - element.offsetHeight / 2) + 'px';
            }
        }
        MudExDomHelper.ensureElementIsInScreenBounds(element);
    }
}


window.MudExDialogPositionHandler = MudExDialogPositionHandler;