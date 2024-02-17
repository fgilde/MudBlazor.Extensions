class MudExColorHelper {

    static ensureHex(v) {
        try {
            if (!this.isHex(v) && v.toLowerCase().startsWith('rgb')) {
                v = this.rgbaToHex(v);
            }
            return v;
        }
        catch (e) {
            return v;
        }
    }
    
    static isHex(h) {
        if (!h) {
            return false;
        } 
        h = h.replace('#', '');
        var a = parseInt(h, 16);
        return (a.toString(16) === h);
    }
    
    static hexToRgbA(hex) {
        try {
            var c;
            if (/^#([A-Fa-f0-9]{3}){1,2}$/.test(hex)) {
                c = hex.substring(1).split('');
                if (c.length == 3) {
                    c = [c[0], c[0], c[1], c[1], c[2], c[2]];
                }
                c = '0x' + c.join('');
                return 'rgba(' + [(c >> 16) & 255, (c >> 8) & 255, c & 255].join(',') + ',1)';
            }
        }
        catch (error) {
            return '';
        }
        return '';
    }
    
    static rgbaToHex(orig) {
        try {
            var a, isPercent, rgb = orig.replace(/\s/g, '').match(/^rgba?\((\d+),(\d+),(\d+),?([^,\s)]+)?/i), alpha = (rgb && rgb[4] || "").trim(), hex = rgb ?
                (rgb[1] | 1 << 8).toString(16).slice(1) +
                (rgb[2] | 1 << 8).toString(16).slice(1) +
                (rgb[3] | 1 << 8).toString(16).slice(1) : orig;
            if (alpha !== "") {
                a = alpha;
            }
            else {
                //a = 01;
                a = 0x1;
            }
            a = ((a * 255) | 1 << 8).toString(16).slice(1);
            hex = hex + a;
            return hex;
        }
        catch (error) {
            return '';
        }
    }
    
    static isTransparent(v) {
        return v && (v.toLowerCase() === 'transparent' || v.includes('NaN'));
    }
    
    static isNone(v) {
        return v && (v.toLowerCase() === 'none' || v.includes('ed'));
    }
    
    static isTransparentOrNone(v) {
        return this.isNone(v) || this.isTransparent(v);
    }
    
    static argbToHex (color) {
        return '#' + ('000000' + (color & 0xFFFFFF).toString(16)).slice(-6);
    }

    static hexToArgb (hexColor) {
        return parseInt(hexColor.replace('#', 'FF'), 16) << 32;
    }

    static hexToHsl(hex) {
        return this.rgbToHsl(this.hexToRgb(hex));
    }

    static hslToHex(hsl) {
        return this.rgbToHex(this.hslToRgb(hsl));
    }

    static setLuminance (hex, luminance) {
        if (!hex) {
            return hex;
        }
        var hsl = this.rgbToHsl(this.hexToRgb(hex));
        hsl.l = Math.max(Math.min(1, luminance), 0);
        return this.rgbToHex(this.hslToRgb(hsl));
    }

    static hexToRgb (hex) {
        const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);

        return result ? {
            r: parseInt(result[1], 16),
            g: parseInt(result[2], 16),
            b: parseInt(result[3], 16)
        } : null;
    }

    static rgbIntToHex (rgb) {
        let hex = Number(rgb).toString(16);

        if (hex.length < 6) {
            hex = '000000'.substring(hex.length) + hex;
        }
        return hex;
    }

    static hslToRgb (hsl) {
        const h = hsl.h,
            s = hsl.s,
            l = hsl.l;
        let hue2rgb,
            r,
            g,
            b,
            p,
            q;

        if (s === 0) {
            r = g = b = l;
        } else {
            hue2rgb = function (p, q, t) {
                if (t < 0) {
                    t += 1;
                }
                if (t > 1) {
                    t -= 1;
                }
                if (t < 1 / 6) {
                    return p + (q - p) * 6 * t;
                }
                if (t < 1 / 2) {
                    return q;
                }
                if (t < 2 / 3) {
                    return p + (q - p) * (2 / 3 - t) * 6;
                }
                return p;
            };

            q = l < 0.5 ? l * (1 + s) : l + s - l * s;
            p = 2 * l - q;
            r = hue2rgb(p, q, h + 1 / 3);
            g = hue2rgb(p, q, h);
            b = hue2rgb(p, q, h - 1 / 3);
        }

        return {
            r: MudExNumber.constrain(Math.round(r * 255), 0, 255),
            g: MudExNumber.constrain(Math.round(g * 255), 0, 255),
            b: MudExNumber.constrain(Math.round(b * 255), 0, 255)
        };
    }

    static rgbToHsl (rgb) {
        if (!rgb) {
            return rgb;
        }

        const r = rgb.r / 255,
            g = rgb.g / 255,
            b = rgb.b / 255,
            max = Math.max(r, g, b),
            min = Math.min(r, g, b),
            l = (max + min) / 2;
        let h,
            s,
            d;

        if (max === min) {
            h = s = 0;
        } else {
            d = max - min;
            s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
            switch (max) {
            case r:
                h = (g - b) / d + (g < b ? 6 : 0);
                break;
            case g:
                h = (b - r) / d + 2;
                break;
            case b:
                h = (r - g) / d + 4;
                break;
            }
            h /= 6;
        }

        return { h: h, s: s, l: l };
    }

    static rgbToHex (rgb) {
        const tohex = function (value) {
            const hex = value.toString(16);

            return hex.length === 1 ? '0' + hex : hex;
        };

        return '#' + tohex(rgb.r) + tohex(rgb.g) + tohex(rgb.b);
    }

    static getOptimalForegroundColor (backgroundColor, lightforegroundResult, darkforegroundResult) {
        if (this.perceivedBrightness(backgroundColor) > 190) {
            return darkforegroundResult || '#000000';
        }
        return lightforegroundResult || '#FFFFFF';
    }

    static perceivedBrightness (color) {
        if (!color) {
            return 0;
        }
        if (typeof color !== 'object') {
            color = this.hexToRgb(color);
        }
        return Math.sqrt(color.r * color.r * 0.241 + color.g * color.g * 0.691 + color.b * color.b * 0.068);
    }
    
}
window.MudExColorHelper = MudExColorHelper;
class MudExCssHelper {
    static getCssVariables() {
        // Get CSS variables from stylesheets
        const sheetVariables = Array.from(document.styleSheets)
            .filter(sheet => sheet.href === null || sheet.href.startsWith(window.location.origin))
            .reduce((acc, sheet) => (acc = [...acc, ...Array.from(sheet.cssRules).reduce((def, rule) => (def = rule.selectorText === ":root"
                ? [...def, ...Array.from(rule.style).filter(name => name.startsWith("--"))]
                : def), [])]), []);

        // Get CSS variables from inline styles
        const inlineStyles = document.documentElement.style;
        const inlineVariables = Array.from(inlineStyles).filter(name => name.startsWith("--"));

        // Combine and remove duplicates
        const allVariables = Array.from(new Set([...sheetVariables, ...inlineVariables]));

        return allVariables.map(name => ({ key: name, value: this.getCssVariableValue(name) }));
    }
    
