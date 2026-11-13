import { eventClipboardData, readClipboardData, sendPaste, writeClipboardData } from './MudExOfficeClipboard.js';

const PAGE = '.mud-ex-notebook-page-item';
const TAB = '.mud-ex-notebook-section-tab';
const SURFACE = '.mud-ex-notebook-editor-surface';
const HEADER = '.mud-ex-notebook-page-header';
const RENAME = '.mud-ex-notebook-section-rename';
const INLINE = `.mud-ex-notebook-title, ${RENAME}`;
const FIELDS = 'input, textarea, select, [contenteditable]';
const SHORTCUT_TARGETS = '.mud-ex-notebook-editor-surface, .mud-ex-notebook-page-list, .mud-ex-notebook-section-tabs';
const SHORTCUTS = 'nyzfe';
const OWN_PASTE = 'input, textarea, [contenteditable], .mud-ex-notebook-text-editor';
const PRINT_ROOT = 'mud-ex-notebook-print-root';
const PRINT_BODY = 'mud-ex-notebook-print-body';
const ZOOM_STEP = 1.1;
const NAVIGATION = ['PageUp', 'PageDown', 'Tab'];
const RASTER = ['image/png', 'image/jpeg', 'image/gif', 'image/bmp', 'image/webp'];
const BEFORE = 'mud-ex-notebook-drop-before';
const AFTER = 'mud-ex-notebook-drop-after';
const INTO = 'mud-ex-notebook-drop-into';

class MudExNotebookEditor {
    constructor(root, dotnet) {
        this.root = root;
        this.dotnet = dotnet;
        this.urls = new Set();
        this.drag = null;
        this.marked = null;
        this.surface = null;
        this.size = null;
        this.frame = 0;
        this.resize = new ResizeObserver(() => this.schedule());
        this.mutations = new MutationObserver(() => this.observe());
        this.mutations.observe(root, { childList: true, subtree: true });
        this.events = [
            ['dragstart', e => this.dragStart(e)], ['dragover', e => this.dragOver(e)], ['drop', e => this.drop(e)], ['dragend', () => this.dragEnd()],
            ['keydown', e => this.keyDown(e)], ['focusin', e => e.target.matches?.(RENAME) && e.target.select()], ['paste', e => this.pasteEvent(e)],
            ['wheel', e => this.wheel(e), { passive: false }]
        ];
        this.events.forEach(([type, handler, options]) => root.addEventListener(type, handler, options));
        this.observe();
    }

    observe() {
        const surface = this.root?.querySelector(SURFACE) ?? null;
        if (surface !== this.surface) {
            if (this.surface) {
                this.resize.unobserve(this.surface);
            }
            this.surface = surface;
            if (surface) {
                this.resize.observe(surface);
            }
        }
        this.schedule();
    }

    schedule() {
        if (this.frame) {
            return;
        }
        this.frame = requestAnimationFrame(() => {
            this.frame = 0;
            this.report();
        });
    }

    report() {
        const surface = this.surface;
        if (!surface || !this.dotnet) {
            return;
        }
        const width = surface.clientWidth;
        const height = Math.max(0, surface.clientHeight - (surface.querySelector(HEADER)?.offsetHeight ?? 0));
        if (this.size && this.size[0] === width && this.size[1] === height) {
            return;
        }
        this.size = [width, height];
        this.dotnet.invokeMethodAsync('OnViewportResized', width, height).catch(() => {});
    }

    register(bytes, contentType) {
        const type = (contentType || '').toLowerCase();
        if (!RASTER.includes(type)) {
            return null;
        }
        const url = URL.createObjectURL(new Blob([bytes], { type }));
        this.urls.add(url);
        return url;
    }

    pasteEvent(e) {
        if (!this.dotnet || e.target.closest?.(OWN_PASTE) || this.root.dataset.readonly === 'true') {
            return;
        }
        e.preventDefault();
        sendPaste(this.dotnet, eventClipboardData(e));
    }

    async readClipboard() {
        sendPaste(this.dotnet, await readClipboardData());
    }

    writeClipboard(text, clip) {
        return writeClipboardData(text, clip);
    }

    wheel(e) {
        if (!(e.ctrlKey || e.metaKey) || !e.deltaY || !e.target.closest?.(SURFACE)) {
            return;
        }
        e.preventDefault();
        this.dotnet?.invokeMethodAsync('OnZoomWheel', e.deltaY < 0 ? ZOOM_STEP : 1 / ZOOM_STEP).catch(() => {});
    }

