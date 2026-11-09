const ROW_HEADER = '.mud-ex-ssg-rh';
const COL_HEADER = '.mud-ex-ssg-ch';
const EDGE = 4;
const COMMAND_KEYS = ['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Home', 'End', 'PageUp', 'PageDown', 'Tab', 'Enter', 'F2', 'Delete', 'Backspace', 'Escape'];

class MudExSpreadsheetGrid {
    constructor(root, dotnet) {
        this.root = root;
        this.dotnet = dotnet;
        this.viewport = root.querySelector('.mud-ex-ssg-viewport');
        this.frame = 0;
        this.pending = false;
        this.again = false;
        this.last = null;
        this.drag = null;
        this.preview = null;
        this.scrollFrame = 0;
        this.onScroll = () => this.schedule();
        this.onKeyDown = e => this.keyDown(e);
        this.onPointerDown = e => this.pointerDown(e);
        this.onPointerMove = e => this.pointerMove(e);
        this.onPointerUp = e => this.pointerUp(e);
        this.onHover = e => this.hover(e);
        this.onDblClick = e => this.dblClick(e);
        this.onPaste = e => this.paste(e);
        this.resizeObserver = new ResizeObserver(() => this.schedule());
        this.mutations = new MutationObserver(() => {
            this.attachViewport();
            this.schedule();
        });
        root.addEventListener('keydown', this.onKeyDown);
        root.addEventListener('paste', this.onPaste);
        this.mutations.observe(root, { attributes: true, attributeFilter: ['data-win'], childList: true });
        this.attachViewport();
        this.schedule();
    }

    attachViewport() {
        const viewport = this.root?.querySelector('.mud-ex-ssg-viewport');
        if (viewport === this.viewport && this.bound) {
            return;
        }
        this.detachViewport();
        this.viewport = viewport;
        if (!viewport) {
            return;
        }
        viewport.addEventListener('scroll', this.onScroll, { passive: true });
        viewport.addEventListener('pointerdown', this.onPointerDown);
        viewport.addEventListener('pointermove', this.onHover);
        viewport.addEventListener('dblclick', this.onDblClick);
        this.resizeObserver.observe(viewport);
        this.bound = true;
    }

