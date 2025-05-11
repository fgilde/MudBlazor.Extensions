class MudExDialogDragHandler extends MudExDialogHandlerBase {
    constructor(options) {
        super(options);
        this.snappedTo = null;   // zeigt aktuellen Snap-Zustand
        this._preSnapState = null;   // gespeicherter Zustand vor Snap
        this._preview = null;   // Vorschau-DIV
        this._handlers = [];     // zum Cleanup
        this._threshold = 20;     // px für Rand-Zone
        this._transition = 'all 0.2s ease';
        this._isDragging = false;
    }

    handle(dialog) {
        super.handle(dialog);
        if (!this.dialog) return;
        const c = document.body;
        switch (this.options.dragMode) {
            case 1:
                return this.dragElement(this.dialog, this.dialogHeader, c, false);
            case 2:
                return this.dragElement(this.dialog, this.dialogHeader, c, true);
            case 3:
                this._createPreview(c);
                this._attachMouseSnap();
                this._attachKeySnap();
                this._attachResizeHandler();
                return;
        }
    }



    dragElement(dialogEl, headerEl, container, disableBoundCheck) {
        const self = this;
        let startPos = { x: 0, y: 0 };
        let cursorPos = { x: 0, y: 0 };
        let startDrag;
        container = container || document.body;

        if (headerEl) {
            headerEl.style.cursor = 'move';
            headerEl.onmousedown = dragMouseDown;
        } else {
            dialogEl.onmousedown = dragMouseDown;
        }

        function dragMouseDown(e) {
            e = e || window.event;
            //e.preventDefault();
            startDrag = true;
            cursorPos = { x: e.clientX, y: e.clientY };
            document.onmouseup = closeDragElement;
            document.onmousemove = elementDrag;
        }

        function elementDrag(e) {
            if (startDrag) {
                startDrag = false;
                self.raiseDialogEvent('OnDragStart');
            }
            e = e || window.event;
            e.preventDefault();

            startPos = {
                x: cursorPos.x - e.clientX,
                y: cursorPos.y - e.clientY,
            };

            cursorPos = { x: e.clientX, y: e.clientY };

            const bounds = {
                x: container.offsetWidth - dialogEl.offsetWidth,
                y: container === document.body ? window.innerHeight - dialogEl.offsetHeight : container.offsetHeight - dialogEl.offsetHeight,
            };

            const newPosition = {
                x: dialogEl.offsetLeft - startPos.x,
                y: dialogEl.offsetTop - startPos.y,
            };

            dialogEl.style.position = 'absolute';

            if (disableBoundCheck || isWithinBounds(newPosition.x, bounds.x)) {
                dialogEl.style.left = newPosition.x + 'px';
            } else if (isOutOfBounds(newPosition.x, bounds.x)) {
                dialogEl.style.left = bounds.x + 'px';
            }

            if (disableBoundCheck || isWithinBounds(newPosition.y, bounds.y)) {
                dialogEl.style.top = newPosition.y + 'px';
            } else if (isOutOfBounds(newPosition.y, bounds.y)) {
                dialogEl.style.top = bounds.y + 'px';
            }
            self.raiseDialogEvent('OnDragging');
        }

        function closeDragElement() {
            self.raiseDialogEvent('OnDragEnd');
            self.setRelativeIf();
            document.onmouseup = null;
            document.onmousemove = null;
        }

        function isWithinBounds(value, maxValue) {
            return value >= 0 && value <= maxValue;
        }

        function isOutOfBounds(value, maxValue) {
            return value > maxValue;
        }
    }


    // ─── Vorschau DIV ────────────────────────────────────────────────────────────
    _createPreview(container) {
        if (this._preview) return;
        const p = document.createElement('div');
        p.className = 'snap-preview';
        p.style.display = 'none';
        // CSS:
        // .snap-preview {
        //   position:absolute;
        //   background:rgba(0,120,215,0.2);
        //   box-shadow:0 0 10px rgba(0,0,0,0.5);
        //   transition:all 0.2s ease;
        //   z-index:9999;
        //   pointer-events:none;
        // }
        container.appendChild(p);
        this._preview = p;
    }

    // ─── Maus-Drag + Snap ────────────────────────────────────────────────────────
    _attachMouseSnap() {
        const hdr = this.dialogHeader || this.dialog;
        hdr.style.cursor = 'move';
        const onMD = e => {
            e.preventDefault();
            this.raiseDialogEvent('OnDragStart');
            this._isDragging = true;
            this._hasMoved = false;
            this._pendingZone = null;

            this._startX = e.clientX; this._startY = e.clientY;
            this._origX = this.dialog.offsetLeft;
            this._origY = this.dialog.offsetTop;

            const onMM = this._onMouseMove.bind(this);
            const onMU = this._onMouseUp.bind(this);
            document.addEventListener('mousemove', onMM);
            document.addEventListener('mouseup', onMU);
            this._handlers.push([document, 'mousemove', onMM], [document, 'mouseup', onMU]);
        };
        hdr.addEventListener('mousedown', onMD);
        this._handlers.push([hdr, 'mousedown', onMD]);
    }

    _onMouseMove(e) {
        if (!this._isDragging) return;
        this.raiseDialogEvent('OnDragging');

        const x = e.clientX, y = e.clientY;
        const W = window.innerWidth, H = window.innerHeight;

        // Beim allerersten Move: immer unsnappen (auch aus top/maximized)
        if (!this._hasMoved && this.snappedTo) {
            this._unsnap(false);
        }
        this._hasMoved = true;

        // Zone erkennen
        let zone = null;
        if (y <= this._threshold) zone = 'top';
        else if (y >= H - this._threshold) zone = 'bottom';
        else if (x <= this._threshold) zone = 'left';
        else if (x >= W - this._threshold) zone = 'right';

        if (!zone) {
            // freies Ziehen
            const dx = x - this._startX, dy = y - this._startY;
            Object.assign(this.dialog.style, {
                transition: 'none',
                position: 'absolute',
                left: this._origX + dx + 'px',
                top: this._origY + dy + 'px'
            });
            // unbedingt pendingZone löschen!
            this._preview.style.display = 'none';
            this._pendingZone = null;
            return;
        }

        // Vorschau
        const r = this._calcRect(zone, W, H);
        Object.assign(this._preview.style, {
            display: 'block',
            left: r.x + 'px',
            top: r.y + 'px',
            width: r.w + 'px',
            height: r.h + 'px'
        });
        this._pendingZone = zone;
    }

    _onMouseUp() {
        if (!this._isDragging) return;
        this.raiseDialogEvent('OnDragEnd');
        this._isDragging = false;
        if (this._pendingZone) {
            this._doSnap(this._pendingZone, true);
        }
        this._preview.style.display = 'none';
    }

    // ─── Keybinds (Ctrl+Arrows) ─────────────────────────────────────────────────
    _attachKeySnap() {
        const onKD = e => {
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
            MudExDomHelper.toAbsolute(this.dialog, false);
            this._handleKeySnap(dir);
        };
        window.addEventListener('keydown', onKD, true);
        this._handlers.push([window, 'keydown', onKD, true]);
    }

    _handleKeySnap(dir) {
        const cur = this.snappedTo;
        const snap = z => this._doSnap(z, true);
        const unsnap = () => this._unsnap(true);

        if (!cur) return snap(dir);

        // RIGHT-Seite
        if (cur === 'right') {
            if (dir === 'right') return snap('left');
            if (dir === 'left') return unsnap();
            if (dir === 'top') return snap('top-right');
            if (dir === 'bottom') return snap('bottom-right');
        }
        if (cur === 'top-right') {
            if (dir === 'top') return snap('top');
            if (dir === 'left') return unsnap();
            if (dir === 'right') return snap('right');
            if (dir === 'bottom') return snap('bottom-right');
        }
        if (cur === 'bottom-right') {
            if (dir === 'top') return snap('top-right');
            if (dir === 'left') return unsnap();
            if (dir === 'right') return snap('right');
            if (dir === 'bottom') {
                return this._isMinimizable()
                    ? this.getHandler(MudExDialogPositionHandler).minimize()
                    : snap('bottom');
            }
        }

        // LEFT-Seite
        if (cur === 'left') {
            if (dir === 'left') return snap('right');
            if (dir === 'right') return unsnap();
            if (dir === 'top') return snap('top-left');
            if (dir === 'bottom') return snap('bottom-left');
        }
        if (cur === 'top-left') {
            if (dir === 'top') return snap('top');
            if (dir === 'right') return unsnap();
            if (dir === 'left') return snap('left');
            if (dir === 'bottom') return snap('bottom-left');
        }
        if (cur === 'bottom-left') {
            if (dir === 'top') return snap('top-left');
            if (dir === 'right') return unsnap();
            if (dir === 'left') return snap('left');
            if (dir === 'bottom') {
                return this._isMinimizable()
                    ? this.getHandler(MudExDialogPositionHandler).minimize()
                    : snap('bottom');
            }
        }

        // TOP (maximized)
        if (cur === 'top') {
            if (dir === 'top') return snap('top-half');
            if (dir === 'left' ||
                dir === 'right') return snap(dir);
            if (dir === 'bottom') return unsnap();
        }
        // TOP-HALF
        if (cur === 'top-half') {
            if (dir === 'top') return snap('top');
            if (dir === 'left' ||
                dir === 'right') return snap(dir);
            if (dir === 'bottom') return unsnap();
        }
        // BOTTOM
        if (cur === 'bottom') {
            if (dir === 'bottom') {
                return this._isMinimizable()
                    ? this.getHandler(MudExDialogPositionHandler).minimize()
                    : snap('top');
            }
            return unsnap();
        }
    }

    // ─── Snap / Unsnap Umsetzung ────────────────────────────────────────────────
    _doSnap(zone, animate) {
        if (!this._preSnapState) this._captureState();
        this.raiseDialogEvent('OnSnapStart', { position: zone });

        if (zone === 'top') {
            console.log('MAXIMIZE');
            this.getHandler(MudExDialogPositionHandler).maximize();
        } else {
            const r = this._calcRect(zone, window.innerWidth, window.innerHeight);
            this._applyRect(r, animate);
        }

        this.snappedTo = zone;
        this.raiseDialogEvent('OnSnap', { position: zone });
        this.raiseDialogEvent('OnSnapEnd', { position: zone });
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
        return this.options.minimizeButton;
    }

    // ─── Helfer für Rects ───────────────────────────────────────────────────────
    _calcRect(zone, W, H) {
        const halfW = Math.floor(W / 2), halfH = Math.floor(H / 2);
        switch (zone) {
            case 'top': return { x: 0, y: 0, w: W, h: H };
            case 'top-half': return { x: 0, y: 0, w: W, h: halfH };
            case 'bottom': return { x: 0, y: halfH, w: W, h: halfH };
            case 'left': return { x: 0, y: 0, w: halfW, h: H };
            case 'right': return { x: halfW, y: 0, w: halfW, h: H };
            case 'top-left': return { x: 0, y: 0, w: halfW, h: halfH };
            case 'top-right': return { x: halfW, y: 0, w: halfW, h: halfH };
            case 'bottom-left': return { x: 0, y: halfH, w: halfW, h: halfH };
            case 'bottom-right': return { x: halfW, y: halfH, w: halfW, h: halfH };
        }
        return { x: 0, y: 0, w: W, h: H };
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

    // ─── Browser-Resize ─────────────────────────────────────────────────────────
    _attachResizeHandler() {
        const onR = () => {
            if (!this.snappedTo || this.snappedTo === 'top') return;
            const r = this._calcRect(this.snappedTo, window.innerWidth, window.innerHeight);
            Object.assign(this.dialog.style, {
                left: r.x + 'px',
                top: r.y + 'px',
                width: r.w + 'px',
                height: r.h + 'px'
            });
        };
        window.addEventListener('resize', onR);
        this._handlers.push([window, 'resize', onR]);
    }

    // ─── Cleanup ─────────────────────────────────────────────────────────────────
    destroy() {
        this._handlers.forEach(([t, e, fn, opt]) => t.removeEventListener(e, fn, opt));
        super.destroy();
    }
}

window.MudExDialogDragHandler = MudExDialogDragHandler;
