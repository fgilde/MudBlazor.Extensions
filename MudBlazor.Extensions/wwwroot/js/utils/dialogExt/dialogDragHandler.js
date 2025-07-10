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
        this._savedMaxConstraints = null;

        this._touchMoveThreshold = 10;
    }

    handle(dialog) {
        super.handle(dialog);
        if (!this.dialog) return;
        const container = document.body;
        this._cleanupHandlers();
        switch (this.options.dragMode) {
            case MudExDialogDragHandler.DragMode.DRAG:
                this._attachDrag(this.dialog, this.dialogHeader, container, false);
                break;
            case MudExDialogDragHandler.DragMode.DRAG_WITHOUT_BOUNDS:
                this._attachDrag(this.dialog, this.dialogHeader, container, true);
                break;
            case MudExDialogDragHandler.DragMode.SNAP:
                this._createPreview(container);
                this._attachResizeSnap();
                this._attachMouseSnap();
                this._attachTouchSnap();
                this._attachKeySnap();
                this._attachResizeHandler();
                break;
        }
    }

    // ---- DRAG-ATTACHER ----
    _attachDrag(dialogEl, headerEl, container, disableBoundCheck) {
        const target = headerEl || dialogEl;
        target.style.cursor = 'move';

        // Shared State for both input methods:
        let startPos = { x: 0, y: 0 };
        let cursorPos = { x: 0, y: 0 };
        let dragging = false;
        let touchId = null;
        let touchMoved = false;

        // ----- Mouse -----
        const mouseDown = e => {
            if (this._isClickable(e.target)) return;
            dragging = true;
            cursorPos = { x: e.clientX, y: e.clientY };
            document.addEventListener('mousemove', mouseMove);
            document.addEventListener('mouseup', mouseUp);
        };

        const mouseMove = e => {
            if (!dragging) return;
            e.preventDefault();
            this._raiseDragStartIfNeeded();
            this._performDrag(e.clientX, e.clientY, dialogEl, container, disableBoundCheck, cursorPos, pos => { cursorPos = pos; });
        };

        const mouseUp = e => {
            if (!dragging) return;
            dragging = false;
            this._raiseDragEnd();
            document.removeEventListener('mousemove', mouseMove);
            document.removeEventListener('mouseup', mouseUp);
        };

        // ----- Touch -----
        const touchStart = e => {
            if (e.touches.length !== 1) return;
            if (this._isClickable(e.target)) {
                setTimeout(() => this._simulateClick(e.target), 1);
                return;
            }
            const touch = e.touches[0];
            touchId = touch.identifier;
            cursorPos = { x: touch.clientX, y: touch.clientY };
            touchMoved = false;
            dragging = false;

            document.addEventListener('touchmove', touchMove, { passive: false });
            document.addEventListener('touchend', touchEnd);
            document.addEventListener('touchcancel', touchEnd);
        };

        const touchMove = ev => {
            const t = this._findTouchById(ev, touchId);
            if (!t) return;
            const deltaX = Math.abs(t.clientX - cursorPos.x);
            const deltaY = Math.abs(t.clientY - cursorPos.y);

            if (!touchMoved && (deltaX > this._touchMoveThreshold || deltaY > this._touchMoveThreshold)) {
                touchMoved = true;
                dragging = true;
            }
            if (touchMoved && dragging) {
                ev.preventDefault();
                this._raiseDragStartIfNeeded();
                this._performDrag(t.clientX, t.clientY, dialogEl, container, disableBoundCheck, cursorPos, pos => { cursorPos = pos; });
            }
        };

        const touchEnd = ev => {
            if (touchMoved && dragging) {
                this._raiseDragEnd();
            }
            document.removeEventListener('touchmove', touchMove);
            document.removeEventListener('touchend', touchEnd);
            document.removeEventListener('touchcancel', touchEnd);
            dragging = false;
            touchMoved = false;
            touchId = null;
        };

        // ----- Attach Events -----
        target.addEventListener('mousedown', mouseDown);
        target.addEventListener('touchstart', touchStart, { passive: true });

        this._handlers.push([target, 'mousedown', mouseDown]);
        this._handlers.push([target, 'touchstart', touchStart]);
    }

    // ---- SHARED DRAG MOVE ----
    _performDrag(clientX, clientY, dialogEl, container, disableBoundCheck, lastPos, updateCursorPos) {
        // lastPos: {x, y}, updateCursorPos: (newPos) => { ... }
        const startPos = { x: lastPos.x - clientX, y: lastPos.y - clientY };
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
        if (updateCursorPos) updateCursorPos({ x: clientX, y: clientY });
        this.raiseDialogEvent('OnDragging');
    }

    // -- Util: Check if element should not start drag --
    _isClickable(el) {
        return el.closest('button, [role="button"], a, input, textarea, select, [tabindex]:not([tabindex="-1"])');
    }

    // -- Util: Simulate click for iOS/Safari workaround --
    _simulateClick(el) {
        const evt = new MouseEvent('click', { bubbles: true, cancelable: true, view: window });
        el.dispatchEvent(evt);
    }

    // -- Util: Find correct touch object by identifier --
    _findTouchById(ev, id) {
        if (!ev.touches || ev.touches.length === 0) return null;
        for (const t of ev.touches) if (t.identifier === id) return t;
        return ev.touches[0]; // fallback
    }

    // -- Helper: Raise drag events only once at start/end --
    _raiseDragStartIfNeeded() {
        if (this._didDragStart) return;
        this._didDragStart = true;
        this.raiseDialogEvent('OnDragStart');
    }
    _raiseDragEnd() {
        this._didDragStart = false;
        this.raiseDialogEvent('OnDragEnd');
        this.setRelativeIf && this.setRelativeIf();
    }

    // =========================
    // SNAP-MODE & OTHER EVENTS:
    // =========================

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
    isSnapped() { return this.snappedTo !== null; }
    snap(direction) { this._doSnap(direction, this.animateSnap); }
    unsnap(animate) {
        if (this.snappedTo) this._unsnap(animate ?? this.animateSnap);
    }
    _isMinimizable() { return this.options.minimizeButton; }

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
    _onResize(dialogId, dialog, rect) { }
    _onResized(dialogId, dialog, rect) { }

    _attachMouseSnap() {
        const hdr = this.dialogHeader || this.dialog;
        hdr.style.cursor = 'move';
        const down = e => {
            if (this._isClickable(e.target)) return;
            e.preventDefault();
            this._startSnapDrag(e.clientX, e.clientY);
            const onMM = this._onMouseMoveSnap.bind(this);
            const onMU = this._onMouseUpSnap.bind(this);
            document.addEventListener('mousemove', onMM);
            document.addEventListener('mouseup', onMU);
            this._handlers.push([document, 'mousemove', onMM], [document, 'mouseup', onMU]);
        };
        hdr.addEventListener('mousedown', down);
        this._handlers.push([hdr, 'mousedown', down]);
    }

    _attachTouchSnap() {
        const hdr = this.dialogHeader || this.dialog;
        let touchId = null, touchMoved = false, dragging = false;
        let startX = 0, startY = 0;

        const touchStart = e => {
            if (e.touches.length !== 1) return;
            if (this._isClickable(e.target)) {
                setTimeout(() => this._simulateClick(e.target), 1);
                return;
            }
            const touch = e.touches[0];
            touchId = touch.identifier;
            startX = touch.clientX;
            startY = touch.clientY;
            touchMoved = false;
            dragging = false;

            document.addEventListener('touchmove', touchMove, { passive: false });
            document.addEventListener('touchend', touchEnd);
            document.addEventListener('touchcancel', touchEnd);
        };

        const touchMove = ev => {
            const t = this._findTouchById(ev, touchId);
            if (!t) return;
            const deltaX = Math.abs(t.clientX - startX);
            const deltaY = Math.abs(t.clientY - startY);

            if (!touchMoved && (deltaX > this._touchMoveThreshold || deltaY > this._touchMoveThreshold)) {
                touchMoved = true;
                dragging = true;
                this._startSnapDrag(startX, startY);
            }
            if (touchMoved && dragging) {
                ev.preventDefault();
                this._onMoveSnap(t.clientX, t.clientY);
            }
        };

        const touchEnd = ev => {
            if (touchMoved && dragging) {
                this._onEndSnap();
            }
            document.removeEventListener('touchmove', touchMove);
            document.removeEventListener('touchend', touchEnd);
            document.removeEventListener('touchcancel', touchEnd);
            dragging = false;
            touchMoved = false;
            touchId = null;
        };

        hdr.addEventListener('touchstart', touchStart, { passive: true });
        this._handlers.push([hdr, 'touchstart', touchStart]);
    }

    _startSnapDrag(clientX, clientY) {
        this.raiseDialogEvent('OnDragStart');
        if (!this.snappedTo) this._captureState();
        this._isDragging = true;
        this._hasMoved = false;
        this._pendingZone = null;
        this._startX = clientX;
        this._startY = clientY;
        this._origX = this.dialog.offsetLeft;
        this._origY = this.dialog.offsetTop;
    }

    _onMouseMoveSnap(e) {
        if (!this._isDragging) return;
        this._onMoveSnap(e.clientX, e.clientY);
    }

    _onMoveSnap(clientX, clientY) {
        this.raiseDialogEvent('OnDragging');
        const x = clientX, y = clientY;
        const W = window.innerWidth, H = window.innerHeight;
        if (!this._hasMoved && this.snappedTo) {
            this._unsnap(false);
        }
        this._hasMoved = true;
        const D = MudExDialogDragHandler.Direction;
        let zone = null;

        // Zuerst Ecken checken
        if (x <= this._threshold && y <= this._thresholdTopHalf) zone = D.TOP_LEFT;
        else if (x >= W - this._threshold && y <= this._thresholdTopHalf) zone = D.TOP_RIGHT;
        else if (x <= this._threshold && y >= H - this._thresholdTopHalf) zone = D.BOTTOM_LEFT;
        else if (x >= W - this._threshold && y >= H - this._thresholdTopHalf) zone = D.BOTTOM_RIGHT;
        // Dann Kanten checken
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
        const offsetX = x - r.x;
        const offsetY = y - r.y;

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

    _onMouseUpSnap() {
        if (!this._isDragging) return;
        this._onEndSnap();
    }

    _onEndSnap() {
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
            if (dir === D.TOP) return snap(D.TOP_RIGHT);
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
            if (dir === D.TOP) return snap(D.TOP_LEFT);
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
        this.removeSizeConstraintsIf && this.removeSizeConstraintsIf();
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
        this.restoreSizeConstraintsIf && this.restoreSizeConstraintsIf();
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
