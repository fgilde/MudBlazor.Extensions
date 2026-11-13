const STAGE = '.mud-ex-slide-show-stage';
const CONTROLS = 'button, a, input, .mud-alert, .mud-ex-slide-show-presenter-bar, .mud-ex-slide-show-presenter-side, .mud-ex-slide-show-presenter-nav';
const NEXT = ['ArrowRight', 'ArrowDown', ' ', 'PageDown', 'n', 'N', 'Enter'];
const PREVIOUS = ['ArrowLeft', 'ArrowUp', 'Backspace', 'PageUp', 'p', 'P'];
const BLANK = { b: 'black', B: 'black', '.': 'black', w: 'white', W: 'white', ',': 'white' };

class MudExSlideShow {
    constructor(root, dotnet) {
        this.root = root;
        this.dotnet = dotnet;
        this.number = '';
        this.fullscreen = false;
        this.popup = null;
        this.frame = 0;
        this.onKey = e => this.keyDown(e);
        this.onClick = e => this.click(e);
        this.onFullscreen = () => this.fullscreenChanged();
        window.addEventListener('keydown', this.onKey, true);
        root.addEventListener('click', this.onClick);
        document.addEventListener('fullscreenchange', this.onFullscreen);
        root.focus({ preventScroll: true });
    }

    start() {
        this.root?.requestFullscreen?.().then(() => this.fullscreen = true).catch(() => {});
    }

    fullscreenChanged() {
        if (this.fullscreen && document.fullscreenElement !== this.root) {
            this.fullscreen = false;
            this.send('exit');
        }
    }

    keyDown(e) {
        if (!this.root || e.ctrlKey || e.metaKey || e.altKey) {
            return;
        }
        const key = e.key;
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
        const popup = window.open('about:blank', '_blank', 'popup');
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
        doc.addEventListener('click', () => doc.fullscreenElement || doc.documentElement.requestFullscreen?.().catch(() => {}));
        this.observer = new MutationObserver(records => records.some(r => this.touchesStage(r)) && this.scheduleMirror());
        this.observer.observe(this.root, { childList: true, subtree: true, attributes: true, characterData: true });
        this.closedCheck = setInterval(() => popup.closed && this.send('exit'), 500);
        this.mirror();
        return true;
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
            this.popup.document.body.replaceChildren(stage.cloneNode(true));
        }
    }

    dispose() {
        window.removeEventListener('keydown', this.onKey, true);
        this.root?.removeEventListener('click', this.onClick);
        document.removeEventListener('fullscreenchange', this.onFullscreen);
        cancelAnimationFrame(this.frame);
        this.observer?.disconnect();
        clearInterval(this.closedCheck);
        if (this.popup && !this.popup.closed) {
            this.popup.close();
        }
        if (this.fullscreen && document.fullscreenElement === this.root) {
            document.exitFullscreen?.().catch(() => {});
        }
        this.popup = null;
        this.root = null;
        this.dotnet = null;
    }
}

export function initializeMudExSlideShow(elementRef, dotnet) {
    return new MudExSlideShow(elementRef, dotnet);
}
