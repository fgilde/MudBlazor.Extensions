const GAP = 16;
const REPORT_DELAY = 80;
const PRINT_FALLBACK = 15000;

const num = (style, name) => parseFloat(style.getPropertyValue(name)) || 0;

export const pageMethods = {
    setView(view) {
        this.view = view === 'web' ? 'web' : 'pages';
        this.sheet.classList.toggle('mud-ex-doc-pages', this.view === 'pages');
        this.lastPage = '';
        this.paginate();
    },

    schedulePages() {
        if (!this.pagesTimer && !this.disposed) {
            this.pagesTimer = setTimeout(() => {
                this.pagesTimer = 0;
                this.paginate();
            }, REPORT_DELAY);
        }
    },

    scheduleReport() {
        if (this.view === 'pages' && !this.reportTimer && !this.disposed) {
            this.reportTimer = setTimeout(() => {
                this.reportTimer = 0;
                this.reportPage();
            }, REPORT_DELAY);
        }
    },

    clearSpacers() {
        for (const child of this.sheet.children) {
            if (child.mxMargin !== undefined) {
                child.style.marginTop = child.mxMargin;
                child.mxMargin = undefined;
            }
        }
    },

    paginate() {
        if (this.disposed) {
            return;
        }
        const sheet = this.sheet;
        this.clearSpacers();
        if (this.view !== 'pages' || this.printing) {
            sheet.style.minHeight = '';
            this.pageTotal = 0;
            return;
        }
        const style = getComputedStyle(sheet);
        const [height, top, bottom] = ['--mx-ph', '--mx-mt', '--mx-mb'].map(name => num(style, name));
        if (height <= 0 || height - top - bottom <= 0) {
            return;
        }
        const zoom = this.zoom || 1;
        const period = height + GAP;
        const origin = sheet.getBoundingClientRect().top;
        const blocks = [...sheet.children].filter(c => c.dataset.b != null);
        const rects = blocks.map(c => c.getBoundingClientRect());
        let shift = 0, page = -1, count = 0, previous = null, force = false, end = 0;
        blocks.forEach((block, i) => {
            const rect = rects[i];
            if (rect.height === 0) {
                return;
            }
            const y = (rect.top - origin) / zoom + shift;
            const h = rect.height / zoom;
            const index = Math.max(0, Math.floor(y / period));
            if (index !== page) {
                page = index;
                count = 0;
            }
            const contentTop = page * period + top;
            const contentBottom = page * period + height - bottom;
            let push = 0;
            if (previous) {
                if (y >= contentBottom || count > 0 && (y + h > contentBottom || force || block.style.breakBefore === 'page')) {
                    push = (page + 1) * period + top - y;
                } else if (y < contentTop) {
                    push = contentTop - y;
                }
            }
            if (push > 0) {
                block.mxMargin = block.style.marginTop;
                block.style.marginTop = `${(rect.top - previous.bottom) / zoom + push}px`;
                shift += push;
                page = Math.floor((y + push) / period);
                count = 0;
            }
            count++;
            end = y + push + h;
            previous = rect;
            force = !!block.querySelector('span[data-k="f"]');
        });
        this.pageTotal = Math.max(1, Math.floor((end - 1) / period) + 1);
        sheet.style.minHeight = `${this.pageTotal * period - GAP}px`;
        this.reportPage();
    },

    currentPage() {
        const style = getComputedStyle(this.sheet);
        const period = num(style, '--mx-ph') + GAP;
        const zoom = this.zoom || 1;
        const origin = this.sheet.getBoundingClientRect().top;
        const selection = document.getSelection();
        let y = null;
        if (selection?.rangeCount && this.sheet.contains(selection.focusNode) && this.sheet.contains(document.activeElement)) {
            const range = selection.getRangeAt(0);
            const container = range.startContainer.nodeType === Node.ELEMENT_NODE ? range.startContainer : range.startContainer.parentElement;
            const rect = range.getClientRects()[0] ?? container?.getBoundingClientRect();
            y = rect ? rect.top : null;
        }
        y ??= this.root.getBoundingClientRect().top + this.root.clientHeight / 3;
        return Math.min(this.pageTotal, Math.max(1, Math.floor((y - origin) / zoom / period) + 1));
    },

    reportPage() {
        if (this.view !== 'pages' || !this.pageTotal || this.disposed) {
            return;
        }
        const current = this.currentPage();
        const key = `${current}/${this.pageTotal}`;
        if (key !== this.lastPage) {
            this.lastPage = key;
            this.dotnet?.invokeMethodAsync('OnPagesAsync', current, this.pageTotal).catch(() => { });
        }
    },

    print() {
        if (this.printing || this.disposed) {
            return;
        }
        const style = getComputedStyle(this.sheet);
        const [width, height, top, right, bottom, left] = ['--mx-pw', '--mx-ph', '--mx-mt', '--mx-mr', '--mx-mb', '--mx-ml'].map(name => num(style, name));
        const page = document.createElement('style');
        if (width > 0 && height > 0) {
            page.textContent = `@page{size:${width}px ${height}px;margin:${top}px ${right}px ${bottom}px ${left}px}`;
        }
        this.printing = true;
        this.paginate();
        const copy = this.sheet.cloneNode(true);
        copy.removeAttribute('contenteditable');
        copy.removeAttribute('role');
        const holder = document.createElement('div');
        holder.className = 'mud-ex-doc-print-root';
        holder.append(copy);
        document.head.append(page);
        document.body.append(holder);
        document.documentElement.classList.add('mud-ex-doc-printing');
        let done = false;
        const finish = () => {
            if (done) {
                return;
            }
            done = true;
            window.removeEventListener('afterprint', finish);
            clearTimeout(timer);
            document.documentElement.classList.remove('mud-ex-doc-printing');
            holder.remove();
            page.remove();
            this.printing = false;
            this.paginate();
        };
        const timer = setTimeout(finish, PRINT_FALLBACK);
        window.addEventListener('afterprint', finish);
        try {
            window.print();
        } catch (e) {
            finish();
        }
    }
};
