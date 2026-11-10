const FRAME = '.mud-ex-slide-canvas-frame';
const SHAPE = '[data-shape-id]';
const SNAP_PX = 6;
const CLICK_PX = 3;
const MIN_PX = 4;
const KEYS = ['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight'];
const TEXT_EDITOR = '.mud-ex-slide-text-editor';
const FORMAT_KEYS = 'biulerj';
const AREA = ['marquee', 'insert'];
const ZOOM_IDLE_MS = 400;

const rad = deg => deg * Math.PI / 180;
const pct = (value, whole) => `${value / whole * 100}%`;

class MudExSlideCanvas {
    constructor(root, dotnet) {
        this.root = root;
        this.dotnet = dotnet;
        this.drag = null;
        this.onMove = e => this.pointerMove(e);
        this.onUp = e => this.pointerUp(e);
        this.events = [['pointerdown', e => this.pointerDown(e)], ['keydown', e => this.keyDown(e)], ['dblclick', e => this.dblClick(e)], ['wheel', e => this.wheel(e)]];
        this.events.forEach(([type, handler]) => root.addEventListener(type, handler));
    }

    frame() {
        return this.root?.querySelector(FRAME);
    }

    selection() {
        return (this.root.dataset.selection || '').split(',').filter(Boolean).map(Number);
    }

    metrics(frame) {
        const rect = frame.getBoundingClientRect();
        const width = +frame.dataset.width;
        return { rect, width, height: +frame.dataset.height, scale: rect.width / width };
    }

    toSlide(m, x, y) {
        return { x: (x - m.rect.left) / m.scale, y: (y - m.rect.top) / m.scale };
    }

    parents(frame) {
        const map = new Map();
        frame.querySelectorAll(SHAPE).forEach(el => map.set(+el.dataset.shapeId, el.dataset.groupId ? +el.dataset.groupId : null));
        return map;
    }

    chain(parents, id) {
        const ids = [];
        for (let current = id; current != null; current = parents.get(current)) {
            ids.push(current);
        }
        return ids;
    }

    boxes(frame, ids) {
        return ids.map(id => frame.querySelector(`.mud-ex-slide-selection[data-id="${id}"]`)).filter(Boolean).map(el => ({
            id: +el.dataset.id, el, x: +el.dataset.x, y: +el.dataset.y, w: +el.dataset.w, h: +el.dataset.h, r: +el.dataset.r,
            fh: el.dataset.fh === 'true', fv: el.dataset.fv === 'true', opaque: el.dataset.opaque === 'true'
        }));
    }

    keyDown(e) {
        if (e.target !== this.root || this.root.dataset.readonly === 'true' || e.altKey) {
            return;
        }
        const ctrl = e.ctrlKey || e.metaKey;
        const selected = !!this.root.dataset.selection;
        const key = e.key.toLowerCase();
        if ((KEYS.includes(e.key) && selected) || (e.key === 'Tab' && !ctrl && this.frame()?.querySelector(SHAPE)) || (ctrl && (key === 'a' || key === 'd'))
            || (selected && ((ctrl && key.length === 1 && FORMAT_KEYS.includes(key)) || (!ctrl && e.key === ' ')))) {
            e.preventDefault();
        }
    }

