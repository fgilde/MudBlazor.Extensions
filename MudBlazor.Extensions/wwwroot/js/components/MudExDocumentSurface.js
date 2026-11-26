import { payload } from './MudExOfficeInterop.js';
import { render, highlight, clearHighlight, pointAt, pointOf, serialise, sameRuns, length, strip, activate, PARAGRAPH, OPAQUE } from './MudExDocumentDom.js';
import { inputMethods } from './MudExDocumentInput.js';
import { compositionMethods } from './MudExDocumentComposition.js';
import { clipboardMethods, CLIPBOARD } from './MudExDocumentClipboard.js';

const TYPING_DELAY = 150;
const RETRY_DELAY = 1000;
const SELECTION_DELAY = 150;
const GAP_TIMEOUT = 10000;
const SUSPEND_TIMEOUT = 10000;
const INLINE_CHARS = 8000;
const RASTER = new Set(['image/png', 'image/jpeg', 'image/gif', 'image/bmp', 'image/webp']);

const FILTERED = [String.fromCharCode(10), String.fromCharCode(13)];

const echoed = text => [...(text ?? '')].filter(c => !FILTERED.includes(c)).join('');

const key = selection => selection ? `${selection.anchor.p}:${selection.anchor.o}:${selection.focus.p}:${selection.focus.o}` : '';

const base64 = bytes => {
    let text = '';
    for (let i = 0; i < bytes.length; i += 32768) {
        text += String.fromCharCode.apply(null, bytes.subarray(i, i + 32768));
    }
    return btoa(text);
};

const nested = (dto, list) => {
    if (dto?.k === 'p') {
        list.push(dto);
    }
    (dto?.blocks ?? []).forEach(block => nested(block, list));
    (dto?.rows ?? []).forEach(row => (row.cells ?? []).forEach(cell => (cell.blocks ?? []).forEach(block => nested(block, list))));
    return list;
};

class MudExDocumentSurface {
    constructor(root, dotnet, options) {
        this.root = root;
        this.sheet = root.querySelector('.mud-ex-doc-sheet') ?? root;
        this.editor = root.closest('.mud-ex-doc-editor');
        this.dotnet = dotnet;
        this.shortcuts = new Set(options?.shortcuts ?? []);
        this.blocks = new Map();
        this.media = new Map();
        this.blobs = new Set();
        this.seen = 0;
        this.early = new Map();
        this.waiters = [];
        this.gap = 0;
        this.broken = false;
        this.disposed = false;
        this.dirty = new Map();
        this.version = 0;
        this.sentText = new Map();
        this.awaiting = new Map();
        this.chain = Promise.resolve();
        this.syncQueued = false;
        this.commands = 0;
        this.resyncing = false;
        this.suspension = null;
        this.timedOut = null;
        this.queue = [];
        this.composing = null;
        this.structural = null;
        this.deferred = null;
        this.held = new Map();
        this.caretFormat = null;
        this.caretSpan = null;
        this.endTyping = false;
        this.lastSelection = '';
        this.typingTimer = 0;
        this.selectionTimer = 0;
        this.typingIn = null;
        this.observer = new MutationObserver(records => this.inspect(records));
        this.observer.observe(this.sheet, { childList: true, subtree: true });
        this.events = [
            [this.sheet, 'beforeinput', e => this.beforeInput(e)], [this.sheet, 'input', e => this.input(e)],
            [this.sheet, 'keydown', e => this.keyDown(e)], [this.sheet, 'compositionstart', () => this.compositionStart()],
            [this.sheet, 'compositionend', e => this.compositionEnd(e)], [this.sheet, 'mousedown', () => this.jumped()], [this.sheet, 'click', e => activate(e)],
            [this.sheet, 'touchstart', () => this.jumped(), { passive: true }], [this.sheet, 'dragstart', e => e.preventDefault()],
            [this.sheet, 'drop', e => e.preventDefault()], [document, 'selectionchange', () => this.selectionChange()],
            [this.sheet, 'copy', e => this.copyEvent(e, false)], [this.sheet, 'cut', e => this.copyEvent(e, true)], [this.sheet, 'paste', e => this.pasteEvent(e)]
        ];
        if (this.editor) {
            this.events.push([this.editor, 'keydown', e => this.editorKeyDown(e)]);
        }
        this.events.forEach(([target, type, handler, flags]) => target.addEventListener(type, handler, flags));
        this.setReadOnly(!!options?.readOnly);
        this.setZoom(options?.zoom);
    }

