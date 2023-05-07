class MudExDialogAnimationHandler extends MudExDialogHandlerBase {
   
    handle(dialog) {
        super.handle(dialog);
        if (this.options.animations != null && Array.isArray(this.options.animations) && this.options.animations.length) {
            this.animate(this.options.animationDescriptions);
        }
    }

    animate(types) {
        //var names = types.map(type => this.options.dialogPositionNames.map(n => `kf-mud-dialog-${type}-${n} ${this.options.animationDurationInMs}ms ${this.options.animationTimingFunctionString} 1 alternate`));
        //this.dialog.style.animation = `${names.join(',')}`;
        this.dialog.style.animation = `${this.options.animationStyle}`;
    }
}


window.MudExDialogAnimationHandler = MudExDialogAnimationHandler;