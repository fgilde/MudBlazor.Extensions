class MudExDialogDragHandler extends MudExDialogHandlerBase {
    constructor(options) {
        super(options);
        this.snappedTo = null;
        this._preSnapState = null;
        this._preview = null;
        this._handlers = [];
        this._threshold = 20;
        this._transition = 'all 0.2s ease';
        this._isDragging = false;
    }

    handle(dialog) {
        super.handle(dialog);
        if (!this.dialog) return;
        const container = document.body;
        switch (this.options.dragMode) {
            case 1:
                this._cleanupHandlers();
                this.dragElement(this.dialog, this.dialogHeader, container, false);
                break;
            case 2:
                this._cleanupHandlers();
                this.dragElement(this.dialog, this.dialogHeader, container, true);
                break;
            case 3:
                this._cleanupHandlers();
                this._createPreview(container);
                this._attachMouseSnap();
                this._attachKeySnap();
                this._attachResizeHandler();
                break;
        }
    }

    dragElement(dialogEl, headerEl, container, disableBoundCheck) {
        const self = this;
        let startPos = { x: 0, y: 0 };
        let cursorPos = { x: 0, y: 0 };
        let startDrag;

        const down = e => {
            startDrag = true;
            cursorPos = { x: e.clientX, y: e.clientY };
            document.onmouseup = up;
            document.onmousemove = move;
        };

        const move = e => {
            if (startDrag) {
                startDrag = false;
                self.raiseDialogEvent('OnDragStart');
            }
            e.preventDefault();
            startPos = { x: cursorPos.x - e.clientX, y: cursorPos.y - e.clientY };
            cursorPos = { x: e.clientX, y: e.clientY };
            const bounds = {
                x: container.offsetWidth - dialogEl.offsetWidth,
                y: container === document.body
                    ? window.innerHeight - dialogEl.offsetHeight
                    : container.offsetHeight - dialogEl.offsetHeight
            };
            const newPos = { x: dialogEl.offsetLeft - startPos.x, y: dialogEl.offsetTop - startPos.y };
            dialogEl.style.position = 'absolute';
            if (disableBoundCheck || (newPos.x >= 0 && newPos.x <= bounds.x)) {
                dialogEl.style.left = newPos.x + 'px';
            } else if (newPos.x > bounds.x) {
                dialogEl.style.left = bounds.x + 'px';
            }
            if (disableBoundCheck || (newPos.y >= 0 && newPos.y <= bounds.y)) {
                dialogEl.style.top = newPos.y + 'px';
            } else if (newPos.y > bounds.y) {
                dialogEl.style.top = bounds.y + 'px';
            }
            self.raiseDialogEvent('OnDragging');
        };

        const up = () => {
            self.raiseDialogEvent('OnDragEnd');
            self.setRelativeIf();
            document.onmouseup = null;
            document.onmousemove = null;
        };

        if (headerEl) {
            headerEl.style.cursor = 'move';
            headerEl.addEventListener('mousedown', down);
            this._handlers.push([headerEl, 'mousedown', down]);
        } else {
            dialogEl.addEventListener('mousedown', down);
            this._handlers.push([dialogEl, 'mousedown', down]);
        }
    }

    _createPreview(container) {
        if (this._preview) return;
        const p = document.createElement('div');
        p.className = 'snap-preview';
        p.style.display = 'none';
        container.appendChild(p);
        this._preview = p;
    }

    _attachMouseSnap() {
        const hdr = this.dialogHeader || this.dialog;
        hdr.style.cursor = 'move';
        const down = e => {
            e.preventDefault();
            this.raiseDialogEvent('OnDragStart');
            this._isDragging = true;
            this._hasMoved = false;
            this._pendingZone = null;
            this._startX = e.clientX;
            this._startY = e.clientY;
            this._origX = this.dialog.offsetLeft;
            this._origY = this.dialog.offsetTop;
            const onMM = this._onMouseMove.bind(this);
            const onMU = this._onMouseUp.bind(this);
            document.addEventListener('mousemove', onMM);
            document.addEventListener('mouseup', onMU);
            this._handlers.push([document, 'mousemove', onMM], [document, 'mouseup', onMU]);
        };
        hdr.addEventListener('mousedown', down);
        this._handlers.push([hdr, 'mousedown', down]);
    }

    _onMouseMove(e) {
        if (!this._isDragging) return;
        this.raiseDialogEvent('OnDragging');
        const x = e.clientX, y = e.clientY;
        const W = window.innerWidth, H = window.innerHeight;
        if (!this._hasMoved && this.snappedTo) {
            this._unsnap(false);
        }
        this._hasMoved = true;
        let zone = null;
        if (y <= this._threshold) zone = 'top';
        else if (y >= H - this._threshold) zone = 'bottom';
        else if (x <= this._threshold) zone = 'left';
        else if (x >= W - this._threshold) zone = 'right';
        if (!zone) {
            const dx = x - this._startX, dy = y - this._startY;
            Object.assign(this.dialog.style, {
                transition: 'none',
                position: 'absolute',
                left: this._origX + dx + 'px',
                top: this._origY + dy + 'px'
            });
            this._preview.style.display = 'none';
            this._pendingZone = null;
            return;
        }
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

    _attachKeySnap() {
        const kd = e => {
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
            //this.getHandler(MudExDialogResizeHandler).checkResizeable();
            this._handleKeySnap(dir);
        };
        window.addEventListener('keydown', kd, true);
        this._handlers.push([window, 'keydown', kd, true]);
    }

    _handleKeySnap(dir) {
        const cur = this.snappedTo;
        const snap = z => this._doSnap(z, true);
        const unsnap = () => this._unsnap(true);
        if (!cur) return snap(dir);
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
                if (this._isMinimizable()) return this.getHandler(MudExDialogPositionHandler).minimize();
                return snap('bottom');
            }
        }
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
                if (this._isMinimizable()) return this.getHandler(MudExDialogPositionHandler).minimize();
                return snap('bottom');
            }
        }
        if (cur === 'top') {
            if (dir === 'top') return snap('top-half');
            if (dir === 'left' || dir === 'right') return snap(dir);
            if (dir === 'bottom') return unsnap();
        }
        if (cur === 'top-half') {
            if (dir === 'top') return snap('top');
            if (dir === 'left' || dir === 'right') return snap(dir);
            if (dir === 'bottom') return unsnap();
        }
        if (cur === 'bottom') {
            if (dir === 'bottom') {
                if (this._isMinimizable()) return this.getHandler(MudExDialogPositionHandler).minimize();
                return snap('top');
            }
            return unsnap();
        }
    }

    _doSnap(zone, animate) {
        if (!this._preSnapState) this._captureState();
        this.raiseDialogEvent('OnSnapStart', { position: zone });
        if (zone === 'top') {
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
        if (this.getHandler(MudExDialogPositionHandler).isMaximized()) {
            // Undo maximize
            this.getHandler(MudExDialogPositionHandler).maximize();
        }
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
        Object.assign(d.style, { position: 'absolute', left: x + 'px', top: y + 'px', width: w + 'px', height: h + 'px' });
    }

    _attachResizeHandler() {
        const onR = () => {
            if (!this.snappedTo || this.snappedTo === 'top') return;
            const r = this._calcRect(this.snappedTo, window.innerWidth, window.innerHeight);
            Object.assign(this.dialog.style, { left: r.x + 'px', top: r.y + 'px', width: r.w + 'px', height: r.h + 'px' });
        };
        window.addEventListener('resize', onR);
        this._handlers.push([window, 'resize', onR]);
    }

    _cleanupHandlers() {
        if (this._preview) { this._preview.remove(); this._preview = null; }
        this._handlers.forEach(([t, e, fn, opt]) => t.removeEventListener(e, fn, opt));
        this._handlers = [];
        this.snappedTo = null;
        this._preSnapState = null;
        this._isDragging = false;
        this._pendingZone = null;
        this._hasMoved = false;
    }

    destroy() {
        this._cleanupHandlers();
        super.destroy();
    }
}

window.MudExDialogDragHandler = MudExDialogDragHandler;