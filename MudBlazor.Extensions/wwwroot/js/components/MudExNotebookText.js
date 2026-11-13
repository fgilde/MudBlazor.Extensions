import { ZWSP, isDiv, fontFamily, richTextMethods } from './MudExRichText.js';

const RUN_KEYS = ['size', 'color', 'highlight', 'font', 'link'];
const FLAGS = ['b', 'i', 'u', 's'];
const PARAGRAPH_KEYS = ['align', 'heading', 'list', 'indent'];
const SHORTCUT_FLAGS = ['b', 'i', 'u'];
const ALIGN = { l: 'left', e: 'center', r: 'right' };
const BLOCKED = ['insertFromDrop', 'insertFromPaste', 'insertHTML', 'insertLink', 'historyUndo', 'historyRedo'];
const AUTO = [['* ', 'bullet'], ['1. ', 'number'], ['[] ', 'todo']];
const BULLETS = ['•', '◦', '▪'];
const HEADING_PT = [11, 20, 16, 14];
const INDENT_PX = 24;
const MAX_INDENT = 8;
const ROMAN = [[1000, 'm'], [900, 'cm'], [500, 'd'], [400, 'cd'], [100, 'c'], [90, 'xc'], [50, 'l'], [40, 'xl'], [10, 'x'], [9, 'ix'], [5, 'v'], [4, 'iv'], [1, 'i']];

const num = value => value == null || value === '' ? null : +value;
const level = div => Math.min(MAX_INDENT, Math.max(0, +div.dataset.indent || 0));
const plain = text => text.split(ZWSP).join('').replace(/ /g, ' ');
const letters = n => {
    let text = '';
    for (; n > 0; n = Math.floor((n - 1) / 26)) {
        text = String.fromCharCode(97 + (n - 1) % 26) + text;
    }
    return text;
};
const roman = n => {
    if (n >= 4000) {
        return String(n);
    }
    let text = '';
    ROMAN.forEach(([value, numeral]) => {
        for (; n >= value; n -= value) {
            text += numeral;
        }
    });
    return text;
};
const label = (n, style) => style === 1 ? letters(n) : style === 2 ? roman(n) : String(n);

class MudExNotebookText {
    constructor(root, dotnet, options) {
        this.root = root;
        this.dotnet = dotnet;
        this.canvas = root.closest('.mud-ex-notebook-canvas');
        this.box = root.closest('.mud-ex-notebook-text-editor') ?? root;
        this.id = options?.id || null;
        this.cell = options?.cell || null;
        this.tableTab = !!options?.tableTab;
        this.element = this.id ? this.canvas?.querySelector(`[data-element-id="${this.id}"]`) : null;
        if (this.cell) {
            this.element = this.placeInCell(this.element?.querySelector(`td[data-cell="${this.cell}"]`));
        }
        this.element?.classList.add('mud-ex-notebook-editing');
        this.composing = false;
        this.commitPending = false;
        this.commitFocus = false;
        this.done = false;
        this.typed = false;
        this.range = null;
        this.linkRange = null;
        this.paragraphTemplate = Object.assign(document.createElement('div'), { className: 'mud-ex-notebook-paragraph' });
        this.template = document.createElement('span');
        this.build(options?.paragraphs || []);
        this.events = [
            [root, 'keydown', e => this.keyDown(e)], [root, 'beforeinput', e => this.beforeInput(e)], [root, 'input', e => this.input(e)],
            [root, 'paste', e => this.paste(e)], [root, 'drop', e => e.preventDefault()], [root, 'pointerdown', e => this.pointerDown(e)],
            [root, 'compositionstart', () => { this.composing = true; }], [root, 'compositionend', () => this.compositionEnd()],
            [document, 'selectionchange', () => this.remember()], [this.canvas ?? document, 'pointerdown', e => this.outside(e), true]
        ];
        this.events.forEach(([target, type, handler, capture]) => target.addEventListener(type, handler, !!capture));
        this.renumber();
        this.start();
    }

