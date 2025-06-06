class MudExDialogHandlerBase {
    constructor(options, dotNet, dotNetService, onDone) {
        this.options = options;
        this.dotNet = dotNet;
        this.dotNetService = dotNetService;
        this.onDone = onDone;

        this.mudDialogSelector = options.mudDialogSelector || '.mud-dialog:not([data-mud-extended=true])';
        this.mudDialogHeaderSelector = options.mudDialogHeaderSelector || '.mud-dialog-title';
        this._updateDialog(document.querySelector(this.mudDialogSelector));
        this.disposed = false;
        MudExDialogHandlerBase._listeners = {};

    }

    order = 99;

    on(eventName, handler) {
        if (!MudExDialogHandlerBase._listeners[eventName]) {
            MudExDialogHandlerBase._listeners[eventName] = [];
        }
        MudExDialogHandlerBase._listeners[eventName].push(handler);
    }

    un(eventName, handler) {
        this.off(eventName, handler);
    }

    off(eventName, handler) {
        if (!MudExDialogHandlerBase._listeners[eventName]) return;
        MudExDialogHandlerBase._listeners[eventName] = MudExDialogHandlerBase._listeners[eventName].filter(h => h !== handler);
    }

    _emit(eventName, ...args) {
        if (!MudExDialogHandlerBase._listeners[eventName]) return;
        for (const handler of MudExDialogHandlerBase._listeners[eventName]) {
            try {
                handler(...args);
            } catch (e) {
                console.error(e);
            }
        }
    }

    async raiseDialogEvent(eventName) {
        // Get viewport dimensions
        var windowHeight = window.innerHeight || document.documentElement.clientHeight;
        var windowWidth = window.innerWidth || document.documentElement.clientWidth;

        // Get scroll positions
        var scrollX = window.pageXOffset || document.documentElement.scrollLeft;
        var scrollY = window.pageYOffset || document.documentElement.scrollTop;

        // Extend the rect object with new properties
        var extendedRect = {
            windowHeight: windowHeight,
            windowWidth: windowWidth,
            scrollX: scrollX,
            scrollY: scrollY
        };
        var result = null;
        const rect = Object.assign(extendedRect, JSON.parse(JSON.stringify(this.dialog.getBoundingClientRect())));        
        if (this.dotNetService) {
            result = await this.dotNetService.invokeMethodAsync('PublishEvent', eventName, this.dialog.id, this.dotNet, rect);
        }
        this._emit(eventName, {
            dialogId: this.dialog.id,
            dialog: this.dialog,
            rect
        });
        return result;
    }

    restoreSizeConstraintsIf() {
        if (!this.options.keepMaxSizeConstraints && this._savedMaxConstraints) {
            if (this._savedMaxConstraints?.maxWidth)
                this.dialog.style.maxWidth = this._savedMaxConstraints.maxWidth;
            else
                this.dialog.style.removeProperty('max-width');

            if (this._savedMaxConstraints?.maxHeight)
                this.dialog.style.maxHeight = this._savedMaxConstraints.maxHeight;
            else
                this.dialog.style.removeProperty('max-height');
            this._savedMaxConstraints = null;
        }
    }

    removeSizeConstraintsIf() {
        if (!this.options.keepMaxSizeConstraints) {
            this._savedMaxConstraints = {
                maxWidth: this.dialog.style.maxWidth,
                maxHeight: this.dialog.style.maxHeight
            };
            //this.dialog.style.maxWidth = 'unset';
            //this.dialog.style.maxHeight = 'unset';
            this.dialog.style.maxWidth = 'none';
            this.dialog.style.maxHeight = 'none';
        }
    }

    isInternalHandler() {
        return this.dialog.getAttribute('data-mud-ex-internal-handler') === 'true';
    }

    setRelativeIf() {
        if (this.options.keepRelations && MudExDomHelper.toRelative) {
            this.dialog.setAttribute('data-mud-ex-internal-handler', 'true');

            var observer = this.getHandler(MudExDialogResizeHandler)?.resizeObserver;
            if (observer) {
                observer.unobserve(this.dialog);
            }
            MudExDomHelper.toRelative(this.dialog);
            if (observer) {
                observer.observe(this.dialog);
            }
            this.removeInternalHandler();
        }
    }

    removeInternalHandler() {
        setTimeout(() => this.dialog.removeAttribute('data-mud-ex-internal-handler'), 100);
    }

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
        debugger;
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
        delete this.dotNetService;
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
                        const instance = new window[key](this.options, this.dotNet, this.dotNetService, this.onDone);
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
            this.dialog.style.position = 'absolute';
            //this.setRelativeIf();
            this.dialog.options = this.options; 
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