    print() {
        const content = this.root?.querySelector('.mud-ex-notebook-page-content');
        if (!content) {
            return;
        }
        const copy = Object.assign(document.createElement('div'), { className: PRINT_ROOT });
        const title = this.root.querySelector('.mud-ex-notebook-title');
        copy.appendChild(document.createElement('h1')).textContent = title?.value ?? title?.textContent ?? '';
        copy.appendChild(document.createElement('div')).textContent = this.root.querySelector('.mud-ex-notebook-page-date')?.textContent ?? '';
        const page = copy.appendChild(Object.assign(document.createElement('div'), { className: 'mud-ex-notebook-page' }));
        const clone = page.appendChild(content.cloneNode(true));
        Object.assign(clone.style, { transform: 'none', position: 'relative' });
        clone.querySelectorAll('.mud-ex-notebook-editing').forEach(el => el.classList.remove('mud-ex-notebook-editing'));
        document.querySelectorAll(`.${PRINT_ROOT}`).forEach(el => el.remove());
        document.body.appendChild(copy);
        document.body.classList.add(PRINT_BODY);
        window.addEventListener('afterprint', () => {
            copy.remove();
            document.body.classList.remove(PRINT_BODY);
        }, { once: true });
        window.print();
    }

    revokeAll() {
        this.urls.forEach(url => URL.revokeObjectURL(url));
        this.urls.clear();
    }

    keyDown(e) {
        if (e.key === 'Enter' && e.target.matches?.(INLINE)) {
            e.preventDefault();
            e.target.blur();
            return;
        }
        if (!(e.ctrlKey || e.metaKey) || e.altKey) {
            return;
        }
        const key = e.key?.toLowerCase();
        if (key === 's') {
            if (this.root?.dataset.readonly !== 'true') {
                e.preventDefault();
            }
            return;
        }
        if ((SHORTCUTS.includes(key) || NAVIGATION.includes(e.key)) && e.target.closest?.(SHORTCUT_TARGETS) && !e.target.matches?.(FIELDS)) {
            e.preventDefault();
        }
    }

    itemOf(e, selector) {
        const item = e.target?.closest?.(selector);
        return item && this.root?.contains(item) ? item : null;
    }

    dragStart(e) {
        const tab = this.itemOf(e, TAB);
        const item = tab ?? this.itemOf(e, PAGE);
        if (!item || item.getAttribute('draggable') !== 'true') {
            return;
        }
        this.drag = { section: !!tab, index: Number(tab ? item.dataset.sectionIndex : item.dataset.pageIndex) };
        e.dataTransfer.effectAllowed = 'move';
        e.dataTransfer.setData('text/plain', '');
    }

    target(e) {
        const tab = this.itemOf(e, TAB);
        if (this.drag.section) {
            if (!tab) {
                return null;
            }
            const box = tab.getBoundingClientRect();
            const after = e.clientX > box.left + box.width / 2;
            return { item: tab, mark: after ? AFTER : BEFORE, method: 'OnSectionDropped', index: Number(tab.dataset.sectionIndex) + (after ? 1 : 0) };
        }
        if (tab) {
            return { item: tab, mark: INTO, method: 'OnPageDroppedOnSection', index: Number(tab.dataset.sectionIndex) };
        }
        const page = this.itemOf(e, PAGE);
        if (!page) {
            return null;
        }
        const box = page.getBoundingClientRect();
        const after = e.clientY > box.top + box.height / 2;
        return { item: page, mark: after ? AFTER : BEFORE, method: 'OnPageDropped', index: Number(page.dataset.pageIndex) + (after ? 1 : 0) };
    }

    files(e) {
        return [...(e.dataTransfer?.types ?? [])].includes('Files');
    }

    dragOver(e) {
        if (this.drag === null) {
            if (!e.defaultPrevented && this.files(e)) {
                e.preventDefault();
                e.dataTransfer.dropEffect = 'none';
            }
            return;
        }
        const target = this.target(e);
        if (!target) {
            return;
        }
        e.preventDefault();
        e.dataTransfer.dropEffect = 'move';
        this.mark(target.item, target.mark);
    }

    drop(e) {
        if (this.drag === null) {
            if (this.files(e)) {
                e.preventDefault();
            }
            return;
        }
        const target = this.target(e);
        const from = this.drag.index;
        this.dragEnd();
        if (!target) {
            return;
        }
        e.preventDefault();
        this.dotnet?.invokeMethodAsync(target.method, from, target.index).catch(() => {});
    }

    dragEnd() {
        this.drag = null;
        this.mark(null, null);
    }

    mark(item, mark) {
        this.marked?.classList.remove(BEFORE, AFTER, INTO);
        this.marked = item;
        if (item && mark) {
            item.classList.add(mark);
        }
    }

    dispose() {
        cancelAnimationFrame(this.frame);
        this.events.forEach(([type, handler, options]) => this.root?.removeEventListener(type, handler, options));
        this.resize.disconnect();
        this.mutations.disconnect();
        this.revokeAll();
        this.marked = null;
        this.surface = null;
        this.root = null;
        this.dotnet = null;
    }
}

export function initializeMudExNotebookEditor(elementRef, dotnet) {
    return new MudExNotebookEditor(elementRef, dotnet);
}
