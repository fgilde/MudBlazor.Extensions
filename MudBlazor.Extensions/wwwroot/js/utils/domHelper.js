class MudExDomHelper {
    dom;
    
    constructor(dom) {
        this.dom = dom;
    }

    _asArray(val) {
        return Array.isArray(val) ? val : [val];
    }

    down(q) {
        var d = this.dom.querySelector(q);
        return d ? new MudExDomHelper(d) : null;
    }

    isVisible(deep) {
        if (!deep) {
            return this.dom.style.display !== 'none' && this.dom.style.visibility !== 'hidden';
        }
        var element = this.dom;
        if (element.offsetWidth === 0 || element.offsetHeight === 0) return false;
        var height = document.documentElement.clientHeight,
            rects = element.getClientRects(),
            onTop = function (r) {
                var x = (r.left + r.right) / 2, y = (r.top + r.bottom) / 2;
                return document.elementFromPoint(x, y) === element;
            };
        for (var i = 0, l = rects.length; i < l; i++) {
            var r = rects[i],
                inViewport = r.top > 0 ? r.top <= height : (r.bottom > 0 && r.bottom <= height);
            if (inViewport && onTop(r)) return true;
        }
        return false;
    }

    setStyle(styles) {
        Object.assign(this.dom.style, styles);
        return this;
    }
    
    getStyleValue(name) {
        return this.dom.style.getPropertyValue(name);
    }

    addCls(names) {
        this._asArray(names).forEach((css) => this.dom.classList.add(css));
        return this;
    }

    removeCls(names) {
        this._asArray(names).forEach((css) => this.dom.classList.remove(css));
        return this;
    }

    toggleCls(names) {
        this._asArray(names).forEach((css) => {
            if (this.dom.classList.contains(css)) {
                this.removeCls(css);
            } else {
                this.addCls(css);
            }
        });
        return this;
    }

    show() {
        this.dom.style.display = '';
        return this;
    }

    hide(animate) {
        this.dom.style.display = 'none';
        return this;
    }

    getBounds() { return this.dom.getBoundingClientRect(); }
    getWidth() { return this.getBounds().width; }
    getHeight() { return this.getBounds().height; }
    getX() { return this.getXY()[0]; }
    getY() { return this.getXY()[1]; }
    getXY() {
        var round = Math.round,
            body = document.body,
            dom = this.dom,
            x = 0,
            y = 0,
            bodyRect, rect;

        if (dom !== document && dom !== body) {
            // IE (including IE10) throws an error when getBoundingClientRect
            // is called on an element not attached to dom
            try {
                bodyRect = body.getBoundingClientRect();
                rect = dom.getBoundingClientRect();

                x = rect.left - bodyRect.left;
                y = rect.top - bodyRect.top;
            }
            catch (ex) {
                // This block is intentionally left blank
            }
        }
        return [round(x), round(y)];
    }

    forcefocus() {
        this.dom.focus({ focusVisible: true });        
    }

    focusDelayed(delay) {
        this.forcefocus();
        setTimeout(() => this.forcefocus(), delay || 50);        
        return this;
    }
    
    static focusElementDelayed(selectorOrElement, delay, eventToStop) {
        let result = this.create(selectorOrElement);
        if (eventToStop) {
            eventToStop.stopPropagation();
            eventToStop.preventDefault();
        }
        return result ? result.focusDelayed(delay) : null;
    }

    static ensureElement(selectorOrElement) {
        return typeof selectorOrElement === 'string' ?
            document.querySelector(selectorOrElement) : selectorOrElement;
    }

    static create(selectorOrElement) {
        let el = this.ensureElement(selectorOrElement);
        return el ? new MudExDomHelper(el) : null;
    }

    static toRelative(element, useWindowAsReference = false) {

        if (!element || !(element instanceof HTMLElement)) {
            console.error("Das angegebene Element ist ungültig.");
            return;
        }

        const parent = element.offsetParent;
        const referenceWidth = useWindowAsReference || !parent ? window.innerWidth : parent.offsetWidth;
        const referenceHeight = useWindowAsReference || !parent ? window.innerHeight : parent.offsetHeight;

        if (!referenceWidth || !referenceHeight) {
            console.warn("No reference to set relatives");
            return;
        }

        const computedStyle = getComputedStyle(element);
        const leftPx = parseFloat(computedStyle.left);
        const topPx = parseFloat(computedStyle.top);
        const widthPx = parseFloat(computedStyle.width);
        const heightPx = parseFloat(computedStyle.height);

        const leftPercent = (leftPx / referenceWidth) * 100;
        const topPercent = (topPx / referenceHeight) * 100;
        const widthPercent = (widthPx / referenceWidth) * 100;
        const heightPercent = (heightPx / referenceHeight) * 100;

        element.style.left = `${leftPercent}%`;
        element.style.top = `${topPercent}%`;
        
        if (element.style.width && element.style.width !== 'auto') {
            element.style.width = `${widthPercent}%`;
        }
        if (element.style.height && element.style.height !== 'auto') {
            element.style.height = `${heightPercent}%`;
        }

    }

    static toAbsolute(element, sizesAuto) {
        element.style.position = 'absolute';
        var rect = element.getBoundingClientRect();
        element.style.left = rect.left + "px";
        element.style.top = rect.top + "px";

        if (sizesAuto) {
            element.style.width = 'auto';
           // element.style.height = 'auto';
        } else {
            element.style.width = rect.width + "px";
           // element.style.height = rect.height + "px";
        }
    }

    static ensureElementIsInScreenBounds(element) {
        var rect = element.getBoundingClientRect();
        var rectIsEmpty = rect.width === 0 && rect.height === 0;
        if (rectIsEmpty) {
            const ro = new ResizeObserver(entries => {
                ro.disconnect();
                this.ensureElementIsInScreenBounds(element);
            });

            ro.observe(element);
            return;
        }

        var animationIsRunning = !!element.getAnimations().length;
        if (animationIsRunning) {
            element.addEventListener('animationend', (e) => this.ensureElementIsInScreenBounds(element), { once: true });
            return;
        }
        if (rect.left < 0) {
            element.style.left = '0px';
        }
        if (rect.top < 0) {
            element.style.top = '0px';
        }
        if (rect.right > window.innerWidth) {
            element.style.left = (window.innerWidth - element.offsetWidth) + 'px';
        }
        if (rect.bottom > window.innerHeight) {
            element.style.top = (window.innerHeight - element.offsetHeight) + 'px';
        }
    }

    static syncSize(main, target, options, callbackRef) {
        options = options || { width: true, height: false, live: true, minWidth: 'auto' };
        var mainEl = MudExObserver.getElement(main);
        var sourceEl = MudExObserver.getElement(target);
        mainEl = mainEl?.parentElement || mainEl;

        if (!mainEl || !sourceEl) return;

        var sourceRect = sourceEl.getBoundingClientRect();
        //var computedStyle = window.getComputedStyle(sourceEl);
        //var initialWidth = computedStyle.width;
        //var initialHeight = computedStyle.height;
        var initialWidth = sourceRect.width + 'px';
        var initialHeight = sourceRect.height + 'px';

        if (options.minWidth === 'auto') {
            options.minWidth = initialWidth;
        }
        if (options.minHeight === 'auto') {
            options.minHeight = initialHeight;
        }

        function updateSize() {
            var rect = mainEl.getBoundingClientRect();
            if (options.width) {
                var width = rect.width;
                sourceEl.style.width = width + 'px';
                if (options.minWidth) {
                    sourceEl.style.minWidth = options.minWidth;
                }
            }
            if (options.height) {
                var height = rect.height;
                sourceEl.style.height = height + 'px';
                if (options.minHeight) {
                    sourceEl.style.minHeight = options.minHeight;
                }
            }
            if (callbackRef) {
                callbackRef.invokeMethodAsync('OnSyncResized', rect, null, null); // TODO: pass mainEl and sourceEl
            }
        }

        updateSize();

        if (options.live) {
            var resizeObserver = new ResizeObserver(() => {
                if (!mainEl.isConnected) {
                    resizeObserver.disconnect();
                    return;
                }
                updateSize();
            });
            resizeObserver.observe(mainEl);
        }
    }

    
}

window.MudExDomHelper = MudExDomHelper;