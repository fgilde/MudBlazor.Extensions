import { ZWSP } from './MudExRichText.js';
import { eventClipboardData, readClipboardData } from './MudExOfficeClipboard.js';
import { PARAGRAPH } from './MudExDocumentDom.js';

export const CLIPBOARD = new Set(['copy', 'cut', 'paste']);

const TAGS = new Set(['p', 'span', 'table', 'colgroup', 'col', 'tbody', 'tr', 'td']);
const ATTRIBUTES = ['style', 'colspan', 'rowspan'];
const NBSP = String.fromCharCode(160);

const escape = text => text.split('&').join('&amp;').split('<').join('&lt;').split('>').join('&gt;').split('"').join('&quot;').split(NBSP).join('&nbsp;');

const plain = node => {
    if (node.nodeType === Node.TEXT_NODE) {
        return node.data.split(ZWSP).join('');
    }
    const kind = node.dataset?.k;
    if (kind === 'l' || kind === 'e' || kind === 'f' || kind === 'i' || node.hidden) {
        return '';
    }
    if (kind === 'v') {
        return '\n';
    }
    return [...node.childNodes].map(plain).join('');
};

const markup = node => {
    if (node.nodeType === Node.TEXT_NODE) {
        return escape(node.data.split(ZWSP).join(''));
    }
    if (node.nodeType !== Node.ELEMENT_NODE || node.hidden) {
        return '';
    }
    const kind = node.dataset?.k;
    if (kind === 'v') {
        return '<br>';
    }
    if (kind === 'e' || kind === 'f' || kind === 'i') {
        return '';
    }
    const children = [...node.childNodes].map(markup).join('');
    const tag = kind === 'l' ? 'span' : node.localName;
    if (!TAGS.has(tag)) {
        return children;
    }
    const attributes = ATTRIBUTES.filter(name => node.hasAttribute(name)).map(name => ` ${name}="${escape(node.getAttribute(name))}"`).join('');
    return `<${tag}${attributes}>${children}</${tag}>${kind === 'l' ? '&nbsp;' : ''}`;
};

const clipId = () => [...crypto.getRandomValues(new Uint8Array(16))].map(b => b.toString(16).padStart(2, '0')).join('');

export const clipboardMethods = {
    copyEvent(e, cut) {
        const selection = document.getSelection();
        if (this.disposed || this.broken || this.composing || !selection?.rangeCount || selection.isCollapsed || !e.clipboardData
            || !this.sheet.contains(selection.anchorNode) || !this.sheet.contains(selection.focusNode)) {
            return;
        }
        const model = this.readSelection();
        if (cut && (this.readOnly || !model)) {
            e.preventDefault();
            return;
        }
        const fragment = selection.getRangeAt(0).cloneContents();
        const paragraphs = [...fragment.querySelectorAll(PARAGRAPH)];
        const clip = clipId();
        e.clipboardData.setData('text/plain', (paragraphs.length ? paragraphs : [fragment]).map(plain).join('\r\n'));
        e.clipboardData.setData('text/html', `<div data-mudex-clip="${clip}">${[...fragment.childNodes].map(markup).join('')}</div>`);
        e.preventDefault();
        e.stopPropagation();
        if (model) {
            this.command(cut ? 'cut' : 'copy', clip, { selection: model });
        }
    },

    pasteEvent(e) {
        if (this.disposed || this.broken || this.composing) {
            return;
        }
        e.preventDefault();
        e.stopPropagation();
        if (!this.readOnly) {
            this.command('paste', null, { paste: eventClipboardData(e) });
        }
    },

    copy(cut) {
        if (this.disposed) {
            return;
        }
        this.focus();
        try {
            document.execCommand(cut ? 'cut' : 'copy');
        } catch {
        }
    },

    async paste() {
        if (this.disposed || this.readOnly) {
            return;
        }
        this.focus();
        const data = await readClipboardData();
        if (!this.disposed) {
            this.command('paste', null, { paste: data });
        }
    },

    async clipboardCall(sync, name, text, paste) {
        if (name === 'paste') {
            const stream = blob => blob?.size ? DotNet.createJSStreamReference(blob) : null;
            const part = value => value ? new Blob([value]) : null;
            return this.dotnet.invokeMethodAsync('OnPasteAsync', sync, stream(part(paste.html)), stream(part(paste.text)), stream(paste.image), paste.image?.type ?? null, paste.clip ?? null);
        }
        return this.dotnet.invokeMethodAsync('OnCopyAsync', sync, text, name === 'cut');
    },

    async flushed(message) {
        await this.take(await this.send(message, null, null), message, false);
        return { sync: { ...message.sync, typed: [], seen: this.seen, selection: this.readSelection() }, sent: new Map() };
    }
};