    detachViewport() {
        const v = this.viewport;
        if (v && this.bound) {
            v.removeEventListener('scroll', this.onScroll);
            v.removeEventListener('pointerdown', this.onPointerDown);
            v.removeEventListener('pointermove', this.onHover);
            v.removeEventListener('dblclick', this.onDblClick);
            this.resizeObserver.unobserve(v);
        }
        this.bound = false;
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

    check() {
        const v = this.viewport;
        if (!v || !this.dotnet) {
            return;
        }
        const w = (this.root.dataset.win || '').split(',').map(Number);
        const outside = w.length < 6 || v.scrollTop < w[0] || v.scrollTop > w[1] || v.scrollLeft < w[2] || v.scrollLeft > w[3]
            || Math.abs(v.clientWidth - w[4]) > 1 || Math.abs(v.clientHeight - w[5]) > 1;
        const now = [v.scrollTop, v.scrollLeft, v.clientWidth, v.clientHeight];
        if (outside && (!this.last || now.some((x, i) => x !== this.last[i]))) {
            this.report(now);
        }
    }

    async report(now) {
        if (this.pending) {
            this.again = true;
            return;
        }
        this.pending = true;
        this.last = now;
        try {
            await this.dotnet.invokeMethodAsync('OnViewportChanged', ...now);
        } catch {
        }
        this.pending = false;
        if (this.again) {
            this.again = false;
            this.schedule();
        }
    }

    zoom() {
        return parseFloat(this.root.dataset.zoom) || 1;
    }

    readOnly() {
        return this.root.dataset.readonly === 'true';
    }

    sticky() {
        const [left, top] = (this.root.dataset.sticky || '0,0').split(',').map(Number);
        return { left, top };
    }

    editor() {
        return this.root.querySelector('textarea.mud-ex-ssg-editor');
    }

    isEditing() {
        return !!this.editor()?.classList.contains('editing');
    }

    assistOpen() {
        return !!this.root.querySelector('.mud-ex-ssg-assist-item');
    }

    isPrintable(e) {
        const ctrl = e.ctrlKey || e.metaKey;
        if (e.key.length !== 1 || (ctrl && !(e.ctrlKey && e.altKey)) || (e.altKey && !ctrl)) {
            return false;
        }
        return !(e.key === ' ' && e.shiftKey);
    }

    isCommand(e) {
        const ctrl = e.ctrlKey || e.metaKey;
        if (COMMAND_KEYS.includes(e.key)) {
            return !(e.key === 'Tab' && ctrl);
        }
        if (e.key === ' ' && (ctrl || e.shiftKey)) {
            return true;
        }
        if (e.altKey && !ctrl && e.key === '=') {
            return true;
        }
        return ctrl && !e.altKey && e.key.length === 1 && 'abiudrzyxc15;:'.includes(e.key.toLowerCase());
    }

    keyDown(e) {
        if (e.isComposing) {
            return;
        }
        if (!this.isEditing()) {
            if (e.target !== this.root) {
                return;
            }
            if (this.isPrintable(e) && !this.readOnly()) {
                this.startTyping();
                e.stopPropagation();
                return;
            }
            if (this.isCommand(e)) {
                e.preventDefault();
            }
            return;
        }
        if (!e.target.classList?.contains('mud-ex-ssg-editor')) {
            return;
        }
        const ed = e.target;
        const ctrl = e.ctrlKey || e.metaKey;
        const assist = this.assistOpen();
        if (e.key === 'Enter' && e.altKey) {
            e.preventDefault();
            e.stopPropagation();
            this.insertText('\n');
            return;
        }
        if (assist && (e.key === 'Tab' || e.key === 'ArrowUp' || e.key === 'ArrowDown' || e.key === 'Escape')) {
            e.preventDefault();
            return;
        }
        const enterMode = ed.dataset.mode !== 'edit';
        if (e.key === 'Enter' || e.key === 'Tab' || e.key === 'Escape' || (enterMode && e.key.startsWith('Arrow'))) {
            e.preventDefault();
            this.endEdit(true);
            return;
        }
        if (e.key === 'F2' || (ctrl && (e.key === ';' || e.key === ':'))) {
            e.preventDefault();
        }
    }

    startTyping() {
        const ed = this.editor();
        if (!ed) {
            return;
        }
        ed.value = '';
        ed.dataset.mode = 'enter';
        ed.classList.add('editing');
        ed.focus({ preventScroll: true });
    }

    beginEdit(text, edit, focus) {
        const ed = this.editor();
        if (!ed) {
            return;
        }
        ed.value = text ?? '';
        ed.dataset.mode = edit ? 'edit' : 'enter';
        ed.classList.add('editing');
        if (focus) {
            ed.focus({ preventScroll: true });
            ed.setSelectionRange(ed.value.length, ed.value.length);
        }
    }

    setEditText(text, focus) {
        const ed = this.editor();
        if (!ed) {
            return;
        }
        ed.value = text ?? '';
        if (focus) {
            ed.focus({ preventScroll: true });
            ed.setSelectionRange(ed.value.length, ed.value.length);
        }
    }

    insertText(text) {
        const ed = this.editor();
        if (!ed) {
            return;
        }
        ed.setRangeText(text, ed.selectionStart, ed.selectionEnd, 'end');
        ed.dispatchEvent(new Event('input', { bubbles: true }));
    }

    endEdit(focus) {
        const ed = this.editor();
        if (ed) {
            ed.value = '';
            ed.classList.remove('editing');
        }
        if (focus) {
            this.focus();
        }
    }

    focus() {
        this.root?.focus({ preventScroll: true });
    }

    scrollTo(top, left) {
        if (this.viewport) {
            this.viewport.scrollTop = top;
            this.viewport.scrollLeft = left;
        }
    }

    ensureVisible(x, y, w, h, stickyLeft, stickyTop) {
        const v = this.viewport;
        if (!v) {
            return;
        }
        if (y >= 0) {
            if (y - v.scrollTop < stickyTop) {
                v.scrollTop = y - stickyTop;
            } else if (y + h - v.scrollTop > v.clientHeight) {
                v.scrollTop = Math.min(y - stickyTop, y + h - v.clientHeight);
            }
        }
        if (x >= 0) {
            if (x - v.scrollLeft < stickyLeft) {
                v.scrollLeft = x - stickyLeft;
            } else if (x + w - v.scrollLeft > v.clientWidth) {
                v.scrollLeft = Math.min(x - stickyLeft, x + w - v.clientWidth);
            }
        }
    }

    async writeClipboard(text) {
        try {
            await navigator.clipboard.writeText(text);
        } catch {
            const area = document.createElement('textarea');
            area.value = text;
            area.style.position = 'fixed';
            area.style.opacity = '0';
            document.body.appendChild(area);
            area.select();
            try {
                document.execCommand('copy');
            } catch {
            }
            area.remove();
        }
        this.focus();
    }

    paste(e) {
        if (this.isEditing() || this.readOnly()) {
            return;
        }
        const text = e.clipboardData?.getData('text/plain');
        if (text == null) {
            return;
        }
        e.preventDefault();
        this.dotnet?.invokeMethodAsync('PasteTextAsync', text);
    }

    headers(selector, pane) {
        return Array.from(this.root.querySelectorAll(`.mud-ex-ssg-pane-${pane} ${selector}`));
    }

    pick(selector, pos, attr, horizontal, clamp) {
        const vr = this.viewport.getBoundingClientRect();
        const lo = horizontal ? vr.left : vr.top;
        const hi = lo + (horizontal ? this.viewport.clientWidth : this.viewport.clientHeight);
        let boundary = lo;
        let best = null;
        let bestDistance = Infinity;
        const consider = (el, from) => {
            const r = el.getBoundingClientRect();
            const a = Math.max(horizontal ? r.left : r.top, from);
            const b = Math.min(horizontal ? r.right : r.bottom, hi);
            if (b <= a) {
                return false;
            }
            if (pos >= a && pos < b) {
                best = +el.dataset[attr];
                bestDistance = 0;
                return true;
            }
            const d = pos < a ? a - pos : pos - b + 1;
            if (clamp && d < bestDistance) {
                bestDistance = d;
                best = +el.dataset[attr];
            }
            return false;
        };
        for (const el of this.headers(selector, 'corner')) {
            const r = el.getBoundingClientRect();
            boundary = Math.max(boundary, horizontal ? r.right : r.bottom);
            if (consider(el, lo)) {
                return best;
            }
        }
        for (const el of this.headers(selector, horizontal ? 'top' : 'left')) {
            if (consider(el, boundary)) {
                return best;
            }
        }
        return clamp ? best : null;
    }

    cellAt(x, y, clamp) {
        const r = this.pick(ROW_HEADER, y, 'r', false, clamp);
        const c = this.pick(COL_HEADER, x, 'c', true, clamp);
        return r == null || c == null ? null : { r, c };
    }

    resizeTarget(e, horizontal) {
        const selector = horizontal ? COL_HEADER : ROW_HEADER;
        const pos = horizontal ? e.clientX : e.clientY;
        for (const el of this.root.querySelectorAll(selector)) {
            const r = el.getBoundingClientRect();
            const edge = horizontal ? r.right : r.bottom;
            const start = horizontal ? r.left : r.top;
            const cross = horizontal ? e.clientY : e.clientX;
            const crossLo = horizontal ? r.top : r.left;
            const crossHi = horizontal ? r.bottom : r.right;
            if (cross >= crossLo && cross < crossHi && Math.abs(pos - edge) <= EDGE) {
                return { index: +el.dataset[horizontal ? 'c' : 'r'], start, size: edge - start };
            }
        }
        return null;
    }

    hover(e) {
        if (this.drag) {
            return;
        }
        const header = e.target.closest?.(`${COL_HEADER}, ${ROW_HEADER}`);
        const cols = header?.matches(COL_HEADER);
        const edge = header && !this.readOnly() && this.resizeTarget(e, cols);
        this.viewport.classList.toggle('mud-ex-ssg-resize-col', !!edge && cols);
        this.viewport.classList.toggle('mud-ex-ssg-resize-row', !!edge && !cols);
    }

    pointerDown(e) {
        if (e.button !== 0 || !this.dotnet) {
            return;
        }
        const t = e.target;
        if (t.closest('.mud-ex-ssg-assist-host')) {
            e.preventDefault();
            return;
        }
        if (t.closest('textarea.mud-ex-ssg-editor')) {
            return;
        }
        if (t.closest('.mud-ex-ssg-corner')) {
            e.preventDefault();
            this.endEditVisual();
            this.focus();
            this.dotnet.invokeMethodAsync('OnSelectAll');
            return;
        }
        if (t.classList.contains('mud-ex-ssg-fill') && !this.readOnly()) {
            const [top, left, bottom, right] = t.dataset.range.split(',').map(Number);
            this.begin(e, { kind: 'fill', source: { top, left, bottom, right }, target: { top, left, bottom, right } });
            return;
        }
        const header = t.closest(`${COL_HEADER}, ${ROW_HEADER}`);
        if (header) {
            const cols = header.matches(COL_HEADER);
            const edge = !this.readOnly() && this.resizeTarget(e, cols);
            if (edge) {
                this.begin(e, { kind: 'resize', cols, index: edge.index, start: edge.start, original: edge.size, size: edge.size });
                return;
            }
            const index = +header.dataset[cols ? 'c' : 'r'];
            this.endEditVisual();
            this.begin(e, { kind: cols ? 'cols' : 'rows', anchor: index, current: index, extend: e.shiftKey, add: e.ctrlKey || e.metaKey });
            return;
        }
        const cell = this.cellAt(e.clientX, e.clientY, false);
        if (!cell) {
            return;
        }
        this.endEditVisual();
        this.begin(e, { kind: 'cells', anchor: cell, current: cell, extend: e.shiftKey, add: e.ctrlKey || e.metaKey });
    }

    endEditVisual() {
        if (this.isEditing()) {
            this.editor()?.classList.remove('editing');
        }
    }

    begin(e, drag) {
        e.preventDefault();
        this.focus();
        this.drag = { ...drag, pointerId: e.pointerId, x: e.clientX, y: e.clientY };
        this.viewport.setPointerCapture(e.pointerId);
        this.viewport.addEventListener('pointermove', this.onPointerMove);
        this.viewport.addEventListener('pointerup', this.onPointerUp);
        this.viewport.addEventListener('pointercancel', this.onPointerUp);
        this.draw();
    }

    pointerMove(e) {
        const d = this.drag;
        if (!d) {
            return;
        }
        d.x = e.clientX;
        d.y = e.clientY;
        this.track();
        this.autoScroll();
    }

    track() {
        const d = this.drag;
        if (d.kind === 'resize') {
            d.size = Math.max(0, (d.cols ? d.x : d.y) - d.start);
        } else if (d.kind === 'cols' || d.kind === 'rows') {
            const cols = d.kind === 'cols';
            const index = this.pick(cols ? COL_HEADER : ROW_HEADER, cols ? d.x : d.y, cols ? 'c' : 'r', cols, true);
            if (index != null) {
                d.current = index;
            }
        } else {
            const cell = this.cellAt(d.x, d.y, true);
            if (cell) {
                if (d.kind === 'cells') {
                    d.current = cell;
                } else {
                    d.target = this.fillTarget(d.source, cell);
                }
            }
        }
        this.draw();
    }

    fillTarget(s, cell) {
        const dr = cell.r > s.bottom ? cell.r - s.bottom : cell.r < s.top ? cell.r - s.top : 0;
        const dc = cell.c > s.right ? cell.c - s.right : cell.c < s.left ? cell.c - s.left : 0;
        const t = { ...s };
        if (dr && Math.abs(dr) >= Math.abs(dc)) {
            if (dr > 0) {
                t.bottom = cell.r;
            } else {
                t.top = cell.r;
            }
        } else if (dc) {
            if (dc > 0) {
                t.right = cell.c;
            } else {
                t.left = cell.c;
            }
        }
        return t;
    }

    autoScroll() {
        if (this.scrollFrame || !this.drag || this.drag.kind === 'resize') {
            return;
        }
        const step = () => {
            this.scrollFrame = 0;
            const d = this.drag;
            if (!d) {
                return;
            }
            const v = this.viewport;
            const vr = v.getBoundingClientRect();
            const s = this.sticky();
            let dy = 0;
            let dx = 0;
            if (d.y > vr.top + v.clientHeight) {
                dy = d.y - vr.top - v.clientHeight;
            } else if (d.y < vr.top + s.top && d.kind !== 'cols') {
                dy = d.y - vr.top - s.top;
            }
            if (d.x > vr.left + v.clientWidth) {
                dx = d.x - vr.left - v.clientWidth;
            } else if (d.x < vr.left + s.left && d.kind !== 'rows') {
                dx = d.x - vr.left - s.left;
            }
            if (!dx && !dy) {
                return;
            }
            v.scrollTop += Math.max(-40, Math.min(40, dy));
            v.scrollLeft += Math.max(-40, Math.min(40, dx));
            this.track();
            this.scrollFrame = requestAnimationFrame(step);
        };
        this.scrollFrame = requestAnimationFrame(step);
    }

    bounds(selector, attr, lo, hi, horizontal) {
        let a = Infinity;
        let b = -Infinity;
        let min = Infinity;
        let max = -Infinity;
        for (const el of this.root.querySelectorAll(selector)) {
            const i = +el.dataset[attr];
            min = Math.min(min, i);
            max = Math.max(max, i);
            if (i < lo || i > hi) {
                continue;
            }
            const r = el.getBoundingClientRect();
            a = Math.min(a, horizontal ? r.left : r.top);
            b = Math.max(b, horizontal ? r.right : r.bottom);
        }
        const vr = this.viewport.getBoundingClientRect();
        const from = horizontal ? vr.left : vr.top;
        const to = from + (horizontal ? this.viewport.clientWidth : this.viewport.clientHeight);
        if (lo < min) {
            a = from;
        }
        if (hi > max) {
            b = to;
        }
        return a < b ? [Math.max(a, from), Math.min(b, to)] : null;
    }

    draw() {
        const d = this.drag;
        if (!d) {
            return;
        }
        if (!this.preview) {
            this.preview = document.createElement('div');
            this.root.appendChild(this.preview);
        }
        const root = this.root.getBoundingClientRect();
        const vr = this.viewport.getBoundingClientRect();
        let x;
        let y;
        let cls = 'mud-ex-ssg-preview';
        if (d.kind === 'resize') {
            cls = 'mud-ex-ssg-guide';
            x = d.cols ? [d.start + d.size - 1, d.start + d.size + 1] : [vr.left, vr.left + this.viewport.clientWidth];
            y = d.cols ? [vr.top, vr.top + this.viewport.clientHeight] : [d.start + d.size - 1, d.start + d.size + 1];
        } else {
            let rows;
            let cols;
            if (d.kind === 'cols') {
                cols = [Math.min(d.anchor, d.current), Math.max(d.anchor, d.current)];
                rows = [0, Infinity];
            } else if (d.kind === 'rows') {
                rows = [Math.min(d.anchor, d.current), Math.max(d.anchor, d.current)];
                cols = [0, Infinity];
            } else if (d.kind === 'cells') {
                rows = [Math.min(d.anchor.r, d.current.r), Math.max(d.anchor.r, d.current.r)];
                cols = [Math.min(d.anchor.c, d.current.c), Math.max(d.anchor.c, d.current.c)];
            } else {
                cls = 'mud-ex-ssg-preview mud-ex-ssg-preview-fill';
                rows = [d.target.top, d.target.bottom];
                cols = [d.target.left, d.target.right];
            }
            y = this.bounds(ROW_HEADER, 'r', rows[0], rows[1], false);
            x = this.bounds(COL_HEADER, 'c', cols[0], cols[1], true);
        }
        if (!x || !y) {
            this.preview.style.display = 'none';
            return;
        }
        this.preview.className = cls;
        Object.assign(this.preview.style, {
            display: 'block',
            left: `${x[0] - root.left}px`,
            top: `${y[0] - root.top}px`,
            width: `${x[1] - x[0]}px`,
            height: `${y[1] - y[0]}px`
        });
    }

    async pointerUp(e) {
        const d = this.drag;
        if (!d) {
            return;
        }
        this.drag = null;
        cancelAnimationFrame(this.scrollFrame);
        this.scrollFrame = 0;
        try {
            this.viewport.releasePointerCapture(d.pointerId);
        } catch {
        }
        this.viewport.removeEventListener('pointermove', this.onPointerMove);
        this.viewport.removeEventListener('pointerup', this.onPointerUp);
        this.viewport.removeEventListener('pointercancel', this.onPointerUp);
        try {
            if (e.type === 'pointercancel') {
                return;
            }
            if (d.kind === 'cells') {
                await this.dotnet.invokeMethodAsync('OnPointerSelect', d.anchor.r, d.anchor.c, d.current.r, d.current.c, d.extend, d.add);
            } else if (d.kind === 'cols' || d.kind === 'rows') {
                await this.dotnet.invokeMethodAsync('OnHeaderSelect', d.kind === 'cols', d.anchor, d.current, d.extend, d.add);
            } else if (d.kind === 'fill') {
                const s = d.source;
                const t = d.target;
                if (t.top !== s.top || t.left !== s.left || t.bottom !== s.bottom || t.right !== s.right) {
                    await this.dotnet.invokeMethodAsync('FillTo', t.top, t.left, t.bottom, t.right);
                }
            } else if (d.kind === 'resize' && Math.abs(d.size - d.original) >= 1) {
                await this.dotnet.invokeMethodAsync(d.cols ? 'ResizeColumn' : 'ResizeRow', d.index, d.size);
            }
        } catch {
        } finally {
            this.preview?.remove();
            this.preview = null;
        }
    }

    async dblClick(e) {
        if (!this.dotnet) {
            return;
        }
        const t = e.target;
        if (t.closest('.mud-ex-ssg-assist-host') || t.closest('textarea.mud-ex-ssg-editor')) {
            return;
        }
        if (t.classList.contains('mud-ex-ssg-fill')) {
            this.dotnet.invokeMethodAsync('FillHandleDoubleClick');
            return;
        }
        const header = t.closest(`${COL_HEADER}, ${ROW_HEADER}`);
        if (header) {
            const cols = header.matches(COL_HEADER);
            const edge = !this.readOnly() && this.resizeTarget(e, cols);
            if (!edge) {
                return;
            }
            if (!cols) {
                this.dotnet.invokeMethodAsync('AutoFitRow', edge.index);
                return;
            }
            const candidates = await this.dotnet.invokeMethodAsync('AutoFitColumnCandidates', edge.index);
            if (!candidates?.length) {
                return;
            }
            this.dotnet.invokeMethodAsync('ResizeColumn', edge.index, Math.ceil(this.measure(candidates) + 10) * this.zoom());
            return;
        }
        const cell = this.cellAt(e.clientX, e.clientY, false);
        if (cell && !this.readOnly()) {
            this.dotnet.invokeMethodAsync('OnCellDoubleClick', cell.r, cell.c);
        }
    }

    measure(candidates) {
        const ctx = (this.canvas ??= document.createElement('canvas')).getContext('2d');
        let width = 0;
        for (const [text, font] of candidates) {
            ctx.font = font;
            width = Math.max(width, ctx.measureText(text).width);
        }
        return width;
    }

    dispose() {
        cancelAnimationFrame(this.frame);
        cancelAnimationFrame(this.scrollFrame);
        this.detachViewport();
        this.root?.removeEventListener('keydown', this.onKeyDown);
        this.root?.removeEventListener('paste', this.onPaste);
        this.resizeObserver.disconnect();
        this.mutations.disconnect();
        this.preview?.remove();
        this.preview = null;
        this.drag = null;
        this.root = null;
        this.viewport = null;
        this.dotnet = null;
    }
}

export function initializeMudExSpreadsheetGrid(elementRef, dotnet) {
    return new MudExSpreadsheetGrid(elementRef, dotnet);
}
