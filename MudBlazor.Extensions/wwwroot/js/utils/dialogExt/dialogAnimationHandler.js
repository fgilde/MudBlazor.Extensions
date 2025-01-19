class MudExDialogAnimationHandler extends MudExDialogHandlerBase {
   
    handle(dialog) {
        super.handle(dialog);
        if (this.options.animations != null && Array.isArray(this.options.animations) && this.options.animations.length) {
            this.animate();
            this.awaitAnimation(() => this.raiseDialogEvent('OnAnimated'));
        } else {
            this.raiseDialogEvent('OnAnimated');
        }        

        this.extendCloseEvents();
        
    }

    async checkCanClose() {
        const callbackName = this.options.canCloseCallbackName;
        const reference = this.options.canCloseCallbackReference || this.dotNet;
        if (callbackName && reference) {
            try {
                const result = await reference.invokeMethodAsync(callbackName);
                if (result === false) {
                    return false;
                }
            } catch (e) {
                console.error(e);
            }
        }
        const closeEvent = await this.raiseDialogEvent('OnDialogClosing');
        if (closeEvent?.cancel === true) {
            return false;
        }
        return true;
    }


    extendCloseEvents() {
        const handleCloseEvent = (element) => {
            const handleClick = async (e) => {
                if (!e.__internalDispatched) {
                    e.__internalDispatched = true;
                    e.preventDefault();
                    e.stopPropagation();
                    const canClose = await this.checkCanClose();
                    if (canClose) {
                        setTimeout(() => {
                            element.dispatchEvent(e);
                        }, 1);
                    }
                    return;
                }
                
                if (this.options.animateClose) {
                    this.closeAnimation();
                    element.removeEventListener('click', handleClick);
                    MudExEventHelper.stopFor(e, element, this.options.animationDurationInMs);
                }
            };
            element.addEventListener('click', handleClick);
        };
        
        const closeButton = this.dialog.querySelector('.mud-button-close');
        if (closeButton) {
            handleCloseEvent(closeButton);
        }

        if (this.dialogOverlay && this.options.modal && this.options.backdropClick !== false) {
            handleCloseEvent(this.dialogOverlay);
        }
    }


    animate() {
       this.dialog.style.animation = `${this.options.animationStyle}`;
    }

    closeAnimation() {
        return MudExDialogAnimationHandler.playCloseAnimation(this.dialog);
    }

    static playCloseAnimation(dialog) {
        if (!dialog) {
            return Promise.resolve();
        }        
        var delay = dialog.options?.animationDurationInMs || 500;        
        dialog.style['animation-duration'] = `${delay}ms`;
        return new Promise((resolve) => {
            MudExDialogAnimationHandler._playCloseAnimation(dialog);
            setTimeout(() => {
                dialog.classList.add('mud-ex-dialog-initial');
                resolve();
            }, delay);
        });
    }

    static _playCloseAnimation(dialog) {        
        const n = dialog.style.animationName;
        dialog.style.animationName = '';
        dialog.style.animationDirection = 'reverse';
        dialog.style['animation-play-state'] = 'paused';
        requestAnimationFrame(() => {
            dialog.style.animationName = n;
            dialog.style['animation-play-state'] = 'running';
        });
    }
    

}


window.MudExDialogAnimationHandler = MudExDialogAnimationHandler;