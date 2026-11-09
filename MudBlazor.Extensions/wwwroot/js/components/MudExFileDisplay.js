class MudExFileDisplay {
    elementRef;
    dotnet;
    id;
    
    constructor(elementRef, dotNet, id) {
        this.elementRef = elementRef;
        this.dotnet = dotNet;
        this.id = id;
        window.__mudExFileDisplay = window.__mudExFileDisplay || {};
        window.__mudExFileDisplay[id] = {
            callBackReference: dotNet
        }

        this._onError = (event) => this.handleContentError(event);
        this._errorTarget = this.elementRef instanceof Element ? this.elementRef : document;
        this._errorTarget.addEventListener('error', this._onError, true);
    }

    handleContentError(event) {
        const target = event.target;
        if (!target || target.getAttribute?.('data-id') !== this.id || !['OBJECT', 'IFRAME', 'IMG'].includes(target.tagName))
            return;
        const showError = () => {
            const element = this._errorTarget.querySelector('#content-type-display-error') || document.getElementById('content-type-display-error');
            element?.classList.add('visible');
        };
        setTimeout(() => this.dotnet.invokeMethodAsync('HandleContentError').then(isHandled => { if (!isHandled) showError(); }, showError), 0);
    }

    setDirty(isDirty) {
        if (isDirty && !this._onBeforeUnload) {
            this._onBeforeUnload = (event) => { event.preventDefault(); event.returnValue = ''; };
            this._onKeyDown = (event) => {
                if ((event.ctrlKey || event.metaKey) && !event.altKey && event.key.toLowerCase() === 's') {
                    event.preventDefault();
                    this.dotnet.invokeMethodAsync('SaveFromShortcut');
                }
            };
            window.addEventListener('beforeunload', this._onBeforeUnload);
            this._errorTarget.addEventListener('keydown', this._onKeyDown);
        } else if (!isDirty) {
            this.removeDirtyHandlers();
        }
    }

    removeDirtyHandlers() {
        if (!this._onBeforeUnload)
            return;
        window.removeEventListener('beforeunload', this._onBeforeUnload);
        this._errorTarget.removeEventListener('keydown', this._onKeyDown);
        this._onBeforeUnload = null;
        this._onKeyDown = null;
    }

    dispose() {
        this.removeDirtyHandlers();
        this._errorTarget.removeEventListener('error', this._onError, true);
        if (window.__mudExFileDisplay && window.__mudExFileDisplay[this.id]) {
            window.__mudExFileDisplay[this.id] = null;
            delete window.__mudExFileDisplay[this.id];
        }
    }
}

window.MudExFileDisplay = MudExFileDisplay;

export function initializeMudExFileDisplay(elementRef, dotnet, id) {
    return new MudExFileDisplay(elementRef, dotnet, id);
}