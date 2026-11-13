import { inkMethods } from './MudExNotebookInk.js';

const PAGE = '.mud-ex-notebook-page';
const ELEMENT = '[data-element-id]';
const TEXT_EDITOR = '.mud-ex-notebook-text-editor';
const LINK = '.mud-ex-notebook-link';
const CLICK_PX = 3;
const MIN_PX = 8;
const MEASURE_MS = 100;
const KEYS = ['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight'];
const MODIFIERS = ['Shift', 'Control', 'Alt', 'AltGraph', 'Meta', 'CapsLock', 'Dead'];
const FORMAT_KEYS = ['b', 'i', 'u'];
const TODO = '.mud-ex-notebook-todo';

const px = value => `${value}px`;
const frameSelector = id => `.mud-ex-notebook-selection[data-id="${id}"]`;
const elementSelector = id => `[data-element-id="${id}"]`;

class MudExNotebookCanvas {
    constructor(root, dotnet) {
        this.root = root;
        this.dotnet = dotnet;
        this.drag = null;
        this.heights = new Map();
        this.pending = new Map();
        this.observed = new Set();
        this.timer = 0;
        this.touches = new Map();
        this.pan = null;
        this.onMove = e => this.pointerMove(e);
        this.onUp = e => this.pointerUp(e);
        this.events = [['pointerdown', e => this.pointerDown(e)], ['keydown', e => this.keyDown(e)], ['click', e => this.click(e)],
            ['pointermove', e => this.touchMove(e)], ['pointerup', e => this.touchUp(e)], ['pointercancel', e => this.touchUp(e)]];
        this.events.forEach(([type, handler]) => root.addEventListener(type, handler));
        this.resizer = new ResizeObserver(entries => this.measured(entries));
        this.mutations = new MutationObserver(() => this.observe());
        this.mutations.observe(root, { childList: true, subtree: true });
        this.observe();
    }

    readOnly() {
        return this.root.dataset.readonly === 'true';
    }

    page() {
        return this.root?.querySelector(PAGE);
    }

    zoom() {
        return +this.root.dataset.zoom || 1;
    }

    toPage(x, y) {
        const rect = this.page().getBoundingClientRect();
        const zoom = this.zoom();
        return { x: (x - rect.left) / zoom, y: (y - rect.top) / zoom };
    }

    selection() {
        return (this.root.dataset.selection || '').split(',').filter(Boolean);
    }

    observe() {
        const page = this.page();
        this.observed.forEach(el => {
            if (!el.isConnected) {
                this.resizer.unobserve(el);
                this.observed.delete(el);
            }
        });
        page?.querySelectorAll('.mud-ex-notebook-text-element, .mud-ex-notebook-table-element').forEach(el => {
            if (!this.observed.has(el) && !el.style.height) {
                this.observed.add(el);
                this.resizer.observe(el);
            }
        });
    }

    measured(entries) {
        entries.forEach(entry => {
            const el = entry.target;
            const id = el.dataset.elementId;
            const height = Math.ceil(el.offsetHeight);
            if (id && el.isConnected && this.heights.get(id) !== height) {
                this.pending.set(id, height);
            }
        });
        if (this.pending.size && !this.timer) {
            this.timer = setTimeout(() => this.report(), MEASURE_MS);
        }
    }

    report() {
        this.timer = 0;
        if (!this.dotnet || !this.pending.size) {
            return;
        }
        const heights = Object.fromEntries(this.pending);
        this.pending.forEach((height, id) => this.heights.set(id, height));
        this.pending.clear();
        this.dotnet.invokeMethodAsync('OnHeightsMeasuredAsync', heights).catch(() => {});
    }

    click(e) {
        const link = e.target.closest?.(LINK);
        if (!link || this.readOnly() || !this.root.contains(link)) {
            return;
        }
        e.preventDefault();
        if (e.ctrlKey || e.metaKey) {
            window.open(link.href, '_blank', 'noopener');
        }
    }

