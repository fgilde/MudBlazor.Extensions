class MudExSplitter {
    elementRef;
    options;
    dotnet;
    splitter;
    
    constructor(elementRef, dotNet, options) {
        this.options = options;
        this.initialize(options);
    }

    initialize(options) {
        this.options = options;
        this.splitter = document.querySelector(`.mud-ex-splitter[data-id="${options.id}"]`);
        if (this.splitter) {
            this.initSplitter(this.splitter);
        }
    }

    initSplitter(splitter) {
        const prevElem = splitter.previousElementSibling;
        const nextElem = splitter.nextElementSibling;

        if (this.options.verticalSplit) {
            this.dragElement(splitter, "V", prevElem, nextElem);
        } else {
            this.dragElement(splitter, "H", prevElem, nextElem);
        }
    }

    dragElement(splitter, direction, prevElem, nextElem) {
        let md; // remember mouse down info

        splitter.onmousedown = onMouseDown;

        function onMouseDown(e) {
            md = {
                e,
                offsetLeft: splitter.offsetLeft,
                offsetTop: splitter.offsetTop,
                prevWidth: prevElem.offsetWidth,
                prevHeight: prevElem.offsetHeight,
                nextWidth: nextElem.offsetWidth,
                nextHeight: nextElem.offsetHeight
            };

            document.onmousemove = onMouseMove;
            document.onmouseup = () => {
                document.onmousemove = document.onmouseup = null;
            }
        }

        function onMouseMove(e) {
            const delta = {
                x: e.clientX - md.e.clientX,
                y: e.clientY - md.e.clientY
            };

            if (direction === "H") { // Horizontal
                // Prevent negative-sized elements
                delta.x = Math.min(Math.max(delta.x, -md.prevWidth), md.nextWidth);

                splitter.style.left = md.offsetLeft + delta.x + "px";
                prevElem.style.width = (md.prevWidth + delta.x) + "px";
                nextElem.style.width = (md.nextWidth - delta.x) + "px";
            } else if (direction === "V") { // Vertical
                // Prevent negative-sized elements
                delta.y = Math.min(Math.max(delta.y, -md.prevHeight), md.nextHeight);

                splitter.style.top = md.offsetTop + delta.y + "px";
                prevElem.style.height = (md.prevHeight + delta.y) + "px";
                nextElem.style.height = (md.nextHeight - delta.y) + "px";
            }
        }
    }

    
}

window.MudExSplitter = MudExSplitter;

export function initializeMudExSplitter(elementRef, dotnet, options) {
    return new MudExSplitter(elementRef, dotnet, options);
}