    get blocked() {
        return this.commands > 0 || this.suspension != null;
    }

    get inert() {
        return this.disposed || this.broken || this.readOnly;
    }

    echoed(text) {
        return echoed(text);
    }

    patch(patches) {
        (patches ?? []).forEach(patch => this.receive(patch, null));
    }

    receive(patch, message) {
        if (this.broken || this.disposed || !patch || patch.seq <= this.seen) {
            return;
        }
        this.early.set(patch.seq, { patch, message });
        let applied = false;
        if (this.early.has(this.seen + 1)) {
            const before = this.capture();
            while (this.early.has(this.seen + 1)) {
                const next = this.early.get(this.seen + 1);
                this.early.delete(this.seen + 1);
                this.apply(next.patch, next.message);
                this.seen = next.patch.seq;
                applied = true;
            }
            this.observer.takeRecords();
            this.restore(before);
        }
        if (!this.early.size) {
            clearTimeout(this.gap);
            this.gap = 0;
        } else if (applied || !this.gap) {
            clearTimeout(this.gap);
            this.gap = setTimeout(() => this.fail(), GAP_TIMEOUT);
        }
        if (applied) {
            this.wake();
        }
    }

    until(seq) {
        return seq <= this.seen || this.broken || this.disposed ? Promise.resolve() : new Promise(resolve => this.waiters.push({ seq, resolve }));
    }

    wake(all) {
        this.waiters = this.waiters.filter(waiter => {
            if (all || waiter.seq <= this.seen) {
                waiter.resolve();
                return false;
            }
            return true;
        });
    }

    fail() {
        this.gap = 0;
        if (this.broken || this.disposed || !this.early.size) {
            return;
        }
        this.broken = true;
        this.early.clear();
        this.sheet.contentEditable = 'false';
        this.wake(true);
        console.error('MudExDocumentSurface: a patch is missing, the editor was made read-only.');
    }

    apply(patch, message) {
        if (patch.reset) {
            this.sheet.replaceChildren();
            [this.blocks, this.dirty, this.held, this.sentText, this.awaiting].forEach(map => map.clear());
            if (!message?.resync) {
                this.queue = [];
            }
            this.resyncing = false;
            this.composing = null;
            this.structural = null;
            this.deferred = null;
            this.caretSpan = null;
            this.caretFormat = null;
            this.endTyping = false;
        }
        if (patch.sheetCss != null) {
            this.sheet.setAttribute('style', patch.sheetCss);
            this.setZoom(this.zoom);
        }
        (patch.remove ?? []).forEach(id => this.removeBlock(id));
        (patch.upsert ?? []).forEach(upsert => this.upsert(upsert, message));
    }

    holds(element) {
        return this.composing != null && element?.contains(this.composing);
    }

    redirtied(id, message) {
        return this.dirty.has(id) && this.dirty.get(id) !== message.sent.get(id);
    }

    removeBlock(id, force) {
        const element = this.blocks.get(id);
        if (!element) {
            return;
        }
        if (!force && this.holds(element)) {
            this.held.set(id, { remove: true });
            return;
        }
        this.unindex(element);
        element.remove();
    }