    build(paragraphs) {
        this.root.replaceChildren(...paragraphs.map(p => {
            const div = this.paragraphTemplate.cloneNode(false);
            if (p.source != null) {
                div.dataset.p = p.source;
            }
            this.setParagraph(div, p);
            const runs = p.runs?.length ? p.runs : [{ text: '' }];
            runs.forEach(r => {
                const span = document.createElement('span');
                if (r.source != null) {
                    span.dataset.r = r.source;
                }
                this.setRun(span, r);
                span.textContent = r.text || ZWSP;
                div.append(span);
            });
            this.paint(div);
            return div;
        }));
    }

    setParagraph(div, p) {
        if (p.list) {
            div.dataset.list = p.list;
        }
        if (p.indent) {
            div.dataset.indent = Math.min(MAX_INDENT, Math.max(0, +p.indent || 0));
        }
        if (p.checked) {
            div.dataset.checked = '1';
        }
        if (p.align && p.align !== 'left') {
            div.dataset.align = p.align;
        }
        if (p.heading) {
            div.dataset.heading = p.heading;
        }
    }

    setRun(span, r) {
        FLAGS.filter(k => r[k]).forEach(k => span.dataset[k] = '1');
        RUN_KEYS.filter(k => r[k] != null && r[k] !== '').forEach(k => span.dataset[k] = r[k]);
    }

    start() {
        this.root.focus({ preventScroll: true });
        const typed = this.canvas?.mudExTyped;
        const caret = this.canvas?.mudExCaret;
        if (this.canvas) {
            this.canvas.mudExTyped = null;
            this.canvas.mudExCaret = null;
        }
        const hit = caret ? this.pointRange(caret[0], caret[1]) : null;
        const last = this.lastText(this.root);
        if (hit) {
            this.select(hit);
        } else if (last) {
            this.caret(last, last.length);
        }
        if (typed?.text && typed.id === this.id) {
            this.insertText(typed.text);
        }
    }

    paint(div) {
        const heading = +div.dataset.heading || 0;
        const indent = level(div);
        div.className = heading ? `mud-ex-notebook-paragraph mud-ex-notebook-h${heading}` : 'mud-ex-notebook-paragraph';
        div.style.textAlign = div.dataset.align || 'left';
        div.style.paddingLeft = indent ? `${indent * INDENT_PX}px` : '';
        div.querySelectorAll('span').forEach(span => this.paintRun(span, heading));
    }

    paintRun(span, heading) {
        const d = span.dataset;
        const style = span.style;
        style.fontSize = `${(num(d.size) ?? HEADING_PT[heading] ?? HEADING_PT[0]) * 4 / 3}px`;
        style.fontWeight = d.b === '1' ? 'bold' : '';
        style.fontStyle = d.i === '1' ? 'italic' : '';
        style.textDecoration = [d.u === '1' && 'underline', d.s === '1' && 'line-through'].filter(Boolean).join(' ');
        style.color = d.color || '';
        style.backgroundColor = d.highlight || '';
        style.fontFamily = d.font ? fontFamily(d.font) : '';
        span.classList.toggle('mud-ex-notebook-link', !!d.link);
    }

    renumber() {
        const counters = new Array(MAX_INDENT + 1).fill(0);
        [...this.root.children].filter(isDiv).forEach(div => {
            const d = div.dataset;
            const at = level(div);
            counters.fill(0, !d.list ? 0 : d.list === 'number' ? at + 1 : at);
            const marker = d.list === 'number' ? `${label(++counters[at], at % 3)}.`
                : d.list === 'bullet' ? BULLETS[at % 3]
                    : d.list === 'todo' ? (d.checked === '1' ? '☑' : '☐') : null;
            if (marker == null) {
                delete d.marker;
            } else if (d.marker !== marker) {
                d.marker = marker;
            }
        });
    }

