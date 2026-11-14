const STAGE = '.mud-ex-slide-show-stage';
const CONTROLS = 'button, a, input, .mud-alert, .mud-ex-slide-show-presenter-bar, .mud-ex-slide-show-presenter-side, .mud-ex-slide-show-presenter-nav';
const NEXT = ['ArrowRight', 'ArrowDown', ' ', 'PageDown', 'n', 'N', 'Enter'];
const PREVIOUS = ['ArrowLeft', 'ArrowUp', 'Backspace', 'PageUp', 'p', 'P'];
const BLANK = { b: 'black', B: 'black', '.': 'black', w: 'white', W: 'white', ',': 'white' };

function adopt(source, twins) {
    const copy = source.cloneNode(true);
    const pair = (a, b) => {
        twins.set(a, b);
        a.childNodes.forEach((child, i) => pair(child, b.childNodes[i]));
    };
    pair(source, copy);
    return copy;
}

function reconcile(source, target, twins) {
    if (source.nodeType !== 1) {
        if (target.data !== source.data) {
            target.data = source.data;
        }
        return;
    }
    for (const { name, value } of [...source.attributes]) {
        if (target.getAttribute(name) !== value) {
            target.setAttribute(name, value);
        }
    }
    for (const { name } of [...target.attributes]) {
        if (!source.hasAttribute(name)) {
            target.removeAttribute(name);
        }
    }
    const wanted = [...source.childNodes].map(child => {
        const twin = twins.get(child);
        if (twin?.parentNode !== target || twin.nodeName !== child.nodeName) {
            return adopt(child, twins);
        }
        reconcile(child, twin, twins);
        return twin;
    });
    const keep = new Set(wanted);
    [...target.childNodes].filter(node => !keep.has(node)).forEach(node => target.removeChild(node));
    wanted.forEach((node, i) => target.childNodes[i] !== node && target.insertBefore(node, target.childNodes[i] ?? null));
}

function takeLaunch(root) {
    for (let node = root?.parentElement; node; node = node.parentElement) {
        if (node.mudExShowLaunch) {
            const launch = node.mudExShowLaunch;
            delete node.mudExShowLaunch;
            return launch;
        }
    }
    return null;
}

export function mirrorInto(source, container, twins) {
    const twin = twins.get(source);
    if (twin?.parentNode === container && container.childNodes.length === 1) {
        reconcile(source, twin, twins);
    } else {
        container.replaceChildren(adopt(source, twins));
    }
}

class MudExSlideShow {
    constructor(root, dotnet) {
        this.root = root;
        this.dotnet = dotnet;
        this.number = '';
        this.fullscreen = null;
        this.popup = null;
        this.frame = 0;
        this.twins = new WeakMap();
        this.launch = takeLaunch(root);
        this.focused = this.launch?.focus ?? (root.contains(document.activeElement) ? null : document.activeElement);
        this.onKey = e => this.keyDown(e);
        this.onClick = e => this.click(e);
        this.onCancel = e => {
            e.preventDefault();
            this.send('exit');
        };
        this.onFullscreen = () => this.fullscreenChanged();
        window.addEventListener('keydown', this.onKey, true);
        root.addEventListener('click', this.onClick);
        root.addEventListener('cancel', this.onCancel);
        document.addEventListener('fullscreenchange', this.onFullscreen);
        this.showModal();
        root.focus({ preventScroll: true });
    }

    showModal() {
        const root = this.root;
        if (typeof root.showModal !== 'function') {
            return;
        }
        try {
            if (root.open) {
                root.close();
            }
            root.showModal();
        } catch {
            root.setAttribute('open', '');
        }
    }

    start() {
        const launch = this.takeLaunch();
        launch?.popup?.close();
        if (launch) {
            this.adoptFullscreen(launch.fullscreen);
            return;
        }
        const root = this.root;
        root?.requestFullscreen?.().then(() => this.root === root && (this.fullscreen = root)).catch(() => {});
    }

    takeLaunch() {
        const launch = this.launch;
        this.launch = null;
        return launch;
    }

    adoptFullscreen(request) {
        request?.then(ok => {
            if (!ok || !document.fullscreenElement) {
                return;
            }
            if (this.root) {
                this.fullscreen = document.fullscreenElement;
            } else {
                document.exitFullscreen?.().catch(() => {});
            }
        });
    }

    fullscreenChanged() {
        if (this.fullscreen && document.fullscreenElement !== this.fullscreen) {
            this.fullscreen = null;
            this.send('exit');
        }
    }

