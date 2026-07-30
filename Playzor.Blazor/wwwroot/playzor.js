// Sizes a playzor embed to its content. The embed posts {__playzor:'resize', height}
// whenever its own content resizes; only messages coming from that iframe are honoured.

const handlers = new WeakMap();

export function observeHeight(iframe) {
    if (!iframe || handlers.has(iframe)) { return; }

    const handler = (event) => {
        if (event.source !== iframe.contentWindow) { return; }
        const data = event.data;
        if (!data || data.__playzor !== 'resize') { return; }

        const height = Number(data.height);
        if (Number.isFinite(height) && height > 0) {
            iframe.style.height = Math.ceil(height) + 'px';
        }
    };

    handlers.set(iframe, handler);
    window.addEventListener('message', handler);
}

export function stopObserving(iframe) {
    const handler = handlers.get(iframe);
    if (!handler) { return; }
    window.removeEventListener('message', handler);
    handlers.delete(iframe);
}