    static findCssVariables(value) {
        value = value.toLowerCase();
        const helper = MudExColorHelper;//window[window['___appJsNameSpace']]['ColorHelper'];
        return this.getCssVariables().filter(v => v.value.toLowerCase().includes(value) || helper.ensureHex(v.value).includes(helper.ensureHex(value)));
    }
    
    static getCssVariableValue(varName) {
        if (!varName.startsWith('--')) {
            varName = '--' + varName;
        }
        return getComputedStyle(document.documentElement).getPropertyValue(varName).trim();
    }
    
    static setCssVariableValue(varName, value) {
        if (!varName.startsWith('--')) {
            varName = '--' + varName;
        }
        document.documentElement.style.setProperty(varName, value);
    }

    static setElementAppearance(selector, className, style, keepExisting) {
        var element = document.querySelector(selector);
        this.setElementAppearanceOnElement(element, className, style, keepExisting);

    }

    static setElementAppearanceOnElement(element, className, style, keepExisting) {
        if (element) {      
            element.className = keepExisting && element.className ? element.className + ' ' + className : className;
            element.style.cssText = keepExisting && element.style.cssText ? element.style.cssText + ' ' + style : style;
        }
    }

    static removeElementAppearance(selector, className, style) {
        var element = document.querySelector(selector);
        this.removeElementAppearanceOnElement(element, className, style);
    }

