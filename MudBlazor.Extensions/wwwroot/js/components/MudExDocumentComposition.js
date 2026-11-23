import { pointAt, pointOf, serialise, strip, PARAGRAPH } from './MudExDocumentDom.js';

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
        const ids = [...this.sheet.querySelectorAll(PARAGRAPH)].filter(p => this.blocks.get(p.dataset.b) === p && range.intersectsNode(p)).map(p => p.dataset.b);
        const typed = ids.filter(id => this.dirty.has(id)).map(id => ({ p: id, t: serialise(this.blocks.get(id)), c: null, f: null }));
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
            const selection = document.getSelection();
            const caret = selection?.rangeCount ? pointOf(this.sheet, selection.focusNode, selection.focusOffset) : null;
            const start = caret?.p === id && data ? Math.max(0, caret.o - strip(data).length) : null;
            let reinsert = false;
            if (deferred?.selection && start != null && p.isConnected) {
                removeText(p, start, caret.o);
                reinsert = true;
            }
            held.forEach((hold, holdId) => {
                if (hold.remove) {
                    this.removeBlock(holdId, true);
                    return;
                }
                if (holdId === id && hold.upsert.block.k === 'p' && !reinsert && hold.upsert.block.text === this.sentText.get(id)) {
                    return;
                }
                this.upsert(hold.upsert, null, true);
                reinsert = reinsert || start != null;
            });
            if (reinsert) {
                this.select(deferred?.selection?.selection ?? { anchor: { p: id, o: start }, focus: { p: id, o: start } }, false);
                if (!this.typeText(data)) {
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
                this.schedule();
            }
        }
    }
};
