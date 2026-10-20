class MudExOrgChart {
    constructor(viewport, dotnet) {
        this.viewport = viewport;
        this.dotnet = dotnet;
        this._onWheel = this.onWheel.bind(this);
        viewport.addEventListener('wheel', this._onWheel, { passive: false });
    }

    // the wheel pans, with Ctrl (or a pinch on a touchpad) it zooms around the pointer
    onWheel(evt) {
        evt.preventDefault();
        if (this._pending) return;
        const r = this.viewport.getBoundingClientRect();
        this._pending = true;
        this.dotnet.invokeMethodAsync('OnWheel', evt.clientX - r.left, evt.clientY - r.top, evt.deltaX, evt.deltaY, evt.ctrlKey || evt.metaKey)
            .finally(() => this._pending = false);
    }

    // the viewport, the unscaled stage and every card in unscaled stage coordinates
    measure(stage) {
        const v = this.viewport.getBoundingClientRect();
        const s = stage.getBoundingClientRect();
        const scale = stage.offsetWidth ? s.width / stage.offsetWidth : 1;
        const boxes = [...stage.querySelectorAll('[data-org-key]')].map(n => {
            const r = n.getBoundingClientRect();
            return { key: +n.dataset.orgKey, x: (r.left - s.left) / scale, y: (r.top - s.top) / scale, width: r.width / scale, height: r.height / scale };
        });
        return { viewportWidth: v.width, viewportHeight: v.height, stageWidth: stage.offsetWidth, stageHeight: stage.offsetHeight, boxes };
    }

    dispose() {
        this.viewport.removeEventListener('wheel', this._onWheel, { passive: false });
    }
}

window.MudExOrgChart = MudExOrgChart;

export function initializeMudExOrgChart(viewport, dotnet) {
    return new MudExOrgChart(viewport, dotnet);
}
