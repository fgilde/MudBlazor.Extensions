const CLIP = /data-mudex-clip="([0-9a-f]{32})"/;

export function clipOf(html) {
    return CLIP.exec(html ?? '')?.[1] ?? null;
}

function clipHtml(text, clip) {
    const root = document.createElement('div');
    root.setAttribute('data-mudex-clip', clip);
    text.split(/\r?\n/).forEach(line => root.appendChild(document.createElement('div')).textContent = line);
    return root.outerHTML;
}

export function eventClipboardData(e) {
    const data = e.clipboardData;
    return { text: data ? data.getData('text/plain') : null, image: [...(data?.files ?? [])].find(f => f.type.startsWith('image/')), clip: clipOf(data?.getData('text/html')) };
}

export async function readClipboardData() {
    let text = null;
    let image = null;
    let clip = null;
    try {
        for (const item of await navigator.clipboard.read()) {
            const type = item.types.find(t => t.startsWith('image/'));
            if (type && !image) {
                image = await item.getType(type);
            }
            if (text === null && item.types.includes('text/plain')) {
                text = await (await item.getType('text/plain')).text();
            }
            if (clip === null && item.types.includes('text/html')) {
                clip = clipOf(await (await item.getType('text/html')).text());
            }
        }
    } catch {
        try {
            text = await navigator.clipboard.readText();
        } catch {
            text = null;
        }
    }
    return { text, image, clip };
}

export function sendPaste(dotnet, { text, image, clip }) {
    const stream = blob => blob ? DotNet.createJSStreamReference(blob) : null;
    dotnet?.invokeMethodAsync('OnPasteAsync', stream(text === null || text === undefined ? null : new Blob([text])), stream(image), image?.type ?? null, clip)
        .catch(() => {});
}

export async function writeClipboardData(text, clip) {
    const html = clipHtml(text, clip);
    try {
        await navigator.clipboard.write([new ClipboardItem({
            'text/plain': new Blob([text], { type: 'text/plain' }),
            'text/html': new Blob([html], { type: 'text/html' })
        })]);
        return true;
    } catch {
    }
    let written = false;
    const copy = e => {
        e.clipboardData.setData('text/plain', text);
        e.clipboardData.setData('text/html', html);
        e.preventDefault();
        written = true;
    };
    document.addEventListener('copy', copy, true);
    try {
        document.execCommand('copy');
    } catch {
    }
    document.removeEventListener('copy', copy, true);
    if (written) {
        return true;
    }
    try {
        await navigator.clipboard.writeText(text);
    } catch {
    }
    return false;
}
