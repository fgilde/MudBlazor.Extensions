const ITEM = '.mud-ex-slide-panel-item';
const BEFORE = 'mud-ex-slide-panel-drop-before';
const AFTER = 'mud-ex-slide-panel-drop-after';
const SHORTCUT_TARGETS = '.mud-ex-slide-canvas, .mud-ex-slide-panel';
const SHORTCUTS = 'fhmyz';
const OWN_PASTE = 'input, textarea, [contenteditable], .mud-ex-slide-text-editor';
const RASTER = ['image/png', 'image/jpeg', 'image/gif', 'image/bmp', 'image/webp'];

class MudExSlideEditor {
    constructor(root, dotnet) {
        this.root = root;
        this.dotnet = dotnet;
        this.urls = new Set();
        this.drag = null;
        this.marked = null;
        this.frame = 0;
        this.thumbnails = new IntersectionObserver(entries => this.intersect(entries), { rootMargin: '200px 0px' });
        this.mutations = new MutationObserver(() => this.schedule());
        this.mutations.observe(root, { childList: true, subtree: true });
        this.events = [
            ['dragstart', e => this.dragStart(e)], ['dragover', e => this.dragOver(e)], ['drop', e => this.drop(e)], ['dragend', () => this.dragEnd()],
            ['keydown', e => this.keyDown(e)], ['paste', e => this.pasteEvent(e)]
        ];
        this.events.forEach(([type, handler]) => root.addEventListener(type, handler));
        this.schedule();
    }

    schedule() {
        if (this.frame) {
            return;
        }
        this.frame = requestAnimationFrame(() => {
            this.frame = 0;
            this.root?.querySelectorAll(`${ITEM}:not([data-observed])`).forEach(item => {
                item.setAttribute('data-observed', '');
                this.thumbnails.observe(item);
            });
        });
    }

    intersect(entries) {
        const keys = [];
        entries.filter(e => e.isIntersecting).forEach(e => {
            keys.push(e.target.dataset.slideKey);
            this.thumbnails.unobserve(e.target);
        });
        if (keys.length) {
            this.dotnet?.invokeMethodAsync('OnThumbnailsVisible', keys).catch(() => {});
        }
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

    revokeAll() {
        this.urls.forEach(url => URL.revokeObjectURL(url));
        this.urls.clear();
    }

    keyDown(e) {
        if (!(e.ctrlKey || e.metaKey) || e.altKey) {
            return;
        }
        const key = e.key?.toLowerCase();
        if ((key === 's' && this.root?.dataset.readonly !== 'true') || (SHORTCUTS.includes(key) && e.target.closest?.(SHORTCUT_TARGETS))) {
            e.preventDefault();
        }
    }

    pasteEvent(e) {
        if (!this.dotnet || e.target.closest?.(OWN_PASTE)) {
            return;
        }
        e.preventDefault();
        const data = e.clipboardData;
        this.paste(data ? data.getData('text/plain') : null, [...(data?.files ?? [])].find(f => f.type.startsWith('image/')));
    }

    async readClipboard() {
        let text = null;
        let image = null;
        try {
            for (const item of await navigator.clipboard.read()) {
                const type = item.types.find(t => t.startsWith('image/'));
                if (type && !image) {
                    image = await item.getType(type);
                }
                if (text === null && item.types.includes('text/plain')) {
                    text = await (await item.getType('text/plain')).text();
                }
            }
        } catch {
            try {
                text = await navigator.clipboard.readText();
            } catch {
                text = null;
            }
        }
        this.paste(text, image);
    }

    paste(text, image) {
        const stream = blob => blob ? DotNet.createJSStreamReference(blob) : null;
        this.dotnet?.invokeMethodAsync('OnPasteAsync', stream(text === null ? null : new Blob([text])), stream(image), image?.type ?? null).catch(() => {});
    }

    async writeClipboard(text) {
        try {
            await navigator.clipboard.writeText(text);
        } catch {
            const copy = e => {
                e.clipboardData.setData('text/plain', text);
                e.preventDefault();
            };
            document.addEventListener('copy', copy, true);
            try {
                document.execCommand('copy');
            } catch {
            }
            document.removeEventListener('copy', copy, true);
        }
    }

    reveal(key) {
        [...(this.root?.querySelectorAll(ITEM) ?? [])].find(item => item.dataset.slideKey === key)?.scrollIntoView({ block: 'nearest' });
    }

    itemOf(e) {
        const item = e.target?.closest?.(ITEM);
        return item && this.root?.contains(item) ? item : null;
    }

    dragStart(e) {
        const item = this.itemOf(e);
        if (!item || item.getAttribute('draggable') !== 'true') {
            return;
        }
        this.drag = Number(item.dataset.slideIndex);
        e.dataTransfer.effectAllowed = 'move';
        e.dataTransfer.setData('text/plain', '');
    }

    target(e) {
        const item = this.itemOf(e);
        if (!item) {
            return null;
        }
        const box = item.getBoundingClientRect();
        const after = e.clientY > box.top + box.height / 2;
        return { item, after, index: Number(item.dataset.slideIndex) + (after ? 1 : 0) };
    }

    dragOver(e) {
        if (this.drag === null) {
            return;
        }
        const target = this.target(e);
        if (!target) {
            return;
        }
        e.preventDefault();
        e.dataTransfer.dropEffect = 'move';
        this.mark(target.item, target.after);
    }

    drop(e) {
        if (this.drag === null) {
            return;
        }
        const target = this.target(e);
        const from = this.drag;
        this.dragEnd();
        if (!target) {
            return;
        }
        e.preventDefault();
        this.dotnet?.invokeMethodAsync('OnSlidesDropped', from, target.index).catch(() => {});
    }

    dragEnd() {
        this.drag = null;
        this.mark(null, false);
    }

    mark(item, after) {
        this.marked?.classList.remove(BEFORE, AFTER);
        this.marked = item;
        item?.classList.add(after ? AFTER : BEFORE);
    }

    dispose() {
        cancelAnimationFrame(this.frame);
        this.events.forEach(([type, handler]) => this.root?.removeEventListener(type, handler));
        this.thumbnails.disconnect();
        this.mutations.disconnect();
        this.revokeAll();
        this.marked = null;
        this.root = null;
        this.dotnet = null;
    }
}

export function initializeMudExSlideEditor(elementRef, dotnet) {
    return new MudExSlideEditor(elementRef, dotnet);
}
