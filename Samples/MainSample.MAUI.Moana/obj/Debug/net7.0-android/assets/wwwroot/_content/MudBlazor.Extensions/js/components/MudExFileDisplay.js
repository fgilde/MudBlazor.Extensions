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
        
    }

    
    dispose() {
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