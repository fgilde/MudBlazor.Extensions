class MudExDialogFinder {
    constructor(options) {
        this.options = options;
        this.mudDialogSelector = options.mudDialogSelector || '.mud-dialog:not([data-mud-extended=true])';
    }

    findDialog() {
        return Array.from(document.querySelectorAll(this.mudDialogSelector)).find(d => !d.__extended);
    }

    observeDialog(callback) {
        const observer = new MutationObserver((mutations) => {
            const dialog = this.findDialog();
            if (dialog) {
                const addedDialogMutation = mutations.find(mutation => mutation.addedNodes[0] === dialog);

                if (addedDialogMutation) {
                    observer.disconnect();
                    callback(dialog);
                }
            }
        });

        observer.observe(document, { characterData: true, childList: true, subtree: true });
    }
}

window.MudExDialogFinder = MudExDialogFinder;