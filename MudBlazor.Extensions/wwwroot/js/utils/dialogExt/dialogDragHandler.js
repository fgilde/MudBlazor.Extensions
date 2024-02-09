class MudExDialogDragHandler extends MudExDialogHandlerBase  {
    
    handle(dialog) {
        super.handle(dialog);
        if (this.options.dragMode !== 0 && this.dialog) {
            this.dragElement(this.dialog, this.dialogHeader, document.body, this.options.dragMode === 2);
        }
    }

    dragElement(dialogEl, headerEl, container, disableBoundCheck) {
        let startPos = { x: 0, y: 0 };
        let cursorPos = { x: 0, y: 0 };
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
            cursorPos = { x: e.clientX, y: e.clientY };
            document.onmouseup = closeDragElement;
            document.onmousemove = elementDrag;
        }

        function elementDrag(e) {
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
        }

        function closeDragElement() {
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
    
}


window.MudExDialogDragHandler = MudExDialogDragHandler;