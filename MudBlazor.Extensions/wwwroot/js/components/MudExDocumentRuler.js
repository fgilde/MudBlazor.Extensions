const SNAP = 20;
const OFF = 30;

class MudExDocumentRuler {
    constructor(root, dotnet) {
        this.root = root;
        this.dotnet = dotnet;
        this.sync = () => this.layout();
        this.down = e => this.onDown(e);
        this.resize = new ResizeObserver(this.sync);
        this.resize.observe(root);
        this.mutation = new MutationObserver(this.sync);
        this.mutation.observe(root, { childList: true, subtree: true, attributes: true, attributeFilter: ['data-w'] });
        root.addEventListener('pointerdown', this.down);
        window.addEventListener('resize', this.sync);
        this.layout();
    }

    layout() {
        const track = this.root.querySelector('.mud-ex-doc-ruler-track');
        const sheet = this.root.parentElement?.querySelector('.mud-ex-doc-sheet');
        if (!track || !sheet) {
            return;
        }

        if (this.sheet !== sheet) {
            this.sheetResize?.disconnect();
            this.scroller?.removeEventListener('scroll', this.sync);
            this.sheet = sheet;
            this.scroller = sheet.parentElement;
            this.sheetResize = new ResizeObserver(this.sync);
            this.sheetResize.observe(sheet);
            this.scroller?.addEventListener('scroll', this.sync, { passive: true });
        }

        const rect = sheet.getBoundingClientRect();
        const origin = this.root.getBoundingClientRect();
        track.style.left = `${rect.left - origin.left}px`;
        track.style.width = `${rect.width}px`;
    }

    send(kind, index, twips, remove) {
        this.dotnet.invokeMethodAsync('OnEdit', kind, index, twips, remove).catch(() => { });
    }

    onDown(e) {
        const track = this.root.querySelector('.mud-ex-doc-ruler-track');
        if (e.button !== 0 || !track || track.dataset.edit !== '1') {
            return;
        }

        const rect = track.getBoundingClientRect();
        const [width, left, text] = [+track.dataset.w, +track.dataset.l, +track.dataset.t];
        if (!rect.width || !width) {
            return;
        }

        const relative = x => Math.round(((x - rect.left) / rect.width * width - left) / SNAP) * SNAP;
        const clamp = x => Math.max(0, Math.min(text, relative(x)));
        const marker = e.target.closest('[data-k]');
        e.preventDefault();
        if (!marker) {
            const position = relative(e.clientX);
            if (e.target.closest('.mud-ex-doc-ruler-ticks') && position > 0 && position < text) {
                this.send('add', 0, position, false);
            }

            return;
        }

        marker.setPointerCapture(e.pointerId);
        const middle = this.root.getBoundingClientRect();
        const centre = middle.top + middle.height / 2;
        const style = marker.getAttribute('style') ?? '';
        let value = null;
        let off = false;
        const move = ev => {
            value = clamp(ev.clientX);
            off = marker.dataset.k === 'tab' && Math.abs(ev.clientY - centre) > OFF;
            marker.style.left = `${(left + value) / width * 100}%`;
            marker.style.opacity = off ? '0.3' : '';
        };
        const finish = commit => {
            marker.removeEventListener('pointermove', move);
            marker.removeEventListener('pointerup', up);
            marker.removeEventListener('pointercancel', cancel);
            if (commit && value !== null) {
                this.send(marker.dataset.k, +marker.dataset.i || 0, value, off);
            } else {
                marker.setAttribute('style', style);
            }
        };
        const up = () => finish(true);
        const cancel = () => finish(false);
        marker.addEventListener('pointermove', move);
        marker.addEventListener('pointerup', up);
        marker.addEventListener('pointercancel', cancel);
    }

    dispose() {
        this.root.removeEventListener('pointerdown', this.down);
        window.removeEventListener('resize', this.sync);
        this.resize.disconnect();
        this.mutation.disconnect();
        this.sheetResize?.disconnect();
        this.scroller?.removeEventListener('scroll', this.sync);
    }
}

export function initializeMudExDocumentRuler(root, dotnet) {
    return new MudExDocumentRuler(root, dotnet);
}