    read() {
        const paragraphs = [];
        let loose = null;
        this.root?.childNodes.forEach(node => {
            if (isDiv(node)) {
                loose = null;
                const runs = [];
                node.childNodes.forEach(n => this.collect(n, runs));
                paragraphs.push(this.paragraphDto(node, runs));
                return;
            }
            if (!loose) {
                loose = { source: null, list: null, indent: 0, checked: false, align: null, heading: 0, runs: [] };
                paragraphs.push(loose);
            }
            this.collect(node, loose.runs);
        });
        return paragraphs;
    }

    paragraphDto(div, runs) {
        const d = div.dataset;
        return { source: num(d.p), list: d.list || null, indent: level(div), checked: d.checked === '1', align: d.align || null, heading: +d.heading || 0, runs };
    }

    collect(node, runs) {
        if (node.nodeType === Node.TEXT_NODE) {
            runs.push({ source: null, text: node.data });
            return;
        }
        if (node.nodeType !== Node.ELEMENT_NODE || node.tagName === 'BR') {
            return;
        }
        if (node.tagName === 'SPAN') {
            runs.push(this.runOf(node));
            return;
        }
        node.childNodes.forEach(n => this.collect(n, runs));
    }

    runOf(span) {
        const d = span.dataset;
        const run = { source: num(d.r), text: span.textContent, size: num(d.size) };
        FLAGS.forEach(k => run[k] = d[k] === '1');
        ['color', 'highlight', 'font', 'link'].forEach(k => run[k] = d[k] || null);
        return run;
    }

    report() {
        clearTimeout(this.reportTimer);
        this.reportTimer = setTimeout(() => {
            const range = this.range;
            if (!range || this.done || !this.dotnet || !this.root?.contains(range.startContainer)) {
                return;
            }
            const selected = this.paragraphs(range).map(div => {
                const spans = [...div.querySelectorAll('span')];
                const hit = spans.filter(s => range.collapsed ? s.contains(range.startContainer) : range.intersectsNode(s));
                return this.paragraphDto(div, (hit.length ? hit : spans.slice(0, 1)).map(s => this.runOf(s)));
            });
            this.dotnet.invokeMethodAsync('OnSelectionFormatAsync', selected).catch(() => {});
        }, 150);
    }

    keyDown(e) {
        if (e.isComposing || this.composing || e.keyCode === 229) {
            return;
        }
        const ctrl = e.ctrlKey || e.metaKey;
        const key = e.key.length === 1 ? e.key.toLowerCase() : e.key;
        if (ctrl && !e.altKey && key === 's') {
            e.preventDefault();
            return;
        }
        if (ctrl && !e.altKey && (key === 'z' || key === 'y')) {
            e.preventDefault();
            this.commit(true);
            return;
        }
        e.stopPropagation();
        if (e.key === 'Escape') {
            e.preventDefault();
            this.commit(true);
        } else if (e.key === 'Tab' && !ctrl && !e.altKey) {
            e.preventDefault();
            if (this.cell || (this.tableTab && !e.shiftKey && this.tableable())) {
                this.tab(e.shiftKey);
            } else {
                this.format('indent', e.shiftKey ? '-1' : '1');
            }
        } else if (e.altKey && e.shiftKey && !ctrl && (e.key === 'ArrowUp' || e.key === 'ArrowDown')) {
            e.preventDefault();
            this.move(e.key === 'ArrowUp');
        } else if (ctrl && !e.altKey && SHORTCUT_FLAGS.includes(key)) {
            e.preventDefault();
            this.format(key, null);
        } else if (ctrl && !e.altKey && ALIGN[key]) {
            e.preventDefault();
            this.format('align', ALIGN[key]);
        } else if (ctrl && !e.altKey && key === '1') {
            e.preventDefault();
            this.format('list', 'todo');
        } else if (ctrl && !e.altKey && key === 'k') {
            e.preventDefault();
            this.requestLink();
        }
    }

