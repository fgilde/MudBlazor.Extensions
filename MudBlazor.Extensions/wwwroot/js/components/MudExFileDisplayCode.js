class MudExFileDisplayCode {
    constructor(area, dotnet) {
        this.area = area;
        this.dotnet = dotnet;
        this.notified = false;
        this.onInput = () => this.input();
        this.onKeyDown = e => this.keyDown(e);
        area.addEventListener('input', this.onInput);
        area.addEventListener('keydown', this.onKeyDown);
    }

    input() {
        if (!this.dotnet) {
            return;
        }
        clearTimeout(this.timer);
        this.timer = setTimeout(() => this.dotnet?.invokeMethodAsync('OnCodeIdle').catch(() => {}), 400);
        if (this.notified) {
            return;
        }
        this.notified = true;
        this.dotnet.invokeMethodAsync('OnCodeInput').catch(() => this.notified = false);
    }

    keyDown(e) {
        if ((e.ctrlKey || e.metaKey) && !e.altKey && e.key.toLowerCase() === 's') {
            e.preventDefault();
            this.dotnet?.invokeMethodAsync('OnSaveKey').catch(() => {});
        }
    }

    readValue() {
        this.notified = false;
        return DotNet.createJSStreamReference(new Blob([this.area?.value ?? '']));
    }

    dispose() {
        clearTimeout(this.timer);
        this.area?.removeEventListener('input', this.onInput);
        this.area?.removeEventListener('keydown', this.onKeyDown);
        this.area = null;
        this.dotnet = null;
    }
}

export function initializeMudExFileDisplayCode(area, dotnet) {
    return new MudExFileDisplayCode(area, dotnet);
}
