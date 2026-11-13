import { payload } from './MudExOfficeInterop.js';

export const ZWSP = String.fromCharCode(8203);

export const isDiv = node => node.nodeType === Node.ELEMENT_NODE && node.tagName === 'DIV';

const SERIF = ['georgia', 'cambria', 'times new roman', 'times', 'garamond', 'book antiqua', 'palatino linotype', 'palatino', 'bookman old style',
    'century schoolbook', 'constantia', 'baskerville old face', 'bodoni mt', 'didot', 'perpetua', 'sylfaen'];
const MONOSPACE = ['consolas', 'courier new', 'courier', 'lucida console', 'lucida sans typewriter', 'cascadia code', 'cascadia mono', 'menlo', 'monaco',
    'source code pro', 'fira code', 'jetbrains mono', 'andale mono'];

const generic = font => {
    const name = font.trim().toLowerCase();
    return SERIF.includes(name) ? 'serif' : MONOSPACE.includes(name) ? 'monospace' : 'sans-serif';
};

export const fontFamily = value => {
    const font = String(value).replace(/['"\\]/g, '');
    return `'${font}', ${generic(font)}`;
};

export const richTextMethods = {
    pointRange(x, y) {
        let range = null;
        if (document.caretPositionFromPoint) {
            const pos = document.caretPositionFromPoint(x, y);
            if (pos) {
                range = document.createRange();
                range.setStart(pos.offsetNode, pos.offset);
            }
        } else if (document.caretRangeFromPoint) {
            range = document.caretRangeFromPoint(x, y);
        }
        if (!range || !this.root.contains(range.startContainer)) {
            return null;
        }
        range.collapse(true);
        return range;
    },

    compositionEnd() {
        this.composing = false;
        this.normalize();
        this.changed();
        if (this.commitPending) {
            this.commit(this.commitFocus);
        }
    },

    remember() {
        const sel = getSelection();
        if (this.root && sel?.rangeCount && this.root.contains(sel.anchorNode)) {
            this.range = sel.getRangeAt(0).cloneRange();
            this.report();
        }
    },

    current() {
        if (!this.root || this.done) {
            return null;
        }
        const sel = getSelection();
        if (sel?.rangeCount && this.root.contains(sel.anchorNode) && this.root.contains(sel.focusNode)) {
            return sel.getRangeAt(0);
        }
        if (this.range && this.root.contains(this.range.startContainer) && this.root.contains(this.range.endContainer)) {
            this.root.focus({ preventScroll: true });
            this.select(this.range);
            return getSelection().getRangeAt(0);
        }
        return null;
    },

    select(range) {
        const sel = getSelection();
        sel.removeAllRanges();
        sel.addRange(range);
        this.range = range.cloneRange();
    },

    caret(node, offset) {
        const range = document.createRange();
        range.setStart(node, offset);
        range.collapse(true);
        this.select(range);
    },

    commit(focus) {
        if (this.done || !this.dotnet) {
            return;
        }
        if (this.composing) {
            this.commitPending = true;
            this.commitFocus = this.commitFocus || focus;
            return;
        }
        this.done = true;
        const paragraphs = this.read();
        if (focus) {
            this.canvas?.focus({ preventScroll: true });
        }
        this.dotnet.invokeMethodAsync('OnTextCommitAsync', ...payload(paragraphs)).catch(() => {});
    },

    paragraphOf(node) {
        let n = node?.nodeType === Node.ELEMENT_NODE ? node : node?.parentElement;
        while (n && n.parentElement !== this.root) {
            n = n.parentElement;
        }
        return n && isDiv(n) ? n : null;
    },

    spanOf(text) {
        const parent = text.parentElement;
        return parent && parent.tagName === 'SPAN' && parent !== this.root && this.root.contains(parent) ? parent : null;
    },

    texts(node) {
        const list = [];
        const walker = document.createTreeWalker(node, NodeFilter.SHOW_TEXT);
        for (let t = walker.nextNode(); t; t = walker.nextNode()) {
            list.push(t);
        }
        return list;
    },

    firstText(node) {
        return this.texts(node)[0] ?? null;
    },

    lastText(node) {
        const list = this.texts(node);
        return list[list.length - 1] ?? null;
    },

    visible(node) {
        return node.textContent.split(ZWSP).join('') !== '';
    },

    blankSpan(div) {
        const span = (div?.querySelector('span') ?? this.template).cloneNode(false);
        span.append(document.createTextNode(''));
        return span;
    },

    textAt(container, offset) {
        if (container.nodeType === Node.TEXT_NODE) {
            return { text: container, offset };
        }
        if (container === this.root) {
            if (!this.root.querySelector('div')) {
                this.root.append(this.paragraphTemplate.cloneNode(false));
            }
            const divs = [...this.root.children].filter(isDiv);
            container = divs[Math.min(offset, divs.length - 1)];
            offset = offset >= divs.length ? container.childNodes.length : 0;
        }
        const before = container.childNodes[offset - 1];
        const after = container.childNodes[offset];
        const left = before ? this.lastText(before) ?? (before.nodeType === Node.TEXT_NODE ? before : null) : null;
        if (left) {
            return { text: left, offset: left.length };
        }
        const right = after ? this.firstText(after) ?? (after.nodeType === Node.TEXT_NODE ? after : null) : null;
        if (right) {
            return { text: right, offset: 0 };
        }
        const div = this.paragraphOf(container) ?? container;
        const span = this.blankSpan(div);
        if (after) {
            after.before(span);
        } else {
            container.append(span);
        }
        return { text: span.firstChild, offset: 0 };
    },

    clear(range) {
        const first = this.paragraphOf(range.startContainer);
        const last = this.paragraphOf(range.endContainer);
        range.deleteContents();
        if (first && last && first !== last && first.isConnected && last.isConnected) {
            while (last.firstChild) {
                first.append(last.firstChild);
            }
            last.remove();
        }
        return this.textAt(range.startContainer, range.startOffset);
    },

    insertText(text) {
        const range = this.current();
        if (!range) {
            return;
        }
        const at = this.clear(range);
        at.text.insertData(at.offset, text);
        this.caret(at.text, at.offset + text.length);
        this.changed();
    },

    split() {
        const range = this.current();
        if (!range) {
            return;
        }
        const at = this.clear(range);
        const div = this.paragraphOf(at.text);
        if (!div) {
            return;
        }
        const tail = document.createRange();
        tail.setStart(at.text, at.offset);
        tail.setEnd(div, div.childNodes.length);
        const next = div.cloneNode(false);
        next.append(tail.extractContents());
        div.after(next);
        this.fill(div);
        this.fill(next);
        this.caret(this.firstText(next), 0);
        this.changed();
    },

    fill(div) {
        if (this.visible(div)) {
            return;
        }
        const span = div.querySelector('span')?.cloneNode(false) ?? this.blankSpan(div.previousElementSibling);
        span.textContent = ZWSP;
        div.replaceChildren(span);
    },

    normalize() {
        const sel = getSelection();
        const range = sel?.rangeCount ? sel.getRangeAt(0) : null;
        const node = range?.startContainer;
        const offset = range?.startOffset ?? 0;
        let div = null;
        const created = [];
        [...this.root.childNodes].forEach(n => {
            if (isDiv(n)) {
                div = null;
                return;
            }
            if (!div) {
                div = this.paragraphTemplate.cloneNode(false);
                n.before(div);
                created.push(div);
            }
            div.append(n);
        });
        if (!this.root.firstChild) {
            this.root.append(div = this.paragraphTemplate.cloneNode(false));
            created.push(div);
        }
        let placed = false;
        [...this.root.children].filter(d => isDiv(d) && !d.querySelector('span') && !this.visible(d)).forEach(d => {
            const had = node && d.contains(node);
            this.fill(d);
            if (had || created.includes(d)) {
                this.caret(this.firstText(d), 1);
                placed = true;
            }
        });
        if (!placed && created.length && node && this.root.contains(node)) {
            this.caret(node, offset);
        }
    },

    isolate(text, start, end) {
        if (end < text.length) {
            text.splitText(end);
        }
        if (start > 0) {
            text = text.splitText(start);
        }
        const host = this.spanOf(text);
        if (host && host.childNodes.length === 1) {
            return host;
        }
        const span = host ? host.cloneNode(false) : document.createElement('span');
        if (host) {
            const after = host.cloneNode(false);
            while (text.nextSibling) {
                after.append(text.nextSibling);
            }
            host.after(span);
            span.append(text);
            if (after.childNodes.length) {
                span.after(after);
            }
            if (!host.childNodes.length) {
                host.remove();
            }
        } else {
            text.replaceWith(span);
            span.append(text);
        }
        return span;
    },

    caretSpan(at) {
        const host = this.spanOf(at.text);
        if (host && !this.visible(host)) {
            return host;
        }
        const span = (host ?? this.template).cloneNode(false);
        span.textContent = ZWSP;
        if (at.offset < at.text.length) {
            at.text.splitText(at.offset);
        }
        if (host) {
            const after = host.cloneNode(false);
            while (at.text.nextSibling) {
                after.append(at.text.nextSibling);
            }
            host.after(span);
            if (after.childNodes.length) {
                span.after(after);
            }
        } else {
            at.text.after(span);
        }
        return span;
    },

    spans(range) {
        const parts = this.texts(this.root).filter(t => range.intersectsNode(t)).map(t => [
            t, t === range.startContainer ? range.startOffset : 0, t === range.endContainer ? range.endOffset : t.length
        ]).filter(([, start, end]) => end > start);
        return parts.map(([t, start, end]) => this.isolate(t, start, end));
    },

    paragraphs(range) {
        return [...this.root.children].filter(d => isDiv(d) && range.intersectsNode(d));
    }
};
