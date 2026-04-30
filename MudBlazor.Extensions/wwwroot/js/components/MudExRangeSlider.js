
class MudExRangeSlider {
    constructor(rootEl, trackEl, dotnetRef) {
        this.rootEl = rootEl;
        this.trackEl = trackEl;
        this.dotnet = dotnetRef;
        this.zoomEnabled = false;
        this._onMove = this._onMove.bind(this);
        this._onUp = this._onUp.bind(this);
        this._onWheel = this._onWheel.bind(this);
        // Always attached so toggling EnableMouseWheelZoom at runtime works without re-init.
        this.trackEl.addEventListener('wheel', this._onWheel, { passive: false });
    }
    init() { }
    setMouseWheelZoom(enabled) { this.zoomEnabled = !!enabled; }
    measureTrack() {
        const r = this.trackEl.getBoundingClientRect();
        return { left: r.left, top: r.top, width: r.width, height: r.height };
    }
    startCapture() {
        window.addEventListener('pointermove', this._onMove, true);
        window.addEventListener('pointerup', this._onUp, true);
    }
    _onMove(evt) {
        if (this._movePending) return;
        this._movePending = true;
        this.dotnet.invokeMethodAsync('OnPointerMove', evt.clientX, evt.clientY)
            .then(() => { this._movePending = false; })
            .catch(() => { this._movePending = false; });
    }
    _onUp() {
        window.removeEventListener('pointermove', this._onMove, true);
        window.removeEventListener('pointerup', this._onUp, true);
        this.dotnet.invokeMethodAsync('OnPointerUp');
    }
    _onWheel(evt) {
        if (!this.zoomEnabled) return;
        evt.preventDefault();
        if (this._wheelPending) return;
        this._wheelPending = true;
        this.dotnet.invokeMethodAsync('OnWheel', evt.clientX, evt.clientY, evt.deltaY)
            .then(() => { this._wheelPending = false; })
            .catch(() => { this._wheelPending = false; });
    }
    dispose() {
        try { this.trackEl.removeEventListener('wheel', this._onWheel, { passive: false }); } catch { }
        try { window.removeEventListener('pointermove', this._onMove, true); } catch { }
        try { window.removeEventListener('pointerup', this._onUp, true); } catch { }
    }
}
window.MudExRangeSlider = MudExRangeSlider;
export function initializeMudExRangeSlider(rootEl, trackEl, dotnetRef) {
    return new MudExRangeSlider(rootEl, trackEl, dotnetRef);
}
