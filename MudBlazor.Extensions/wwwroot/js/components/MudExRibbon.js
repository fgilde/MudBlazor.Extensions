class MudExRibbon {
    constructor(elementRef, dotnet, panelId) {
        this.elementRef = elementRef;
        this.dotnet = dotnet;
        this.panelId = panelId;
        this.step = 0;
        this.max = 0;
        this.widths = [];
        this.frame = 0;
        this.busy = false;
        this.observed = null;
        this.header = null;
        this.backstageId = null;
        this.observer = new ResizeObserver(() => {
            this.placeBackstage(this.backstageId);
            this.schedule();
        });
        document.fonts?.ready.then(() => this.schedule());
    }

    update(step, max, reset) {
        if (reset) {
            this.widths = [];
        }
        this.step = step;
        this.max = max;
        this.schedule();
    }

    schedule() {
        if (this.frame || !this.dotnet) {
            return;
        }
        this.frame = requestAnimationFrame(() => {
            this.frame = 0;
            this.check();
        });
    }

    body() {
        const body = document.getElementById(this.panelId);
        if (body !== this.observed) {
            if (this.observed) {
                this.observer.unobserve(this.observed);
            }
            this.observed = body;
            if (body) {
                this.observer.observe(body);
            }
        }
        return body;
    }

    check() {
        const body = this.body();
        if (!body || this.busy || !this.dotnet) {
            return;
        }
        const width = body.clientWidth;
        const overflows = body.scrollWidth > width;
        let next = this.step;
        if (overflows && this.step < this.max) {
            this.widths[this.step] = body.scrollWidth;
            next++;
        } else if (!overflows && this.step > 0 && width >= (this.widths[this.step - 1] ?? 0) + 8) {
            next--;
        }
        if (next === this.step) {
            return;
        }
        this.step = next;
        this.busy = true;
        this.dotnet.invokeMethodAsync('SetReductionStep', next)
            .catch(() => { })
            .finally(() => {
                this.busy = false;
                this.schedule();
            });
    }

    placeBackstage(id) {
        this.backstageId = id;
        const backstage = id ? document.getElementById(id) : null;
        const header = backstage?.previousElementSibling;
        if (!header) {
            return;
        }
        backstage.style.top = `${header.offsetTop + header.offsetHeight}px`;
        if (header !== this.header) {
            if (this.header) {
                this.observer.unobserve(this.header);
            }
            this.header = header;
            this.observer.observe(header);
        }
    }

    dispose() {
        cancelAnimationFrame(this.frame);
        this.observer.disconnect();
        this.elementRef = null;
        this.dotnet = null;
        this.observed = null;
        this.header = null;
    }
}

export function initializeMudExRibbon(elementRef, dotnet, panelId) {
    return new MudExRibbon(elementRef, dotnet, panelId);
}