    upsert(upsert, message, force) {
        const dto = upsert?.block;
        if (!dto?.id) {
            return;
        }
        const old = this.blocks.get(dto.id);
        if (!force && this.holds(old)) {
            this.held.set(dto.id, { upsert, message });
            return;
        }
        if (!force && message && !this.awaiting.has(dto.id) && this.redirtied(dto.id, message) && dto.text === echoed(this.sentText.get(dto.id))) {
            return;
        }
        const after = upsert.after != null ? this.blocks.get(upsert.after) : null;
        if (upsert.inPlace && !old && after?.parentElement !== this.sheet) {
            return;
        }
        if (upsert.inPlace && old?.isConnected && sameRuns(old, dto)) {
            this.dirty.delete(dto.id);
            this.awaiting.delete(dto.id);
            return;
        }
        const keep = new Map();
        if (!force && message && old && dto.k !== 'p') {
            nested(dto, []).filter(p => this.redirtied(p.id, message) && p.text === echoed(this.sentText.get(p.id)) && this.blocks.get(p.id)?.isConnected)
                .forEach(p => keep.set(p.id, this.blocks.get(p.id)));
        }
        const element = render(dto, this.media);
        if (keep.size) {
            element.querySelectorAll('[data-b]').forEach(node => keep.has(node.dataset.b) && node.replaceWith(keep.get(node.dataset.b)));
        }
        if (upsert.inPlace && old?.isConnected) {
            this.unindex(old, keep);
            old.replaceWith(element);
        } else {
            if (old) {
                this.unindex(old, keep);
                old.remove();
            }
            if (after?.parentElement === this.sheet) {
                after.after(element);
            } else if (upsert.after == null) {
                this.sheet.prepend(element);
            } else {
                this.sheet.append(element);
            }
        }
        this.index(element, keep);
    }

    index(element, keep) {
        [element, ...element.querySelectorAll('[data-b]')].forEach(node => {
            const id = node.dataset.b;
            this.blocks.set(id, node);
            this.awaiting.delete(id);
            if (!keep?.has(id)) {
                this.dirty.delete(id);
            }
        });
    }

    unindex(element, keep) {
        [element, ...element.querySelectorAll('[data-b]')].forEach(node => {
            const id = node.dataset.b;
            if (this.blocks.get(id) === node && !keep?.has(id)) {
                this.blocks.delete(id);
                this.dirty.delete(id);
                this.awaiting.delete(id);
            }
        });
    }

    inspect(records) {
        if (this.disposed || this.broken) {
            return;
        }
        const indexed = node => node.nodeType === Node.ELEMENT_NODE && [node, ...node.querySelectorAll('[data-b]')]
            .some(e => this.blocks.get(e.dataset.b) === e && !e.isConnected && !this.awaiting.has(e.dataset.b));
        const foreign = node => node.parentNode === this.sheet && !(node.nodeType === Node.ELEMENT_NODE && this.blocks.get(node.dataset.b) === node);
        if (records.some(r => [...r.removedNodes].some(indexed) || r.target === this.sheet && [...r.addedNodes].some(foreign))) {
            this.resync();
        }
    }

    resync() {
        if (!this.resyncing && !this.disposed && !this.broken) {
            this.resyncing = true;
            this.command('resync');
        }
    }

    paragraphOf(node) {
        const element = node?.nodeType === Node.TEXT_NODE ? node.parentElement : node;
        const p = element?.closest?.(PARAGRAPH);
        return p && this.sheet.contains(p) && !p.parentElement.closest(OPAQUE) ? p : null;
    }

    readSelection() {
        const selection = document.getSelection();
        if (!selection?.rangeCount || !this.sheet.contains(selection.anchorNode) || !this.sheet.contains(selection.focusNode)) {
            return null;
        }
        const anchor = pointOf(this.sheet, selection.anchorNode, selection.anchorOffset);
        const focus = pointOf(this.sheet, selection.focusNode, selection.focusOffset);
        return anchor && focus ? { anchor, focus } : null;
    }

