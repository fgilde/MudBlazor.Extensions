const CHART = '.mud-ex-ssg-chart';
const NOTE_BOX = '.mud-ex-ssg-note-box';
const MIN_SIZE = 20;

const px = b => ({ left: `${b.left}px`, top: `${b.top}px`, width: `${b.width}px`, height: `${b.height}px` });

export const objectMethods = {
    objectPointerDown(e) {
        const t = e.target;
        if (t.closest(NOTE_BOX)) {
            return true;
        }
        const chart = t.closest(CHART);
        if (chart) {
            this.chartPointerDown(e, chart);
            return true;
        }
        const link = (e.ctrlKey || e.metaKey) && !e.shiftKey ? t.closest('.mud-ex-ssg-link') : null;
        if (link) {
            e.preventDefault();
            this.endEditVisual();
            this.focus();
            this.dotnet.invokeMethodAsync('OnLinkClick', +link.dataset.r, +link.dataset.c).catch(() => {});
            return true;
        }
        return false;
    },

    objectDblClick(e) {
        const t = e.target;
        if (t.closest(NOTE_BOX)) {
            return true;
        }
        const chart = t.closest(CHART);
        if (chart && chart.dataset.locked !== 'true') {
            this.dotnet.invokeMethodAsync('OnChartDoubleClick', chart.dataset.chart).catch(() => {});
        }
        return !!chart;
    },

    objectHover(e) {
        const cell = e.target.closest?.('.mud-ex-ssg-noted');
        const key = cell ? `${cell.dataset.r},${cell.dataset.c}` : '';
        if (key === (this.noteKey ?? '')) {
            return;
        }
        this.noteKey = key;
        this.dotnet?.invokeMethodAsync('OnNoteHover', cell ? +cell.dataset.r : -1, cell ? +cell.dataset.c : -1).catch(() => {});
    },

    chartPointerDown(e, el) {
        e.preventDefault();
        this.endEditVisual();
        this.focus();
        if (el.dataset.locked === 'true' || this.chartDrag) {
            return;
        }
        this.chartDrag = {
            el,
            id: el.dataset.chart,
            handle: e.target.closest('.mud-ex-ssg-chart-handle')?.dataset.h ?? '',
            box: { left: el.offsetLeft, top: el.offsetTop, width: el.offsetWidth, height: el.offsetHeight },
            x: e.clientX,
            y: e.clientY,
            pointerId: e.pointerId,
            moved: false
        };
        this.onChartMove ??= ev => this.chartMove(ev);
        this.onChartUp ??= ev => this.chartUp(ev);
        el.setPointerCapture(e.pointerId);
        el.addEventListener('pointermove', this.onChartMove);
        el.addEventListener('pointerup', this.onChartUp);
        el.addEventListener('pointercancel', this.onChartUp);
    },

    chartBox(d, dx, dy) {
        const b = { ...d.box };
        const h = d.handle;
        if (!h) {
            b.left += dx;
            b.top += dy;
        }
        if (h.includes('w')) {
            const w = Math.max(MIN_SIZE, b.width - dx);
            b.left += b.width - w;
            b.width = w;
        }
        if (h.includes('e')) {
            b.width = Math.max(MIN_SIZE, b.width + dx);
        }
        if (h.includes('n')) {
            const height = Math.max(MIN_SIZE, b.height - dy);
            b.top += b.height - height;
            b.height = height;
        }
        if (h.includes('s')) {
            b.height = Math.max(MIN_SIZE, b.height + dy);
        }
        b.left = Math.max(0, b.left);
        b.top = Math.max(0, b.top);
        return b;
    },

    chartMove(e) {
        const d = this.chartDrag;
        if (!d) {
            return;
        }
        const dx = e.clientX - d.x;
        const dy = e.clientY - d.y;
        if (!d.moved && Math.abs(dx) + Math.abs(dy) < 3) {
            return;
        }
        d.moved = true;
        Object.assign(d.el.style, px(this.chartBox(d, dx, dy)));
    },

    async chartUp(e) {
        const d = this.chartDrag;
        if (!d) {
            return;
        }
        this.chartDrag = null;
        try {
            d.el.releasePointerCapture(d.pointerId);
        } catch {
        }
        d.el.removeEventListener('pointermove', this.onChartMove);
        d.el.removeEventListener('pointerup', this.onChartUp);
        d.el.removeEventListener('pointercancel', this.onChartUp);
        if (e.type === 'pointercancel') {
            Object.assign(d.el.style, px(d.box));
            return;
        }
        if (!d.moved) {
            await this.dotnet?.invokeMethodAsync('OnChartSelect', d.id).catch(() => {});
            return;
        }
        const b = this.chartBox(d, e.clientX - d.x, e.clientY - d.y);
        await this.dotnet?.invokeMethodAsync('OnChartMoved', d.id, b.left, b.top, b.width, b.height).catch(() => {});
    },

    openLink(url) {
        if (/^(https?|mailto):/i.test(url ?? '')) {
            window.open(url, '_blank', 'noopener');
        }
    },

    printNode(el) {
        if (!el) {
            return;
        }
        const copy = el.cloneNode(true);
        copy.classList.add('mud-ex-ss-print-active');
        document.body.appendChild(copy);
        window.addEventListener('afterprint', () => copy.remove(), { once: true });
        window.print();
    }
};
