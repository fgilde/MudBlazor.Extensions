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
        }
    }

    initSplitter(splitter) {
        this.splitter = splitter;
        this.prevElem = splitter.previousElementSibling;
        this.nextElem = splitter.nextElementSibling;
        if(!this.prevElem || !this.nextElem) {
            console.warn("MudExSplitter can only work between 2 elements!");
            return;
        }
        this.direction = this.options.verticalSplit ? "V" : "H";
        this.stretchOtherDirection();
        this.dragElement();
    }

    dragElement() {
        let mouseDownInfo;

        this.splitter.onmousedown = (event) => {
            mouseDownInfo = this.getMouseDownInfo(event);
            document.onmousemove = onMouseMove;
            document.onmouseup = () => {
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