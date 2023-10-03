class MudExSplitter {

    constructor(elementRef, dotNet, options) {
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
            prevElemStyle: { ...this.prevElem.style },
            nextElemStyle: { ...this.nextElem.style }
        };
    }

    // Reset the splitter
    reset() {
        this.splitter.style = this.initialState.splitterStyle;
        this.prevElem.style = this.initialState.prevElemStyle;
        this.nextElem.style = this.initialState.nextElemStyle;
        // May need to remove/reset event listeners or any other added state as well
    }

    // Restore the splitter
    restore() {
        // For restore, you need to store the current state before reset
        this.splitter.style = this.currentState.splitterStyle;
        this.prevElem.style = this.currentState.prevElemStyle;
        this.nextElem.style = this.currentState.nextElemStyle;
    }

    initSplitter(splitter) {        
        this.splitter = splitter;
        this.prevElem = splitter.previousElementSibling;

        this.prevElem = this.findSiblingElement(splitter, el => el.previousElementSibling, ['mud-ex-splitter-internal']);
        this.nextElem = this.findSiblingElement(splitter, el => el.nextElementSibling, ['mud-ex-splitter-internal']);

        if(!this.prevElem || !this.nextElem) {
            console.warn("MudExSplitter can only work between 2 elements!");
            return;
        }
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
            document.onmouseup = () => {
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

            if (this.direction === "H") { // Horizontal
                this.updateHorizontalElements(x, mouseDownInfo);
            } else if (this.direction === "V") { // Vertical
                this.updateVerticalElements(y, mouseDownInfo);
            }
        };
    }


    apply() {

        this.currentState = {
            splitterStyle: { ...this.splitter.style },
            prevElemStyle: { ...this.prevElem.style },
            nextElemStyle: { ...this.nextElem.style }
        };
        
        if (this.options.percentage) {
            this.updateElementsInPercentage();
        }

        this.stretchOtherDirection();
    }

    stretchOtherDirection() {
        if (this.direction === "H") {
            this.prevElem.style.height = "unset";
            this.nextElem.style.height = "unset";
        } else {
            this.prevElem.style.width = "100%";
            this.nextElem.style.width = "100%";
        }
    }

    getMouseDownInfo(event) {
        return {
            event,
            offsetLeft: this.splitter.offsetLeft,
            offsetTop: this.splitter.offsetTop,
            prevWidth: this.prevElem.offsetWidth,
            prevHeight: this.prevElem.offsetHeight,
            nextWidth: this.nextElem.offsetWidth,
            nextHeight: this.nextElem.offsetHeight
        };
    }

    getDelta(event, mouseDownInfo) {
        return {
            x: this.options.reverse ? mouseDownInfo.event.clientX - event.clientX : event.clientX - mouseDownInfo.event.clientX,
            y: this.options.reverse ? mouseDownInfo.event.clientY - event.clientY : event.clientY - mouseDownInfo.event.clientY
        };
    }

    getLimitedDelta(delta, mouseDownInfo) {
        if (this.direction === "H") { // Horizontal
            delta.x = Math.min(Math.max(delta.x, -mouseDownInfo.prevWidth), mouseDownInfo.nextWidth);
        } else if (this.direction === "V") { // Vertical
            delta.y = Math.min(Math.max(delta.y, -mouseDownInfo.prevHeight), mouseDownInfo.nextHeight);
        }

        return delta;
    }

    updateHorizontalElements(deltaX, mouseDownInfo) {
        this.splitter.style.left = mouseDownInfo.offsetLeft + deltaX + "px";
        this.prevElem.style.width = (mouseDownInfo.prevWidth + deltaX) + "px";
        this.nextElem.style.width = (mouseDownInfo.nextWidth - deltaX) + "px";
    }

    updateVerticalElements(deltaY, mouseDownInfo) {
        this.splitter.style.top = mouseDownInfo.offsetTop + deltaY + "px";
        this.prevElem.style.height = (mouseDownInfo.prevHeight + deltaY) + "px";
        this.nextElem.style.height = (mouseDownInfo.nextHeight - deltaY) + "px";
    }

    updateElementsInPercentage() {
        const parentWidth = this.prevElem.parentElement.clientWidth;
        const parentHeight = this.prevElem.parentElement.clientHeight;
        const prevElemWidthPercentage = (this.prevElem.offsetWidth / parentWidth) * 100;
        const prevElemHeightPercentage = (this.prevElem.offsetHeight / parentHeight) * 100;
        const nextElemWidthPercentage = (this.nextElem.offsetWidth / parentWidth) * 100;
        const nextElemHeightPercentage = (this.nextElem.offsetHeight / parentHeight) * 100;

        this.prevElem.style.width = `${prevElemWidthPercentage}%`;
        this.prevElem.style.height = `${prevElemHeightPercentage}%`;
        this.nextElem.style.width = `${nextElemWidthPercentage}%`;
        this.nextElem.style.height = `${nextElemHeightPercentage}%`;
    }

    resetDocumentMouseEvents() {
        document.onmousemove = this.documentMouseMove;
        document.onmouseup = this.documentMouseUp;
    }

    getSize() {        
        var prevSize = this.prevElem.getBoundingClientRect();
        var nextSize = this.nextElem.getBoundingClientRect();
        
        return {
            prev: {
                width: this.prevElem.style.width,
                height: this.prevElem.style.height,
                bounds: prevSize
            }
,
            next: {
                width: this.nextElem.style.width,
                height: this.nextElem.style.height,
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