class MudExSplitter {

    constructor(elementRef, dotNet, options) {
        this.dotNet = dotNet;
        this.initialize(options);
        this.documentMouseUp = document.onmouseup;
        this.documentMouseMove = document.onmousemove;
    }


    initialize(options) {
        this.options = options;
        const splitter = document.querySelector(`.mud-ex-splitter[data-id="${options.id}"]`);
        if (splitter) {
            this.initSplitter(splitter);
            this.saveInitialState();
        }
    }

    // Save initial state
    saveInitialState() {
        this.initialState = {
            splitterStyle: { ...this.splitter.style },
            prevElemStyle: { ...this.prevElem?.style },
            nextElemStyle: { ...this.nextElem?.style }
        };
    }

    // Reset the splitter
    reset() {
        this.splitter.style = this.initialState.splitterStyle;
        if (this.prevElem) this.prevElem.style = this.initialState.prevElemStyle;
        if (this.nextElem) this.nextElem.style = this.initialState.nextElemStyle;
        // May need to remove/reset event listeners or any other added state as well
    }

    // Restore the splitter
    restore() {
        // For restore, you need to store the current state before reset
        this.splitter.style = this.currentState.splitterStyle;
        if (this.prevElem) this.prevElem.style = this.currentState.prevElemStyle;
        if (this.nextElem) this.nextElem.style = this.currentState.nextElemStyle;
    }

    initSplitter(splitter) {
        this.splitter = splitter;

        splitter.style.opacity = this.options.opacity;
        splitter.addEventListener('mouseenter', () => splitter.style.opacity = this.options.hoverOpacity);
        splitter.addEventListener('mouseleave', () => splitter.style.opacity = this.options.opacity);

        this.prevElem = splitter.previousElementSibling;
        this.prevElem = this.options?.prevElement || this.findSiblingElement(splitter, el => el.previousElementSibling, ['mud-ex-splitter-internal']);
        this.nextElem = this.options?.nextElement ||this.findSiblingElement(splitter, el => el.nextElementSibling, ['mud-ex-splitter-internal']);

        this.direction = this.options.verticalSplit ? "V" : "H";
        this.stretchOtherDirection();
        this.dragElement();
    }

    findSiblingElement(elem, directionFunc, ignoredClasses) {
        ignoredClasses = Array.isArray(ignoredClasses) ? ignoredClasses : [ignoredClasses];
        let sibling = directionFunc(elem);

        while (sibling) {
            if (!ignoredClasses.some(className => sibling.classList.contains(className))) {
                return sibling;
            }
            sibling = directionFunc(sibling);
        }

        return null;
    }

    dragElement() {
        let mouseDownInfo;
        let overlay;

        this.splitter.onmousedown = (event) => {
            mouseDownInfo = this.getMouseDownInfo(event);
            this.splitter.style.opacity = this.options.hoverOpacity;
            // Create the overlay and append it to the body
            overlay = document.createElement('div');
            overlay.style.position = 'fixed';
            overlay.style.top = '0';
            overlay.style.right = '0';
            overlay.style.bottom = '0';
            overlay.style.left = '0';
            overlay.style.zIndex = '99999'; // High z-index to make sure it's on top
            overlay.style.cursor = 'default'; // You can set this to whatever cursor you want during dragging
            document.body.appendChild(overlay);

            document.onmousemove = onMouseMove;
            this.dotNet.invokeMethodAsync('OnDragStart');
            document.onmouseup = () => {
                this.splitter.style.opacity = this.options.opacity;
                // Remove the overlay when dragging is done
                if (overlay) {
                    overlay.parentNode.removeChild(overlay);
                }
                this.resetDocumentMouseEvents();
                this.apply();
            };
        };

        const onMouseMove = (event) => {
            const delta = this.getDelta(event, mouseDownInfo);
            const { x, y } = this.getLimitedDelta(delta, mouseDownInfo);
            this.splitter.style.opacity = this.options.hoverOpacity;
            if (this.direction === "H") { // Horizontal
                this.updateHorizontalElements(x, mouseDownInfo);
            } else if (this.direction === "V") { // Vertical
                this.updateVerticalElements(y, mouseDownInfo);
            }
            if (this.options.draggingAttached) {
                this.dotNet.invokeMethodAsync('OnDragging',
                    this.prevElem?.getBoundingClientRect(),
                    this.nextElem?.getBoundingClientRect());
            }
        };
    }


    apply() {
        if (this.options.dontApply) {
            this.reset();
            return;
        }
        this.currentState = {
            splitterStyle: { ...this.splitter.style },
            prevElemStyle: { ...this.prevElem?.style },
            nextElemStyle: { ...this.nextElem?.style }
        };

        if (this.options.percentage) {
            this.updateElementsInPercentage();
        }

        this.stretchOtherDirection();
        this.dotNet.invokeMethodAsync('OnDragEnd', this.prevElem?.getBoundingClientRect(), this.nextElem?.getBoundingClientRect());
    }

    stretchOtherDirection() {
        if (this.nextElem && this.prevElem) {
            if (this.direction === "H") {
                if (this.prevElem) this.prevElem.style.height = "unset";
                if (this.nextElem) this.nextElem.style.height = "unset";
            } else {
                if (this.prevElem) this.prevElem.style.width = "100%";
                if (this.nextElem) this.nextElem.style.width = "100%";
            }
        }
    }

