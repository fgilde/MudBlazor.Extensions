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

    move(selector, mode, position, queryOwner, useParent) {
        this.mode = mode;
        this.position = position;
        this.selector = Array.isArray(selector) ? selector : [selector];


        queryOwner = this.getOwner(queryOwner, useParent);
        
        for (let sel of this.selector) {
            this.elFromSelector = queryOwner.querySelector(sel);
            if (this.elFromSelector) break; // Stop if a match is found
        }

        this.dotnet.invokeMethodAsync('ElementFoundChanged', !!this.elFromSelector);

        var el = this.getSourceAndTarget(mode);
        if (this.elFromSelector && el.source && el.target && !el.target.contains(el.source) && !el.source.contains(el.target)) {
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

    getOwner(queryOwner, useParent) {
        if ((!queryOwner || !queryOwner.querySelector) && useParent && this.elementRef) {
            queryOwner = this.elementRef.parentElement;
        }

        if (!queryOwner || !queryOwner.querySelector) {
            queryOwner = document;
        } else if (useParent) {
            var parentCount = (typeof useParent === 'number') ? useParent : 1;
            for (let i = 0; i < parentCount; i++) {
                if (queryOwner.parentElement) {
                    queryOwner = queryOwner.parentElement;
                } else {
                    break; // Break out of the loop if there's no more parent element
                }
            }
        }
        return queryOwner;
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