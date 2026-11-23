import { offsetOf, pointAt, pointOf, length, strip, units } from './MudExDocumentDom.js';

const LOCAL = new Set(['insertText', 'insertReplacementText', 'deleteContentBackward', 'deleteContentForward', 'deleteWordBackward', 'deleteWordForward']);
const QUEUED = /^(insertText|insertReplacementText|insertParagraph|insertLineBreak|delete(Content|Word|Soft|Hard|Entire))/;
const NAVIGATION = new Set(['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', 'Home', 'End', 'PageUp', 'PageDown']);
const BLOCKED = /^(insertFromPaste|insertFromDrop|insertFromYank|deleteByCut|deleteByDrag|format|history)/;

const stop = e => {
    e.preventDefault();
    e.stopPropagation();
};

const shortcutOf = e => {
    const name = e.key?.length === 1 ? e.key.toUpperCase() : e.key;
    return `${e.ctrlKey || e.metaKey ? 'Ctrl+' : ''}${e.altKey ? 'Alt+' : ''}${e.shiftKey ? 'Shift+' : ''}${name}`;
};

const dataOf = e => e.data ?? e.dataTransfer?.getData('text/plain') ?? '';

const toRange = r => {
    const range = document.createRange();
    range.setStart(r.startContainer, r.startOffset);
    range.setEnd(r.endContainer, r.endOffset);
    return range;
};

