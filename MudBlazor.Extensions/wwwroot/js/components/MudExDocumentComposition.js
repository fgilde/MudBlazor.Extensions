import { pointAt, pointOf, serialise, strip, PARAGRAPH, OPAQUE } from './MudExDocumentDom.js';

const removeText = (p, from, to) => {
    const start = pointAt(p, from);
    const end = pointAt(p, to);
    const range = document.createRange();
    range.setStart(start.node, start.offset);
    range.setEnd(end.node, end.offset);
    range.deleteContents();
};

export const compositionMethods = {
    compositionStart() {
        const selection = document.getSelection();
        this.deferred = null;
        this.structural = null;
        this.composing = this.paragraphOf(selection?.anchorNode);
        if (!selection?.rangeCount || this.localParagraph([], false, false)) {
            return;
        }
        const range = selection.getRangeAt(0);
        const elements = [...this.sheet.querySelectorAll('[data-b]')].filter(e => this.blocks.get(e.dataset.b) === e && range.intersectsNode(e));
        const paragraphs = elements.filter(e => e.matches(PARAGRAPH) && !e.parentElement.closest(OPAQUE));
        const ids = elements.map(e => e.dataset.b);
        const typed = paragraphs.filter(p => this.dirty.has(p.dataset.b)).map(p => ({ p: p.dataset.b, t: serialise(p), c: null, f: null }));
        this.composing = this.paragraphOf(range.startContainer) ?? paragraphs[0] ?? this.composing;
        ids.forEach(id => {
            this.awaiting.set(id, true);
            this.dirty.delete(id);
        });
        this.structural = { selection: this.readSelection(), ids, typed };
    },

    compositionEnd(e) {
        const p = this.composing;
        const structural = this.structural;
        const deferred = this.deferred;
        const held = this.held;
        this.composing = null;
        this.structural = null;
        this.deferred = null;
        this.held = new Map();
        const data = e.data ?? '';
        try {
            if (structural) {
                held.forEach((hold, id) => hold.remove ? this.removeBlock(id, true) : this.upsert(hold.upsert, null, true));
                if (structural.selection) {
                    this.command('replace', data, { selection: structural.selection, typed: structural.typed, awaiting: structural.ids });
                } else {
                    structural.ids.forEach(id => this.awaiting.delete(id));
                    this.resync();
                }
                return;
            }
            if (!p) {
                return;
            }
            const id = p.dataset.b;
            const queued = this.blocked || this.queue.length > 0 || this.awaiting.has(id);
            const selection = document.getSelection();
            const caret = selection?.rangeCount ? pointOf(this.sheet, selection.focusNode, selection.focusOffset) : null;
            const start = caret?.p === id && data ? Math.max(0, caret.o - strip(data).length) : null;
            let removed = false;
            if (start != null && p.isConnected && (queued || deferred?.selection)) {
                removeText(p, start, caret.o);
                removed = true;
            }
            let rendered = false;
            held.forEach((hold, holdId) => {
                if (hold.remove) {
                    this.removeBlock(holdId, true);
                    return;
                }
                if (holdId === id && hold.upsert.block.k === 'p' && !removed && hold.upsert.block.text === this.echoed(this.sentText.get(id))) {
                    return;
                }
                this.upsert(hold.upsert, null, true);
                rendered = true;
            });
            if (removed || rendered && start != null) {
                this.select(deferred?.selection?.selection ?? { anchor: { p: id, o: start }, focus: { p: id, o: start } }, false);
                if (queued) {
                    this.queue.push({ type: 'insertText', data });
                } else if (!this.typeText(data)) {
                    this.command('replace', data);
                }
            } else {
                if (deferred?.selection) {
                    this.select(deferred.selection.selection, deferred.selection.scroll);
                }
                this.touch(this.blocks.get(id));
            }
            if (deferred && 'format' in deferred) {
                this.takeCaretFormat(deferred.format);
            }
        } finally {
            this.observer.takeRecords();
            if (!this.blocked) {
                this.replay();
            }
        }
    }
};
