class MudExRibbon {
    constructor(elementRef, dotnet) {
        this.elementRef = elementRef;
        this.dotnet = dotnet;
    }

    dispose() {
        this.elementRef = null;
        this.dotnet = null;
    }
}

export function initializeMudExRibbon(elementRef, dotnet) {
    return new MudExRibbon(elementRef, dotnet);
}