export const inputMethods = {
    localParagraph(ranges, backward, forward) {
        const selection = document.getSelection();
        if (!selection?.rangeCount) {
            return null;
        }
        const p = this.paragraphOf(selection.anchorNode);
        if (!p || p !== this.paragraphOf(selection.focusNode)) {
            return null;
        }
        const keyed = [...p.querySelectorAll('[data-k]')].filter(node => node.dataset.k !== 'e');
        for (const range of [selection.getRangeAt(0), ...ranges.map(toRange)]) {
            if (!p.contains(range.startContainer) || !p.contains(range.endContainer)
                || keyed.some(node => node.contains(range.startContainer) || node.contains(range.endContainer) || range.intersectsNode(node))) {
                return null;
            }
        }
        if (selection.isCollapsed && (backward || forward)) {
            const o = offsetOf(p, selection.focusNode, selection.focusOffset);
            if (o == null || backward && o === 0 || forward && o >= length(p)) {
                return null;
            }
        }
        return p;
    },

    beforeInput(e) {
        const type = e.inputType;
        if (this.readOnly || this.broken || this.disposed) {
            e.preventDefault();
            return;
        }
        if (type === 'insertCompositionText' || type === 'deleteCompositionText' || type === 'insertFromComposition' || type === 'deleteByComposition') {
            return;
        }
        if (this.blocked) {
            if (QUEUED.test(type)) {
                this.queue.push({ type, data: dataOf(e) });
            }
            e.preventDefault();
            return;
        }
        this.perform({ type, data: dataOf(e) }, e);
    },

    perform(item, e) {
        if (item.name) {
            this.command(item.name, item.text);
            return;
        }
        const type = item.type;
        const backward = type.endsWith('Backward');
        const forward = type.endsWith('Forward');
        if (e && LOCAL.has(type) && this.localParagraph(e.getTargetRanges?.() ?? [], backward, forward)) {
            this.typingIn = this.paragraphOf(document.getSelection().focusNode);
            return;
        }
        e?.preventDefault();
        if (BLOCKED.test(type)) {
            return;
        }
        if (type === 'insertParagraph') {
            this.command('split');
        } else if (type === 'insertLineBreak') {
            this.lineBreak();
        } else if (type === 'insertText' || type === 'insertReplacementText') {
            if (e || !this.typeText(item.data ?? '')) {
                this.command('replace', item.data ?? '');
            }
        } else if (backward || forward) {
            if (e || !this.deleteLocal(type, backward)) {
                this.command(backward ? 'deleteBackward' : 'deleteForward');
            }
        }
    },

    typeText(text) {
        const p = this.localParagraph([], false, false);
        if (!p) {
            return false;
        }
        const selection = document.getSelection();
        const range = selection.getRangeAt(0);
        if (!range.collapsed) {
            range.deleteContents();
        }
        let node = range.startContainer;
        let offset = range.startOffset;
        if (node.nodeType !== Node.TEXT_NODE) {
            const point = pointAt(p, offsetOf(p, node, offset) ?? 0);
            if (point.node.nodeType === Node.TEXT_NODE) {
                ({ node, offset } = point);
            } else {
                const created = document.createTextNode('');
                range.setStart(point.node, point.offset);
                range.collapse(true);
                range.insertNode(created);
                node = created;
                offset = 0;
            }
        }
        node.insertData(offset, text);
        selection.collapse(node, offset + text.length);
        this.touch(p);
        return true;
    },

    deleteLocal(type, backward) {
        const selection = document.getSelection();
        if (!selection?.rangeCount) {
            return false;
        }
        if (selection.isCollapsed) {
            const p = this.localParagraph([], backward, !backward);
            if (!p) {
                return false;
            }
            selection.modify('extend', backward ? 'backward' : 'forward', type.includes('Word') ? 'word' : 'character');
        }
        const p = this.localParagraph([], false, false);
        if (!p) {
            selection.collapseToStart();
            return false;
        }
        selection.getRangeAt(0).deleteContents();
        selection.collapseToStart();
        this.touch(p);
        return true;
    },

    lineBreak() {
        const p = this.localParagraph([], false, false);
        if (!p) {
            this.command('replace', '\v');
            return;
        }
        const selection = document.getSelection();
        const range = selection.getRangeAt(0);
        if (!range.collapsed) {
            range.deleteContents();
        }
        const br = document.createElement('br');
        br.dataset.k = 'v';
        range.insertNode(br);
        const all = units(p);
        if (all[all.length - 1]?.node === br && p.lastChild?.dataset?.k !== 'e') {
            const end = document.createElement('br');
            end.dataset.k = 'e';
            p.append(end);
        }
        const o = offsetOf(p, br.parentNode, Array.prototype.indexOf.call(br.parentNode.childNodes, br) + 1);
        const point = pointAt(p, o);
        selection.collapse(point.node, point.offset);
        this.touch(p);
    },

    input(e) {
        if (e.isComposing || this.composing) {
            return;
        }
        const p = this.typingIn?.isConnected ? this.typingIn : this.paragraphOf(document.getSelection()?.focusNode);
        this.typingIn = null;
        this.touch(p);
    },

    keyDown(e) {
        if (e.isComposing || e.keyCode === 229 || this.disposed) {
            return;
        }
        const shortcut = shortcutOf(e);
        const ctrl = (e.ctrlKey || e.metaKey) && !e.altKey;
        const letter = e.key?.toLowerCase();
        if (this.shortcuts.has(shortcut)) {
            stop(e);
            this.command('shortcut', shortcut);
        } else if (ctrl && (letter === 'z' || letter === 'y')) {
            stop(e);
            if (!this.readOnly) {
                this.command(letter === 'z' && !e.shiftKey ? 'undo' : 'redo');
            }
        } else if (ctrl && e.key === 'Enter') {
            stop(e);
            if (!this.readOnly) {
                this.command('pageBreak');
            }
        } else if (e.key === 'Tab' && !e.ctrlKey && !e.metaKey && !e.altKey && !this.readOnly) {
            stop(e);
            this.command(e.shiftKey ? 'untab' : 'tab');
        } else if (NAVIGATION.has(e.key)) {
            this.jumped();
        }
    },

    editorKeyDown(e) {
        if (!this.sheet.contains(e.target) && (e.ctrlKey || e.metaKey) && !e.altKey && e.key?.toLowerCase() === 's') {
            e.preventDefault();
        }
    },

    compositionStart() {
        this.composing = this.paragraphOf(document.getSelection()?.focusNode);
    },

    compositionEnd(e) {
        const p = this.composing;
        this.composing = null;
        if (!p) {
            return;
        }
        const id = p.dataset.b;
        const held = this.held;
        this.held = new Map();
        const data = e.data ?? '';
        let typed = false;
        held.forEach((hold, holdId) => {
            if (hold.remove) {
                this.removeBlock(holdId, true);
                return;
            }
            if (holdId !== id || hold.upsert.block.k !== 'p') {
                this.upsert(hold.upsert, null, true);
                return;
            }
            if (hold.upsert.block.text === this.sentText.get(id)) {
                return;
            }
            const caret = pointOf(this.sheet, document.getSelection().focusNode, document.getSelection().focusOffset);
            const start = caret?.p === id ? Math.max(0, caret.o - strip(data).length) : null;
            this.upsert(hold.upsert, null, true);
            const fresh = this.blocks.get(id);
            if (fresh && start != null && data) {
                this.select({ anchor: { p: id, o: start }, focus: { p: id, o: start } }, false);
                typed = this.typeText(data);
            }
        });
        if (!typed) {
            this.touch(this.blocks.get(id));
        }
        if (!this.blocked) {
            this.schedule();
        }
    }
};
