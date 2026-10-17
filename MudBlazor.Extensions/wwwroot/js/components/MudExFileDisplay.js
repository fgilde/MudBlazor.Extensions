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
        // a listener instead of an onerror attribute, which a Content-Security-Policy without 'unsafe-inline' blocks
        this._onError = (event) => this.handleContentError(event);
        this.elementRef?.addEventListener('error', this._onError, true);
    }

    handleContentError(event) {
        const target = event.target;
        if (!target || target.getAttribute?.('data-id') !== this.id || !['OBJECT', 'IFRAME', 'IMG'].includes(target.tagName))
            return;
        const showError = () => {
            const element = this.elementRef?.querySelector('#content-type-display-error') || document.getElementById('content-type-display-error');
            element?.classList.add('visible');
        };
        setTimeout(() => this.dotnet.invokeMethodAsync('HandleContentError').then(isHandled => { if (!isHandled) showError(); }, showError), 0);
    }

    
    dispose() {
        this.elementRef?.removeEventListener('error', this._onError, true);
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