    getMouseDownInfo(event) {
        return {
            event,
            offsetLeft: this.splitter.offsetLeft,
            offsetTop: this.splitter.offsetTop,
            prevWidth: this.prevElem?.offsetWidth,
            prevHeight: this.prevElem?.offsetHeight,
            nextWidth: this.nextElem?.offsetWidth,
            nextHeight: this.nextElem?.offsetHeight
        };
    }

    getDelta(event, mouseDownInfo) {
        return {
            x: this.options.reverse ? mouseDownInfo.event.clientX - event.clientX : event.clientX - mouseDownInfo.event.clientX,
            y: this.options.reverse ? mouseDownInfo.event.clientY - event.clientY : event.clientY - mouseDownInfo.event.clientY
        };
    }

    getLimitedDelta(delta, mouseDownInfo) {
        const minSize = 50; 

        if (this.direction === "H") { 
            if (this.prevElem) {
                const maxDeltaXForPrev = mouseDownInfo.prevWidth - minSize;
                delta.x = Math.min(delta.x, maxDeltaXForPrev);
            }
            if (this.nextElem) {
                const maxDeltaXForNext = minSize - mouseDownInfo.nextWidth;
                delta.x = Math.max(delta.x, maxDeltaXForNext);
            }
        } else {
            if (this.prevElem) {
                const maxDeltaYForPrev = mouseDownInfo.prevHeight - minSize;
                delta.y = Math.min(delta.y, maxDeltaYForPrev);
            }
            if (this.nextElem) {
                const maxDeltaYForNext = minSize - mouseDownInfo.nextHeight;
                delta.y = Math.max(delta.y, maxDeltaYForNext);
            }
        }

        return delta;
    }


    updateHorizontalElements(deltaX, mouseDownInfo) {
        if (this.prevElem && this.nextElem) {
            this.splitter.style.left = mouseDownInfo.offsetLeft + deltaX + "px";
        }

        if (this.prevElem) {
            this.prevElem.style.width = (mouseDownInfo.prevWidth + deltaX) + "px";
        }
        if (this.nextElem) {
            const newWidth = this.prevElem ? (mouseDownInfo.nextWidth - deltaX) : (mouseDownInfo.nextWidth + deltaX);
            this.nextElem.style.width = newWidth + "px";
        }
    }

    updateVerticalElements(deltaY, mouseDownInfo) {
        if (this.prevElem && this.nextElem) {
            this.splitter.style.top = mouseDownInfo.offsetTop + deltaY + "px";
        }

        if (this.prevElem) {
            this.prevElem.style.height = (mouseDownInfo.prevHeight + deltaY) + "px";
        }
        if (this.nextElem) {
            const newHeight = this.prevElem ? (mouseDownInfo.nextHeight - deltaY) : (mouseDownInfo.nextHeight + deltaY);
            this.nextElem.style.height = newHeight + "px";
        }
    }


    updateElementsInPercentage() {
        const parentWidth = this.prevElem?.parentElement?.clientWidth;
        const parentHeight = this.prevElem?.parentElement?.clientHeight;
        const prevElemWidthPercentage = !this.prevElem ? null : (this.prevElem.offsetWidth / parentWidth) * 100;
        const prevElemHeightPercentage = !this.prevElem ? null : (this.prevElem.offsetHeight / parentHeight) * 100;
        const nextElemWidthPercentage = !this.nextElem ? null : (this.nextElem.offsetWidth / parentWidth) * 100;
        const nextElemHeightPercentage = !this.nextElem ? null : (this.nextElem.offsetHeight / parentHeight) * 100;

        if (this.prevElem && prevElemWidthPercentage) this.prevElem.style.width = `${prevElemWidthPercentage}%`;
        if (this.prevElem && prevElemHeightPercentage) this.prevElem.style.height = `${prevElemHeightPercentage}%`;
        if (this.nextElem && nextElemWidthPercentage) this.nextElem.style.width = `${nextElemWidthPercentage}%`;
        if (this.nextElem && nextElemHeightPercentage) this.nextElem.style.height = `${nextElemHeightPercentage}%`;
    }

    resetDocumentMouseEvents() {
        document.onmousemove = this.documentMouseMove;
        document.onmouseup = this.documentMouseUp;
    }

    getSize() {
        var prevSize = this.prevElem?.getBoundingClientRect();
        var nextSize = this.nextElem?.getBoundingClientRect();

        return {
            prev: {
                width: this.prevElem?.style?.width,
                height: this.prevElem?.style?.height,
                bounds: prevSize
            }
            ,
            next: {
                width: this.nextElem?.style?.width,
                height: this.nextElem?.style?.height,
                bounds: nextSize
            }
        };
    }

    dispose() {
        this.splitter.onmousedown = null;
        this.resetDocumentMouseEvents();
        this.documentMouseMove = this.documentMouseUp = null;
        this.direction = this.splitter = this.prevElem = this.nextElem = null;
    }
}


window.MudExSplitter = MudExSplitter;

export function initializeMudExSplitter(elementRef, dotnet, options) {
    return new MudExSplitter(elementRef, dotnet, options);
}