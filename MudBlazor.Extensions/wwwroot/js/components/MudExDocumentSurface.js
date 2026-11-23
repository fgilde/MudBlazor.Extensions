import { payload } from './MudExOfficeInterop.js';
import { ZWSP } from './MudExRichText.js';
import { render, pointAt, pointOf, serialise, sameRuns, length, strip, PARAGRAPH, OPAQUE } from './MudExDocumentDom.js';
import { inputMethods } from './MudExDocumentInput.js';

const TYPING_DELAY = 150;
const SELECTION_DELAY = 150;
const GAP_TIMEOUT = 10000;
const SUSPEND_TIMEOUT = 10000;
const INLINE_CHARS = 8000;

const key = selection => selection ? `${selection.anchor.p}:${selection.anchor.o}:${selection.focus.p}:${selection.focus.o}` : '';

class MudExDocumentSurface {
    constructor(root, dotnet, options) {
        this.root = root;
        this.sheet = root.querySelector('.mud-ex-doc-sheet') ?? root;
        this.editor = root.closest('.mud-ex-doc-editor');
        this.dotnet = dotnet;
        this.shortcuts = new Set(options?.shortcuts ?? []);
        this.blocks = new Map();
        this.media = new Map();
        this.seen = 0;
        this.early = new Map();
        this.gap = 0;
        this.broken = false;
        this.disposed = false;
        this.dirty = new Map();
        this.version = 0;
        this.sentText = new Map();
        this.chain = Promise.resolve();
        this.syncQueued = false;
        this.commands = 0;
        this.suspension = null;
        this.queue = [];
        this.composing = null;
        this.held = new Map();
        this.caretFormat = null;
        this.caretSpan = null;
        this.endTyping = false;
        this.lastSelection = '';
        this.typingTimer = 0;
        this.selectionTimer = 0;
        this.typingIn = null;
        this.events = [
            [this.sheet, 'beforeinput', e => this.beforeInput(e)], [this.sheet, 'input', e => this.input(e)],
            [this.sheet, 'keydown', e => this.keyDown(e)], [this.sheet, 'compositionstart', () => this.compositionStart()],
            [this.sheet, 'compositionend', e => this.compositionEnd(e)], [this.sheet, 'mousedown', () => this.jumped()],
            [this.sheet, 'touchstart', () => this.jumped(), { passive: true }], [this.sheet, 'dragstart', e => e.preventDefault()],
            [this.sheet, 'drop', e => e.preventDefault()], [document, 'selectionchange', () => this.selectionChange()]
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

    patch(patches) {
        (patches ?? []).forEach(patch => this.receive(patch, null));
    }

    receive(patch, message) {
        if (this.broken || this.disposed || !patch || patch.seq <= this.seen) {
            return;
        }
        this.early.set(patch.seq, { patch, message });
        if (this.early.has(this.seen + 1)) {
            const before = this.capture();
            while (this.early.has(this.seen + 1)) {
                const next = this.early.get(this.seen + 1);
                this.early.delete(this.seen + 1);
                this.apply(next.patch, next.message);
                this.seen = next.patch.seq;
            }
            this.restore(before);
        }
        if (!this.early.size) {
            clearTimeout(this.gap);
            this.gap = 0;
        } else if (!this.gap) {
            this.gap = setTimeout(() => this.fail(), GAP_TIMEOUT);
        }
    }

    fail() {
        this.gap = 0;
        if (this.broken || this.disposed || !this.early.size) {
            return;
        }
        this.broken = true;
        this.early.clear();
        this.sheet.contentEditable = 'false';
        console.error('MudExDocumentSurface: a patch is missing, the editor was made read-only.');
    }

    apply(patch, message) {
        if (patch.reset) {
            this.sheet.replaceChildren();
            [this.blocks, this.dirty, this.held, this.sentText].forEach(map => map.clear());
            this.composing = null;
            this.caretSpan = null;
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
        if (!force && message && this.dirty.has(dto.id) && this.dirty.get(dto.id) !== message.sent.get(dto.id)) {
            return;
        }
        const after = upsert.after != null ? this.blocks.get(upsert.after) : null;
        if (upsert.inPlace && !old && after?.parentElement !== this.sheet) {
            return;
        }
        if (upsert.inPlace && old && sameRuns(old, dto)) {
            this.dirty.delete(dto.id);
            return;
        }
        const element = render(dto, this.media);
        if (upsert.inPlace && old) {
            this.unindex(old);
            old.replaceWith(element);
        } else {
            if (old) {
                this.unindex(old);
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
        this.index(element);
    }

    index(element) {
        this.blocks.set(element.dataset.b, element);
        this.dirty.delete(element.dataset.b);
        element.querySelectorAll('[data-b]').forEach(node => {
            this.blocks.set(node.dataset.b, node);
            this.dirty.delete(node.dataset.b);
        });
    }

    unindex(element) {
        [element, ...element.querySelectorAll('[data-b]')].forEach(node => {
            if (this.blocks.get(node.dataset.b) === node) {
                this.blocks.delete(node.dataset.b);
                this.dirty.delete(node.dataset.b);
            }
        });
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
        const model = this.readSelection();
        return model && {
            model, anchorNode: selection.anchorNode, anchorOffset: selection.anchorOffset, focusNode: selection.focusNode, focusOffset: selection.focusOffset
        };
    }

    restore(before) {
        const selection = document.getSelection();
        if (before && (selection.anchorNode !== before.anchorNode || selection.anchorOffset !== before.anchorOffset
            || selection.focusNode !== before.focusNode || selection.focusOffset !== before.focusOffset || !before.focusNode.isConnected)) {
            this.select(before.model, false);
        }
    }

    select(selection, scroll) {
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

    touch(p) {
        if (!p?.dataset.b) {
            return;
        }
        this.dirty.set(p.dataset.b, ++this.version);
        this.schedule();
    }

    schedule() {
        if (!this.typingTimer && this.dirty.size) {
            this.typingTimer = setTimeout(() => {
                this.typingTimer = 0;
                this.sync();
            }, TYPING_DELAY);
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
                await this.exchange(null, null);
            }
        });
    }

    enqueue(job) {
        this.chain = this.chain.then(job).catch(e => console.error(e));
    }

    collect() {
        const selection = this.readSelection();
        const caret = selection && selection.anchor.p === selection.focus.p && selection.anchor.o === selection.focus.o ? selection.focus : null;
        const typed = [];
        const sent = new Map();
        for (const [id, version] of [...this.dirty]) {
            const p = this.blocks.get(id);
            if (!p?.isConnected) {
                this.dirty.delete(id);
                continue;
            }
            if (p === this.composing) {
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

    async exchange(name, text) {
        clearTimeout(this.typingTimer);
        this.typingTimer = 0;
        const message = this.collect();
        const result = await this.send(message, name, text);
        if (!this.disposed) {
            this.take(result, message, name != null);
        }
    }

    async send(message, name, text) {
        if (this.disposed || !this.dotnet) {
            return null;
        }
        const [sync, stream] = payload(message.sync);
        try {
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

    take(result, message, scroll) {
        if (!result || result.retry) {
            this.endTyping = this.endTyping || !!message.sync.endTyping;
            this.lastSelection = '';
            if (result?.retry) {
                this.schedule();
            }
            return;
        }
        (result.patches ?? []).forEach(patch => this.receive(patch, message));
        if (result.selection) {
            this.select(result.selection, scroll);
        }
        this.takeCaretFormat(result.caretFormat);
    }

    takeCaretFormat(format) {
        const old = this.caretFormat;
        this.caretFormat = format?.token ? format : null;
        if (old && old.token !== this.caretFormat?.token) {
            this.dropCaretSpan(true);
        }
        if (this.caretFormat && old?.token !== this.caretFormat.token) {
            this.insertCaretSpan();
        }
    }

    dropCaretSpan(always) {
        const span = this.caretSpan;
        if (!span) {
            return;
        }
        if (!span.isConnected) {
            this.caretSpan = null;
            return;
        }
        const selection = document.getSelection();
        if (strip(span.textContent) || !always && span.contains(selection.focusNode)) {
            return;
        }
        this.caretSpan = null;
        const p = this.paragraphOf(span);
        const restore = p && selection.rangeCount && span.contains(selection.focusNode) ? pointOf(this.sheet, span, 0) : null;
        span.remove();
        if (restore) {
            this.select({ anchor: restore, focus: restore }, false);
        }
    }

    insertCaretSpan() {
        const selection = document.getSelection();
        if (!selection?.rangeCount || !selection.isCollapsed) {
            return;
        }
        const p = this.paragraphOf(selection.focusNode);
        if (!p || selection.focusNode.parentElement?.closest('[data-k]') && p.contains(selection.focusNode.parentElement.closest('[data-k]'))) {
            return;
        }
        const span = document.createElement('span');
        span.dataset.p = this.caretFormat.token;
        span.dataset.c = this.caretFormat.css ?? '';
        if (this.caretFormat.css) {
            span.setAttribute('style', this.caretFormat.css);
        }
        span.textContent = ZWSP;
        const range = selection.getRangeAt(0);
        const text = range.startContainer;
        const run = text.nodeType === Node.TEXT_NODE ? text.parentElement : null;
        if (run && run !== p && run.parentElement === p && !run.dataset.k && !run.dataset.p) {
            const right = text.splitText(range.startOffset);
            const tail = run.cloneNode(false);
            for (let node = right; node;) {
                const next = node.nextSibling;
                tail.append(node);
                node = next;
            }
            run.after(span, tail);
        } else {
            range.insertNode(span);
        }
        this.caretSpan = span;
        selection.collapse(span.firstChild, 1);
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

    command(name, text) {
        if (this.blocked) {
            this.queue.push({ name, text });
            return;
        }
        if (this.disposed || this.broken) {
            return;
        }
        this.commands++;
        this.enqueue(async () => {
            try {
                await this.exchange(name, text);
            } finally {
                this.commands--;
                this.replay();
            }
        });
    }

    replay() {
        while (!this.blocked && !this.disposed && this.queue.length) {
            this.perform(this.queue.shift(), null);
        }
        if (!this.blocked) {
            this.schedule();
        }
    }

    suspend() {
        const empty = () => ({ seen: this.seen, typed: [], selection: null, endTyping: false });
        if (this.disposed) {
            return Promise.resolve(empty());
        }
        clearTimeout(this.typingTimer);
        this.typingTimer = 0;
        const suspension = { sent: new Map(), done: false, timer: 0, resolve: null, empty };
        this.suspension = suspension;
        return new Promise(resolve => {
            suspension.resolve = resolve;
            this.enqueue(async () => {
                if (suspension.done) {
                    return;
                }
                let message = this.collect();
                if (JSON.stringify(message.sync).length > INLINE_CHARS) {
                    const result = await this.send(message, null, null);
                    if (suspension.done) {
                        return;
                    }
                    this.take(result, message, false);
                    message = { sync: { ...message.sync, typed: [], seen: this.seen, selection: this.readSelection() }, sent: new Map() };
                }
                suspension.sent = message.sent;
                suspension.done = true;
                suspension.timer = setTimeout(() => this.release(suspension), SUSPEND_TIMEOUT);
                resolve(message.sync);
            });
        });
    }

    resume(result, scroll) {
        const suspension = this.suspension;
        if (suspension) {
            clearTimeout(suspension.timer);
        }
        this.suspension = null;
        if (!this.disposed) {
            this.take(result, { sync: { endTyping: false }, sent: suspension?.sent ?? new Map() }, !!scroll);
            this.replay();
        }
    }

    release(suspension) {
        if (this.suspension === suspension) {
            this.suspension = null;
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
        if (part == null) {
            return;
        }
        const old = this.media.get(part);
        if (old) {
            URL.revokeObjectURL(old);
        }
        this.media.delete(part);
        if (bytes && contentType) {
            this.media.set(part, URL.createObjectURL(new Blob([bytes], { type: contentType })));
        }
    }

    dispose() {
        if (this.disposed) {
            return;
        }
        this.disposed = true;
        this.events.forEach(([target, type, handler, flags]) => target.removeEventListener(type, handler, flags));
        [this.typingTimer, this.selectionTimer, this.gap, this.suspension?.timer].forEach(timer => clearTimeout(timer));
        this.media.forEach(url => URL.revokeObjectURL(url));
        this.media.clear();
        const suspension = this.suspension;
        if (suspension && !suspension.done) {
            suspension.done = true;
            suspension.resolve(suspension.empty());
        }
        this.suspension = null;
        this.queue = [];
        this.dotnet = null;
    }
}

Object.assign(MudExDocumentSurface.prototype, inputMethods);

export function initializeMudExDocumentSurface(root, dotnet, options) {
    return new MudExDocumentSurface(root, dotnet, options);
}
