class MudExDialogDragHandler extends MudExDialogHandlerBase {
    static Direction = {
        LEFT: 'left',
        RIGHT: 'right',
        TOP: 'top',
        BOTTOM: 'bottom',
        TOP_HALF: 'top-half',
        TOP_LEFT: 'top-left',
        TOP_RIGHT: 'top-right',
        BOTTOM_LEFT: 'bottom-left',
        BOTTOM_RIGHT: 'bottom-right',
        CUSTOM_HEIGHT: 'custom-height',
        CUSTOM_WIDTH: 'custom-width'
    };
    static DragMode = {
        NONE: 0,
        DRAG: 1,
        DRAG_WITHOUT_BOUNDS: 2,
        SNAP: 3
    };

    constructor(options, dotNet, dotNetService, onDone) {
        super(options, dotNet, dotNetService, onDone);
        this.snapAnimationDuration = 200;
        this.snappedTo = null;
        this._preSnapState = null;
        this._preview = null;
        this._handlers = [];
        this._threshold = 20;
        this._thresholdTopHalf = 80;
        this._transition = `all ${this.snapAnimationDuration}ms ease`;
        this._isDragging = false;
        this.animateSnap = true;
        this._snapPreviewClassName = 'snap-preview';

        this._savedMaxConstraints = null; // store original maxWidth/Height
    }

    handle(dialog) {
        super.handle(dialog);
        if (!this.dialog) return;
        const container = document.body;
        this._cleanupHandlers();
        switch (this.options.dragMode) {
            case MudExDialogDragHandler.DragMode.DRAG:
                this.dragElement(this.dialog, this.dialogHeader, container, false);
                break;
            case MudExDialogDragHandler.DragMode.DRAG_WITHOUT_BOUNDS:
                this.dragElement(this.dialog, this.dialogHeader, container, true);
                break;
            case MudExDialogDragHandler.DragMode.SNAP:
                this._createPreview(container);
                this._attachResizeSnap();
                this._attachMouseSnap();
                this._attachKeySnap();
                this._attachResizeHandler();
                break;
        }
    }

