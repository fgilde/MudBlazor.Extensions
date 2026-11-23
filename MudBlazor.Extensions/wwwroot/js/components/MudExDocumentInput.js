import { ZWSP } from './MudExRichText.js';
import { offsetOf, pointAt, pointOf, length, strip, units } from './MudExDocumentDom.js';

const LOCAL = new Set(['insertText', 'insertReplacementText', 'deleteContentBackward', 'deleteContentForward', 'deleteWordBackward', 'deleteWordForward']);
const QUEUED = /^(insertText|insertReplacementText|insertParagraph|insertLineBreak|delete(Content|Word)(Backward|Forward))$/;
const COMPOSITION = new Set(['insertCompositionText', 'deleteCompositionText', 'insertFromComposition', 'deleteByComposition']);
const NAVIGATION = new Set(['ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown', 'Home', 'End', 'PageUp', 'PageDown']);
const STRUCTURAL = new Set(['Enter', 'Backspace', 'Delete']);
const CODES = {
    Space: 'Space', Equal: '=', Minus: '-', BracketLeft: '[', BracketRight: ']', Comma: ',', Period: '.', Slash: '/', Backslash: '\\',
    Semicolon: ';', Quote: '\'', Backquote: '`'
};

const stop = e => {
    e.preventDefault();
    e.stopPropagation();
};

const keyName = e => {
    const code = e.code ?? '';
    if (/^Key[A-Z]$/.test(code)) {
        return /^[a-z]$/i.test(e.key ?? '') ? e.key.toUpperCase() : code.slice(3);
    }
    if (/^(Digit|Numpad)[0-9]$/.test(code)) {
        return code.slice(-1);
    }
    if (CODES[code]) {
        return CODES[code];
    }
    return e.key?.length === 1 ? e.key.toUpperCase() : e.key;
};

const typesCharacter = e => e.getModifierState?.('AltGraph') || !e.metaKey && e.altKey && [...(e.key ?? '')].length === 1 && e.key.toUpperCase() !== keyName(e).toUpperCase();

export const shortcutOf = e => `${e.ctrlKey || e.metaKey ? 'Ctrl+' : ''}${e.altKey ? 'Alt+' : ''}${e.shiftKey ? 'Shift+' : ''}${keyName(e)}`;

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
        if (COMPOSITION.has(type)) {
            return;
        }
        if (this.blocked || this.inAwaiting()) {
            if (QUEUED.test(type)) {
                this.queue.push({ type, data: dataOf(e) });
            }
            e.preventDefault();
            return;
        }
        this.perform({ type, data: dataOf(e) }, e);
    },

    inAwaiting() {
        const selection = document.getSelection();
        return this.awaiting.size > 0 && [selection?.anchorNode, selection?.focusNode].some(node => this.awaiting.has(this.paragraphOf(node)?.dataset.b));
    },

    targetSelection(e) {
        const range = e?.getTargetRanges?.()[0];
        const anchor = range && pointOf(this.sheet, range.startContainer, range.startOffset);
        const focus = range && pointOf(this.sheet, range.endContainer, range.endOffset);
        return anchor && focus ? { selection: { anchor, focus } } : undefined;
    },

    perform(item, e) {
        if (this.inert) {
            e?.preventDefault();
            return;
        }
        if (item.name) {
            this.command(item.name, item.text, item.extra);
            return;
        }
        const type = item.type;
        const backward = /(Backward)$/.test(type);
        const forward = /(Forward)$/.test(type);
        if (e && LOCAL.has(type) && this.localParagraph(e.getTargetRanges?.() ?? [], backward, forward)) {
            this.typingIn = this.paragraphOf(document.getSelection().focusNode);
            return;
        }
        e?.preventDefault();
        if (!QUEUED.test(type)) {
            return;
        }
        if (type === 'insertParagraph') {
            this.command('split');
        } else if (type === 'insertLineBreak') {
            this.lineBreak();
        } else if (type === 'insertText' || type === 'insertReplacementText') {
            if (e || !this.typeText(item.data ?? '')) {
                this.command('replace', item.data ?? '', type === 'insertReplacementText' ? this.targetSelection(e) : undefined);
            }
        } else if (e || !this.deleteLocal(type, backward)) {
            this.command(backward ? 'deleteBackward' : 'deleteForward');
        }
    },

    typeText(text) {
        if (this.inert) {
            return true;
        }
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
        const origin = [selection.anchorNode, selection.anchorOffset, selection.focusNode, selection.focusOffset];
        if (selection.isCollapsed) {
            if (!this.localParagraph([], backward, !backward)) {
                return false;
            }
            selection.modify('extend', backward ? 'backward' : 'forward', type.includes('Word') ? 'word' : 'character');
        }
        const p = this.localParagraph([], false, false);
        if (!p) {
            selection.setBaseAndExtent(...origin);
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
        if (typesCharacter(e)) {
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
        } else if (STRUCTURAL.has(e.key)) {
            e.stopPropagation();
        } else if (NAVIGATION.has(e.key)) {
            this.jumped();
        }
    },

    takeCaretFormat(format) {
        if (this.composing) {
            this.deferred = { ...this.deferred, format };
            return;
        }
        const old = this.caretFormat;
        this.caretFormat = format?.token ? format : null;
        if (old && old.token !== this.caretFormat?.token) {
            this.dropCaretSpan(true);
        }
        if (this.caretFormat && old?.token !== this.caretFormat.token) {
            this.insertCaretSpan();
        }
    },

    dropCaretSpan(always) {
        const span = this.caretSpan;
        if (!span || this.composing) {
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
        const restore = selection.rangeCount && span.contains(selection.focusNode) ? pointOf(this.sheet, span, 0) : null;
        span.remove();
        if (restore) {
            this.select({ anchor: restore, focus: restore }, false);
        }
    },

    insertCaretSpan() {
        const selection = document.getSelection();
        if (!selection?.rangeCount || !selection.isCollapsed) {
            return;
        }
        const p = this.paragraphOf(selection.focusNode);
        const keyed = selection.focusNode.parentElement?.closest('[data-k]');
        if (!p || keyed && p.contains(keyed)) {
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
    },

    editorKeyDown(e) {
        if (!this.sheet.contains(e.target) && (e.ctrlKey || e.metaKey) && !e.altKey && e.key?.toLowerCase() === 's') {
            e.preventDefault();
        }
    }
};
