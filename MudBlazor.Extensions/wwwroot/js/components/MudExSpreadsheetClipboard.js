const CONTEXT_IGNORE = 'textarea.mud-ex-ssg-editor.editing, .mud-ex-ssg-assist-host, .mud-ex-ssg-filter, .mud-ex-ssg-dv, .mud-ex-ssg-chart, .mud-ex-ssg-note-box';

export const clipboardMethods = {
    async writeClipboard(text, html) {
        try {
            if (html && window.ClipboardItem && navigator.clipboard?.write) {
                await navigator.clipboard.write([new ClipboardItem({
                    'text/plain': new Blob([text], { type: 'text/plain' }),
                    'text/html': new Blob([html], { type: 'text/html' })
                })]);
            } else {
                await navigator.clipboard.writeText(text);
            }
        } catch {
            const copy = e => {
                e.clipboardData.setData('text/plain', text);
                if (html) {
                    e.clipboardData.setData('text/html', html);
                }
                e.preventDefault();
            };
            document.addEventListener('copy', copy, true);
            try {
                document.execCommand('copy');
            } catch {
            }
            document.removeEventListener('copy', copy, true);
        }
        this.focus();
    },

    paste(e) {
        if (this.isEditing() || this.readOnly() || e.target.closest?.('.mud-ex-ssg-note-box')) {
            return;
        }
        const text = e.clipboardData?.getData('text/plain');
        const html = e.clipboardData?.getData('text/html');
        if (!text && !html) {
            return;
        }
        e.preventDefault();
        const stream = s => DotNet.createJSStreamReference(new Blob([s]));
        this.dotnet?.invokeMethodAsync('PasteStreamAsync', stream(text ?? ''), html ? stream(html) : null).catch(() => {});
    },

    async readClipboard(type) {
        if (type === 'text/html') {
            try {
                for (const item of await navigator.clipboard.read()) {
                    if (item.types.includes('text/html')) {
                        return DotNet.createJSStreamReference(await item.getType('text/html'));
                    }
                }
            } catch {
            }
            return null;
        }
        const text = await navigator.clipboard.readText();
        return DotNet.createJSStreamReference(new Blob([text ?? '']));
    },

    contextMenu(e) {
        const t = e.target;
        if (!this.dotnet || t.closest(CONTEXT_IGNORE)) {
            return;
        }
        const header = t.closest('.mud-ex-ssg-ch, .mud-ex-ssg-rh');
        const cell = header ? null : this.cellAt(e.clientX, e.clientY, false);
        if (!header && !cell) {
            return;
        }
        e.preventDefault();
        this.endEditVisual();
        this.focus();
        const kind = header ? (header.matches('.mud-ex-ssg-ch') ? 'column' : 'row') : 'cell';
        const r = header ? +(header.dataset.r ?? -1) : cell.r;
        const c = header ? +(header.dataset.c ?? -1) : cell.c;
        this.dotnet.invokeMethodAsync('OnContextMenu', kind, r, c, e.clientX, e.clientY).catch(() => {});
    }
};