    dragElement(dialogEl, headerEl, container, disableBoundCheck) {
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
                this.raiseDialogEvent('OnDragStart');
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
            this.raiseDialogEvent('OnDragging');
        };
        const up = () => {
            this.raiseDialogEvent('OnDragEnd');
            this.setRelativeIf();
            document.onmouseup = null;
            document.onmousemove = null;
        };
        const target = headerEl || dialogEl;
        target.style.cursor = 'move';
        target.addEventListener('mousedown', down);
        this._handlers.push([target, 'mousedown', down]);
    }

    toggleSnap(direction) {
        if (this.snappedTo === direction) {
            if (this._lastSnap) {
                this._doSnap(this._lastSnap, this.animateSnap);
                this._lastSnap = null;
            } else {
                this.unsnap(this.animateSnap);
            }
        } else {
            this._lastSnap = this.snappedTo;
            this.snap(direction);
        }
    }

    isSnapped() {
        return this.snappedTo !== null;
    }

    snap(direction) {
        this._doSnap(direction, this.animateSnap);
    }

    unsnap(animate) {
        if (this.snappedTo) {
            this._unsnap(animate ?? this.animateSnap);
        }
    }

    _isMinimizable() {
        return this.options.minimizeButton;
    }
    _hidePreview() {
        const p = this._preview;
        if (!p) return;
        p.style.transform = 'scale(0)';        
    }

    _createPreview(container) {
        if (this._preview) return;
        const p = document.createElement('div');
        p.className = this._snapPreviewClassName;
        p.style.height = 0;
        p.style.width = 0;
        container.appendChild(p);
        this._preview = p;
    }

    _attachResizeSnap() {
        if (this.options.resizeable) {
            this.on('OnResizing', this._onResize);
            this.on('OnResized', this._onResized);
        }
    }

    _onResize(dialogId, dialog, rect) {
        this._isResizing = true;
    }

    _onResized(dialogId, dialog, rect) {
        this._isResizing = true;
    }

    _attachMouseSnap() {
        const hdr = this.dialogHeader || this.dialog;
        hdr.style.cursor = 'move';
        const down = e => {
            e.preventDefault();
            this.raiseDialogEvent('OnDragStart');
            if (!this.snappedTo) this._captureState();
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
        const D = MudExDialogDragHandler.Direction;
        let zone = null;

        // --- NEU: Zuerst Ecken checken ---
        if (x <= this._threshold && y <= this._thresholdTopHalf) zone = D.TOP_LEFT;
        else if (x >= W - this._threshold && y <= this._thresholdTopHalf) zone = D.TOP_RIGHT;
        else if (x <= this._threshold && y >= H - this._thresholdTopHalf) zone = D.BOTTOM_LEFT;
        else if (x >= W - this._threshold && y >= H - this._thresholdTopHalf) zone = D.BOTTOM_RIGHT;
        // --- Dann wie gehabt Kanten checken ---
        else if (y <= this._threshold) zone = D.TOP;
        else if (y >= H - this._threshold) zone = D.BOTTOM;
        else if (x <= this._threshold) zone = D.LEFT;
        else if (x >= W - this._threshold) zone = D.RIGHT;

        if (!zone) {
            const dx = x - this._startX, dy = y - this._startY;
            Object.assign(this.dialog.style, {
                transition: 'none',
                position: 'absolute',
                left: this._origX + dx + 'px',
                top: this._origY + dy + 'px'
            });
            this._hidePreview();
            this._pendingZone = null;
            return;
        }
        const r = this._calcRect(zone, W, H);
        const offsetX = e.clientX - r.x;
        const offsetY = e.clientY - r.y;

        Object.assign(this._preview.style, {
            display: 'block',
            left: `${r.x}px`,
            top: `${r.y}px`,
            width: `${r.w}px`,
            height: `${r.h}px`,
            transformOrigin: `${offsetX}px ${offsetY}px`,
            transform: 'scale(1)'
        });

        this._pendingZone = zone;
    }


    _onMouseUp() {
        if (!this._isDragging) return;
        this.raiseDialogEvent('OnDragEnd');
        this._isDragging = false;
        if (this._pendingZone) {
            this._doSnap(this._pendingZone, this.animateSnap);
        } else {
            if (!this.snappedTo) this._captureState();
        }
        this._hidePreview();
    }

    _attachKeySnap() {
        const kd = e => {
            if (!e.ctrlKey) return;
            let dir = null;
            const D = MudExDialogDragHandler.Direction;
            switch (e.key) {
                case 'ArrowLeft': dir = D.LEFT; break;
                case 'ArrowRight': dir = D.RIGHT; break;
                case 'ArrowUp': dir = D.TOP; break;
                case 'ArrowDown': dir = D.BOTTOM; break;
                default: return;
            }
            e.preventDefault();
            this._handleKeySnap(dir);
        };
        window.addEventListener('keydown', kd, true);
        this._handlers.push([window, 'keydown', kd, true]);
    }

    _handleKeySnap(dir) {
        const D = MudExDialogDragHandler.Direction;
        const cur = this.snappedTo;
        const snap = z => this._doSnap(z, this.animateSnap);
        const unsnap = () => this._unsnap(this.animateSnap);
        if (!cur) return snap(dir);

        if (cur === D.RIGHT) {
            if (dir === D.RIGHT) return snap(D.LEFT);
            if (dir === D.LEFT) return unsnap();
            if (dir === D.TOP) return snap(D.TOP_RIGHT);
            if (dir === D.BOTTOM) return snap(D.BOTTOM_RIGHT);
        }
        if (cur === D.TOP_RIGHT) {
            if (dir === D.TOP) return snap(D.TOP);
            if (dir === D.LEFT) return snap(D.TOP_LEFT);
            if (dir === D.RIGHT) return snap(D.RIGHT);
            if (dir === D.BOTTOM) return snap(D.BOTTOM_RIGHT);
        }
        if (cur === D.BOTTOM_RIGHT) {
            if (dir === D.TOP) {
                return snap(D.TOP_RIGHT);
            }
            if (dir === D.LEFT) return snap(D.BOTTOM_LEFT);
            if (dir === D.RIGHT) return snap(D.RIGHT);
            if (dir === D.BOTTOM) return snap(D.BOTTOM);
        }
        if (cur === D.LEFT) {
            if (dir === D.LEFT) return snap(D.RIGHT);
            if (dir === D.RIGHT) return unsnap();
            if (dir === D.TOP) return snap(D.TOP_LEFT);
            if (dir === D.BOTTOM) return snap(D.BOTTOM_LEFT);
        }
        if (cur === D.TOP_LEFT) {
            if (dir === D.TOP) return snap(D.TOP);
            if (dir === D.RIGHT) return snap(D.TOP_RIGHT);
            if (dir === D.LEFT) return snap(D.LEFT);
            if (dir === D.BOTTOM) return snap(D.BOTTOM_LEFT);
        }
        if (cur === D.BOTTOM_LEFT) {
            if (dir === D.TOP) {
                return snap(D.TOP_LEFT);
            }
            if (dir === D.RIGHT) return snap(D.BOTTOM_RIGHT);
            if (dir === D.LEFT) return snap(D.LEFT);
            if (dir === D.BOTTOM) return snap(D.BOTTOM);
        }
        if (cur === D.TOP) {
            if (dir === D.TOP) return snap(D.TOP_HALF);
            if (dir === D.LEFT || dir === D.RIGHT) return snap(dir);
            if (dir === D.BOTTOM) return unsnap();
        }
        if (cur === D.TOP_HALF) {
            if (dir === D.RIGHT) return snap(D.TOP_RIGHT);
            if (dir === D.LEFT) return snap(D.TOP_LEFT);
            if (dir === D.TOP) return snap(D.TOP);
            if (dir === D.LEFT || dir === D.RIGHT) return snap(dir);
            if (dir === D.BOTTOM) return unsnap();
        }
        if (cur === D.BOTTOM) {
            if (dir === D.RIGHT) return snap(D.BOTTOM_RIGHT);
            if (dir === D.LEFT) return snap(D.BOTTOM_LEFT);
            if (dir === D.BOTTOM) {
                return this._isMinimizable()
                    ? this.getHandler(MudExDialogPositionHandler).minimize()
                    : snap(D.TOP);
            }
            return unsnap();
        }
    }

    _doSnap(zone, animate) {
        // remove max constraints if needed
        this.removeSizeConstraintsIf();

        if (!this._preSnapState) this._captureState();
        this.raiseDialogEvent('OnSnapStart', { position: zone });
        const r = this._calcRect(zone, window.innerWidth, window.innerHeight);
        this._applyRect(r, animate);
        this.snappedTo = zone;
        this.raiseDialogEvent('OnSnap', { position: zone });
        this.raiseDialogEvent('OnSnapEnd', { position: zone });
        setTimeout(() => { this.dialog.style.transition = 'none'; }, this.snapAnimationDuration);
    }

    _unsnap(animate) {
        if (!this._preSnapState) return;
        this.raiseDialogEvent('OnSnapStart', { position: null });
        const s = this._preSnapState;
        this._applyRect({ x: s.x, y: s.y, w: s.width, h: s.height }, animate);
        this.snappedTo = null;
        this._preSnapState = null;
        this.raiseDialogEvent('OnSnapEnd', { position: null });
        this.restoreSizeConstraintsIf();
        setTimeout(() => { this.dialog.style.transition = 'none'; }, this.snapAnimationDuration);
    }

    _captureState() {
        this._preSnapState = {
            x: this.dialog.offsetLeft,
            y: this.dialog.offsetTop,
            width: this.dialog.offsetWidth,
            height: this.dialog.offsetHeight
        };
    }

    _calcRect(zone, W, H) {
        const D = MudExDialogDragHandler.Direction;
        const halfW = Math.floor(W / 2), halfH = Math.floor(H / 2);
        switch (zone) {
            case D.TOP: return { x: 0, y: 0, w: W, h: H };
            case D.TOP_HALF: return { x: 0, y: 0, w: W, h: halfH };
            case D.BOTTOM: return { x: 0, y: halfH, w: W, h: halfH };
            case D.LEFT: return { x: 0, y: 0, w: halfW, h: H };
            case D.RIGHT: return { x: halfW, y: 0, w: halfW, h: H };
            case D.TOP_LEFT: return { x: 0, y: 0, w: halfW, h: halfH };
            case D.TOP_RIGHT: return { x: halfW, y: 0, w: halfW, h: halfH };
            case D.BOTTOM_LEFT: return { x: 0, y: halfH, w: halfW, h: halfH };
            case D.BOTTOM_RIGHT: return { x: halfW, y: halfH, w: halfW, h: halfH };
            default: return { x: 0, y: 0, w: W, h: H };
        }
    }

    _applyRect({ x, y, w, h }, animate) {
        const d = this.dialog;
        d.style.transition = animate ? this._transition : 'none';
        Object.assign(d.style, { position: 'absolute', left: x + 'px', top: y + 'px', width: w + 'px', height: h + 'px' });
    }

    _attachResizeHandler() {
        const onR = () => {
            if (!this.snappedTo) return;
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
        if (this.options.resizeable) {
            this.un('OnResizing', this._onResize);
            this.un('OnResized', this._onResized);
        }
    }

    dispose() {
        this._cleanupHandlers();
        super.dispose();
    }
}

window.MudExDialogDragHandler = MudExDialogDragHandler;