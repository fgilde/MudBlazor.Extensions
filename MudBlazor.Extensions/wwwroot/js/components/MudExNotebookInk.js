const SVG_NS = 'http://www.w3.org/2000/svg';
const DRAW_TOOLS = ['pen', 'highlighter', 'eraser', 'lasso', 'shape'];
const ERASER_PX = 6;
const MIN_STEP_PX = 0.25;
const ERASED = 'mud-ex-notebook-erased';
const COLOR = /^#[0-9a-f]{6}$/i;
const NUMBER = /-?\d*\.?\d+(?:e[-+]?\d+)?/gi;

const pointSegment = (p, a, b) => {
    const dx = b.x - a.x;
    const dy = b.y - a.y;
    const length = dx * dx + dy * dy;
    const t = length === 0 ? 0 : Math.max(0, Math.min(1, ((p.x - a.x) * dx + (p.y - a.y) * dy) / length));
    return Math.hypot(p.x - a.x - t * dx, p.y - a.y - t * dy);
};

const cross = (o, p, q) => (p.x - o.x) * (q.y - o.y) - (p.y - o.y) * (q.x - o.x);

const intersects = (a, b, c, d) => {
    const [d1, d2, d3, d4] = [cross(c, d, a), cross(c, d, b), cross(a, b, c), cross(a, b, d)];
    return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));
};

const segmentDistance = (a, b, c, d) => intersects(a, b, c, d) ? 0
    : Math.min(pointSegment(a, c, d), pointSegment(b, c, d), pointSegment(c, a, b), pointSegment(d, a, b));

const pathOf = points => points.length ? `M${points.map(p => `${p.x} ${p.y}`).join('L')}${points.length === 1 ? `L${points[0].x} ${points[0].y}` : ''}` : '';

function shapeOf(kind, a, b) {
    if (!a || !b) {
        return '';
    }
    const [l, t, r, btm] = [Math.min(a.x, b.x), Math.min(a.y, b.y), Math.max(a.x, b.x), Math.max(a.y, b.y)];
    const [cx, cy, rx, ry] = [(l + r) / 2, (t + btm) / 2, (r - l) / 2, (btm - t) / 2];
    switch (kind) {
        case 'line':
        case 'arrow':
            return `M${a.x} ${a.y}L${b.x} ${b.y}`;
        case 'ellipse':
            return `M${l} ${cy}A${rx} ${ry} 0 1 0 ${r} ${cy}A${rx} ${ry} 0 1 0 ${l} ${cy}Z`;
        case 'triangle':
            return `M${cx} ${t}L${r} ${btm}L${l} ${btm}Z`;
        case 'diamond':
            return `M${cx} ${t}L${r} ${cy}L${cx} ${btm}L${l} ${cy}Z`;
        default:
            return `M${l} ${t}H${r}V${btm}H${l}Z`;
    }
}

function strokeOf(svg) {
    const el = svg.closest('[data-element-id]');
    const path = svg.querySelector('path');
    const numbers = (path?.getAttribute('d') || '').match(NUMBER)?.map(Number) ?? [];
    const x = parseFloat(el.style.left) || 0;
    const y = parseFloat(el.style.top) || 0;
    const points = [];
    for (let i = 0; i + 1 < numbers.length; i += 2) {
        points.push({ x: x + numbers[i], y: y + numbers[i + 1] });
    }
    const xs = points.map(p => p.x);
    const ys = points.map(p => p.y);
    const half = (+path?.getAttribute('stroke-width') || 0) / 2;
    return { el, id: el.dataset.elementId, points, half, box: { l: Math.min(...xs), t: Math.min(...ys), r: Math.max(...xs), b: Math.max(...ys) } };
}