    beforeInput(e) {
        const type = e.inputType || '';
        if (type === 'insertParagraph') {
            e.preventDefault();
            this.enter();
        } else if (type === 'insertLineBreak') {
            e.preventDefault();
            this.insertText('\n');
        } else if (type === 'deleteContentBackward' && this.edge(true)) {
            e.preventDefault();
            this.backspace();
        } else if (type === 'deleteContentForward' && this.edge(false)) {
            e.preventDefault();
            const div = this.edge(false);
            const next = div.nextElementSibling;
            if (isDiv(next)) {
                this.join(div, next);
            }
        } else if (type.startsWith('format') || BLOCKED.includes(type)) {
            e.preventDefault();
        }
    }

    input(e) {
        if (e.isComposing || this.composing) {
            return;
        }
        this.normalize();
        [...this.root.children].filter(d => isDiv(d) && !d.style.textAlign).forEach(d => this.paint(d));
        if (e.inputType === 'insertText' && e.data === ' ') {
            this.autoList();
        }
        this.renumber();
        this.changed();
    }

    pointerDown(e) {
        const div = e.target;
        if (!isDiv(div) || div.parentElement !== this.root || div.dataset.list !== 'todo') {
            return;
        }
        const first = div.querySelector('span');
        if (first && e.clientX >= first.getBoundingClientRect().left) {
            return;
        }
        e.preventDefault();
        div.dataset.checked = div.dataset.checked === '1' ? '0' : '1';
        this.renumber();
        this.changed();
    }

    outside(e) {
        if (!this.box.contains(e.target)) {
            this.commit(false);
        }
    }

    changed() {
        if (!this.dotnet || this.done) {
            return;
        }
        this.report();
        if (!this.typed) {
            this.typed = true;
            this.dotnet.invokeMethodAsync('OnTextInputAsync').catch(() => {});
        }
    }

    edge(start) {
        const range = this.current();
        if (!range || !range.collapsed) {
            return null;
        }
        const div = this.paragraphOf(range.startContainer);
        if (!div) {
            return null;
        }
        const part = document.createRange();
        if (start) {
            part.setStart(div, 0);
            part.setEnd(range.startContainer, range.startOffset);
        } else {
            part.setStart(range.startContainer, range.startOffset);
            part.setEnd(div, div.childNodes.length);
        }
        return plain(part.toString()) === '' ? div : null;
    }

    enter() {
        const range = this.current();
        if (!range) {
            return;
        }
        const div = this.paragraphOf(range.startContainer);
        if (div && range.collapsed && div.dataset.list && !this.visible(div)) {
            delete div.dataset.list;
            delete div.dataset.checked;
            this.paint(div);
            this.renumber();
            this.changed();
            return;
        }
        this.split();
        const next = this.paragraphOf(getSelection().anchorNode);
        if (next && next !== div) {
            delete next.dataset.checked;
            delete next.dataset.p;
            this.paint(next);
        }
        this.renumber();
    }

    backspace() {
        const div = this.edge(true);
        if (div.dataset.list) {
            delete div.dataset.list;
            delete div.dataset.checked;
            this.paint(div);
            this.renumber();
            this.changed();
            return;
        }
        const previous = div.previousElementSibling;
        if (isDiv(previous)) {
            this.join(previous, div);
        }
    }

    join(first, second) {
        const shown = this.visible(first);
        const end = shown ? this.lastText(first) : null;
        const offset = end?.length ?? 0;
        if (!shown) {
            first.replaceChildren();
        }
        if (this.visible(second) || !shown) {
            while (second.firstChild) {
                first.append(second.firstChild);
            }
        }
        second.remove();
        this.fill(first);
        this.paint(first);
        if (end?.isConnected) {
            this.caret(end, offset);
        } else {
            this.caret(this.firstText(first), 0);
        }
        this.renumber();
        this.changed();
    }