    static removeElementAppearanceOnElement(element, className, style) {
        if (element) {
            if (className) {
                element.classList.remove(className);
            }
            if (style) {
                let styles = element.style.cssText.split(';')
                    .map(s => s.trim())
                    .filter(s => s !== style && !s.startsWith(style.split(':')[0]))
                    .join('; ');
                element.style.cssText = styles;
            }
        }
    }

    static createTemporaryClass(style, className) {
        className = className || 'class_' + Math.random().toString(36).substr(2, 9);

        // Remove leading dot if exists
        className = className.startsWith('.') ? className.slice(1) : className;

        let styleElement = document.getElementById('mud-ex-dynamic-styles');

        if (!styleElement) {
            styleElement = document.createElement('style');
            styleElement.type = 'text/css';
            styleElement.id = 'mud-ex-dynamic-styles';
            document.head.appendChild(styleElement);
        }

        styleElement.sheet.insertRule('.' + className + ' { ' + style + ' }', 0);

        return className;
    }

    static deleteClassRule(className) {
        let styleElement = document.getElementById('mud-ex-dynamic-styles');
        if (!styleElement) {
            return;
        }
        let styleSheet = styleElement.sheet;
        var rules = styleSheet.cssRules || styleSheet.rules;

        // Remove leading dot if exists
        className = className.startsWith('.') ? className.slice(1) : className;

        for (var i = 0; i < rules.length; i++) {
            if (rules[i].selectorText === '.' + className) {
                if (styleSheet.deleteRule) {
                    styleSheet.deleteRule(i);
                } else if (styleSheet.removeRule) {
                    styleSheet.removeRule(i);
                }
                break;
            }
        }
    }
    
}
window.MudExCssHelper = MudExCssHelper;
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

    static toAbsolute(element) {
        element.style.position = 'absolute';
        var rect = element.getBoundingClientRect();
        element.style.left = rect.left + "px";
        element.style.top = rect.top + "px";
        element.style.width = rect.width + "px";
        element.style.height = rect.height + "px";
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
    
}

window.MudExDomHelper = MudExDomHelper;
class MudExEventHelper {
    static isWithin(event, element) {
        if (!element || !event) {
            return false;
        }
        let rect = element.getBoundingClientRect();
        return (event.clientX > rect.left &&
        event.clientX < rect.right &&
        event.clientY < rect.bottom &&
        event.clientY > rect.top);
    }

    static clickElementById(elementId) {
        var retries = 5;
        function tryClick() {
            var elem = document.querySelector('#' + elementId) || document.getElementById(elementId);
            if (elem) {
                elem.click();
            } else if (retries > 0) {
                retries--;
                setTimeout(tryClick, 100);  // try again after 100ms
            }
        }
        tryClick();
    }

    static stringifyEvent(e) {
        const obj = {};
        for (let k in e) {
            obj[k] = e[k];
        }
        return JSON.stringify(obj, (k, v) => {
            if (v instanceof Node)
                return 'Node';
            if (v instanceof Window)
                return 'Window';
            return v;
        }, ' ');
    }

    static cloneEvent(e, serializable) {
        if (serializable) {
            return JSON.parse(this.stringifyEvent(event));
        }
        if (e === undefined || e === null)
            return undefined;
        function ClonedEvent() { }
        ;
        let clone = new ClonedEvent();
        for (let p in e) {
            let d = Object.getOwnPropertyDescriptor(e, p);
            if (d && (d.get || d.set))
                Object.defineProperty(clone, p, d);
            else
                clone[p] = e[p];
        }
        Object.setPrototypeOf(clone, e);
        return clone;
    }
}

window.MudExEventHelper = MudExEventHelper;
class MudExNumber {
    static constrain (number, min, max) {
        var x = parseFloat(number);
        if (min === null) {
            min = number;
        }

        if (max === null) {
            max = number;
        }
        return (x < min) ? min : ((x > max) ? max : x);
    }
}

window.MudExNumber = MudExNumber;
class MudExUriHelper {
    static createBlobUrlFromByteArray(byteArray, mimeType) {
        const blob = new Blob([new Uint8Array(byteArray)], { type: mimeType });
        return URL.createObjectURL(blob);
    }

    static async readBlobAsText(blobUrl) {
        const response = await fetch(blobUrl);
        const blob = await response.blob();
        return await blob.text();
    }

