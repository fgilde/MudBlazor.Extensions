class MudExFileDisplayCode {
    constructor(area, dotnet) {
        this.area = area;
        this.dotnet = dotnet;
        this.notified = false;
        this.onInput = () => this.input();
        area.addEventListener('input', this.onInput);
    }

    input() {
        if (this.notified || !this.dotnet) {
            return;
        }
        this.notified = true;
        this.dotnet.invokeMethodAsync('OnCodeInput').catch(() => this.notified = false);
    }

    readValue() {
        this.notified = false;
        return DotNet.createJSStreamReference(new Blob([this.area?.value ?? '']));
    }

    dispose() {
        this.area?.removeEventListener('input', this.onInput);
        this.area = null;
        this.dotnet = null;
    }
}

export function initializeMudExFileDisplayCode(area, dotnet) {
    return new MudExFileDisplayCode(area, dotnet);
}