    pointerDown(e) {
        const frame = this.frame();
        if (e.button !== 0 || this.drag || !frame || !this.dotnet || this.root.dataset.readonly === 'true' || this.onScrollbar(e)) {
            return;
        }
        if (e.target.closest(TEXT_EDITOR)) {
            return;
        }
        e.preventDefault();
        this.root.focus({ preventScroll: true });
        if (this.root.dataset.insert) {
            const m = this.metrics(frame);
            this.begin(e, frame, { kind: 'insert', m, start: this.toSlide(m, e.clientX, e.clientY) });
            return;
        }
        const toggle = e.shiftKey || e.ctrlKey || e.metaKey;
        const handle = e.target.closest('[data-handle]');
        const m = this.metrics(frame);
        const start = this.toSlide(m, e.clientX, e.clientY);
        if (handle) {
            const kind = handle.dataset.handle === 'rotate' ? 'rotate' : 'resize';
            this.begin(e, frame, { kind, handle: handle.dataset.handle, grabbed: +handle.closest('[data-id]').dataset.id, ids: this.selection(), m, start });
            return;
        }
        const shape = e.target.closest(SHAPE);
        if (!shape || !frame.contains(shape)) {
            this.begin(e, frame, { kind: 'marquee', toggle, m, start });
            return;
        }
        const id = +shape.dataset.shapeId;
        const selection = this.selection();
        const covered = this.chain(this.parents(frame), id).some(i => selection.includes(i));
        if (covered && toggle) {
            this.dotnet.invokeMethodAsync('OnShapeClickAsync', id, true).catch(() => {});
            return;
        }
        const drag = this.begin(e, frame, { kind: 'move', click: covered ? id : null, ids: covered ? selection : null, m, start });
        if (!covered) {
            this.dotnet.invokeMethodAsync('OnShapeClickAsync', id, toggle).then(ids => {
                if (this.drag === drag) {
                    drag.ids = ids;
                }
            }).catch(() => {});
        }
    }

    dblClick(e) {
        const frame = this.frame();
        const shape = e.target.closest(SHAPE);
        if (!frame || !shape || !frame.contains(shape) || !this.dotnet || this.root.dataset.readonly === 'true' || e.target.closest(TEXT_EDITOR)) {
            return;
        }
        this.dotnet.invokeMethodAsync('OnShapeDoubleClickAsync', +shape.dataset.shapeId, e.clientX, e.clientY).catch(() => {});
    }

    wheel(e) {
        const frame = this.frame();
        if (!(e.ctrlKey || e.metaKey) || !frame || !this.dotnet) {
            return;
        }
        e.preventDefault();
        const now = performance.now();
        const base = this.zoom && now - this.zoom.at < ZOOM_IDLE_MS ? this.zoom.value : frame.getBoundingClientRect().width / (+frame.dataset.width / 9525);
        const value = Math.min(4, Math.max(0.1, base * Math.exp(-e.deltaY / 500)));
        this.zoom = { value, at: now };
        this.dotnet.invokeMethodAsync('OnWheelZoomAsync', value).catch(() => {});
    }

    onScrollbar(e) {
        const r = this.root.getBoundingClientRect();
        return e.clientX >= r.left + this.root.clientLeft + this.root.clientWidth || e.clientY >= r.top + this.root.clientTop + this.root.clientHeight;
    }

    begin(e, frame, drag) {
        this.drag = { ...drag, frame, pointerId: e.pointerId, x: e.clientX, y: e.clientY, x0: e.clientX, y0: e.clientY, moved: false, saved: new Map() };
        try {
            this.root.setPointerCapture(e.pointerId);
        } catch {
        }
        this.root.addEventListener('pointermove', this.onMove);
        this.root.addEventListener('pointerup', this.onUp);
        this.root.addEventListener('pointercancel', this.onUp);
        return this.drag;
    }

    pointerMove(e) {
        const d = this.drag;
        if (!d) {
            return;
        }
        d.x = e.clientX;
        d.y = e.clientY;
        d.shift = e.shiftKey;
        if (!d.moved && Math.hypot(d.x - d.x0, d.y - d.y0) < CLICK_PX) {
            return;
        }
        if (!AREA.includes(d.kind) && !d.ids) {
            return;
        }
        if (!d.moved) {
            d.moved = true;
            d.boxes = AREA.includes(d.kind) ? [] : this.boxes(d.frame, d.ids);
            d.elements = this.elements(d);
            if (d.kind === 'move') {
                d.targets = this.snapTargets(d);
            }
        }
        this.preview(d, this.result(d));
    }