    keyDown(e) {
        if (e.key === 'Escape' && this.drag?.ink) {
            e.preventDefault();
            this.cancelGesture();
            return;
        }
        if (e.target !== this.root || this.readOnly() || e.altKey) {
            return;
        }
        const ctrl = e.ctrlKey || e.metaKey;
        const selected = !!this.root.dataset.selection;
        const key = e.key.toLowerCase();
        this.typed(e, ctrl);
        const tab = e.key === 'Tab' && !ctrl && !this.leaves(e.shiftKey);
        if ((KEYS.includes(e.key) && selected) || tab || (ctrl && (key === 'a' || key === 'd')) || (selected && !ctrl && e.key === ' ')
            || (selected && ctrl && FORMAT_KEYS.includes(key))) {
            e.preventDefault();
        }
    }

    typed(e, ctrl) {
        const ids = this.selection();
        if (ctrl || ids.length !== 1) {
            this.root.mudExTyped = null;
            return;
        }
        if (MODIFIERS.includes(e.key) || e.isComposing) {
            return;
        }
        const typed = this.root.mudExTyped;
        this.root.mudExTyped = e.key.length !== 1 ? null : { id: ids[0], text: (typed?.id === ids[0] ? typed.text : '') + e.key };
    }

    leaves(back) {
        const ids = [...(this.page()?.querySelectorAll(ELEMENT) ?? [])].map(el => el.dataset.elementId);
        const selection = this.selection();
        if (!selection.length) {
            return back || !ids.length;
        }
        const index = ids.indexOf(selection[selection.length - 1]) + (back ? -1 : 1);
        return index < 0 || index >= ids.length;
    }

    onScrollbar(e) {
        const r = this.root.getBoundingClientRect();
        return e.clientX >= r.left + this.root.clientLeft + this.root.clientWidth || e.clientY >= r.top + this.root.clientTop + this.root.clientHeight;
    }

    pointerDown(e) {
        if (e.pointerType === 'touch' && e.isPrimary) {
            this.touches.clear();
        }
        if (this.touchDown(e)) {
            return;
        }
        const page = this.page();
        if (e.button !== 0 || this.drag || !page || !this.dotnet || this.readOnly() || e.target.closest(TEXT_EDITOR)) {
            return;
        }
        if ((e.ctrlKey || e.metaKey) && e.target.closest(LINK)) {
            return;
        }
        this.root.mudExTyped = null;
        const tool = this.root.dataset.tool;
        const start = this.toPage(e.clientX, e.clientY);
        const handle = e.target.closest('[data-handle]');
        if (handle) {
            this.focus(e);
            const frame = handle.closest('[data-id]');
            if (handle.dataset.handle === 'move') {
                this.begin(e, { kind: 'move', ids: this.selection(), start });
            } else {
                const box = { x: +frame.dataset.x, y: +frame.dataset.y, w: +frame.dataset.w, h: +frame.dataset.h };
                this.begin(e, { kind: 'resize', id: frame.dataset.id, type: frame.dataset.kind, handle: handle.dataset.handle, box, start });
            }
            return;
        }
        if (tool !== 'text' && tool !== 'select') {
            this.inkDown(e, tool, start);
            return;
        }
        const el = e.target.closest(ELEMENT);
        const id = el && page.contains(el) ? el.dataset.elementId : null;
        const todo = id ? e.target.closest(TODO) : null;
        if (todo) {
            this.focus(e);
            const paragraph = todo.closest('.mud-ex-notebook-paragraph');
            this.dotnet.invokeMethodAsync('OnTodoToggleAsync', id, [...paragraph.parentElement.children].indexOf(paragraph)).catch(() => {});
            return;
        }
        const toggle = e.shiftKey || e.ctrlKey || e.metaKey;
        this.focus(e);
        if (tool === 'text' && !toggle && (!id || el.classList.contains('mud-ex-notebook-text-element'))) {
            this.begin(e, { kind: 'click', start });
            return;
        }
        if (!id) {
            this.begin(e, { kind: 'marquee', toggle, start });
            return;
        }
        const selection = this.selection();
        const covered = selection.includes(id);
        const ids = toggle ? (covered ? selection.filter(i => i !== id) : [...selection, id]) : (covered ? selection : [id]);
        if (toggle || !covered) {
            this.dotnet.invokeMethodAsync('OnSelectionAsync', ids).catch(() => {});
        }
        this.begin(e, { kind: 'move', ids: toggle && covered ? [] : ids, click: covered && !toggle ? id : null, start });
    }