export const inkMethods = {
    drawTool(tool) {
        return DRAW_TOOLS.includes(tool);
    },

    touchDown(e) {
        if (e.pointerType !== 'touch' || this.readOnly() || !this.drawTool(this.root.dataset.tool)) {
            return false;
        }
        this.touches.set(e.pointerId, { x: e.clientX, y: e.clientY });
        if (this.touches.size < 2) {
            return false;
        }
        if (this.drag) {
            this.cancelGesture();
        }
        this.pan = { ...this.touchCenter(), scroller: this.scroller() };
        e.preventDefault();
        return true;
    },

    touchMove(e) {
        if (!this.touches.has(e.pointerId)) {
            return;
        }
        this.touches.set(e.pointerId, { x: e.clientX, y: e.clientY });
        if (!this.pan) {
            return;
        }
        const center = this.touchCenter();
        this.pan.scroller.scrollBy(this.pan.x - center.x, this.pan.y - center.y);
        Object.assign(this.pan, center);
    },

    touchUp(e) {
        this.touches.delete(e.pointerId);
        if (this.touches.size < 2) {
            this.pan = null;
        }
    },

    touchCenter() {
        const points = [...this.touches.values()];
        return { x: points.reduce((s, p) => s + p.x, 0) / points.length, y: points.reduce((s, p) => s + p.y, 0) / points.length };
    },

    scroller() {
        for (let el = this.root.parentElement; el; el = el.parentElement) {
            const style = getComputedStyle(el);
            if (/(auto|scroll)/.test(style.overflow + style.overflowX + style.overflowY)) {
                return el;
            }
        }
        return document.scrollingElement;
    },

    inkDown(e, tool, start) {
        const page = this.page();
        if (tool === 'lasso') {
            const el = e.target.closest('[data-element-id]');
            const id = el && page.contains(el) ? el.dataset.elementId : null;
            if (id && this.selection().includes(id)) {
                this.focus(e);
                this.begin(e, { kind: 'move', ids: this.selection(), start });
                return;
            }
        }
        if (!DRAW_TOOLS.includes(tool)) {
            return;
        }
        this.focus(e);
        const kind = { eraser: 'erase', lasso: 'lasso', shape: 'shape' }[tool] ?? 'stroke';
        const d = { kind, ink: true, tool, shape: this.root.dataset.shape, points: [], frame: 0 };
        if (d.kind === 'erase') {
            d.strokes = [...page.querySelectorAll('.mud-ex-notebook-ink')].map(strokeOf).filter(s => s.points.length);
            d.hits = new Set();
        } else {
            d.svg = this.liveLayer(d);
        }
        this.begin(e, d);
        this.inkPoint(this.drag, e, start);
        this.renderInk(this.drag);
    },

    liveLayer(d) {
        const host = this.root.querySelector('.mud-ex-notebook-canvas-overlay');
        if (!host) {
            return null;
        }
        const zoom = this.zoom();
        const page = this.page().getBoundingClientRect();
        const origin = host.getBoundingClientRect();
        const svg = document.createElementNS(SVG_NS, 'svg');
        svg.setAttribute('class', d.tool === 'highlighter' ? 'mud-ex-notebook-live mud-ex-notebook-highlighter' : 'mud-ex-notebook-live');
        svg.style.left = `${page.left - origin.left}px`;
        svg.style.top = `${page.top - origin.top}px`;
        const g = document.createElementNS(SVG_NS, 'g');
        g.setAttribute('transform', `scale(${zoom})`);
        const path = document.createElementNS(SVG_NS, 'path');
        path.setAttribute('fill', 'none');
        path.setAttribute('stroke-linecap', 'round');
        path.setAttribute('stroke-linejoin', 'round');
        if (d.kind === 'lasso' || d.kind === 'shape') {
            path.setAttribute('class', 'mud-ex-notebook-live-lasso');
            path.setAttribute('vector-effect', 'non-scaling-stroke');
        } else {
            const color = this.root.dataset.inkColor;
            path.setAttribute('stroke', COLOR.test(color) ? color : '#000000');
            path.setAttribute('stroke-width', `${+this.root.dataset.inkWidth || 2}`);
        }
        g.appendChild(path);
        svg.appendChild(g);
        host.appendChild(svg);
        d.path = path;
        return svg;
    },

    inkMove(d, e) {
        const events = e.getCoalescedEvents?.() ?? [];
        (events.length ? events : [e]).forEach(ev => this.inkPoint(d, ev, this.toPage(ev.clientX, ev.clientY)));
        if (!d.frame) {
            d.frame = requestAnimationFrame(() => {
                d.frame = 0;
                this.renderInk(d);
            });
        }
    },

    inkPoint(d, e, p) {
        const last = d.points[d.points.length - 1];
        if (last && Math.hypot(p.x - last.x, p.y - last.y) < MIN_STEP_PX) {
            return;
        }
        d.points.push({ x: p.x, y: p.y, pressure: e.pointerType === 'pen' && e.pressure > 0 ? e.pressure : 0.5 });
        if (d.kind === 'erase') {
            this.erase(d, last ?? p, p);
        }
    },

    erase(d, a, b) {
        d.strokes.forEach(s => {
            if (d.hits.has(s)) {
                return;
            }
            const reach = ERASER_PX + s.half;
            if (Math.max(a.x, b.x) < s.box.l - reach || Math.min(a.x, b.x) > s.box.r + reach
                || Math.max(a.y, b.y) < s.box.t - reach || Math.min(a.y, b.y) > s.box.b + reach) {
                return;
            }
            for (let i = 0; i < s.points.length; i++) {
                if (segmentDistance(a, b, s.points[i], s.points[Math.min(i + 1, s.points.length - 1)]) <= reach) {
                    d.hits.add(s);
                    s.el.classList.add(ERASED);
                    return;
                }
            }
        });
    },

    renderInk(d) {
        if (!d.path) {
            return;
        }
        d.path.setAttribute('d', d.kind === 'shape' ? shapeOf(d.shape, d.points[0], d.points[d.points.length - 1]) : pathOf(d.points) + (d.kind === 'lasso' && d.points.length > 2 ? 'Z' : ''));
    },

    async inkUp(d, cancel) {
        cancelAnimationFrame(d.frame);
        try {
            if (cancel || !this.dotnet) {
                return;
            }
            if (d.kind === 'stroke' && d.points.length) {
                this.renderInk(d);
                await this.dotnet.invokeMethodAsync('OnStrokeAsync', d.points).catch(() => {});
            } else if (d.kind === 'erase' && d.hits.size) {
                await this.dotnet.invokeMethodAsync('OnEraseAsync', [...d.hits].map(s => s.id)).catch(() => {});
            } else if (d.kind === 'shape' && d.points.length) {
                const [a, b] = [d.points[0], d.points[d.points.length - 1]];
                await this.dotnet.invokeMethodAsync('OnShapeAsync', a.x, a.y, b.x, b.y).catch(() => {});
            } else if (d.kind === 'lasso') {
                const polygon = d.points.length > 2 ? d.points.map(p => ({ x: p.x, y: p.y })) : [];
                await this.dotnet.invokeMethodAsync('OnLassoAsync', polygon).catch(() => {});
            }
        } finally {
            d.svg?.remove();
            d.hits?.forEach(s => s.el.classList.remove(ERASED));
        }
    },

    cancelGesture() {
        const d = this.drag;
        if (!d) {
            return;
        }
        this.end(d);
        if (d.ink) {
            this.inkUp(d, true);
        } else {
            this.restore(d, true);
        }
    }
};