    static async readBlobAsByteArray(blobUrl) {
        const response = await fetch(blobUrl);
        const blob = await response.blob();
        const arrayBuffer = await blob.arrayBuffer();
        return Array.from(new Uint8Array(arrayBuffer));
    }

    static blobToByteArray(blob) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = function () {
                const arrayBuffer = this.result;
                const byteArray = new Uint8Array(arrayBuffer);
                resolve(byteArray);
            };
            reader.onerror = reject;
            reader.readAsArrayBuffer(blob);
        });
    }

    static blobToBase64(blob) {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.onload = function () {
                const base64String = btoa(String.fromCharCode(...new Uint8Array(this.result)));
                resolve(base64String);
            };
            reader.onerror = reject;
            reader.readAsArrayBuffer(blob);
        });
    }

    static revokeBlobUrl(blobUrl) {
        URL.revokeObjectURL(blobUrl);
    }
}

window.MudExUriHelper = MudExUriHelper;
class MudExDialogHandlerBase {
    constructor(options, dotNet, onDone) {
        this.options = options;
        this.dotNet = dotNet;
        this.onDone = onDone;

        this.mudDialogSelector = options.mudDialogSelector || '.mud-dialog:not([data-mud-extended=true])';
        this.mudDialogHeaderSelector = options.mudDialogHeaderSelector || '.mud-dialog-title';
        this._updateDialog(document.querySelector(this.mudDialogSelector));
        this.disposed = false;

    }

    order = 99;

    getAnimationDuration() {
        // TODO: 
        return this.options.animationDurationInMs + 150;
    }

    awaitAnimation(callback) {
        setTimeout(() => callback(this.dialog), this.getAnimationDuration());
    }

    handle(dialog) {
        this._updateDialog(dialog);
    }

    handleAll(dialog) {
        const handlers = this.getHandlers();
        handlers.forEach(handlerInstance => {
            handlerInstance.handle(dialog);
            handlerInstance._handlersCache = this._handlersCache;
        });
    }

    dispose() {
        this.disposed = true;
        this._handlersCache.forEach(handlerInstance => {
            if (!handlerInstance.disposed) {
                handlerInstance.dispose();
            }
        });
        delete this._handlersCache;
        delete this.dialog;
        delete this.dialogHeader;
        delete this.dotNet;
        delete this.onDone;
        delete this.options;
        
    }

    getHandlers() {
        if (this._handlersCache) {
            return this._handlersCache;
        }

        const handlerInstances = [];

        for (const key in window) {
            if (window.hasOwnProperty(key) && typeof window[key] === 'function') {
                try {
                    const superClass = Object.getPrototypeOf(window[key].prototype);
                    if (superClass && superClass.constructor === MudExDialogHandlerBase && window[key].prototype.constructor !== this.constructor) {
                        const instance = new window[key](this.options, this.dotNet, this.onDone);
                        handlerInstances.push(instance);
                    }
                } catch (error) {
                    // Ignore errors caused by non-class objects
                }
            }
        }

        this._handlersCache = handlerInstances.sort((a, b) => a.order - b.order);
        return handlerInstances;
    }

    getHandler(HandlerClass) {
        return this.getHandlers().find(handlerInstance => handlerInstance instanceof HandlerClass);
    }

    _updateDialog(dialog) {
        this.dialog = dialog || this.dialog;
        if (this.dialog) {
            this.dialogHeader = this.dialog.querySelector(this.mudDialogHeaderSelector);
            this.dialogTitleEl = this.dialog.querySelector('.mud-dialog-title');
            this.dialogTitle = this.dialogTitleEl ? this.dialogTitleEl.innerText.trim() : '';
            this.dialogId = this.dialog.id;
            this.dialogContainerReference = this.dialog.parentElement;
            if (this.dialogContainerReference) { 
                this.dialogOverlay = this.dialogContainerReference.querySelector('.mud-overlay');
            }
        }
    }
}

window.MudExDialogHandlerBase = MudExDialogHandlerBase;
class MudExDialogAnimationHandler extends MudExDialogHandlerBase {
   
    handle(dialog) {
        super.handle(dialog);
        if (this.options.animations != null && Array.isArray(this.options.animations) && this.options.animations.length) {
            this.animate(this.options.animationDescriptions);
        }
    }