    keyDown(e) {
        if (!this.root || e.ctrlKey || e.metaKey || e.altKey) {
            return;
        }
        const key = e.key;
        if ((key === ' ' || key === 'Enter') && e.target.closest?.('button, a, input')) {
            e.stopPropagation();
            return;
        }
        let command = null;
        if (/^[0-9]$/.test(key)) {
            this.number += key;
        } else if (key === 'Enter' && this.number) {
            command = ['goto', Number(this.number)];
        } else if (NEXT.includes(key)) {
            command = ['next'];
        } else if (PREVIOUS.includes(key)) {
            command = ['previous'];
        } else if (key === 'Home' || key === 'End') {
            command = [key === 'Home' ? 'first' : 'last'];
        } else if (key === 'Escape') {
            command = ['exit'];
        } else if (BLANK[key]) {
            this.setBlank(this.blank() === BLANK[key] ? null : BLANK[key]);
        } else if (key !== 'F5') {
            return;
        }
        e.preventDefault();
        e.stopPropagation();
        if (!/^[0-9]$/.test(key)) {
            this.number = '';
        }
        if (command && command[0] !== 'exit' && this.blank()) {
            this.setBlank(null);
        } else if (command) {
            this.send(...command);
        }
    }

    click(e) {
        if (e.button !== 0 || e.target.closest?.(CONTROLS)) {
            return;
        }
        if (this.blank()) {
            this.setBlank(null);
        } else {
            this.send('click');
        }
    }

    stage() {
        return this.root?.querySelector(STAGE);
    }

    blank() {
        return this.stage()?.dataset.blank;
    }

    setBlank(color) {
        const stage = this.stage();
        if (!stage) {
            return;
        }
        if (color) {
            stage.dataset.blank = color;
        } else {
            delete stage.dataset.blank;
        }
    }

    send(command, number = 0) {
        this.dotnet?.invokeMethodAsync('OnShowCommand', command, number).catch(() => {});
    }

    openAudience(title) {
        const launch = this.takeLaunch();
        this.adoptFullscreen(launch?.fullscreen);
        const popup = launch && 'popup' in launch ? launch.popup : window.open('about:blank', '_blank', 'popup');
        if (!popup) {
            return false;
        }
        this.popup = popup;
        const doc = popup.document;
        doc.title = title;
        const base = doc.createElement('base');
        base.href = document.baseURI;
        doc.head.appendChild(base);
        document.querySelectorAll('link[rel="stylesheet"], style').forEach(node => doc.head.appendChild(node.cloneNode(true)));
        doc.body.classList.add('mud-ex-slide-show-audience');
        doc.body.style.margin = '0';
        doc.body.style.background = '#000';
        doc.body.style.cursor = 'pointer';
        popup.addEventListener('keydown', this.onKey, true);
        doc.addEventListener('click', () => doc.fullscreenElement ? this.send('click') : doc.documentElement.requestFullscreen?.().catch(() => {}));
        this.observer = new MutationObserver(records => records.some(r => this.touchesStage(r)) && this.scheduleMirror());
        this.observer.observe(this.root, { childList: true, subtree: true, attributes: true, characterData: true });
        this.closedCheck = setInterval(() => popup.closed && this.send('exit'), 500);
        this.mirror();
        return true;
    }

    utcOffset() {
        return -new Date().getTimezoneOffset();
    }

    touchesStage(record) {
        const target = record.target.nodeType === Node.ELEMENT_NODE ? record.target : record.target.parentElement;
        return !!target?.closest?.(STAGE) || [...record.addedNodes, ...record.removedNodes].some(n => n.matches?.(STAGE) || n.querySelector?.(STAGE));
    }

    scheduleMirror() {
        if (!this.frame) {
            this.frame = requestAnimationFrame(() => {
                this.frame = 0;
                this.mirror();
            });
        }
    }

    mirror() {
        const stage = this.stage();
        if (this.popup && !this.popup.closed && stage) {
            mirrorInto(stage, this.popup.document.body, this.twins);
        }
    }

    dispose() {
        window.removeEventListener('keydown', this.onKey, true);
        this.root?.removeEventListener('click', this.onClick);
        this.root?.removeEventListener('cancel', this.onCancel);
        document.removeEventListener('fullscreenchange', this.onFullscreen);
        cancelAnimationFrame(this.frame);
        this.observer?.disconnect();
        clearInterval(this.closedCheck);
        if (this.popup && !this.popup.closed) {
            this.popup.close();
        }
        if (this.fullscreen && document.fullscreenElement === this.fullscreen) {
            document.exitFullscreen?.().catch(() => {});
        }
        this.takeLaunch()?.popup?.close();
        if (this.root?.open) {
            this.root.close();
        }
        if (this.focused?.isConnected) {
            this.focused.focus({ preventScroll: true });
        }
        this.popup = null;
        this.fullscreen = null;
        this.focused = null;
        this.root = null;
        this.dotnet = null;
    }
}

export function initializeMudExSlideShow(elementRef, dotnet) {
    return new MudExSlideShow(elementRef, dotnet);
}
