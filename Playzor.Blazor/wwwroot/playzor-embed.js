/*
 * <playzor-playground> — the Playzor embed as a plain web component, for pages that are not
 * Blazor apps.
 *
 *   <script type="module" src="https://playzor.net/_content/Playzor.Blazor/playzor-embed.js"></script>
 *
 *   <playzor-playground height="420px" view="split">
 *     <script type="text/plain" data-playzor-file="__Main.razor">
 *       <h3>Hello Playzor</h3>
 *     </script>
 *   </playzor-playground>
 *
 * A script child per file is the reliable way to pass razor: the browser does not parse its
 * content, so markup, generics and @code blocks survive verbatim. Plain text content works for
 * one liners without markup, and code, files (json) and snippet-id attributes work as well.
 * The snippet is deflated and base64url encoded into the url — nothing is uploaded.
 */

const SEPARATOR = String.fromCharCode(31);
const DEFAULT_HOST = 'https://playzor.net';

/** Removes the indentation the snippet carries from the surrounding html. */
function dedent(text) {
    const lines = (text ?? '').replace(/\t/g, '    ').split('\n');
    while (lines.length && !lines[0].trim()) lines.shift();
    while (lines.length && !lines[lines.length - 1].trim()) lines.pop();

    const indent = Math.min(...lines.filter(l => l.trim()).map(l => l.length - l.trimStart().length), Infinity);
    return Number.isFinite(indent) && indent > 0 ? lines.map(l => l.slice(indent)).join('\n') : lines.join('\n');
}

function base64Url(bytes) {
    let binary = '';
    for (const b of bytes) binary += String.fromCharCode(b);
    return btoa(binary).replace(/=+$/, '').replace(/\+/g, '-').replace(/\//g, '_');
}

async function deflateRaw(text) {
    if (typeof CompressionStream !== 'function') {
        throw new Error('This browser cannot compress the snippet. Use the snippet-id attribute instead.');
    }

    const stream = new Blob([new TextEncoder().encode(text)]).stream().pipeThrough(new CompressionStream('deflate-raw'));
    return new Uint8Array(await new Response(stream).arrayBuffer());
}

/** Same format as Playzor.Core.InlineCode: path, content, path, content … joined by a unit separator. */
export async function encodeFiles(files) {
    const parts = [];
    for (const [path, content] of Object.entries(files)) parts.push(path, content ?? '');
    return base64Url(await deflateRaw(parts.join(SEPARATOR)));
}

/** Builds the embed url without rendering anything — useful to hand a link to the user. */
export async function buildEmbedUrl(options = {}) {
    const host = (options.host || DEFAULT_HOST).replace(/\/+$/, '');
    const path = options.snippetId
        ? options.snippetId
        : await encodeFiles(options.files && Object.keys(options.files).length
            ? options.files
            : { '__Main.razor': options.code ?? '<h1>Hello Playzor</h1>' });

    const query = new URLSearchParams();
    if (options.view && options.view !== 'split') query.set('view', options.view);
    if (options.file) query.set('file', options.file);
    if (options.readOnly) query.set('readonly', 'true');
    if (options.autoRun === false) query.set('autorun', 'false');
    if (options.theme && options.theme !== 'auto') query.set('theme', options.theme);
    if (options.hideHeader) query.set('hideheader', 'true');

    const search = query.toString();
    return `${host}/embed/${path}${search ? '?' + search : ''}`;
}

class PlayzorPlaygroundElement extends HTMLElement {
    static get observedAttributes() {
        return ['code', 'files', 'snippet-id', 'host', 'view', 'theme', 'file',
                'readonly', 'autorun', 'hide-header', 'height', 'auto-height'];
    }

    #iframe = null;
    #onMessage = null;
    #initialCode = null;
    #inlineFiles = null;

    connectedCallback() {
        // the light dom is read once: rendering moves nothing out of it, but attribute changes
        // must not re-read a tree the page may have replaced meanwhile
        if (this.#inlineFiles === null) this.#inlineFiles = this.#readInlineFiles();
        if (this.#initialCode === null) this.#initialCode = dedent(this.textContent);

        if (!this.#iframe) {
            const shadow = this.attachShadow({ mode: 'open' });
            const style = document.createElement('style');
            style.textContent = ':host{display:block}iframe{width:100%;border:0;display:block}';

            this.#iframe = document.createElement('iframe');
            this.#iframe.setAttribute('title', this.getAttribute('title') || 'Playzor playground');
            this.#iframe.setAttribute('loading', 'lazy');
            this.#iframe.setAttribute('allow', 'clipboard-write');

            shadow.append(style, this.#iframe);
        }

        this.#applyHeight();
        this.#observeHeight();
        this.#render();
    }

    disconnectedCallback() {
        if (this.#onMessage) {
            window.removeEventListener('message', this.#onMessage);
            this.#onMessage = null;
        }
    }

    attributeChangedCallback() {
        if (!this.#iframe) return;
        this.#applyHeight();
        this.#render();
    }

    /** The url the iframe currently points at. */
    get src() {
        return this.#iframe?.src ?? '';
    }

    #bool(name) {
        const value = this.getAttribute(name);
        return value !== null && value !== 'false';
    }

    /** One script child per file — the only way markup and @code survive the html parser. */
    #readInlineFiles() {
        const scripts = this.querySelectorAll('script[type="text/plain"][data-playzor-file]');
        if (!scripts.length) return null;

        const files = {};
        for (const script of scripts) files[script.dataset.playzorFile] = dedent(script.textContent);
        return files;
    }

    #applyHeight() {
        this.#iframe.style.height = this.#bool('auto-height') ? '' : (this.getAttribute('height') || '500px');
    }

    #observeHeight() {
        if (!this.#bool('auto-height') || this.#onMessage) return;

        this.#onMessage = (event) => {
            if (event.source !== this.#iframe.contentWindow) return;
            const data = event.data;
            if (!data || data.__playzor !== 'resize') return;

            const height = Number(data.height);
            if (Number.isFinite(height) && height > 0) this.#iframe.style.height = Math.ceil(height) + 'px';
        };

        window.addEventListener('message', this.#onMessage);
    }

    async #render() {
        let files = this.#inlineFiles;
        const raw = this.getAttribute('files');
        if (raw) {
            try {
                files = JSON.parse(raw);
            } catch {
                console.warn('playzor-playground: files is not valid json');
            }
        }

        try {
            const url = await buildEmbedUrl({
                host: this.getAttribute('host'),
                snippetId: this.getAttribute('snippet-id'),
                files,
                code: this.getAttribute('code') ?? this.#initialCode,
                view: this.getAttribute('view'),
                theme: this.getAttribute('theme'),
                file: this.getAttribute('file'),
                readOnly: this.#bool('readonly'),
                autoRun: this.getAttribute('autorun') !== 'false',
                hideHeader: this.#bool('hide-header'),
            });

            if (this.#iframe.src !== url) this.#iframe.src = url;
        } catch (e) {
            console.error('playzor-playground:', e.message);
        }
    }
}

if (!customElements.get('playzor-playground')) {
    customElements.define('playzor-playground', PlayzorPlaygroundElement);
}

export { PlayzorPlaygroundElement };