    autoList() {
        const range = this.current();
        const div = range?.collapsed ? this.paragraphOf(range.startContainer) : null;
        if (!div || div.dataset.list) {
            return;
        }
        const before = document.createRange();
        before.setStart(div, 0);
        before.setEnd(range.startContainer, range.startOffset);
        const hit = AUTO.find(([prefix]) => plain(before.toString()) === prefix);
        if (!hit) {
            return;
        }
        before.deleteContents();
        div.dataset.list = hit[1];
        if (this.visible(div)) {
            const at = this.textAt(before.startContainer, before.startOffset);
            this.caret(at.text, at.offset);
        } else {
            this.fill(div);
            const text = this.firstText(div);
            this.caret(text, text.length);
        }
        this.paint(div);
    }

    move(up) {
        const range = this.current();
        const divs = range ? this.paragraphs(range) : [];
        if (!divs.length) {
            return;
        }
        const other = up ? divs[0].previousElementSibling : divs[divs.length - 1].nextElementSibling;
        if (!isDiv(other)) {
            return;
        }
        if (up) {
            divs[divs.length - 1].after(other);
        } else {
            divs[0].before(other);
        }
        this.renumber();
        this.changed();
    }

    format(key, value) {
        const range = this.current();
        if (!range) {
            return;
        }
        if (PARAGRAPH_KEYS.includes(key)) {
            const divs = this.paragraphs(range);
            const off = key === 'list' && divs.every(d => d.dataset.list === value);
            divs.forEach(d => {
                if (key === 'indent') {
                    d.dataset.indent = Math.min(MAX_INDENT, Math.max(0, level(d) + (value === '-1' ? -1 : 1)));
                } else if (off || value == null || value === '' || value === '0' || (key === 'align' && value === 'left') || (key === 'list' && value === 'none')) {
                    delete d.dataset[key];
                } else {
                    d.dataset[key] = value;
                }
                this.paint(d);
            });
            this.renumber();
            this.changed();
            return;
        }
        const collapsed = range.collapsed;
        const spans = collapsed ? [this.caretSpan(this.textAt(range.startContainer, range.startOffset))] : this.spans(range);
        if (!spans.length) {
            return;
        }
        const on = value ?? (FLAGS.includes(key) ? (spans.every(s => s.dataset[key] === '1') ? '0' : '1') : null);
        if (on == null) {
            return;
        }
        spans.forEach(s => {
            if (on === '' || (FLAGS.includes(key) && on === '0')) {
                delete s.dataset[key];
            } else {
                s.dataset[key] = on;
            }
            this.paintRun(s, +this.paragraphOf(s)?.dataset.heading || 0);
        });
        this.root.focus({ preventScroll: true });
        if (collapsed) {
            this.caret(spans[0].firstChild, spans[0].firstChild.length);
        } else {
            const next = document.createRange();
            next.setStart(spans[0].firstChild, 0);
            const end = spans[spans.length - 1].lastChild;
            next.setEnd(end, end.length);
            this.select(next);
        }
        this.changed();
    }

    requestLink() {
        const range = this.current();
        if (!range || !this.dotnet || this.done) {
            return;
        }
        this.linkRange = range.cloneRange();
        const host = range.startContainer.nodeType === Node.TEXT_NODE ? this.spanOf(range.startContainer) : null;
        this.dotnet.invokeMethodAsync('OnLinkRequestAsync', plain(range.toString()), host?.dataset.link || null).catch(() => {});
    }

    link(text, href) {
        const range = this.linkRange;
        this.linkRange = null;
        if (!range || this.done || !this.root?.contains(range.startContainer) || !href) {
            return;
        }
        this.root.focus({ preventScroll: true });
        this.select(range);
        const at = this.clear(range);
        const span = this.caretSpan(at);
        delete span.dataset.r;
        span.dataset.link = href;
        span.textContent = text || href;
        this.paintRun(span, +this.paragraphOf(span)?.dataset.heading || 0);
        this.caret(span.firstChild, span.firstChild.length);
        this.changed();
    }