    animate(types) {
        //var names = types.map(type => this.options.dialogPositionNames.map(n => `kf-mud-dialog-${type}-${n} ${this.options.animationDurationInMs}ms ${this.options.animationTimingFunctionString} 1 alternate`));
        //this.dialog.style.animation = `${names.join(',')}`;
        this.dialog.style.animation = `${this.options.animationStyle}`;
    }
}


window.MudExDialogAnimationHandler = MudExDialogAnimationHandler;
class MudExDialogButtonHandler extends MudExDialogHandlerBase {

    handle(dialog) {
        super.handle(dialog);
        if (this.options.buttons && this.options.buttons.length) {
            var dialogButtonWrapper = document.createElement('div');
            dialogButtonWrapper.classList.add('mud-ex-dialog-header-actions');
            if (!this.options.closeButton) {
                dialogButtonWrapper.style.right = '8px'; // No close button, so we need to move the buttons to the right (48 - button width of 40)
            }

            if (this.dialogHeader) {
                dialogButtonWrapper = this.dialogHeader.insertAdjacentElement('beforeend', dialogButtonWrapper);
            }
            this.options.buttons.reverse().forEach(b => {
                if (dialogButtonWrapper) {
                    dialogButtonWrapper.insertAdjacentHTML('beforeend', b.html);
                    var btnEl = dialogButtonWrapper.querySelector('#' + b.id);
                    btnEl.onclick = () => {
                        if (b.id.indexOf('mud-button-maximize') >= 0) {
                            this.getHandler(MudExDialogPositionHandler).maximize();
                        }
                        if (b.id.indexOf('mud-button-minimize') >= 0) {
                            this.getHandler(MudExDialogPositionHandler).minimize();
                            
                        } else {
                            b.callBackReference.invokeMethodAsync(b.callbackName);
                        }
                    }
                }
            });
        }
    }
}


window.MudExDialogButtonHandler = MudExDialogButtonHandler;
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
class MudExDialogFinder {
    constructor(options) {
        this.options = options;
        this.mudDialogSelector = options.mudDialogSelector || '.mud-dialog:not([data-mud-extended=true])';
    }

    findDialog() {
        return Array.from(document.querySelectorAll(this.mudDialogSelector)).find(d => !d.__extended);
    }

    observeDialog(callback) {
        const observer = new MutationObserver((mutations) => {
            const dialog = this.findDialog();
            if (dialog) {
                const addedDialogMutation = mutations.find(mutation => mutation.addedNodes[0] === dialog);

                if (addedDialogMutation) {
                    observer.disconnect();
                    callback(dialog);
                }
            }
        });

        observer.observe(document, { characterData: true, childList: true, subtree: true });
    }
}

window.MudExDialogFinder = MudExDialogFinder;
class MudExDialogHandler extends MudExDialogHandlerBase {

    handle(dialog) {
        setTimeout(() => {
            super.handle(dialog);
            setTimeout(() => {
                dialog.classList.remove('mud-ex-dialog-initial');
            }, 50);
            dialog.__extended = true;
            dialog.setAttribute('data-mud-extended', true);
            dialog.classList.add('mud-ex-dialog');
            this.handleAll(dialog);
            if (this.onDone) this.onDone();
        }, 50);

    }
}

window.MudExDialogHandler = MudExDialogHandler;
class MudExDialogNoModalHandler extends MudExDialogHandlerBase {

    static handled = [];

    handle(dialog) {
        super.handle(dialog);

        if (!this.options.modal) {
            this._updateHandledDialogs(dialog);

            this.appOrBody = this.dialogContainerReference.parentElement;
            this._modifyDialogAppearance();

            this.dialog.onmousedown = this._handleDialogMouseDown.bind(this);

            this.observer = new MutationObserver(this._checkMutationsForRemove.bind(this));
            this.observer.observe(this.appOrBody, { childList: true });
        }
    }

    _updateHandledDialogs(dialog) {
        const index = MudExDialogNoModalHandler.handled.findIndex(h => h.id === dialog.id);
        if (index !== -1) {
            MudExDialogNoModalHandler.handled.splice(index, 1);
        }

        MudExDialogNoModalHandler.handled.push({
            id: this.dialog.id,
            dialog: this.dialog,
            options: this.options,
            dotNet: this.dotNet
        });
    }

