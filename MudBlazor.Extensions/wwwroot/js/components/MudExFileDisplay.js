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

    
    dispose() {
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