    async pointerUp(e) {
        const d = this.drag;
        if (!d) {
            return;
        }
        this.drag = null;
        try {
            this.root.releasePointerCapture(d.pointerId);
        } catch {
        }
        this.root.removeEventListener('pointermove', this.onMove);
        this.root.removeEventListener('pointerup', this.onUp);
        this.root.removeEventListener('pointercancel', this.onUp);
        this.showGuides(d.frame, null, null);
        this.showMarquee(d.frame, null);
        if (e.type === 'pointercancel' || !this.dotnet) {
            this.restore(d);
            return;
        }
        if (d.kind === 'insert') {
            const p = this.toSlide(d.m, d.x, d.y);
            const [w, h] = d.moved ? [p.x - d.start.x, p.y - d.start.y] : [0, 0];
            await this.dotnet.invokeMethodAsync('OnInsertAsync', d.start.x, d.start.y, w, h).catch(() => {});
            return;
        }
        if (!d.moved) {
            if (d.kind === 'marquee' && !d.toggle) {
                await this.dotnet.invokeMethodAsync('OnSelectionAsync', []).catch(() => {});
            } else if (d.kind === 'move' && d.click != null) {
                await this.dotnet.invokeMethodAsync('OnShapeClickAsync', d.click, false).catch(() => {});
            }
            return;
        }
        const result = this.result(d);
        if (d.kind === 'marquee') {
            const ids = this.inside(d.frame, d.m, result);
            await this.dotnet.invokeMethodAsync('OnSelectionAsync', d.toggle ? [...new Set([...this.selection(), ...ids])] : ids).catch(() => {});
            return;
        }
        const transforms = {};
        result.forEach(b => transforms[b.id] = { x: b.x, y: b.y, width: b.w, height: b.h, rotation: b.r });
        await this.dotnet.invokeMethodAsync('OnTransformsAsync', transforms).catch(() => this.restore(d));
    }

    result(d) {
        const p = this.toSlide(d.m, d.x, d.y);
        let dx = p.x - d.start.x;
        let dy = p.y - d.start.y;
        if (AREA.includes(d.kind)) {
            return { x: Math.min(d.start.x, p.x), y: Math.min(d.start.y, p.y), w: Math.abs(dx), h: Math.abs(dy) };
        }
        if (d.kind === 'rotate') {
            return this.rotate(d, p);
        }
        if (d.kind === 'resize') {
            return this.resize(d, dx, dy);
        }
        if (d.shift) {
            if (Math.abs(dx) > Math.abs(dy)) {
                dy = 0;
            } else {
                dx = 0;
            }
        }
        const snapped = this.snap(d, dx, dy);
        d.guides = snapped.guides;
        return d.boxes.map(b => ({ ...b, x: b.x + snapped.dx, y: b.y + snapped.dy }));
    }

    resize(d, dx, dy) {
        const grabbed = d.boxes.find(b => b.id === d.grabbed) ?? d.boxes[0];
        if (!grabbed) {
            return [];
        }
        const hx = d.handle.includes('w') ? -1 : d.handle.includes('e') ? 1 : 0;
        const hy = d.handle.includes('n') ? -1 : d.handle.includes('s') ? 1 : 0;
        const a = rad(grabbed.r);
        const lx = dx * Math.cos(a) + dy * Math.sin(a);
        const ly = -dx * Math.sin(a) + dy * Math.cos(a);
        const min = MIN_PX / d.m.scale;
        let sx = Math.max(min, grabbed.w + hx * lx) / grabbed.w;
        let sy = Math.max(min, grabbed.h + hy * ly) / grabbed.h;
        if (!hx) {
            sx = d.shift ? sy : 1;
        } else if (!hy) {
            sy = d.shift ? sx : 1;
        } else if (d.shift) {
            sx = sy = Math.max(sx, sy);
        }
        return d.boxes.map(b => {
            const w = b.w * sx;
            const h = b.h * sy;
            const r = rad(b.r);
            const cx = hx * (w - b.w) / 2;
            const cy = hy * (h - b.h) / 2;
            const centerX = b.x + b.w / 2 + cx * Math.cos(r) - cy * Math.sin(r);
            const centerY = b.y + b.h / 2 + cx * Math.sin(r) + cy * Math.cos(r);
            return { ...b, x: centerX - w / 2, y: centerY - h / 2, w, h };
        });
    }