    _modifyDialogAppearance() {
        this.changeCls();
        this.awaitAnimation(() => {
            this.dialog.style['animation-duration'] = '0s';
            MudExDomHelper.toAbsolute(this.dialog);
            this.appOrBody.insertBefore(this.dialog, this.appOrBody.firstChild);
            Object.assign(this.dialogContainerReference.style, {
                display: 'none',
                height: '2px',
                width: '2px'
            });
        });
    }

    _handleDialogMouseDown(e) {
        if (!this.dialogHeader || !Array.from(this.dialogHeader.querySelectorAll('button')).some(b => MudExEventHelper.isWithin(e, b))) {
            MudExDialogNoModalHandler.bringToFront(this.dialog);
        }
    }

    reInitOtherDialogs() {
        const dialogsToReInit = Array.from(document.querySelectorAll('.mud-ex-dialog-initial'))
            .filter(d => d.getAttribute('data-mud-extended') !== 'true');

        dialogsToReInit.forEach(this.reInitDialog.bind(this));
    }

    reInitDialog(d) {
        const dialogInfo = MudExDialogNoModalHandler.handled.find(h => h.id === d.id);
        if (dialogInfo) {
            const currentStyle = d.style;
            const savedPosition = {
                top: currentStyle.top,
                left: currentStyle.left,
                width: currentStyle.width,
                height: currentStyle.height,
                position: currentStyle.position
            };
            d.style.display = 'none';

            const handleInfo = { ...dialogInfo, options: { ...dialogInfo.options, animations: null } };
            const index = MudExDialogNoModalHandler.handled.indexOf(dialogInfo);
            MudExDialogNoModalHandler.handled.splice(index, 1);

            const handler = new MudExDialogHandler(handleInfo.options, handleInfo.dotNet, handleInfo.onDone);
            handler.handle(d);

            d.style.display = 'block';
            Object.assign(d.style, savedPosition);
        }
    }

    _checkMutationsForRemove(mutationsList) {
        for (const mutation of mutationsList) {
            if (mutation.type === 'childList') {
                for (const removedNode of mutation.removedNodes) {
                    if (removedNode === this.dialogContainerReference) {
                        this.observer.disconnect();

                        const index = MudExDialogNoModalHandler.handled.findIndex(h => h.id === this.dialog.id);
                        if (index !== -1) {
                            MudExDialogNoModalHandler.handled.splice(index, 1);
                        }

                        this.dialog.remove();
                        this.reInitOtherDialogs();
                        break;
                    }
                }
            }
        }
    }

    changeCls() {
        this.dialog.classList.add('mudex-dialog-no-modal');
        this.dialogContainerReference.classList.add('mudex-dialog-ref-no-modal');
        this.dialogContainerReference.setAttribute('data-modal', false);
        this.dialogContainerReference.setAttribute('data-dialog-id', this.dialog.id);
        this.dialogOverlay.remove();
    }

    static bringToFront(targetDlg, animate) {
        const allDialogs = this.getAllNonModalDialogs();
        if (targetDlg) {
            const app = targetDlg.parentElement;            
            //const targetRef = MudExDialogNoModalHandler.getDialogReference(targetDlg);
            const lastDialog = allDialogs[allDialogs.length - 1];
            if (targetDlg !== lastDialog) {
                //const lastDialogRef = MudExDialogNoModalHandler.getDialogReference(lastDialog);
                app.insertBefore(targetDlg, lastDialog.nextSibling);                
            }
        }
    }

    static getDialogReference(dialog) {
        return MudExDialogNoModalHandler.getAllDialogReferences().filter(r => r && r.getAttribute('data-dialog-id') === dialog.id)[0] || dialog.parentElement;
    }

    static getAllDialogReferences() {
        return Array.from(document.querySelectorAll('.mud-dialog-container')).filter(c => c.getAttribute('data-modal') === 'false');
    }

    static getAllNonModalDialogs() {
        return Array.from(document.querySelectorAll('.mudex-dialog-no-modal'));
    }
}

window.MudExDialogNoModalHandler = MudExDialogNoModalHandler;

class MudExDialogPositionHandler extends MudExDialogHandlerBase {

