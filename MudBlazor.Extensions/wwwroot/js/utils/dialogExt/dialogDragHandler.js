class MudExDialogDragHandler extends MudExDialogHandlerBase {
    constructor(options) {
        super(options);
        this._preSnapState = null;
        this._currentSnap = null; // 'left'|'right'|'top'|null
        this._keyDownHandler = this._onKeyDown.bind(this);
    }

    handle(dialog) {
        super.handle(dialog);
        if (!this.dialog || this.options.dragMode === 0) return;

        this.options.dragMode = 3;

        const { dragMode } = this.options;
        if (dragMode === 3) {
            this.snapDragElement(this.dialog, this.dialogHeader, document.body);
            // Keybindings global auf den Dialog anwenden
            window.addEventListener('keydown', this._keyDownHandler, true);
        } else {
            const disableBoundCheck = dragMode === 2;
            this.dragElement(this.dialog, this.dialogHeader, document.body, disableBoundCheck);
        }
    }

    // -------- SNAP-DRAG IMPLEMENTATION --------
    snapDragElement(dialogEl, headerEl, container) {
        const self = this;
        let dragging = false;
        let snapZone = null;
        const threshold = 20; // px zum Rand
        container = container || document.body;

        // Preview-Overlay
        const previewEl = document.createElement('div');
        previewEl.className = 'snap-preview';
        previewEl.style.display = 'none';
        container.appendChild(previewEl);

        // Für normales Bewegen
        let startX, startY, origX, origY;

        // Mousedown starten
        (headerEl || dialogEl).style.cursor = 'move';
        (headerEl || dialogEl).addEventListener('mousedown', onMouseDown);

        function onMouseDown(e) {
            e.preventDefault();
            dragging = true;
            self.raiseDialogEvent('OnDragStart');

            // Start-Koordinaten für free-drag
            startX = e.clientX;
            startY = e.clientY;
            origX = dialogEl.offsetLeft;
            origY = dialogEl.offsetTop;

            document.addEventListener('mousemove', onMouseMove);
            document.addEventListener('mouseup', onMouseUp);
        }

        function onMouseMove(e) {
            if (!dragging) return;
            self.raiseDialogEvent('OnDragging');

            const x = e.clientX;
            const y = e.clientY;
            const W = container.clientWidth;
            const H = container === document.body
                ? window.innerHeight
                : container.clientHeight;

            // 1) Snap-Zone bestimmen
            if (x <= threshold) snapZone = 'left';
            else if (x >= W - threshold) snapZone = 'right';
            else if (y <= threshold) snapZone = 'top';
            else snapZone = null;

            // 2) Free-drag Bewegung, wenn nicht in einer Snap-Zone
            if (!snapZone) {
                // Fenster verschieben
                const dx = x - startX;
                const dy = y - startY;
                dialogEl.style.position = 'absolute';
                dialogEl.style.left = origX + dx + 'px';
                dialogEl.style.top = origY + dy + 'px';
                // Vorschau ausblenden
                previewEl.style.display = 'none';
                return;
            }

            // 3) In Snap-Zone: Vorschau berechnen & anzeigen
            let rect;
            if (snapZone === 'left') {
                rect = { left: 0, top: 0, width: W / 2, height: H };
            } else if (snapZone === 'right') {
                rect = { left: W / 2, top: 0, width: W / 2, height: H };
            } else if (snapZone === 'top') {
                rect = { left: 0, top: 0, width: W, height: H };
            }

            Object.assign(previewEl.style, {
                display: 'block',
                left: rect.left + 'px',
                top: rect.top + 'px',
                width: rect.width + 'px',
                height: rect.height + 'px'
            });
        }

        function onMouseUp(e) {
            if (!dragging) return;
            self.raiseDialogEvent('OnDragEnd');
            dragging = false;
            document.removeEventListener('mousemove', onMouseMove);
            document.removeEventListener('mouseup', onMouseUp);

            // Wenn in Snap-Zone, Fenster auf Vorschau-Größe setzen
            if (snapZone) {
                const W = container.clientWidth;
                const H = container === document.body
                    ? window.innerHeight
                    : container.clientHeight;
                let final;
                if (snapZone === 'left') {
                    final = { left: 0, top: 0, width: W / 2, height: H };
                } else if (snapZone === 'right') {
                    final = { left: W / 2, top: 0, width: W / 2, height: H };
                } else if (snapZone === 'top') {
                    final = { left: 0, top: 0, width: W, height: H };
                }
                dialogEl.style.position = 'absolute';
                dialogEl.style.left = final.left + 'px';
                dialogEl.style.top = final.top + 'px';
                dialogEl.style.width = final.width + 'px';
                dialogEl.style.height = final.height + 'px';
                this._currentSnap = snapZone;
            }

            // Vorschau wieder verstecken
            previewEl.style.display = 'none';
            snapZone = null;
        }
    }


    // -------- KEYBINDINGS --------
    _onKeyDown(e) {
        if (!e.ctrlKey) return;
        let targetSnap = null;

        switch (e.key) {
            case 'ArrowLeft': targetSnap = 'left'; break;
            case 'ArrowRight': targetSnap = 'right'; break;
            case 'ArrowUp': targetSnap = 'top'; break;
            case 'ArrowDown': targetSnap = 'down'; break;
            default: return;
        }
        e.preventDefault();
        this._toggleSnap(targetSnap);
    }

    _toggleSnap(direction) {
        const dlg = this.dialog;
        const W = document.body.clientWidth;
        const H = window.innerHeight;

        // Minimize?
        if (direction === 'down') {
            return typeof dlg.minimize === 'function'
                ? dlg.minimize()
                : this.raiseDialogEvent('OnMinimize');
        }

        // Wenn bereits in dieser Zone, zurück zur alten Größe
        if (this._currentSnap === direction && this._preSnapState) {
            Object.assign(dlg.style, {
                left: this._preSnapState.x + 'px',
                top: this._preSnapState.y + 'px',
                width: this._preSnapState.width + 'px',
                height: this._preSnapState.height + 'px'
            });
            this._currentSnap = null;
            return;
        }

        // Andernfalls Snap anwenden
        let final;
        if (direction === 'left') {
            final = { x: 0, y: 0, width: W / 2, height: H };
        } else if (direction === 'right') {
            final = { x: W / 2, y: 0, width: W / 2, height: H };
        } else if (direction === 'top') {
            final = { x: 0, y: 0, width: W, height: H };
        }
        // Vorherigen Zustand speichern
        if (!this._preSnapState) {
            this._preSnapState = {
                x: dlg.offsetLeft,
                y: dlg.offsetTop,
                width: dlg.offsetWidth,
                height: dlg.offsetHeight
            };
        }
        // Setze neuen Zustand
        Object.assign(dlg.style, {
            position: 'absolute',
            left: final.x + 'px',
            top: final.y + 'px',
            width: final.width + 'px',
            height: final.height + 'px'
        });
        this._currentSnap = direction;
    }
}

window.MudExDialogDragHandler = MudExDialogDragHandler;
