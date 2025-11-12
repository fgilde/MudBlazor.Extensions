
class MudExRangeSlider {
    constructor(rootEl, trackEl, dotnetRef) {
        this.rootEl = rootEl;
        this.trackEl = trackEl;
        this.dotnet = dotnetRef;
        this._onMove = this._onMove.bind(this);
        this._onUp = this._onUp.bind(this);
    }
    init() { }
    measureTrack() {
        const r = this.trackEl.getBoundingClientRect();
        return { left: r.left, top: r.top, width: r.width, height: r.height };
    }
    startCapture() {
        window.addEventListener('pointermove', this._onMove, true);
        window.addEventListener('pointerup', this._onUp, true);
    }
    _onMove(evt) {
        this.dotnet.invokeMethodAsync('OnPointerMove', evt.clientX, evt.clientY);
    }
    _onUp() {
        window.removeEventListener('pointermove', this._onMove, true);
        window.removeEventListener('pointerup', this._onUp, true);
        this.dotnet.invokeMethodAsync('OnPointerUp');
    }
}
window.MudExRangeSlider = MudExRangeSlider;
export function initializeMudExRangeSlider(rootEl, trackEl, dotnetRef) {
    return new MudExRangeSlider(rootEl, trackEl, dotnetRef);
}
