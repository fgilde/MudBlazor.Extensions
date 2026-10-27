class MudExTurntable {
    constructor(element, dotnet) {
        this.element = element;
        this.dotnet = dotnet;
        // the events also fire while Blazor applies a render batch (e.g. mode attribute changed), .NET can not be called from there
        this._onMode = e => setTimeout(() => this.dotnet.invokeMethodAsync('OnModeChanged', e.detail.mode));
        this._onDeck = e => setTimeout(() => this.dotnet.invokeMethodAsync('OnDeckChanged', e.detail));
        element.addEventListener('modechange', this._onMode);
        element.addEventListener('deckchange', this._onDeck);
    }

    // Set only after the element is upgraded, a property set before would shadow the records setter of the web component
    setRecords(records) {
        this.element.records = records;
    }

    play(record, track, deck) {
        this.element.play?.(record, track, deck);
    }

    pause(deck) {
        this.element.pause?.(deck);
    }

    next(deck) {
        this.element.next?.(deck);
    }

    previous(deck) {
        this.element.previous?.(deck);
    }

    dispose() {
        this.element.removeEventListener('modechange', this._onMode);
        this.element.removeEventListener('deckchange', this._onDeck);
    }
}

window.MudExTurntable = MudExTurntable;

export async function initializeMudExTurntable(element, dotnet, scriptUrl) {
    await import(scriptUrl);
    await customElements.whenDefined('audiola-turntable');
    return new MudExTurntable(element, dotnet);
}
