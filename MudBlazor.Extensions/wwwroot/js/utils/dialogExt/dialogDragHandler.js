class MudExDialogDragHandler extends MudExDialogHandlerBase {
    constructor(options) {
        super(options);
        this.snappedTo = null;     // 'left'|'right'|'top'|'bottom'|'top-half'|'top-left'|'top-right'|'bottom-left'|'bottom-right'|'maximized'
        this._preSnapState = null;     // gespeicherter Zustand vor dem Snap
        this._isDragging = false;
        this._pendingZone = null;
        this._handlers = [];
        this._transition = 'left 0.2s ease, top 0.2s ease, width 0.2s ease, height 0.2s ease';
        this._threshold = 20;       // px, Zone-Größe für Snap
    }

    handle(dialog) {
        super.handle(dialog);
        if (!this.dialog) return;
        const container = document.body;

        this.options.dragMode = 3; 
        if (this.options.dragMode === 1) {
            this.dragElement(this.dialog, this.dialogHeader, container, false);
            return;
        }
        if (this.options.dragMode === 2) {
            this.dragElement(this.dialog, this.dialogHeader, container, true);
            return;
        }
        // dragMode 3 → Snap
        this._createPreview(container);
        this._attachMouseSnap(container);
        this._attachKeySnap();
        this._attachResizeHandler();
    }

    // ────────────────────────────────────────────────────────────────────────────
    // PREVIEW-ELEMENT
    // ────────────────────────────────────────────────────────────────────────────
    _createPreview(container) {
        if (this._preview) return;
        const p = document.createElement('div');
        p.className = 'snap-preview';
        p.style.display = 'none';
        // Du brauchst im CSS:
        // .snap-preview { position:absolute; background:rgba(0,120,215,0.2);
        //   box-shadow:0 0 10px rgba(0,0,0,0.5);
        //   transition:all 0.2s ease; z-index:9999; pointer-events:none; }
        container.appendChild(p);
        this._preview = p;
    }

    // ────────────────────────────────────────────────────────────────────────────
    // MAUS-DRAG + SNAP
    // ────────────────────────────────────────────────────────────────────────────
    _attachMouseSnap(container) {
        const header = this.dialogHeader || this.dialog;
        header.style.cursor = 'move';

        this._md = this._onMouseDown.bind(this);
        header.addEventListener('mousedown', this._md);
        this._handlers.push([header, 'mousedown', this._md]);
    }

    _onMouseDown(e) {
        e.preventDefault();
        this.raiseDialogEvent('OnDragStart');
        this._isDragging = true;
        this._pendingZone = null;

        // beim ersten richtigen Move unSnap, außer maximiert
        this._hasMoved = false;

        this._startX = e.clientX;
        this._startY = e.clientY;
        this._origX = this.dialog.offsetLeft;
        this._origY = this.dialog.offsetTop;

        this._mm = this._onMouseMove.bind(this);
        this._mu = this._onMouseUp.bind(this);
        document.addEventListener('mousemove', this._mm);
        document.addEventListener('mouseup', this._mu);
        this._handlers.push([document, 'mousemove', this._mm]);
        this._handlers.push([document, 'mouseup', this._mu]);
    }

    _onMouseMove(e) {
        if (!this._isDragging) return;
        e.preventDefault();
        this.raiseDialogEvent('OnDragging');

        const x = e.clientX, y = e.clientY;
        const W = window.innerWidth, H = window.innerHeight;

        // beim allerersten Move: unSnap, außer maximiert
        if (!this._hasMoved && this.snappedTo && !this.dialog.classList.contains('maximized')) {
            this._unsnap(false);
        }
        this._hasMoved = true;

        // Zone erkennen (top/bottom/left/right)
        let zone = null;
        if (y <= this._threshold) zone = 'top';
        else if (y >= H - this._threshold) zone = 'bottom';
        else if (x <= this._threshold) zone = 'left';
        else if (x >= W - this._threshold) zone = 'right';

        if (!zone) {
            // freies Verschieben
            const dx = x - this._startX, dy = y - this._startY;
            Object.assign(this.dialog.style, {
                position: 'absolute',
                left: this._origX + dx + 'px',
                top: this._origY + dy + 'px'
            });
            this._preview.style.display = 'none';
            return;
        }

        // Preview anzeigen
        const rect = this._calcRect(zone, W, H);
        Object.assign(this._preview.style, {
            display: 'block',
            left: rect.x + 'px',
            top: rect.y + 'px',
            width: rect.w + 'px',
            height: rect.h + 'px'
        });
        this._pendingZone = zone;
    }

    _onMouseUp(e) {
        if (!this._isDragging) return;
        this.raiseDialogEvent('OnDragEnd');
        this._isDragging = false;

        // Snap anwenden, falls in Zone
        if (this._pendingZone) {
            this._doSnap(this._pendingZone, true);
        }
        this._preview.style.display = 'none';
    }

    // ────────────────────────────────────────────────────────────────────────────
    // KEY-BINDINGS (Ctrl+Arrows)
    // ────────────────────────────────────────────────────────────────────────────
    _attachKeySnap() {
        this._kd = this._onKeyDown.bind(this);
        window.addEventListener('keydown', this._kd, true);
        this._handlers.push([window, 'keydown', this._kd, true]);
    }

    _onKeyDown(e) {
        if (!e.ctrlKey) return;
        let dir = null;
        switch (e.key) {
            case 'ArrowLeft': dir = 'left'; break;
            case 'ArrowRight': dir = 'right'; break;
            case 'ArrowUp': dir = 'top'; break;
            case 'ArrowDown': dir = 'bottom'; break;
            default: return;
        }
        e.preventDefault();
        this._handleKeySnap(dir);
    }

    _handleKeySnap(dir) {
        const cur = this.snappedTo;
        // 1) nicht gesnapped?
        if (!cur) {
            return this._doSnap(dir, true);
        }

        // 2) Regeln für 'right'
        if (cur === 'right') {
            if (dir === 'right') return this._doSnap('left', true);
            if (dir === 'left') return this._unsnap(true);
            if (dir === 'top') return this._doCorner('top-right');
            if (dir === 'bottom') return this._doCorner('bottom-right');
        }
        // 3) Regeln für 'left'
        if (cur === 'left') {
            if (dir === 'left') return this._doSnap('right', true);
            if (dir === 'right') return this._unsnap(true);
            if (dir === 'top') return this._doCorner('top-left');
            if (dir === 'bottom') return this._doCorner('bottom-left');
        }
        // 4) Regeln für 'top' (maximized)
        if (cur === 'top') {
            if (dir === 'top') return this._toggleTopHalf();
            if (dir === 'left' ||
                dir === 'right') return this._doSnap(dir, true);
            if (dir === 'bottom') return this._unsnap(true);
        }
        // 5) Regeln für 'top-half'
        if (cur === 'top-half') {
            if (dir === 'top') return this._doSnap('top', true);
            if (dir === 'left' ||
                dir === 'right') return this._doSnap(dir, true);
            if (dir === 'bottom') return this._unsnap(true);
        }
        // 6) Regeln für 'bottom'
        if (cur === 'bottom') {
            if (dir === 'bottom') {
                if (this._isMinimizable()) {
                    return this.getHandler(MudExDialogPositionHandler).minimize();
                }
                return this._doSnap('top', true);
            } else {
                return this._unsnap(true);
            }
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // APPLICATION DER SNAPS (Maus & Key)
    // ────────────────────────────────────────────────────────────────────────────
    _doSnap(zone, animate) {
        // Zustand merken
        if (!this._preSnapState) this._captureState();

        this.raiseDialogEvent('OnSnapStart', { position: zone });

        if (zone === 'top') {
            this.getHandler(MudExDialogPositionHandler).maximize();
            this.snappedTo = 'top';
        }
        else if (zone === 'bottom') {
            const W = window.innerWidth, H = window.innerHeight;
            const r = { x: 0, y: Math.floor(H / 2), w: W, h: Math.floor(H / 2) };
            this._applyRect(r, animate);
            this.snappedTo = 'bottom';
        }
        else if (zone === 'left' || zone === 'right') {
            const W = window.innerWidth, H = window.innerHeight;
            const halfW = Math.floor(W / 2);
            const r = zone === 'left'
                ? { x: 0, y: 0, w: halfW, h: H }
                : { x: halfW, y: 0, w: halfW, h: H };
            this._applyRect(r, animate);
            this.snappedTo = zone;
        }

        this.raiseDialogEvent('OnSnap', { position: zone });
        this.raiseDialogEvent('OnSnapEnd', { position: zone });
    }

    _doCorner(corner) {
        // z.B. 'top-left', 'top-right', 'bottom-left', 'bottom-right'
        if (!this._preSnapState) this._captureState();
        this.raiseDialogEvent('OnSnapStart', { position: corner });

        const W = window.innerWidth, H = window.innerHeight;
        const halfW = Math.floor(W / 2), halfH = Math.floor(H / 2);
        let r;
        if (corner === 'top-left') r = { x: 0, y: 0, w: halfW, h: halfH };
        if (corner === 'top-right') r = { x: halfW, y: 0, w: halfW, h: halfH };
        if (corner === 'bottom-left') r = { x: 0, y: halfH, w: halfW, h: halfH };
        if (corner === 'bottom-right') r = { x: halfW, y: halfH, w: halfW, h: halfH };

        this._applyRect(r, true);
        this.snappedTo = corner;
        this.raiseDialogEvent('OnSnap', { position: corner });
        this.raiseDialogEvent('OnSnapEnd', { position: corner });
    }

    _toggleTopHalf() {
        // Zwischen maximized <-> top-half toggeln
        if (this.snappedTo === 'top') {
            // zu top-half
            this.snappedTo = 'top-half';
            const W = window.innerWidth, H = window.innerHeight;
            const r = { x: 0, y: 0, w: W, h: Math.floor(H / 2) };
            this._applyRect(r, true);
        }
        else {
            // zurück maximize
            this._doSnap('top', true);
        }
    }

    _unsnap(animate) {
        if (!this._preSnapState) return;
        this.raiseDialogEvent('OnSnapStart', { position: null });
        const s = this._preSnapState;
        this._applyRect({ x: s.x, y: s.y, w: s.width, h: s.height }, animate);
        this.snappedTo = null;
        this._preSnapState = null;
        this.raiseDialogEvent('OnSnapEnd', { position: null });
    }

    _captureState() {
        this._preSnapState = {
            x: this.dialog.offsetLeft,
            y: this.dialog.offsetTop,
            width: this.dialog.offsetWidth,
            height: this.dialog.offsetHeight
        };
    }

    _isMinimizable() {
        // aktuell immer true (Später kannst du hier logik ergänzen)
        return true;
    }

    // ────────────────────────────────────────────────────────────────────────────
    // RECT & STYLE HELPERS
    // ────────────────────────────────────────────────────────────────────────────
    _calcRect(zone, W, H) {
        const halfW = Math.floor(W / 2), halfH = Math.floor(H / 2);
        switch (zone) {
            case 'top': return { x: 0, y: 0, w: W, h: H };
            case 'bottom': return { x: 0, y: halfH, w: W, h: halfH };
            case 'left': return { x: 0, y: 0, w: halfW, h: H };
            case 'right': return { x: halfW, y: 0, w: halfW, h: H };
            default: return { x: 0, y: 0, w: W, h: H };
        }
    }

    _applyRect({ x, y, w, h }, animate) {
        const d = this.dialog;
        d.style.transition = animate ? this._transition : 'none';
        Object.assign(d.style, {
            position: 'absolute',
            left: x + 'px',
            top: y + 'px',
            width: w + 'px',
            height: h + 'px'
        });
    }

    // ────────────────────────────────────────────────────────────────────────────
    // BROWSER-RESIZE: aktuellen Snap neu anwenden
    // ────────────────────────────────────────────────────────────────────────────
    _attachResizeHandler() {
        this._rz = () => {
            if (!this.snappedTo) return;
            // Maus-Snaps und Key-Snaps benutzen denselben Calc-Code
            if (this.snappedTo.includes('-')) {
                // Ecke
                this._doCorner(this.snappedTo);
            } else {
                this._doSnap(this.snappedTo, false);
            }
        };
        window.addEventListener('resize', this._rz);
        this._handlers.push([window, 'resize', this._rz]);
    }

    // ────────────────────────────────────────────────────────────────────────────
    // CLEANUP
    // ────────────────────────────────────────────────────────────────────────────
    destroy() {
        // alle EventListener entfernen
        this._handlers.forEach(([t, ev, fn, opt]) =>
            t.removeEventListener(ev, fn, opt)
        );
        super.destroy();
    }
}

window.MudExDialogDragHandler = MudExDialogDragHandler;