    capture() {
        const selection = document.getSelection();
        const model = this.composing ? null : this.readSelection();
        return model && {
            model, anchorNode: selection.anchorNode, anchorOffset: selection.anchorOffset, focusNode: selection.focusNode, focusOffset: selection.focusOffset
        };
    }

    restore(before) {
        const selection = document.getSelection();
        if (before && !this.composing && (selection.anchorNode !== before.anchorNode || selection.anchorOffset !== before.anchorOffset
            || selection.focusNode !== before.focusNode || selection.focusOffset !== before.focusOffset || !before.focusNode.isConnected)) {
            this.select(before.model, false);
        }
    }

    select(selection, scroll) {
        if (this.composing) {
            this.deferred = { ...this.deferred, selection: { selection, scroll } };
            return;
        }
        const anchor = this.blocks.get(selection?.anchor?.p);
        const focus = this.blocks.get(selection?.focus?.p);
        if (!anchor?.isConnected || !focus?.isConnected || anchor.tagName !== 'P' || focus.tagName !== 'P') {
            return;
        }
        const at = (p, o) => pointAt(p, Math.max(0, Math.min(o ?? 0, length(p))));
        const a = at(anchor, selection.anchor.o);
        const f = at(focus, selection.focus.o);
        document.getSelection().setBaseAndExtent(a.node, a.offset, f.node, f.offset);
        this.lastSelection = key(selection);
        if (scroll) {
            focus.scrollIntoView({ block: 'nearest' });
        }
    }

    highlight(selections) {
        if (!this.disposed) {
            highlight(this.blocks, selections);
        }
    }

    touch(p) {
        if (!p?.dataset.b) {
            return;
        }
        this.dirty.set(p.dataset.b, ++this.version);
        this.schedule();
    }

    schedule(delay) {
        if (!this.typingTimer && this.dirty.size && !this.disposed && !this.broken) {
            this.typingTimer = setTimeout(() => {
                this.typingTimer = 0;
                this.sync();
            }, delay ?? TYPING_DELAY);
        }
    }

    sync() {
        if (this.blocked || this.syncQueued || this.disposed || this.broken) {
            return;
        }
        this.syncQueued = true;
        this.enqueue(async () => {
            this.syncQueued = false;
            if (!this.blocked) {
                await this.exchange(null);
            }
        });
    }

    enqueue(job) {
        this.chain = this.chain.then(job).catch(e => console.error(e));
    }

    collect(op) {
        const selection = op && 'selection' in op ? op.selection : this.readSelection();
        const caret = selection && selection.anchor.p === selection.focus.p && selection.anchor.o === selection.focus.o ? selection.focus : null;
        const typed = [...(op?.typed ?? [])];
        typed.forEach(entry => this.sentText.set(entry.p, entry.t));
        const sent = new Map();
        for (const [id, version] of [...this.dirty]) {
            const p = this.blocks.get(id);
            if (this.awaiting.has(id) || p === this.composing) {
                continue;
            }
            if (!p?.isConnected) {
                this.dirty.delete(id);
                this.resync();
                continue;
            }
            const t = serialise(p);
            const format = this.caretSpan?.isConnected && p.contains(this.caretSpan) && strip(this.caretSpan.textContent) ? this.caretFormat?.token ?? null : null;
            typed.push({ p: id, t, c: caret?.p === id ? caret.o : null, f: format });
            sent.set(id, version);
            this.sentText.set(id, t);
        }
        this.lastSelection = key(selection);
        const sync = { seen: this.seen, typed, selection, endTyping: this.endTyping };
        this.endTyping = false;
        return { sync, sent };
    }

    async exchange(op) {
        clearTimeout(this.typingTimer);
        this.typingTimer = 0;
        let message = this.collect(op);
        if (CLIPBOARD.has(op?.name) && JSON.stringify(message.sync).length > INLINE_CHARS) {
            message = await this.flushed(message);
        }
        message.resync = op?.name === 'resync';
        const result = await this.send(message, op?.name, op?.text, op?.paste);
        if (!this.disposed) {
            await this.take(result, message, op?.name != null);
        }
    }

