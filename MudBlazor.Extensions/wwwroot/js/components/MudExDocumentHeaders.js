import { render } from './MudExDocumentDom.js';

const GAP = 16;
const num = (style, name) => parseFloat(style.getPropertyValue(name)) || 0;

const fill = (box, current, total) => box.querySelectorAll('[data-fld]').forEach(node => {
    node.textContent = String(node.dataset.fld === 'PAGE' ? current : total);
});

export const headerMethods = {
    setHeaderFooter(payload) {
        this.hf = payload;
        this.hfVersion = (this.hfVersion ?? 0) + 1;
        this.layHeaders();
    },

    setMarks(on) {
        this.sheet.classList.toggle('mud-ex-doc-marks', !!on);
    },

    partsFor(kind, page) {
        const parts = this.hf?.[kind];
        if (!parts) {
            return null;
        }
        if (page === 1 && this.hf.titlePage) {
            return parts.first;
        }
        return page % 2 === 0 && this.hf.evenOdd ? parts.even : parts.default;
    },

    fillBox(box, blocks, current, total) {
        (blocks ?? []).forEach(block => box.append(render(block, this.media)));
        box.querySelectorAll('[data-b]').forEach(node => node.removeAttribute('data-b'));
        fill(box, current, total);
    },

    layHeaders() {
        if (this.disposed) {
            return;
        }
        if (!this.layer) {
            this.layer = document.createElement('div');
            this.layer.className = 'mud-ex-doc-opaque mud-ex-doc-hf';
            this.layer.contentEditable = 'false';
            this.layer.setAttribute('aria-hidden', 'true');
        }
        if (this.layer.parentNode !== this.sheet) {
            this.sheet.append(this.layer);
        }
        const total = this.hf && this.view === 'pages' && !this.printing ? this.pageTotal || 0 : 0;
        const geometry = ['--mx-pw', '--mx-ph', '--mx-ml', '--mx-mr', '--mx-hd', '--mx-fd'].map(name => num(getComputedStyle(this.sheet), name));
        const key = `${this.hfVersion}|${total}|${geometry.join()}`;
        if (key === this.hfKey) {
            return;
        }
        this.hfKey = key;
        this.layer.replaceChildren();
        const [width, height, left, right, header, footer] = geometry;
        if (!total || width - left - right <= 0) {
            return;
        }
        const period = height + GAP;
        for (let page = 1; page <= total; page++) {
            for (const kind of ['header', 'footer']) {
                const box = document.createElement('div');
                box.className = `mud-ex-doc-hf-box mud-ex-doc-hf-${kind}`;
                box.style.left = `${left}px`;
                box.style.width = `${width - left - right}px`;
                box.style.top = `${(page - 1) * period + (kind === 'header' ? header : height - footer)}px`;
                box.addEventListener('dblclick', () => {
                    if (!this.readOnly) {
                        this.dotnet?.invokeMethodAsync('OnHeaderFooterAsync', kind).catch(() => { });
                    }
                });
                this.fillBox(box, this.partsFor(kind, page), page, total);
                this.layer.append(box);
            }
        }
    },

    printHeaders(copy) {
        copy.querySelector('.mud-ex-doc-hf')?.remove();
        if (!this.hf) {
            return;
        }
        const style = getComputedStyle(this.sheet);
        const [top, bottom, header, footer] = ['--mx-mt', '--mx-mb', '--mx-hd', '--mx-fd'].map(name => num(style, name));
        for (const kind of ['header', 'footer']) {
            const parts = this.hf[kind];
            const blocks = parts?.default ?? parts?.first;
            if (!blocks?.length) {
                continue;
            }
            const box = document.createElement('div');
            box.className = `mud-ex-doc-hf-print mud-ex-doc-hf-${kind}`;
            box.style[kind === 'header' ? 'top' : 'bottom'] = kind === 'header' ? `${header - top}px` : `${footer - bottom}px`;
            this.fillBox(box, blocks, 1, this.pageTotal || 1);
            box.querySelectorAll('[data-fld="PAGE"]').forEach(node => {
                node.textContent = '';
            });
            copy.append(box);
        }
    }
};
