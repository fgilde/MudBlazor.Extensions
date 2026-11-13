import { ZWSP, isDiv, fontFamily, richTextMethods } from './MudExRichText.js';

const RUN_KEYS = ['b', 'i', 'u', 's', 'size', 'color', 'font'];
const FLAGS = ['b', 'i', 'u', 's'];
const ALIGN = { l: 'left', e: 'center', r: 'right', j: 'justify' };
const BLOCKED = ['insertFromDrop', 'insertFromPaste', 'insertHTML', 'insertLink', 'historyUndo', 'historyRedo'];

const num = value => value == null || value === '' ? null : +value;

class MudExSlideText {
    constructor(root, dotnet, options) {
        this.root = root;
        this.dotnet = dotnet;
        this.slideWidth = options?.slideWidth || 12192000;
        this.cell = !!options?.cell;
        this.canvas = root.closest('.mud-ex-slide-canvas');
        this.box = root.closest('.mud-ex-slide-text-editor') ?? root;
        this.composing = false;
        this.commitPending = false;
        this.commitFocus = false;
        this.sending = false;
        this.again = false;
        this.done = false;
        this.range = null;
        this.build(options?.paragraphs || []);
        this.paragraphTemplate = root.querySelector('div')?.cloneNode(false) ?? Object.assign(document.createElement('div'), { className: 'mud-ex-slide-paragraph' });
        this.template = root.querySelector('span')?.cloneNode(false) ?? document.createElement('span');
        this.events = [
            [root, 'keydown', e => this.keyDown(e)], [root, 'beforeinput', e => this.beforeInput(e)], [root, 'input', e => this.input(e)],
            [root, 'paste', e => this.paste(e)], [root, 'drop', e => e.preventDefault()],
            [root, 'compositionstart', () => { this.composing = true; }], [root, 'compositionend', () => this.compositionEnd()],
            [document, 'selectionchange', () => this.remember()], [this.canvas ?? document, 'pointerdown', e => this.outside(e), true]
        ];
        this.events.forEach(([target, type, handler, capture]) => target.addEventListener(type, handler, !!capture));
        this.start(options || {});
    }

    build(paragraphs) {
        this.root.replaceChildren(...paragraphs.map(p => {
            const div = document.createElement('div');
            div.className = 'mud-ex-slide-paragraph';
            if (p.source != null) {
                div.dataset.p = p.source;
            }
            if (p.align) {
                div.dataset.align = p.align;
            }
            if (p.bullet) {
                div.dataset.bullet = p.bullet;
            }
            if (p.marker) {
                div.dataset.marker = p.marker;
            }
            div.style.cssText = p.style || '';
            (p.runs || []).forEach(r => {
                const span = document.createElement('span');
                if (r.source != null) {
                    span.dataset.r = r.source;
                }
                RUN_KEYS.forEach(k => {
                    if (r[k] != null) {
                        span.dataset[k] = r[k];
                    }
                });
                span.style.cssText = r.style || '';
                span.textContent = r.text || ZWSP;
                div.append(span);
            });
            return div;
        }));
    }

    start(options) {
        this.root.focus({ preventScroll: true });
        const first = this.firstText(this.root);
        const last = this.lastText(this.root);
        const typed = this.canvas?.mudExTyped?.text;
        if (this.canvas) {
            this.canvas.mudExTyped = null;
        }
        if (options.text != null && first && last) {
            const all = document.createRange();
            all.setStart(first, 0);
            all.setEnd(last, last.length);
            this.select(all);
            this.insertText(typed?.startsWith(options.text) ? typed : options.text);
            return;
        }
        const caret = options.caret;
        const hit = caret ? this.pointRange(caret[0], caret[1]) : null;
        if (hit) {
            this.select(hit);
        } else if (last) {
            this.caret(last, last.length);
        }
    }

    read() {
        const paragraphs = [];
        let loose = null;
        this.root.childNodes.forEach(node => {
            if (isDiv(node)) {
                loose = null;
                const runs = [];
                node.childNodes.forEach(n => this.collect(n, runs));
                paragraphs.push({ source: num(node.dataset.p), align: node.dataset.align ?? null, bullet: node.dataset.bullet ?? null, runs });
                return;
            }
            if (!loose) {
                loose = { source: null, align: null, bullet: null, runs: [] };
                paragraphs.push(loose);
            }
            this.collect(node, loose.runs);
        });
        return paragraphs;
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
        const run = { source: num(span.dataset.r), text: span.textContent };
        RUN_KEYS.forEach(k => {
            run[k] = span.dataset[k] ?? null;
        });
        return run;
    }