    focus(e) {
        e.preventDefault();
        this.root.focus({ preventScroll: true });
    }

    begin(e, drag) {
        this.drag = { ...drag, pointerId: e.pointerId, x: e.clientX, y: e.clientY, x0: e.clientX, y0: e.clientY, moved: false, saved: new Map() };
        try {
            this.root.setPointerCapture(e.pointerId);
        } catch {
        }
        this.root.addEventListener('pointermove', this.onMove);
        this.root.addEventListener('pointerup', this.onUp);
        this.root.addEventListener('pointercancel', this.onUp);
    }

    end(d) {
        this.drag = null;
        try {
            this.root.releasePointerCapture(d.pointerId);
        } catch {
        }
        this.root.removeEventListener('pointermove', this.onMove);
        this.root.removeEventListener('pointerup', this.onUp);
        this.root.removeEventListener('pointercancel', this.onUp);
        this.showMarquee(null);
    }

    pointerMove(e) {
        const d = this.drag;
        if (!d) {
            return;
        }
        if (d.ink) {
            if (e.pointerId === d.pointerId) {
                this.inkMove(d, e);
            }
            return;
        }
        d.x = e.clientX;
        d.y = e.clientY;
        d.shift = e.shiftKey;
        if (!d.moved && Math.hypot(d.x - d.x0, d.y - d.y0) < CLICK_PX) {
            return;
        }
        d.moved = true;
        const p = this.toPage(d.x, d.y);
        if (d.kind === 'marquee') {
            this.showMarquee(this.rect(d.start, p));
        } else if (d.kind === 'move') {
            this.previewMove(d, p);
        } else if (d.kind === 'resize') {
            this.previewResize(d, this.resized(d, p));
        }
    }

    async pointerUp(e) {
        const d = this.drag;
        if (!d) {
            return;
        }
        if (d.ink && e.pointerId !== d.pointerId) {
            return;
        }
        this.end(d);
        if (d.ink) {
            await this.inkUp(d, e.type === 'pointercancel');
            return;
        }
        if (e.type === 'pointercancel' || !this.dotnet) {
            this.restore(d, true);
            return;
        }
        const p = this.toPage(d.x, d.y);
        if (d.kind === 'click') {
            if (!d.moved) {
                this.root.mudExCaret = [d.x0, d.y0];
                await this.dotnet.invokeMethodAsync('OnPageClickAsync', d.start.x, d.start.y).catch(() => {});
            }
            return;
        }
        if (d.kind === 'marquee') {
            if (d.moved) {
                const ids = this.inside(this.rect(d.start, p));
                await this.dotnet.invokeMethodAsync('OnSelectionAsync', d.toggle ? [...new Set([...this.selection(), ...ids])] : ids).catch(() => {});
            } else if (!d.toggle) {
                await this.dotnet.invokeMethodAsync('OnSelectionAsync', []).catch(() => {});
            }
            return;
        }
        if (!d.moved) {
            if (d.click != null) {
                await this.dotnet.invokeMethodAsync('OnSelectionAsync', [d.click]).catch(() => {});
            }
            return;
        }
        if (d.kind === 'move') {
            const positions = {};
            this.moved(d, p).forEach(m => positions[m.id] = { x: m.x, y: m.y });
            await this.dotnet.invokeMethodAsync('OnMoveAsync', positions).catch(() => {});
        } else if (d.kind === 'resize') {
            const b = this.resized(d, p);
            await this.dotnet.invokeMethodAsync('OnResizeAsync', { [d.id]: { x: b.x, y: b.y, width: b.w, height: b.h, free: !!d.shift } }).catch(() => {});
        }
        this.restore(d, false);
    }

    rect(a, b) {
        return { x: Math.min(a.x, b.x), y: Math.min(a.y, b.y), w: Math.abs(b.x - a.x), h: Math.abs(b.y - a.y) };
    }

    inside(rect) {
        return [...this.page().querySelectorAll(ELEMENT)].filter(el => {
            const r = el.getBoundingClientRect();
            const a = this.toPage(r.left, r.top);
            const b = this.toPage(r.right, r.bottom);
            return a.x >= rect.x && a.y >= rect.y && b.x <= rect.x + rect.w && b.y <= rect.y + rect.h;
        }).map(el => el.dataset.elementId);
    }