    handle(dialog) {
        super.handle(dialog);

        if (this.options.showAtCursor) {
            this.moveElementToMousePosition(dialog);
        }
                
        if (this.options.fullWidth && this.options.disableSizeMarginX) {
            this.dialog.classList.remove('mud-dialog-width-full');
            this.dialog.classList.add('mud-dialog-width-full-no-margin');
            if (this.dialog.classList.contains('mud-dialog-width-false')) {
                this.dialog.classList.remove('mud-dialog-width-false');
            }
        }
    }
    

    minimize() {
        let targetElement = document.querySelector(`.mud-ex-task-bar-item-for-${this.dialog.id}`);
        this.moveToElement(this.dialog, targetElement, () => {
            this.dialog.style.visibility = 'hidden';            
        }); 
    }


    moveToElement(sourceElement, targetElement, callback) {
        
        // Get the bounding client rectangles of the target element and the dialog
        var targetRect = targetElement.getBoundingClientRect();
        var dialogRect = sourceElement.getBoundingClientRect();

        // Calculate the scaling factors for width and height
        var scaleX = targetRect.width / dialogRect.width;
        var scaleY = targetRect.height / dialogRect.height;

        // Calculate the translation distances for X and Y
        var translateX = targetRect.left - dialogRect.left;
        var translateY = targetRect.top - dialogRect.top;

        var lastDuration = sourceElement.style['animation-duration'];
        sourceElement.style['animation-duration'] = '.3s';
        // Apply the transformation using the calculated scaling factors and translation distances
        sourceElement.style.transform = `translate(${translateX}px, ${translateY}px) scale(${scaleX}, ${scaleY})`;
        sourceElement.style.transition = 'transform 0.3s ease-in-out';

        // Remove the transition after the animation is done
        setTimeout(() => {
            sourceElement.style.removeProperty('transform');
            sourceElement.style.removeProperty('transition');
            sourceElement.style['animation-duration'] = lastDuration;
            if (callback) callback();
        }, 300);

    }

    restore() {
        this.dialog.style.visibility = 'visible';
    }

    maximize() {
        if (this._oldStyle) {
            this.dialog.style.cssText = this._oldStyle;
            delete this._oldStyle;
        } else {
            this._oldStyle = this.dialog.style.cssText;
            this.dialog.style.position = 'absolute';
            this.dialog.style.left = "0";
            this.dialog.style.top = "0";
            this.dialog.style.maxWidth = this.dialog.style.width = window.innerWidth + 'px';
            this.dialog.style.maxHeight = this.dialog.style.height = window.innerHeight + 'px';
        }
        this.getHandler(MudExDialogResizeHandler).checkResizeable();
    }
    
    moveElementToMousePosition(element) {
        var e = MudBlazorExtensions.getCurrentMousePosition();
        var x = e.clientX;
        var y = e.clientY;
        var origin = this.options.cursorPositionOriginName.split('-');

        var maxWidthFalseOrLargest = this.options.maxWidth === 6 || this.options.maxWidth === 4; // 4=xxl 6=false
        setTimeout(() => {
            if (!this.options.fullWidth || !maxWidthFalseOrLargest) {
                if (origin[1] === 'left') {
                    element.style.left = x + 'px';
                } else if (origin[1] === 'right') {
                    element.style.left = (x - element.offsetWidth) + 'px';
                } else if (origin[1] === 'center') {
                    element.style.left = (x - element.offsetWidth / 2) + 'px';
                }
            }
            if (!this.options.fullHeight) {
                if (origin[0] === 'top') {
                    element.style.top = y + 'px';
                } else if (origin[0] === 'bottom') {
                    element.style.top = (y - element.offsetHeight) + 'px';
                } else if (origin[0] === 'center') {
                    element.style.top = (y - element.offsetHeight / 2) + 'px';
                }
            }
            MudExDomHelper.ensureElementIsInScreenBounds(element);
        }, 50);

    }
}


window.MudExDialogPositionHandler = MudExDialogPositionHandler;
class MudExDialogResizeHandler extends MudExDialogHandlerBase {

    handle(dialog) {
        super.handle(dialog);
        this.awaitAnimation(() => this.checkResizeable());
    }