    report() {
        clearTimeout(this.reportTimer);
        this.reportTimer = setTimeout(() => {
            const range = this.range;
            if (!range || this.done || !this.dotnet || !this.root.contains(range.startContainer)) {
                return;
            }
            const selected = this.paragraphs(range).map(div => {
                const spans = [...div.querySelectorAll('span')];
                const hit = spans.filter(s => range.collapsed ? s.contains(range.startContainer) : range.intersectsNode(s));
                const runs = (hit.length ? hit : spans.slice(0, 1)).map(s => this.runOf(s));
                return { source: num(div.dataset.p), align: div.dataset.align ?? null, bullet: div.dataset.bullet ?? null, runs };
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
        } else if (e.key === 'Tab' && !ctrl && !e.altKey && this.cell) {
            e.preventDefault();
            this.tab(e.shiftKey);
        } else if (e.key === 'Tab' && !ctrl && !e.altKey) {
            e.preventDefault();
            this.insertText('\t');
        } else if (ctrl && !e.altKey && FLAGS.includes(key) && key !== 's') {
            e.preventDefault();
            this.format(key, null);
        } else if (ctrl && !e.altKey && ALIGN[key]) {
            e.preventDefault();
            this.format('align', ALIGN[key]);
        }
    }

    beforeInput(e) {
        const type = e.inputType || '';
        if (type === 'insertParagraph' || type === 'insertLineBreak') {
            e.preventDefault();
            if (type === 'insertParagraph') {
                this.split();
            } else {
                this.insertText('\n');
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
        if (this.sending) {
            this.again = true;
            return;
        }
        this.sending = true;
        this.dotnet.invokeMethodAsync('OnTextInputAsync', this.read()).catch(() => {}).finally(() => {
            this.sending = false;
            if (this.again) {
                this.again = false;
                this.changed();
            }
        });
    }

    tab(back) {
        if (this.done || !this.dotnet) {
            return;
        }
        this.done = true;
        const paragraphs = this.read();
        this.canvas?.focus({ preventScroll: true });
        this.dotnet.invokeMethodAsync('OnTextTabAsync', paragraphs, back).catch(() => {});
    }

    format(key, value) {
        const range = this.current();
        if (!range) {
            return;
        }
        if (key === 'align' || key === 'bullet') {
            const divs = this.paragraphs(range);
            const on = value ?? (divs.every(d => d.dataset.bullet === '1') ? '0' : '1');
            divs.forEach(d => {
                d.dataset[key] = on;
                if (key === 'align') {
                    d.style.textAlign = on;
                }
            });
            this.changed();
            return;
        }
        if (value == null && !FLAGS.includes(key)) {
            return;
        }
        const collapsed = range.collapsed;
        const spans = collapsed ? [this.caretSpan(this.textAt(range.startContainer, range.startOffset))] : this.spans(range);
        if (!spans.length) {
            return;
        }
        const on = value ?? (spans.every(s => this.flag(s, key)) ? '0' : '1');
        spans.forEach(s => this.apply(s, key, on));
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

    flag(span, key) {
        const style = getComputedStyle(span);
        switch (key) {
            case 'b':
                return parseInt(style.fontWeight, 10) >= 600;
            case 'i':
                return style.fontStyle !== 'normal';
            case 'u':
                return (style.textDecorationLine || '').includes('underline');
            case 's':
                return (style.textDecorationLine || '').includes('line-through');
            default:
                return false;
        }
    }

    apply(span, key, value) {
        span.dataset[key] = value;
        const style = span.style;
        const on = value === '1';
        switch (key) {
            case 'b':
                style.fontWeight = on ? 'bold' : 'normal';
                break;
            case 'i':
                style.fontStyle = on ? 'italic' : 'normal';
                break;
            case 'u':
            case 's': {
                const underline = key === 'u' ? on : this.flag(span, 'u');
                const strike = key === 's' ? on : this.flag(span, 's');
                style.textDecoration = [underline && 'underline', strike && 'line-through'].filter(Boolean).join(' ') || 'none';
                break;
            }
            case 'size':
                style.fontSize = `${+value * 12700 / this.slideWidth * 100}cqw`;
                break;
            case 'color':
                style.color = value;
                break;
            case 'font': {
                style.fontFamily = fontFamily(value);
                break;
            }
        }
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
                target.after(next);
                touched.push(target = next);
            }
            (p.runs || []).filter(r => r.text).forEach(r => {
                const span = template.cloneNode(false);
                span.textContent = r.text;
                target.append(span);
                ['b', 'i', 'u'].filter(k => r[k] === '1').forEach(k => this.apply(span, k, '1'));
                if (r.color) {
                    this.apply(span, 'color', r.color);
                }
                last = span;
            });
        });
        const resume = this.firstText(rest);
        target.append(rest);
        touched.forEach(d => this.fill(d));
        if (last?.isConnected && last.lastChild) {
            this.caret(last.lastChild, last.lastChild.length);
        } else if (resume?.isConnected) {
            this.caret(resume, 0);
        }
        this.changed();
    }

    dispose() {
        clearTimeout(this.reportTimer);
        this.events?.forEach(([target, type, handler, capture]) => target.removeEventListener(type, handler, !!capture));
        this.events = null;
        this.done = true;
        this.root = null;
        this.dotnet = null;
    }
}

Object.assign(MudExSlideText.prototype, richTextMethods);

export function initializeMudExSlideText(elementRef, dotnet, options) {
    return new MudExSlideText(elementRef, dotnet, options);
}