    rotate(d, p) {
        const grabbed = d.boxes.find(b => b.id === d.grabbed) ?? d.boxes[0];
        if (!grabbed) {
            return [];
        }
        const cx = grabbed.x + grabbed.w / 2;
        const cy = grabbed.y + grabbed.h / 2;
        const angle = Math.atan2(p.y - cy, p.x - cx) - Math.atan2(d.start.y - cy, d.start.x - cx);
        let target = grabbed.r + angle * 180 / Math.PI;
        if (d.shift) {
            target = Math.round(target / 15) * 15;
        }
        const delta = target - grabbed.r;
        return d.boxes.map(b => b.opaque ? b : { ...b, r: ((b.r + delta) % 360 + 360) % 360 });
    }

    bounds(b) {
        const a = rad(b.r);
        const hw = (Math.abs(b.w * Math.cos(a)) + Math.abs(b.h * Math.sin(a))) / 2;
        const hh = (Math.abs(b.w * Math.sin(a)) + Math.abs(b.h * Math.cos(a))) / 2;
        const cx = b.x + b.w / 2;
        const cy = b.y + b.h / 2;
        return { left: cx - hw, top: cy - hh, right: cx + hw, bottom: cy + hh };
    }

    snapTargets(d) {
        const parents = this.parents(d.frame);
        const moving = new Set(d.boxes.map(b => b.id));
        const xs = [0, d.m.width / 2, d.m.width];
        const ys = [0, d.m.height / 2, d.m.height];
        d.frame.querySelectorAll(SHAPE).forEach(el => {
            if (this.chain(parents, +el.dataset.shapeId).some(id => moving.has(id))) {
                return;
            }
            const r = el.getBoundingClientRect();
            const a = this.toSlide(d.m, r.left, r.top);
            const b = this.toSlide(d.m, r.right, r.bottom);
            xs.push(a.x, (a.x + b.x) / 2, b.x);
            ys.push(a.y, (a.y + b.y) / 2, b.y);
        });
        return { xs, ys };
    }

    snap(d, dx, dy) {
        if (!d.boxes.length) {
            return { dx, dy, guides: {} };
        }
        const all = d.boxes.map(b => this.bounds(b));
        const box = {
            left: Math.min(...all.map(b => b.left)), right: Math.max(...all.map(b => b.right)),
            top: Math.min(...all.map(b => b.top)), bottom: Math.max(...all.map(b => b.bottom))
        };
        const tolerance = SNAP_PX / d.m.scale;
        const best = (edges, targets) => {
            let found = null;
            edges.forEach(edge => targets.forEach(target => {
                const diff = target - edge;
                if (Math.abs(diff) <= tolerance && (!found || Math.abs(diff) < Math.abs(found.diff))) {
                    found = { diff, at: target };
                }
            }));
            return found;
        };
        const lockX = d.shift && dx === 0;
        const lockY = d.shift && dy === 0;
        const sx = lockX ? null : best([box.left + dx, (box.left + box.right) / 2 + dx, box.right + dx], d.targets.xs);
        const sy = lockY ? null : best([box.top + dy, (box.top + box.bottom) / 2 + dy, box.bottom + dy], d.targets.ys);
        return { dx: dx + (sx?.diff ?? 0), dy: dy + (sy?.diff ?? 0), guides: { x: sx?.at, y: sy?.at } };
    }

    inside(frame, m, rect) {
        return [...frame.querySelectorAll(SHAPE)].filter(el => !el.dataset.groupId).filter(el => {
            const r = el.getBoundingClientRect();
            const a = this.toSlide(m, r.left, r.top);
            const b = this.toSlide(m, r.right, r.bottom);
            return a.x >= rect.x && a.y >= rect.y && b.x <= rect.x + rect.w && b.y <= rect.y + rect.h;
        }).map(el => +el.dataset.shapeId);
    }

