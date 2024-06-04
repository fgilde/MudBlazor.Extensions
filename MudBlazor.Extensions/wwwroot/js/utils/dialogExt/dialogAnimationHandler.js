class MudExDialogAnimationHandler extends MudExDialogHandlerBase {
   
    handle(dialog) {
        super.handle(dialog);
        if (this.options.animations != null && Array.isArray(this.options.animations) && this.options.animations.length) {
            this.animate();
        }        
        if (this.options.animateClose) {
            this.extendCloseEvents();
        }
        
    }

    extendCloseEvents() {
        var closeButton = this.dialog.querySelector('.mud-button-close');
        //this.dialogOverlay 
        if (this.dialogOverlay && this.options.modal && !this.options.disableBackdropClick) {
            const handleClick = (e) => {
                this.closeAnimation();
                this.dialogOverlay.removeEventListener('click', handleClick);
                MudExEventHelper.stopFor(e, this.dialogOverlay, this.options.animationDurationInMs);
            };

            this.dialogOverlay.addEventListener('click', handleClick);
        }
        if (closeButton) {
            
            const handleClick = (e) => {                
                this.closeAnimation();
                closeButton.removeEventListener('click', handleClick);
                MudExEventHelper.stopFor(e, closeButton, this.options.animationDurationInMs);
            };

            closeButton.addEventListener('click', handleClick);
        }
    }

    animate() {
       this.dialog.style.animation = `${this.options.animationStyle}`;
    }

    closeAnimation() {
        MudExDialogAnimationHandler.playCloseAnimation(this.dialog);
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