    async send(message, name, text, paste) {
        if (this.disposed || !this.dotnet) {
            return null;
        }
        try {
            const [sync, stream] = payload(message.sync);
            if (CLIPBOARD.has(name)) {
                return await this.clipboardCall(sync, name, text, paste);
            }
            return name == null
                ? await this.dotnet.invokeMethodAsync('OnSyncAsync', sync, stream)
                : await this.dotnet.invokeMethodAsync('OnCommandAsync', sync, stream, name, text ?? null);
        } catch (e) {
            if (!this.disposed) {
                console.error(e);
            }
            return null;
        }
    }

    async take(result, message, scroll) {
        if (!result || result.retry) {
            this.endTyping = this.endTyping || !!message.sync.endTyping;
            this.lastSelection = '';
            this.schedule(result ? TYPING_DELAY : RETRY_DELAY);
            return;
        }
        let last = 0;
        (result.patches ?? []).forEach(patch => {
            last = Math.max(last, patch?.seq ?? 0);
            this.receive(patch, message);
        });
        await this.until(last);
        if (this.disposed || this.broken) {
            return;
        }
        if (result.selection) {
            this.select(result.selection, scroll);
        }
        this.takeCaretFormat(result.caretFormat);
    }

    selectionChange() {
        if (this.selectionTimer || this.disposed) {
            return;
        }
        this.selectionTimer = setTimeout(() => {
            this.selectionTimer = 0;
            if (this.disposed || this.composing) {
                return;
            }
            this.dropCaretSpan(false);
            const selection = this.readSelection();
            if (selection && key(selection) !== this.lastSelection) {
                this.sync();
            }
        }, SELECTION_DELAY);
    }

    jumped() {
        this.endTyping = true;
        if (this.dirty.size) {
            this.sync();
        }
    }

    command(name, text, extra) {
        if (this.disposed || this.broken) {
            return;
        }
        if (this.blocked) {
            this.queue.push({ name, text, extra });
            return;
        }
        this.commands++;
        const op = { name, text, selection: extra && 'selection' in extra ? extra.selection : this.readSelection(), typed: extra?.typed, paste: extra?.paste };
        this.enqueue(async () => {
            try {
                await this.exchange(op);
            } finally {
                this.commands--;
                if (name === 'resync') {
                    this.resyncing = false;
                }
                if (extra?.awaiting?.some(id => this.awaiting.has(id))) {
                    extra.awaiting.forEach(id => this.awaiting.delete(id));
                    this.resync();
                }
                this.replay();
            }
        });
    }

    replay() {
        try {
            while (!this.blocked && !this.inert && !this.composing && this.queue.length) {
                this.perform(this.queue.shift(), null);
            }
        } finally {
            if (!this.blocked) {
                this.schedule();
            }
        }
    }

    suspend() {
        const empty = () => ({ seen: this.seen, typed: [], selection: null, endTyping: false });
        if (this.disposed) {
            return Promise.resolve(empty());
        }
        clearTimeout(this.typingTimer);
        this.typingTimer = 0;
        this.timedOut = null;
        const suspension = { sent: new Map(), done: false, timer: 0, resolve: null, empty };
        this.suspension = suspension;
        return new Promise(resolve => {
            suspension.resolve = resolve;
            this.enqueue(async () => {
                if (suspension.done) {
                    return;
                }
                let sync = empty();
                try {
                    let message = this.collect();
                    if (JSON.stringify(message.sync).length > INLINE_CHARS) {
                        const result = await this.send(message, null, null);
                        if (suspension.done) {
                            return;
                        }
                        await this.take(result, message, false);
                        message = { sync: { ...message.sync, typed: [], seen: this.seen, selection: this.readSelection() }, sent: new Map() };
                    }
                    suspension.sent = message.sent;
                    sync = message.sync;
                } catch (e) {
                    console.error(e);
                } finally {
                    if (!suspension.done) {
                        suspension.done = true;
                        if (!this.disposed) {
                            suspension.timer = setTimeout(() => this.release(suspension), SUSPEND_TIMEOUT);
                        }
                        resolve(sync);
                    }
                }
            });
        });
    }