    save(d, el) {
        if (el && !d.saved.has(el)) {
            d.saved.set(el, el.style.cssText);
        }
    }

    restore(d) {
        d.saved.forEach((css, el) => el.style.cssText = css);
        d.saved.clear();
    }

    elements(d) {
        const parents = this.parents(d.frame);
        const shapes = [...d.frame.querySelectorAll(SHAPE)];
        return new Map(d.boxes.map(b => [b.id, d.kind === 'move'
            ? shapes.filter(el => this.chain(parents, +el.dataset.shapeId).includes(b.id))
            : shapes.filter(el => +el.dataset.shapeId === b.id)]));
    }

    preview(d, result) {
        if (AREA.includes(d.kind)) {
            this.showMarquee(d.frame, result);
            return;
        }
        const m = d.m;
        result.forEach(b => {
            const original = d.boxes.find(o => o.id === b.id);
            [b.el, ...d.elements.get(b.id)].forEach(el => {
                this.save(d, el);
                el.style.cssText = d.saved.get(el);
                if (d.kind === 'move') {
                    el.style.translate = `${(b.x - original.x) * m.scale}px ${(b.y - original.y) * m.scale}px`;
                    return;
                }
                const flips = el === b.el ? '' : `${b.fh ? ' scaleX(-1)' : ''}${b.fv ? ' scaleY(-1)' : ''}`;
                Object.assign(el.style, {
                    left: pct(b.x, m.width), top: pct(b.y, m.height), width: pct(b.w, m.width), height: pct(b.h, m.height),
                    transform: `rotate(${b.r}deg)${flips}`
                });
            });
        });
        this.showGuides(d.frame, d.guides?.x, d.guides?.y);
    }

    overlay(frame, cls) {
        const host = frame.querySelector('.mud-ex-slide-canvas-guides');
        if (!host) {
            return null;
        }
        let el = host.querySelector(`.${cls}`);
        if (!el) {
            el = document.createElement('div');
            el.className = cls.startsWith('mud-ex-slide-guide') ? `mud-ex-slide-guide ${cls}` : cls;
            host.appendChild(el);
        }
        return el;
    }

    showGuides(frame, x, y) {
        const m = this.metrics(frame);
        const vertical = this.overlay(frame, 'mud-ex-slide-guide-x');
        const horizontal = this.overlay(frame, 'mud-ex-slide-guide-y');
        if (vertical) {
            vertical.style.display = x == null ? 'none' : 'block';
            vertical.style.left = x == null ? '' : pct(x, m.width);
        }
        if (horizontal) {
            horizontal.style.display = y == null ? 'none' : 'block';
            horizontal.style.top = y == null ? '' : pct(y, m.height);
        }
    }

    showMarquee(frame, rect) {
        const el = this.overlay(frame, 'mud-ex-slide-marquee');
        if (!el) {
            return;
        }
        if (!rect) {
            el.style.display = 'none';
            return;
        }
        const m = this.metrics(frame);
        const x = Math.max(0, rect.x);
        const y = Math.max(0, rect.y);
        Object.assign(el.style, {
            display: 'block', left: pct(x, m.width), top: pct(y, m.height),
            width: pct(Math.max(0, Math.min(m.width, rect.x + rect.w) - x), m.width), height: pct(Math.max(0, Math.min(m.height, rect.y + rect.h) - y), m.height)
        });
    }

    dispose() {
        const d = this.drag;
        this.drag = null;
        if (d) {
            this.restore(d);
        }
        this.events.forEach(([type, handler]) => this.root?.removeEventListener(type, handler));
        this.root?.removeEventListener('pointermove', this.onMove);
        this.root?.removeEventListener('pointerup', this.onUp);
        this.root?.removeEventListener('pointercancel', this.onUp);
        this.root = null;
        this.dotnet = null;
    }
}

export function initializeMudExSlideCanvas(elementRef, dotnet) {
    return new MudExSlideCanvas(elementRef, dotnet);
}