    moved(d, p) {
        const dx = p.x - d.start.x;
        const dy = p.y - d.start.y;
        return d.ids.map(id => this.page().querySelector(elementSelector(id))).filter(Boolean).map(el => {
            const x = parseFloat(el.style.left) || 0;
            const y = parseFloat(el.style.top) || 0;
            return { id: el.dataset.elementId, el, dx: Math.max(0, x + dx) - x, dy: Math.max(0, y + dy) - y, x: Math.max(0, x + dx), y: Math.max(0, y + dy) };
        });
    }

    previewMove(d, p) {
        const zoom = this.zoom();
        this.moved(d, p).forEach(m => {
            this.style(d, m.el, s => s.translate = `${px(m.dx)} ${px(m.dy)}`);
            this.style(d, this.root.querySelector(frameSelector(m.id)), s => s.translate = `${px(m.dx * zoom)} ${px(m.dy * zoom)}`);
        });
    }

    resized(d, p) {
        const b = d.box;
        const h = d.handle;
        const dx = p.x - d.start.x;
        const dy = p.y - d.start.y;
        let w = h.includes('e') ? b.w + dx : h.includes('w') ? b.w - dx : b.w;
        let height = h.includes('s') ? b.h + dy : h.includes('n') ? b.h - dy : b.h;
        w = Math.max(MIN_PX, w);
        height = Math.max(MIN_PX, height);
        if (d.type === 'image' && !d.shift && b.w > 0 && b.h > 0) {
            if (h === 'n' || h === 's') {
                w = height * b.w / b.h;
            } else {
                height = w * b.h / b.w;
            }
        }
        const x = Math.max(0, h.includes('w') ? b.x + b.w - w : b.x);
        const y = Math.max(0, h.includes('n') ? b.y + b.h - height : b.y);
        return { x, y, w, h: d.type === 'text' ? b.h : height };
    }

    previewResize(d, b) {
        const zoom = this.zoom();
        this.style(d, this.page().querySelector(elementSelector(d.id)), s => {
            Object.assign(s, { left: px(b.x), top: px(b.y), width: px(b.w) });
            if (d.type !== 'text') {
                s.height = px(b.h);
            }
        });
        this.style(d, this.root.querySelector(frameSelector(d.id)), s => Object.assign(s, {
            left: px(b.x * zoom), top: px(b.y * zoom), width: px(b.w * zoom), height: px(b.h * zoom)
        }));
    }

    style(d, el, change) {
        if (!el) {
            return;
        }
        if (!d.saved.has(el)) {
            d.saved.set(el, { original: el.style.cssText, preview: null });
        }
        const saved = d.saved.get(el);
        el.style.cssText = saved.original;
        change(el.style);
        saved.preview = el.style.cssText;
    }

    restore(d, always) {
        d.saved.forEach((saved, el) => {
            if (always || el.style.cssText === saved.preview) {
                el.style.cssText = saved.original;
            }
        });
        d.saved.clear();
    }

    showMarquee(rect) {
        const host = this.root?.querySelector('.mud-ex-notebook-canvas-overlay');
        if (!host) {
            return;
        }
        let el = host.querySelector('.mud-ex-notebook-marquee');
        if (!rect) {
            el?.remove();
            return;
        }
        if (!el) {
            el = document.createElement('div');
            el.className = 'mud-ex-notebook-marquee';
            host.appendChild(el);
        }
        const zoom = this.zoom();
        Object.assign(el.style, { left: px(rect.x * zoom), top: px(rect.y * zoom), width: px(rect.w * zoom), height: px(rect.h * zoom) });
    }

    dispose() {
        this.cancelGesture();
        clearTimeout(this.timer);
        this.resizer.disconnect();
        this.mutations.disconnect();
        this.events.forEach(([type, handler]) => this.root?.removeEventListener(type, handler));
        this.root = null;
        this.dotnet = null;
    }
}

Object.assign(MudExNotebookCanvas.prototype, inkMethods);

export function initializeMudExNotebookCanvas(elementRef, dotnet) {
    return new MudExNotebookCanvas(elementRef, dotnet);
}
