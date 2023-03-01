class MoveContent {
    elementRef;
    dotnet;
    selector;
    elFromSelector;
    mode;
    position;
    
    constructor(elementRef, dotNet) {
        this.elementRef = elementRef;
        this.dotnet = dotNet;
    }

    getSourceAndTarget(mode) {
        return mode === 'MoveFromSelector'
            ? { source: this.elFromSelector, target: this.elementRef }
            : { source: this.elementRef, target: this.elFromSelector };
    }

    move(selector, mode, position) {
        this.mode = mode;
        this.position = position;
        this.selector = selector;
        this.elFromSelector = document.querySelector(selector);
        var el = this.getSourceAndTarget(mode);

        if (this.elFromSelector && !el.target.contains(el.source) && !el.source.contains(el.target)) {
            if (position === 'BeforeBegin') {
                el.target.insertBefore(el.source, el.target.firstChild);
            } else if (position === 'AfterBegin') { 
                el.target.insertBefore(el.source, el.target.firstChild.nextSibling);
            } else if (position === 'BeforeEnd') {
                el.target.insertBefore(el.source, el.target.lastChild);
            } else if (position === 'AfterEnd') {
                el.target.appendChild(el.source);
            }
        }
    }

    dispose() {
        try {
            var el = this.getSourceAndTarget(this.mode);
            if (el && el.target && el.source && el.target.contains(el.source)) {
                el.target.removeChild(el.source);
            }
        } catch (e) {

        }
    }
}

window.MoveContent = MoveContent;

export function initializeMoveContent(elementRef, dotnet) {
    return new MoveContent(elementRef, dotnet);
}