    async paste(e) {
        e.preventDefault();
        const data = e.clipboardData;
        if (!data || !this.dotnet || this.done) {
            return;
        }
        const html = data.getData('text/html');
        const text = data.getData('text/plain');
        let paragraphs = html ? await this.dotnet.invokeMethodAsync('OnPasteHtml', html).catch(() => null) : null;
        if (!paragraphs?.length) {
            paragraphs = text ? text.replace(/\r\n?/g, '\n').split('\n').map(line => ({ runs: [{ text: line }] })) : [];
        }
        if (paragraphs.length) {
            this.insertParagraphs(paragraphs);
        }
    }

    insertParagraphs(paragraphs) {
        const range = this.current();
        if (!range) {
            return;
        }
        const at = this.clear(range);
        const div = this.paragraphOf(at.text);
        if (!div) {
            return;
        }
        const template = this.spanOf(at.text) ?? this.template;
        const tail = document.createRange();
        tail.setStart(at.text, at.offset);
        tail.setEnd(div, div.childNodes.length);
        const rest = tail.extractContents();
        const touched = [div];
        let target = div;
        let last = null;
        paragraphs.forEach((p, index) => {
            if (index > 0) {
                const next = div.cloneNode(false);
                delete next.dataset.p;
                delete next.dataset.checked;
                target.after(next);
                touched.push(target = next);
            }
            if (p.list && (index > 0 || !this.visible(div))) {
                target.dataset.list = p.list;
                target.dataset.indent = Math.min(MAX_INDENT, Math.max(0, +p.indent || 0));
            }
            (p.runs || []).filter(r => r.text).forEach(r => {
                const span = template.cloneNode(false);
                delete span.dataset.r;
                this.setRun(span, r);
                span.textContent = r.text;
                target.append(span);
                last = span;
            });
        });
        const resume = this.firstText(rest);
        target.append(rest);
        touched.forEach(d => {
            this.fill(d);
            this.paint(d);
        });
        if (last?.isConnected && last.lastChild) {
            this.caret(last.lastChild, last.lastChild.length);
        } else if (resume?.isConnected) {
            this.caret(resume, 0);
        }
        this.renumber();
        this.changed();
    }

    placeInCell(td) {
        const page = this.canvas?.querySelector('.mud-ex-notebook-page');
        if (!td || !page) {
            return null;
        }
        const zoom = +this.canvas.dataset.zoom || 1;
        const cell = td.getBoundingClientRect();
        const origin = page.getBoundingClientRect();
        const padding = parseFloat(getComputedStyle(td).paddingLeft) || 0;
        this.box.style.left = `px`;
        this.box.style.top = `px`;
        return td.firstElementChild;
    }

    tableable() {
        const divs = [...this.root.children].filter(isDiv);
        const range = this.current();
        if (divs.length !== 1 || divs[0].dataset.list || level(divs[0]) || !range?.collapsed || !plain(divs[0].textContent).trim()) {
            return false;
        }
        const after = document.createRange();
        after.setStart(range.endContainer, range.endOffset);
        after.setEnd(divs[0], divs[0].childNodes.length);
        return !plain(after.toString());
    }

    tab(back) {
        if (this.done || !this.dotnet) {
            return;
        }
        this.done = true;
        const paragraphs = this.read();
        this.canvas?.focus({ preventScroll: true });
        this.dotnet.invokeMethodAsync('OnTabAsync', paragraphs, back).catch(() => {});
    }

    dispose() {
        clearTimeout(this.reportTimer);
        this.events?.forEach(([target, type, handler, capture]) => target.removeEventListener(type, handler, !!capture));
        this.events = null;
        this.element?.classList.remove('mud-ex-notebook-editing');
        this.done = true;
        this.root = null;
        this.dotnet = null;
    }
}

Object.assign(MudExNotebookText.prototype, richTextMethods);

export function initializeMudExNotebookText(elementRef, dotnet, options) {
    return new MudExNotebookText(elementRef, dotnet, options);
}