    resume(result, scroll) {
        const suspension = this.suspension ?? this.timedOut;
        clearTimeout(this.suspension?.timer);
        this.timedOut = null;
        if (this.disposed) {
            return;
        }
        new Promise(resolve => resolve(this.take(result, { sync: { endTyping: false }, sent: suspension?.sent ?? new Map() }, !!scroll)))
            .catch(e => console.error(e))
            .finally(() => {
                if (this.suspension === suspension) {
                    this.suspension = null;
                }
                this.replay();
            });
    }

    release(suspension) {
        if (this.suspension === suspension) {
            this.suspension = null;
            this.timedOut = suspension;
            this.replay();
        }
    }

    focus() {
        const selection = document.getSelection();
        const kept = selection?.rangeCount && this.sheet.contains(selection.anchorNode)
            ? [selection.anchorNode, selection.anchorOffset, selection.focusNode, selection.focusOffset] : null;
        this.sheet.focus({ preventScroll: true });
        if (kept) {
            selection.setBaseAndExtent(...kept);
        }
    }

    setReadOnly(readOnly) {
        this.readOnly = !!readOnly;
        this.sheet.contentEditable = this.readOnly || this.broken ? 'false' : 'true';
        this.sheet.tabIndex = 0;
        this.sheet.setAttribute('aria-readonly', this.readOnly ? 'true' : 'false');
    }

    setZoom(zoom) {
        this.zoom = Number.isFinite(zoom) && zoom > 0 ? zoom : 1;
        this.sheet.style.zoom = String(this.zoom);
    }

    setShortcuts(shortcuts) {
        this.shortcuts = new Set(shortcuts ?? []);
    }

    registerMedia(part, bytes, contentType) {
        if (part == null || this.disposed) {
            return;
        }
        const old = this.media.get(part);
        if (this.blobs.delete(old)) {
            URL.revokeObjectURL(old);
        }
        this.media.delete(part);
        const type = String(contentType ?? '').split(';')[0].trim().toLowerCase();
        if (!bytes || !type) {
            return;
        }
        const data = bytes instanceof Uint8Array ? bytes : new Uint8Array(bytes);
        if (RASTER.has(type)) {
            const url = URL.createObjectURL(new Blob([data], { type }));
            this.blobs.add(url);
            this.media.set(part, url);
        } else if (type === 'image/svg+xml') {
            this.media.set(part, `data:image/svg+xml;base64,${base64(data)}`);
        }
    }

    dispose() {
        if (this.disposed) {
            return;
        }
        this.disposed = true;
        clearHighlight();
        this.events.forEach(([target, type, handler, flags]) => target.removeEventListener(type, handler, flags));
        this.observer.disconnect();
        [this.typingTimer, this.selectionTimer, this.gap, this.suspension?.timer].forEach(timer => clearTimeout(timer));
        this.blobs.forEach(url => URL.revokeObjectURL(url));
        this.blobs.clear();
        this.media.clear();
        const suspension = this.suspension;
        if (suspension && !suspension.done) {
            suspension.done = true;
            suspension.resolve(suspension.empty());
        }
        this.suspension = null;
        this.timedOut = null;
        this.queue = [];
        this.wake(true);
        this.dotnet = null;
    }
}

Object.assign(MudExDocumentSurface.prototype, inputMethods, compositionMethods, clipboardMethods);

export function initializeMudExDocumentSurface(root, dotnet, options) {
    return new MudExDocumentSurface(root, dotnet, options);
}
