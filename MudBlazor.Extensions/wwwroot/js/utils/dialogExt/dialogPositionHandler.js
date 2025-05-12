class MudExDialogPositionHandler extends MudExDialogHandlerBase {

    handle(dialog) {
        super.handle(dialog);        
        if (this.options.showAtCursor) {
            this.moveElementToMousePosition(dialog);
        } else if (this.options.customPosition) {
            this.dialog.style.position = 'absolute';
            this.dialog.style.left = this.options.customPosition.left.cssValue;
            this.dialog.style.top = this.options.customPosition.top.cssValue;
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
        this.moveToElement(this.dialog, targetElement, () => {
            this.dialog.style.visibility = 'hidden';            
        }); 
    }


    moveToElement(sourceElement, targetElement, callback) {
        
        // Get the bounding client rectangles of the target element and the dialog
        var targetRect = targetElement.getBoundingClientRect();
        var dialogRect = sourceElement.getBoundingClientRect();

        // Calculate the scaling factors for width and height
        var scaleX = targetRect.width / dialogRect.width;
        var scaleY = targetRect.height / dialogRect.height;

        // Calculate the translation distances for X and Y
        var translateX = targetRect.left - dialogRect.left;
        var translateY = targetRect.top - dialogRect.top;

        var lastDuration = sourceElement.style['animation-duration'];
        sourceElement.style['animation-duration'] = '.3s';
        // Apply the transformation using the calculated scaling factors and translation distances
        sourceElement.style.transform = `translate(${translateX}px, ${translateY}px) scale(${scaleX}, ${scaleY})`;
        sourceElement.style.transition = 'transform 0.3s ease-in-out';

        // Remove the transition after the animation is done
        setTimeout(() => {
            sourceElement.style.removeProperty('transform');
            sourceElement.style.removeProperty('transition');
            sourceElement.style['animation-duration'] = lastDuration;
            if (callback) callback();
        }, 300);

    }

    restore() {
        this.dialog.style.visibility = 'visible';
    }


    maximize() {
        var handler = this.getHandler(MudExDialogDragHandler);
        handler.toggleSnap(MudExDialogDragHandler.Direction.TOP);
    }
    
    moveElementToMousePosition(element) {
        var e = MudBlazorExtensions.getCurrentMousePosition();
        var x = e.clientX;
        var y = e.clientY;
        var origin = this.options.cursorPositionOriginName.split('-');

        var maxWidthFalseOrLargest = this.options.maxWidth === 6 || this.options.maxWidth === 4; // 4=xxl 6=false
        setTimeout(() => {
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
        }, 50);

    }
}


window.MudExDialogPositionHandler = MudExDialogPositionHandler;