    checkResizeable() {
        MudExDomHelper.toAbsolute(this.dialog);
        if (this.options.resizeable) {
            this.dialog.style['resize'] = 'both';
            this.dialog.style['overflow'] = 'auto';


            if (!this.dialog.style.maxWidth || this.dialog.style.maxWidth === 'none') {
                this.dialog.style.maxWidth = window.innerWidth + 'px';
            }

            if (!this.dialog.style.maxHeight || this.dialog.style.maxHeight === 'none') {
                this.dialog.style.maxHeight = window.innerHeight + 'px';
            }

            if (!this.dialog.style.minWidth || this.dialog.style.minWidth === 'none') {
                this.dialog.style.minWidth = '100px';
            }
            
            if (!this.dialog.style.minHeight || this.dialog.style.minHeight === 'none') {
                this.dialog.style.minHeight = '100px';
            }
            
        }
    }
}

window.MudExDialogResizeHandler = MudExDialogResizeHandler;
class MudBlazorExtensionHelper {
    constructor(options, dotNet, onDone) {
        this.dialogFinder = new MudExDialogFinder(options);
        this.dialogHandler = new MudExDialogHandler(options, dotNet, onDone);
    }

    init() {
        const dialog = this.dialogFinder.findDialog();
        if (dialog) {
            this.dialogHandler.handle(dialog);
        } else {
            this.dialogFinder.observeDialog(dialog => this.dialogHandler.handle(dialog));
        }
    }
}



window.MudBlazorExtensionHelper = MudBlazorExtensionHelper;


window.MudBlazorExtensions = {
    helper: null,
    currentMouseArgs: null,

    __bindEvents: function () {
        var onMouseUpdate = function(e) {
            window.MudBlazorExtensions.currentMouseArgs = e;
        }
        document.addEventListener('mousemove', onMouseUpdate, false);
        document.addEventListener('mouseenter', onMouseUpdate, false);
    },
    
    getCurrentMousePosition: function() {
        return window.MudBlazorExtensions.currentMouseArgs;
    },

    setNextDialogOptions: function (options, dotNet) {
        new MudBlazorExtensionHelper(options, dotNet, () => {
            MudBlazorExtensions.helper = null;
            delete MudBlazorExtensions.helper;
        }).init();
    },

    addCss: function (cssContent) {
        var css = cssContent,
            head = document.head || document.getElementsByTagName('head')[0],
            style = document.createElement('style');

        head.appendChild(style);

        style.type = 'text/css';
        if (style.styleSheet) {
            // This is required for IE8 and below.
            style.styleSheet.cssText = css;
        } else {
            style.appendChild(document.createTextNode(css));
        }
    },

    openWindowAndPostMessage: function(url, message) {
        var newWindow = window.open(url);
        newWindow.onload = function () {
            console.log("POST "+ message);
            newWindow.postMessage(message, url);
        };
    },

    downloadFile(options) {
        var fileUrl = options.url || "data:" + options.mimeType + ";base64," + options.base64String;
        fetch(fileUrl)
            .then(response => response.blob())
            .then(blob => {
                var link = window.document.createElement("a");
                //link.href = window.URL.createObjectURL(blob, { type: options.mimeType });
                link.href = window.URL.createObjectURL(blob);
                link.download = options.fileName;
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);
            });
    },

    attachDialog(dialogId) {
        if (dialogId) {
            let dialog = document.getElementById(dialogId);            
            if (dialog) {
                let titleCmp = dialog.querySelector('.mud-dialog-title');
                let iconCmp = null;
                if (titleCmp) {
                    const svgElements = titleCmp.querySelectorAll('svg');
                    const filteredSvgElements = Array.from(svgElements).filter(c => !c.parentElement.classList.contains('mud-ex-dialog-header-actions'));

                    if (filteredSvgElements.length > 0) {
                        iconCmp = filteredSvgElements[0];
                    }
                }

                const res = {
                    title: titleCmp ? titleCmp.innerText : 'Unnamed window',
                    icon: iconCmp ? iconCmp.innerHTML : ''
                }
                return res;
            }
        }
        return null;
    },

    getElement(selector) {
        return document.querySelector(selector);
    },

    showDialog(dialogId) {
        if (dialogId) { 
            let dialog = document.getElementById(dialogId);
            if (dialog) {
                dialog.style.visibility = 'visible';
                MudExDialogNoModalHandler.bringToFront(dialog, true);
            }
        }
    }


};

window.MudBlazorExtensions.__bindEvents();